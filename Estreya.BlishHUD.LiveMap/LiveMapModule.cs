namespace Estreya.BlishHUD.LiveMap;

using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Graphics.UI;
using Blish_HUD.Modules;
using Blish_HUD.Settings;
using Estreya.BlishHUD.LiveMap.SignalR;
using Flurl.Util;
using Gw2Sharp.WebApi.V2;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Models.Player;
using Shared.Extensions;
using Shared.Helpers;
using Shared.Modules;
using Shared.Settings;
using Shared.Threading;
using Shared.Utils;
using SocketIOClient;
using SocketIOClient.Transport;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UI.Views;

[Export(typeof(Module))]
public class LiveMapModule : BaseModule<LiveMapModule, ModuleSettings>
{
    public const string LIVE_MAP_BROWSER_URL = "https://gw2map.estreya.de/";

    private string _accountName;
    private TimeSpan _guildFetchInterval = TimeSpan.FromSeconds(30);
    private readonly AsyncRef<double> _lastGuildFetch = new AsyncRef<double>(0);
    private readonly AsyncRef<double> _lastSend = new AsyncRef<double>(0);
    private Player _lastSendPlayer;
    private readonly AsyncRef<double> _lastWvWFetch = new AsyncRef<double>(0);
    private Map _map;

    private TimeSpan _sendInterval = TimeSpan.FromMilliseconds(250);
    private PlayerWvW _wvw;
    private TimeSpan _wvwFetchInterval = TimeSpan.FromHours(1);

    private Microsoft.AspNetCore.SignalR.Client.HubConnection _hubConnection;

