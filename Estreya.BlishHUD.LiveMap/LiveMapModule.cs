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
    using Flurl.Util;
    using Gw2Sharp.WebApi.V2.Models;
    using Humanizer;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Octokit;
    using SocketIOClient.JsonSerializer;
    using SocketIOClient.Messages;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Net;
    using System.Security.Policy;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Media.Animation;

    [Export(typeof(Blish_HUD.Modules.Module))]
    public class LiveMapModule : BaseModule<LiveMapModule, ModuleSettings>
    {
        private static readonly Logger Logger = Logger.GetLogger<LiveMapModule>();
        private const string LIVE_MAP_BASE_API_URL = "http://localhost:3004/v1/live-map";
        private const string LIVE_MAP_GLOBAL_API_URL = $"{LIVE_MAP_BASE_API_URL}/write";
        public const string LIVE_MAP_GLOBAL_URL = $"https://gw2map.estreya.de/";

        private SocketIOClient.SocketIO GlobalSocket = new SocketIOClient.SocketIO(LIVE_MAP_GLOBAL_API_URL, new SocketIOClient.SocketIOOptions()
        {
            Transport = SocketIOClient.Transport.TransportProtocol.WebSocket
        });

        private TimeSpan _sendInterval = TimeSpan.FromMilliseconds(250);
        private AsyncRef<double> _lastSend = new AsyncRef<double>(0);
        private Player _lastSendPlayer;
        private TimeSpan _guildFetchInterval = TimeSpan.FromSeconds(30);
        private AsyncRef<double> _lastGuildFetch = new AsyncRef<double>(0);
        private TimeSpan _wvwFetchInterval = TimeSpan.FromHours(1);
        private AsyncRef<double> _lastWvWFetch = new AsyncRef<double>(0);

        private string _accountName;
        private string _guildId;
        private PlayerWvW _wvw;
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

            _ = Task.Run(this.GlobalSocket.ConnectAsync);

            await this.FetchAccountName();
            await this.FetchGuildId();
            await this.FetchWvW();
        }

        public static byte[] Compress(byte[] bytes)
        {
            using var memoryStream = new MemoryStream();
            using (var gzipStream = new GZipStream(memoryStream, CompressionLevel.Optimal))
            {
                gzipStream.Write(bytes, 0, bytes.Length);
            }

            return memoryStream.ToArray();
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
            _lastWvWFetch.Value = _wvwFetchInterval.TotalMilliseconds;
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

        private async Task FetchWvW()
        {
            var color = "white";
            var matchId = "0-0";

            if (this.Gw2ApiManager.HasPermissions(new[] { TokenPermission.Account }))
            {
                try
                {
                    var account = await this.Gw2ApiManager.Gw2ApiClient.V2.Account.GetAsync();
                    var worldId = account.World;
                    var matches = await this.Gw2ApiManager.Gw2ApiClient.V2.Wvw.Matches.AllAsync();

                    var match = matches.Where(m => m.AllWorlds.Green.Contains(worldId)).FirstOrDefault();
                    if (match != null)
                    {
                        color = "green";
                        matchId = match.Id;
                    }

                    match = matches.Where(m => m.AllWorlds.Red.Contains(worldId)).FirstOrDefault();
                    if (match != null)
                    {
                        color = "red";
                        matchId = match.Id;
                    }

                    match = matches.Where(m => m.AllWorlds.Blue.Contains(worldId)).FirstOrDefault();
                    if (match != null)
                    {
                        color = "blue";
                        matchId = match.Id;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Debug(ex, "Failed to fetch wvw team color:");
                }
            }

            this._wvw = new PlayerWvW()
            {
                Match = matchId,
                TeamColor = color,
            };
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
                var orig = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(player));
                var compressed = Compress(orig);

                await this.PublishToGlobal(compressed);
            }
            catch (Exception ex)
            {
                Logger.Debug(ex.Message);
            }
        }

        private async Task PublishToGlobal(byte[] data)
        {
            if (this.GlobalSocket.Connected)
            {
                await this.GlobalSocket.EmitAsync("update", data);
            }
        }

        protected override void Update(GameTime gameTime)
        {
            _ = UpdateUtil.UpdateAsync(this.SendPosition, gameTime, _sendInterval.TotalMilliseconds, _lastSend, false);
            _ = UpdateUtil.UpdateAsync(this.FetchGuildId, gameTime, _guildFetchInterval.TotalMilliseconds, _lastGuildFetch);
            _ = UpdateUtil.UpdateAsync(this.FetchWvW, gameTime, _wvwFetchInterval.TotalMilliseconds, _lastWvWFetch);
        }

        /// <inheritdoc />
        protected override void Unload()
        {
            base.Unload();
            this.Gw2ApiManager.SubtokenUpdated -= this.Gw2ApiManager_SubtokenUpdated;
            GameService.Gw2Mumble.PlayerCharacter.NameChanged -= this.PlayerCharacter_NameChanged;

            AsyncHelper.RunSync(this.GlobalSocket.DisconnectAsync);

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
                WvW = this._wvw,
                Commander = !this.ModuleSettings.HideCommander.Value && GameService.Gw2Mumble.PlayerCharacter.IsCommander
            };

            return player;
        }

        private string GetGlobalUrl(bool formatPositions = true)
        {
            var baseUrl = LIVE_MAP_GLOBAL_URL;
            var url = baseUrl;

            if (this._map?.ContinentId == 1)
            {
                url = Path.Combine(url, "tyria");
            }
            else if (this._map?.ContinentId == 2)
            {
                url = Path.Combine(url, "mists");
                if (this._wvw != null)
                {
                    url = Path.Combine(url, this._wvw.Match);
                }
            }
            else
            {
                return baseUrl;
            }

            return formatPositions ? this.FormatUrlWithPosition(url) : url;
        }

        private string GetGuildUrl(bool formatPositions = true)
        {
            var baseUrl = this.GetGlobalUrl(false);
            var url = baseUrl;

            if (!string.IsNullOrWhiteSpace(GuildId))
            {
                url = Path.Combine(url, "guild", GuildId);
            }
            else
            {
                return baseUrl;
            }

            return formatPositions ? this.FormatUrlWithPosition(url) : url;
        }

        private string FormatUrlWithPosition(string url)
        {
            var player = this.GetPlayer();
            return $"{url}?posX={player.Map.Position.X.ToInvariantString()}&posY={player.Map.Position.Y.ToInvariantString()}&zoom=6";
        }

        public override IView GetSettingsView()
        {
            return new UI.Views.SettingsView(this.Gw2ApiManager, this.IconState, this.TranslationState, this.SettingEventState, this.ModuleSettings,
               () =>this.GetGlobalUrl(), () => this.GetGuildUrl() );
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

