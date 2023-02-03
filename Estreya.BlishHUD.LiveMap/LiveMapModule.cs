namespace Estreya.BlishHUD.LiveMap
{
    using Blish_HUD;
    using Blish_HUD.Content;
    using Blish_HUD.Controls;
    using Blish_HUD.Graphics.UI;
    using Blish_HUD.Gw2Mumble;
    using Blish_HUD.Modules;
    using Blish_HUD.Modules.Managers;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.LiveMap;
    using Estreya.BlishHUD.LiveMap.Models.Player;
    using Estreya.BlishHUD.Shared.Extensions;
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
    using Octokit;
    using SocketIOClient.JsonSerializer;
    using SocketIOClient.Messages;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Linq;
    using System.Net;
    using System.Security.Policy;
    using System.Threading;
    using System.Threading.Tasks;

    [Export(typeof(Blish_HUD.Modules.Module))]
    public class LiveMapModule : BaseModule<LiveMapModule, ModuleSettings>
    {
        private static readonly Logger Logger = Logger.GetLogger<LiveMapModule>();
        private const string LIVE_MAP_BASE_API_URL = "https://gw2map.api.estreya.de/v2";
        private const string LIVE_MAP_GLOBAL_API_URL = $"{LIVE_MAP_BASE_API_URL}/global/write";
        private const string LIVE_MAP_GUILD_API_URL = $"{LIVE_MAP_BASE_API_URL}/guild/write";
        public string LIVE_MAP_GLOBAL_URL => $"https://gw2map.estreya.de/{this._map?.ContinentId ?? 1}";
        public string LIVE_MAP_GUILD_URL => $"{LIVE_MAP_GLOBAL_URL}/guild/{{0}}";

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
        private TimeSpan _wvwColorFetchInterval = TimeSpan.FromHours(1);
        private AsyncRef<double> _lastWvWColorFetch = new AsyncRef<double>(0);

        private string _accountName;
        private string _guildId;
        private string _wvwColor;
        private Map _map;

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
            GameService.Gw2Mumble.CurrentMap.MapChanged += this.CurrentMap_MapChanged;

            this._lastSend.Value = _sendInterval.TotalMilliseconds;
            this._lastGuildFetch.Value = _guildFetchInterval.TotalMilliseconds;

            Instance = this;
        }

        private void CurrentMap_MapChanged(object sender, ValueEventArgs<int> e)
        {
            this.Gw2ApiManager.Gw2ApiClient.V2.Maps.GetAsync(e.Value)
            .ContinueWith(response =>
            {
                if (response.Exception != null || response.IsFaulted || response.IsCanceled) return;
                var result = response.Result;

                this._map = result;
            });
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
            await this.FetchWvWColor();
        }

        protected override void OnModuleLoaded(EventArgs e)
        {
            // Base handler must be called
            base.OnModuleLoaded(e);
        }

        private void Gw2ApiManager_SubtokenUpdated(object sender, ValueEventArgs<IEnumerable<TokenPermission>> e)
        {
            Task.Run(this.FetchAccountName);
            _lastGuildFetch.Value = _guildFetchInterval.TotalMilliseconds;
            _lastWvWColorFetch.Value = _wvwColorFetchInterval.TotalMilliseconds;
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

            try
            {
                var character = await this.Gw2ApiManager.Gw2ApiClient.V2.Characters.GetAsync(GameService.Gw2Mumble.PlayerCharacter.Name);
                this._guildId = character.Guild.ToString();
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, "Failed to fetch guild id:");
            }
        }

        private async Task FetchWvWColor()
        {
            var color = "white";

            if (this.Gw2ApiManager.HasPermissions(new[] { TokenPermission.Account }))
            {
                try
                {
                    var account = await this.Gw2ApiManager.Gw2ApiClient.V2.Account.GetAsync();
                    var worldId = account.World;
                    var matches = await this.Gw2ApiManager.Gw2ApiClient.V2.Wvw.Matches.AllAsync();


                    if (matches.Where(m => m.AllWorlds.Green.Contains(worldId)).Any())
                    {
                        color = "green";
                    }

                    if (matches.Where(m => m.AllWorlds.Red.Contains(worldId)).Any())
                    {
                        color = "red";
                    }

                    if (matches.Where(m => m.AllWorlds.Blue.Contains(worldId)).Any())
                    {
                        color = "blue";
                    }
                }
                catch (Exception ex)
                {
                    Logger.Debug(ex, "Failed to fetch wvw team color:");
                }
            }

            this._wvwColor = color;
        }

        private async Task SendPosition()
        {
            if (string.IsNullOrWhiteSpace(_accountName) || !GameService.Gw2Mumble.IsAvailable || GameService.Gw2Mumble.TimeSinceTick.TotalSeconds > 0.5 || (this.ModuleSettings.StreamerModeEnabled.Value && StreamerUtils.IsStreaming()))
            {
                return;
            }

            var player = this.GetPlayer();


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
            _ = UpdateUtil.UpdateAsync(this.FetchWvWColor, gameTime, _wvwColorFetchInterval.TotalMilliseconds, _lastWvWColorFetch);
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

        public Player GetPlayer()
        {
            var position = this._map?.WorldMeterCoordsToMapCoords(GameService.Gw2Mumble.PlayerCharacter.Position) ?? Vector2.Zero;

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
                Map = new PlayerMap()
                {
                    Continent = this._map?.ContinentId ?? -1,
                    Name = this._map?.Name,
                    ID = this._map?.Id ?? -1,
                    Position = new PlayerPosition()
                    {
                        X = position.X,
                        Y = position.Y
                    }
                },
                Facing = new PlayerFacing()
                {
                    Angle = cameraAngle
                },
                WvW = new PlayerWvW()
                {
                    TeamColor = this._wvwColor
                },
                Commander = !this.ModuleSettings.HideCommander.Value && GameService.Gw2Mumble.PlayerCharacter.IsCommander
            };

            return player;
        }

        public override IView GetSettingsView()
        {
            return new UI.Views.SettingsView(this.Gw2ApiManager, this.IconState, this.TranslationState, this.ModuleSettings,
                () => this.GuildId, () => this.GetPlayer().Map.Position.X, () => this.GetPlayer().Map.Position.Y, () => LIVE_MAP_GLOBAL_URL, () => LIVE_MAP_GUILD_URL);
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