    [ImportingConstructor]
    public LiveMapModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) { }

    private string LIVE_MAP_API_URL => $"{this.MODULE_API_URL}/writer"; //$"http://localhost:3004/blish-hud/v{this.API_VERSION_NO}/{this.WebsiteModuleName}/write";

    public string GuildId { get; private set; }

    protected override string UrlModuleName => "live-map";

    protected override string API_VERSION_NO => "1";

    protected override bool NeedsBackend => true;

    protected override void Initialize()
    {
        base.Initialize();

        this._hubConnection = new HubConnectionBuilder()
            .WithUrl(LIVE_MAP_API_URL)
            .ConfigureLogging(options =>
            {
                options.SetMinimumLevel(LogLevel.Debug);
                options.AddProvider(new LoggerProvider());
                //options.ClearProviders();
            })
            .WithAutomaticReconnect(new UnlimitedRetryPolicy(TimeSpan.FromSeconds(5)))
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.General);
            })
            .Build();

        //this.GlobalSocket = new SocketIO(this.LIVE_MAP_API_URL, new SocketIOOptions {  
        //    Path= "/blish-hud/socket.io",
        //    Transport = TransportProtocol.WebSocket
        //});
        
        this.Gw2ApiManager.SubtokenUpdated += this.Gw2ApiManager_SubtokenUpdated;
        GameService.Gw2Mumble.PlayerCharacter.NameChanged += this.PlayerCharacter_NameChanged;
        GameService.Gw2Mumble.CurrentMap.MapChanged += this.CurrentMap_MapChanged;
        GameService.ArcDps.Common.Activate();

        this._lastSend.Value = this._sendInterval.TotalMilliseconds;
        this._lastGuildFetch.Value = this._guildFetchInterval.TotalMilliseconds;
    }

    private void CurrentMap_MapChanged(object sender, ValueEventArgs<int> e)
    {
        this.Gw2ApiManager.Gw2ApiClient.V2.Maps.GetAsync(e.Value)
            .ContinueWith(response =>
            {
                if (response.Exception != null || response.IsFaulted || response.IsCanceled)
                {
                    return;
                }

                Map result = response.Result;

                this._map = result;
            });
    }

    private async Task<bool> ConnectWithRetryAsync(CancellationToken token)
    {
        // Keep trying to until we can start or the token is canceled.
        while (true)
        {
            try
            {
                await this._hubConnection.StartAsync(token);
                return true;
            }
            catch when (token.IsCancellationRequested)
            {
                return false;
            }
            catch
            {
                await Task.Delay(5000);
            }
        }
    }

    protected override async Task LoadAsync()
    {
        await base.LoadAsync();

        this._hubConnection.Closed += this.HubConnection_Closed;
        this._hubConnection.Reconnecting += this.HubConnection_Reconnecting;
        this._hubConnection.Reconnected += this.HubConnection_Reconnected;

        this._hubConnection.On<int>("SetSendingInterval", intervalMs =>
        {
            this._sendInterval = TimeSpan.FromMilliseconds(intervalMs);
        });

        await this.ConnectWithRetryAsync(new CancellationToken());

        //this.GlobalSocket.OnConnected += this.GlobalSocket_OnConnected;
        //this.GlobalSocket.OnDisconnected += this.GlobalSocket_OnDisconnected;
        //this.GlobalSocket.OnError += this.GlobalSocket_OnError;
        //this.GlobalSocket.OnReconnectAttempt += this.GlobalSocket_OnReconnectAttempt;
        //this.GlobalSocket.OnReconnectFailed += this.GlobalSocket_OnReconnectFailed;
        //this.GlobalSocket.OnReconnectError += this.GlobalSocket_OnReconnectError;

        //this.GlobalSocket.On("interval", resp =>
        //{
        //    int interval = resp.GetValue<int>();
        //    this._sendInterval = TimeSpan.FromMilliseconds(interval);
        //});

        //_ = Task.Run(this.GlobalSocket.ConnectAsync);

        await this.FetchAccountName();
        await this.FetchGuildId();
        await this.FetchWvW();
    }

    private Task HubConnection_Reconnected(string arg)
    {
        this.Logger.Info("Reconnected.");
        return Task.CompletedTask;
    }

    private Task HubConnection_Reconnecting(Exception ex)
    {
        this.Logger.Info($"Attempt reconnect: {ex.Message}");

        return Task.CompletedTask;
    }

    private Task HubConnection_Closed(Exception ex)
    {
        this.Logger.Warn("Disconnected: " + ex.Message);
        return Task.CompletedTask;
    }

    private void GlobalSocket_OnConnected(object sender, EventArgs e)
    {
        this.Logger.Info("Connected.");
    }

    private void GlobalSocket_OnDisconnected(object sender, string e)
    {
        this.Logger.Warn("Disconnected: " + e);
        if (e == DisconnectReason.IOServerDisconnect)
        {
            this.Logger.Info($"Trying to reconnect...");
            //_ = this.GlobalSocket.ConnectAsync();
        }
    }

    private void GlobalSocket_OnError(object sender, string e)
    {
        this.Logger.Warn($"Error: {e}");
    }

    private void GlobalSocket_OnReconnectAttempt(object sender, int e)
    {
        this.Logger.Info($"Attempt reconnect: {e}");
    }

    private void GlobalSocket_OnReconnectFailed(object sender, EventArgs e)
    {
        this.Logger.Warn("Reconnect failed.");
    }

    private void GlobalSocket_OnReconnectError(object sender, Exception e)
    {
        this.Logger.Warn(e, "Could not reconnect");
    }

    public static byte[] Compress(byte[] bytes)
    {
        using MemoryStream memoryStream = new MemoryStream();
        using (GZipStream gzipStream = new GZipStream(memoryStream, CompressionLevel.Optimal))
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
        this._lastGuildFetch.Value = this._guildFetchInterval.TotalMilliseconds;
        this._lastWvWFetch.Value = this._wvwFetchInterval.TotalMilliseconds;
    }

    private void PlayerCharacter_NameChanged(object sender, ValueEventArgs<string> e)
    {
        this._lastGuildFetch.Value = this._guildFetchInterval.TotalMilliseconds;
    }

    private async Task FetchAccountName()
    {
        if (!this.Gw2ApiManager.HasPermissions(new[]
            {
                TokenPermission.Account
            }))
        {
            return;
        }

        Account account = await this.Gw2ApiManager.Gw2ApiClient.V2.Account.GetAsync();
        this._accountName = account.Name;
    }

    private async Task FetchGuildId()
    {
        if (!GameService.Gw2Mumble.IsAvailable || !this.Gw2ApiManager.HasPermissions(new[]
            {
                TokenPermission.Characters
            }))
        {
            return;
        }

        try
        {
            Character character = await this.Gw2ApiManager.Gw2ApiClient.V2.Characters.GetAsync(GameService.Gw2Mumble.PlayerCharacter.Name);
            this.GuildId = character.Guild.ToString();
        }
        catch (Exception ex)
        {
            this.Logger.Debug(ex, "Failed to fetch guild id:");
        }
    }

    private async Task FetchWvW()
    {
        string color = "white";
        string matchId = "0-0";

        if (this.Gw2ApiManager.HasPermissions(new[]
            {
                TokenPermission.Account
            }))
        {
            try
            {
                Account account = await this.Gw2ApiManager.Gw2ApiClient.V2.Account.GetAsync();
                int worldId = account.World;
                IApiV2ObjectList<WvwMatch> matches = await this.Gw2ApiManager.Gw2ApiClient.V2.Wvw.Matches.AllAsync();

                WvwMatch match = matches.Where(m => m.AllWorlds.Green.Contains(worldId)).FirstOrDefault();
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
                this.Logger.Debug(ex, "Failed to fetch wvw team color:");
            }
        }

        this._wvw = new PlayerWvW
        {
            Match = matchId,
            TeamColor = color
        };
    }

    private async Task SendPosition()
    {
        if (string.IsNullOrWhiteSpace(this._accountName) || !GameService.Gw2Mumble.IsAvailable || GameService.Gw2Mumble.TimeSinceTick.TotalSeconds > 0.5 || (this.ModuleSettings.StreamerModeEnabled.Value && StreamerUtils.IsStreaming()))
        {
            return;
        }

        Player player = this.GetPlayer();

        if (this._lastSendPlayer != null && player.Equals(this._lastSendPlayer))
        {
            return;
        }

        try
        {
            //byte[] orig = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(player));
            //byte[] compressed = Compress(orig);

            await this.PublishToGlobal(player);
            this._lastSendPlayer = player;
        }
        catch (Exception ex)
        {
            this.Logger.Debug(ex.Message);
        }
    }

    private async Task PublishToGlobal(Player player)
    {
        if (this._hubConnection.State == HubConnectionState.Connected)
        {
            await this._hubConnection.InvokeAsync("UpdatePlayer", player);
        }
        //if (this.GlobalSocket.Connected)
        //{
        //    await this.GlobalSocket.EmitAsync("update", data);
        //}
    }

    protected override void Update(GameTime gameTime)
    {
        _ = UpdateUtil.UpdateAsync(this.SendPosition, gameTime, this._sendInterval.TotalMilliseconds, this._lastSend, false);
        _ = UpdateUtil.UpdateAsync(this.FetchGuildId, gameTime, this._guildFetchInterval.TotalMilliseconds, this._lastGuildFetch);
        _ = UpdateUtil.UpdateAsync(this.FetchWvW, gameTime, this._wvwFetchInterval.TotalMilliseconds, this._lastWvWFetch);
    }

    /// <inheritdoc />
    protected override void Unload()
    {
        base.Unload();
        this.Gw2ApiManager.SubtokenUpdated -= this.Gw2ApiManager_SubtokenUpdated;
        GameService.Gw2Mumble.PlayerCharacter.NameChanged -= this.PlayerCharacter_NameChanged;
        //this.GlobalSocket.OnConnected -= this.GlobalSocket_OnConnected;
        //this.GlobalSocket.OnDisconnected -= this.GlobalSocket_OnDisconnected;
        //this.GlobalSocket.OnError -= this.GlobalSocket_OnError;
        //this.GlobalSocket.OnReconnectAttempt -= this.GlobalSocket_OnReconnectAttempt;
        //this.GlobalSocket.OnReconnectFailed -= this.GlobalSocket_OnReconnectFailed;
        //this.GlobalSocket.OnReconnectError -= this.GlobalSocket_OnReconnectError;
        this._hubConnection.Closed -= this.HubConnection_Closed;
        this._hubConnection.Reconnecting -= this.HubConnection_Reconnecting;
        this._hubConnection.Reconnected -= this.HubConnection_Reconnected;

        //AsyncHelper.RunSync(this.GlobalSocket.DisconnectAsync);
        AsyncHelper.RunSync(async () =>
        {
            await this._hubConnection.StopAsync();
            await this._hubConnection.DisposeAsync();
        });
    }

    public Player GetPlayer()
    {
        Vector2 position = this._map?.WorldMeterCoordsToMapCoords(GameService.Gw2Mumble.PlayerCharacter.Position) ?? Vector2.Zero;

        Vector3 forward = GameService.Gw2Mumble.PlayerCharacter.Forward;
        double angle = Math.Atan2(forward.X, forward.Y) * 180 / Math.PI;
        if (angle < 0)
        {
            angle += 360;
        }

        Player player = new Player
        {
            Identification = new PlayerIdentification
            {
                Account = this._accountName,
                Character = GameService.Gw2Mumble.PlayerCharacter.Name,
                GuildId = this.GuildId
            },
            Map = new PlayerMap
            {
                Continent = this.GetContinentId(this._map),
                //Name = this._map?.Name,
                //ID = this._map?.Id ?? -1,
                Position = new PlayerPosition
                {
                    X = position.X,
                    Y = position.Y
                }
            },
            Facing = new PlayerFacing { Angle = angle },
            WvW = this._wvw,
            Group = new PlayerGroup { Squad = this.ModuleSettings.SendGroupInformation.Value ? GameService.ArcDps.Common.PlayersInSquad.Values.Select(p => p.AccountName.Trim(':')).Where(p => p != this._accountName).ToArray() : null },
            Commander = !this.ModuleSettings.HideCommander.Value && GameService.Gw2Mumble.PlayerCharacter.IsCommander
        };

        return player;
    }

    private int GetContinentId(Map map)
    {
        if (map == null)
        {
            return -1;
        }

        return map.Id switch
        {
            1206 => 1, // Mistlock Sanctuary
            _ => map.ContinentId
        };
    }

    private string GetGlobalUrl(bool formatPositions = true)
    {
        string baseUrl = LIVE_MAP_BROWSER_URL;
        string url = baseUrl;

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
        string baseUrl = this.GetGlobalUrl(false);
        string url = baseUrl;

        if (!string.IsNullOrWhiteSpace(this.GuildId))
        {
            url = Path.Combine(url, "guild", this.GuildId);
        }
        else
        {
            return baseUrl;
        }

        return formatPositions ? this.FormatUrlWithPosition(url) : url;
    }

    private string FormatUrlWithPosition(string url)
    {
        Player player = this.GetPlayer();
        return $"{url}?posX={player.Map.Position.X.ToInvariantString()}&posY={player.Map.Position.Y.ToInvariantString()}&zoom=6{(!string.IsNullOrWhiteSpace(this._accountName) ? $"&account={this._accountName}" : "")}&follow={(this.ModuleSettings.FollowOnMap.Value ? "true" : "false")}";
    }

    public override IView GetSettingsView()
    {
        return new SettingsView(this.Gw2ApiManager, this.IconService, this.TranslationService, this.SettingEventService, this.ModuleSettings,
            () => this.GetGlobalUrl(), () => this.GetGuildUrl());
    }

    protected override BaseModuleSettings DefineModuleSettings(SettingCollection settings)
    {
        return new ModuleSettings(settings, this.Version);
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

    protected override int CornerIconPriority => 1_289_351_275;
}