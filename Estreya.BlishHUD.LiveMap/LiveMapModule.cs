namespace Estreya.BlishHUD.LiveMap
{
    using Blish_HUD;
    using Blish_HUD.Content;
    using Blish_HUD.Controls;
    using Blish_HUD.Graphics.UI;
    using Blish_HUD.Modules;
    using Blish_HUD.Modules.Managers;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.LiveMap;
    using Estreya.BlishHUD.LiveMap.Models.Player;
    using Estreya.BlishHUD.Shared.Helpers;
    using Estreya.BlishHUD.Shared.Modules;
    using Estreya.BlishHUD.Shared.Settings;
    using Estreya.BlishHUD.Shared.Threading;
    using Estreya.BlishHUD.Shared.Utils;
    using Gw2Sharp.WebApi.V2.Models;
    using Humanizer;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Newtonsoft.Json;
    using SocketIOClient.JsonSerializer;
    using SocketIOClient.Messages;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Net;
    using System.Security.Policy;
    using System.Threading;
    using System.Threading.Tasks;

    [Export(typeof(Blish_HUD.Modules.Module))]
    public class LiveMapModule : BaseModule<LiveMapModule, ModuleSettings>
    {
        private static readonly Logger Logger = Logger.GetLogger<LiveMapModule>();
        private const string LIVE_MAP_BASE_API_URL = "http://localhost:3002/v1";
        private const string LIVE_MAP_GLOBAL_API_URL = $"{LIVE_MAP_BASE_API_URL}/global/write";
        private const string LIVE_MAP_GUILD_API_URL = $"{LIVE_MAP_BASE_API_URL}/guild/write";
        public const string LIVE_MAP_GLOBAL_URL = "https://gw2map.estreya.de";
        public const string LIVE_MAP_GUILD_URL = $"{LIVE_MAP_GLOBAL_URL}/guild/{{0}}";

        private SocketIOClient.SocketIO GlobalSocket = new SocketIOClient.SocketIO(LIVE_MAP_GLOBAL_API_URL, new SocketIOClient.SocketIOOptions()
        {
            Transport = SocketIOClient.Transport.TransportProtocol.WebSocket
        });

        private SocketIOClient.SocketIO GuildSocket = new SocketIOClient.SocketIO(LIVE_MAP_GUILD_API_URL, new SocketIOClient.SocketIOOptions()
        {
            Transport = SocketIOClient.Transport.TransportProtocol.WebSocket
        });

        private TimeSpan _sendInterval = TimeSpan.FromMilliseconds(250);
        private AsyncRef<double> _lastSend = new AsyncRef<double>(0);
        private Player _lastSendPlayer;
        private TimeSpan _guildFetchInterval = TimeSpan.FromSeconds(30);
        private AsyncRef<double> _lastGuildFetch = new AsyncRef<double>(0);

        private string _accountName;
        private string _guildId;

        public string GuildId => _guildId;

        public override string WebsiteModuleName => "live-map";

        protected override string API_VERSION_NO => "1";

        [ImportingConstructor]
        public LiveMapModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) { }

        protected override void Initialize()
        {
            base.Initialize();

            this.Gw2ApiManager.SubtokenUpdated += this.Gw2ApiManager_SubtokenUpdated;
            GameService.Gw2Mumble.PlayerCharacter.NameChanged += this.PlayerCharacter_NameChanged;

            this._lastSend.Value = _sendInterval.TotalMilliseconds;
            this._lastGuildFetch.Value = _guildFetchInterval.TotalMilliseconds;

            Instance = this;
        }

        protected override async Task LoadAsync()
        {
            await base.LoadAsync();

            this.GlobalSocket.On("interval", (resp) =>
            {
                var interval = resp.GetValue<int>();
                _sendInterval = TimeSpan.FromMilliseconds(interval);
            });
            await this.GlobalSocket.ConnectAsync();

            await this.GuildSocket.ConnectAsync();
            await this.FetchAccountName();
            await this.FetchGuildId();
        }

        protected override void OnModuleLoaded(EventArgs e)
        {
            // Base handler must be called
            base.OnModuleLoaded(e);
        }

        private void Gw2ApiManager_SubtokenUpdated(object sender, ValueEventArgs<IEnumerable<TokenPermission>> e)
        {
            Task.Run(this.FetchAccountName);
            _lastGuildFetch = _guildFetchInterval.TotalMilliseconds;
        }

        private void PlayerCharacter_NameChanged(object sender, ValueEventArgs<string> e)
        {
            _lastGuildFetch = _guildFetchInterval.TotalMilliseconds;
        }

        private async Task FetchAccountName()
        {
            if (!this.Gw2ApiManager.HasPermissions(new[] { TokenPermission.Account }))
            {
                return;
            }

            var account = await this.Gw2ApiManager.Gw2ApiClient.V2.Account.GetAsync();
            this._accountName = account.Name;
        }

        private async Task FetchGuildId()
        {
            if (!GameService.Gw2Mumble.IsAvailable || !this.Gw2ApiManager.HasPermissions(new[] { TokenPermission.Characters }))
            {
                return;
            }

            var character = await this.Gw2ApiManager.Gw2ApiClient.V2.Characters.GetAsync(GameService.Gw2Mumble.PlayerCharacter.Name);
            this._guildId = character.Guild.ToString();
        }

        private async Task SendPosition()
        {
            if (string.IsNullOrWhiteSpace(_accountName) || !GameService.Gw2Mumble.IsAvailable || GameService.Gw2Mumble.TimeSinceTick.TotalSeconds > 0.5)
            {
                return;
            }

            var position = GameService.Gw2Mumble.UI.MapPosition;

            var cameraForward = this.ModuleSettings.PlayerFacingType.Value == LiveMap.Models.PlayerFacingType.Camera ? GameService.Gw2Mumble.PlayerCamera.Forward : GameService.Gw2Mumble.PlayerCharacter.Forward;
            var cameraAngle = Math.Atan2(cameraForward.X, cameraForward.Y) * 180 / Math.PI;
            if (cameraAngle < 0)
            {
                cameraAngle += 360;
            }

            var player = new Player()
            {
                Identification = new PlayerIdentification()
                {
                    Account = this._accountName,
                    Character = GameService.Gw2Mumble.PlayerCharacter.Name,
                    GuildId = this._guildId
                },
                Position = new PlayerPosition()
                {
                    X = position.X,
                    Y = position.Y
                },
                Facing = new PlayerFacing()
                {
                    Angle = cameraAngle
                }
            };

            if (_lastSendPlayer != null && player.Equals(_lastSendPlayer))
            {
                return;
            }

            _lastSendPlayer = player;

            try
            {
                switch (this.ModuleSettings.PublishType.Value)
                {
                    case LiveMap.Models.PublishType.Global:
                        await this.PublishToGlobal(player);
                        break;
                    case LiveMap.Models.PublishType.Guild:
                        await this.PublishToGuild(player);
                        break;
                    case LiveMap.Models.PublishType.Both:
                        await this.PublishToGlobal(player);
                        await this.PublishToGuild(player);
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Debug(ex.Message);
            }
        }

        private async Task PublishToGlobal(Player player)
        {
            await this.GlobalSocket.EmitAsync("update", player);
        }

        private async Task PublishToGuild(Player player)
        {
            if (string.IsNullOrWhiteSpace(player.Identification.GuildId))
            {
                return;
            }

            await this.GuildSocket.EmitAsync("update", player);
        }

        protected override void Update(GameTime gameTime)
        {
            _ = UpdateUtil.UpdateAsync(this.SendPosition, gameTime, _sendInterval.TotalMilliseconds, _lastSend, false);
            _ = UpdateUtil.UpdateAsync(this.FetchGuildId, gameTime, _guildFetchInterval.TotalMilliseconds, _lastGuildFetch);
        }

        /// <inheritdoc />
        protected override void Unload()
        {
            base.Unload();
            this.Gw2ApiManager.SubtokenUpdated -= this.Gw2ApiManager_SubtokenUpdated;
            GameService.Gw2Mumble.PlayerCharacter.NameChanged -= this.PlayerCharacter_NameChanged;

            AsyncHelper.RunSync(this.GlobalSocket.DisconnectAsync);
            AsyncHelper.RunSync(this.GuildSocket.DisconnectAsync);

            Instance = null;
        }

        public override IView GetSettingsView()
        {
            return new UI.Views.SettingsView(this.Gw2ApiManager, this.IconState, this.TranslationState, this.ModuleSettings,
                () => this.GuildId, () => GameService.Gw2Mumble.UI.MapPosition.X, () => GameService.Gw2Mumble.UI.MapPosition.Y);
        }

        protected override BaseModuleSettings DefineModuleSettings(SettingCollection settings)
        {
            return new ModuleSettings(settings);
        }

        protected override string GetDirectoryName()
        {
            return null;
        }

        protected override AsyncTexture2D GetEmblem()
        {
            return null;
        }

        protected override AsyncTexture2D GetCornerIcon()
        {
            return null;
        }
    }
}

