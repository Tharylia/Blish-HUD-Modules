namespace Estreya.BlishHUD.Shared.Modules;

using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Graphics.UI;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Estreya.BlishHUD.Shared.Helpers;
using Estreya.BlishHUD.Shared.Security;
using Estreya.BlishHUD.Shared.Settings;
using Estreya.BlishHUD.Shared.Services;
using Estreya.BlishHUD.Shared.UI.Views;
using Estreya.BlishHUD.Shared.Utils;
using Flurl.Http;
using Gw2Sharp.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.BitmapFonts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using Estreya.BlishHUD.Shared.Extensions;
using System.Windows.Forms;
using System.Drawing.Text;

public abstract class BaseModule<TModule, TSettings> : Module where TSettings : Settings.BaseModuleSettings where TModule : class
{
    protected Logger Logger { get; }

    protected const string FILE_ROOT_URL = "https://files.estreya.de";
    protected const string FILE_BLISH_ROOT_URL = $"{FILE_ROOT_URL}/blish-hud";
    protected const string API_ROOT_URL = "https://blish-hud.api.estreya.de";

    protected const string GITHUB_OWNER = "Tharylia";
    protected const string GITHUB_REPOSITORY = "Blish-HUD-Modules";
    private const string GITHUB_CLIENT_ID = "Iv1.9e4dc29d43243704";

    protected GitHubHelper GithubHelper { get; private set; }

    protected PasswordManager PasswordManager { get; private set; }

    public string MODULE_FILE_URL => $"{FILE_BLISH_ROOT_URL}/{this.UrlModuleName}";
    public string MODULE_API_URL => $"{API_ROOT_URL}/v{this.API_VERSION_NO}/{this.UrlModuleName}";
    public abstract string UrlModuleName { get; }

    /// <summary>
    /// Specifies the api version to use with <see cref="API_ROOT_URL"/>
    /// </summary>
    protected abstract string API_VERSION_NO { get; }

    public bool IsPrerelease => !string.IsNullOrWhiteSpace(this.Version?.PreRelease);

    private ModuleSettingsView _defaultSettingView;

    private FlurlClient _flurlClient;

    private ConcurrentDictionary<string, string> _loadingTexts = new ConcurrentDictionary<string, string>();

    protected IFlurlClient GetFlurlClient()
    {
        if (this._flurlClient == null)
        {
            this._flurlClient = new FlurlClient();
            this._flurlClient.WithHeader("User-Agent", $"{this.Name} {this.Version}");
        }

        return this._flurlClient;
    }

    #region Service Managers
    protected SettingsManager SettingsManager => this.ModuleParameters.SettingsManager;
    protected ContentsManager ContentsManager => this.ModuleParameters.ContentsManager;
    protected DirectoriesManager DirectoriesManager => this.ModuleParameters.DirectoriesManager;
    protected Gw2ApiManager Gw2ApiManager => this.ModuleParameters.Gw2ApiManager;
    #endregion

#if DEBUG
    protected bool Debug => true;
#else
    protected bool Debug => false;
#endif

    protected bool ShowUI { get; private set; } = true;

    public TSettings ModuleSettings { get; private set; }

    protected CornerIcon CornerIcon { get; set; }

    private LoadingSpinner _loadingSpinner;

    protected TabbedWindow2 SettingsWindow { get; private set; }

    public virtual BitmapFont Font => GameService.Content.DefaultFont16;

    #region Services
    private readonly AsyncLock _servicesLock = new AsyncLock();
    private SynchronizedCollection<ManagedService> _services = new SynchronizedCollection<ManagedService>();

    public IconService IconService { get; private set; }
    public TranslationService TranslationService { get; private set; }
    public SettingEventService SettingEventService { get; private set; }
    public NewsService NewsService { get; private set; }
    public WorldbossService WorldbossService { get; private set; }
    public MapchestService MapchestService { get; private set; }
    public PointOfInterestService PointOfInterestService { get; private set; }
    public AccountService AccountService { get; private set; }
    public SkillService SkillService { get; private set; }
    public TradingPostService TradingPostService { get; private set; }
    public ItemService ItemService { get; private set; }
    public ArcDPSService ArcDPSService { get; private set; }
    public BlishHudApiService BlishHUDAPIService { get; private set; }

    public AchievementService AchievementService { get; private set; }
    public AccountAchievementService AccountAchievementService { get; private set; }
    #endregion

    public BaseModule(ModuleParameters moduleParameters) : base(moduleParameters)
    {
        this.Logger = Logger.GetLogger(this.GetType());
    }

    protected sealed override void DefineSettings(SettingCollection settings)
    {
        this.ModuleSettings = this.DefineModuleSettings(settings) as TSettings;
    }

    protected abstract BaseModuleSettings DefineModuleSettings(SettingCollection settings);

    protected override void Initialize()
    {
        string directoryName = this.GetDirectoryName();

        if (!string.IsNullOrWhiteSpace(directoryName))
        {
            var directoryPath = this.DirectoriesManager.GetFullDirectoryPath(directoryName);
            this.PasswordManager = new PasswordManager(directoryPath);
            this.PasswordManager.InitializeEntropy(Encoding.UTF8.GetBytes(this.Namespace));
        }
    }

    protected override async Task LoadAsync()
    {
        this.Logger.Debug("Initialize states");
        await Task.Factory.StartNew(this.InitializeServices, TaskCreationOptions.LongRunning).Unwrap();

        this.GithubHelper = new GitHubHelper(GITHUB_OWNER, GITHUB_REPOSITORY, GITHUB_CLIENT_ID, this.Name, this.PasswordManager, this.IconService, this.TranslationService, this.ModuleSettings);

        this.ModuleSettings.UpdateLocalization(this.TranslationService);

        this.ModuleSettings.ModuleSettingsChanged += this.ModuleSettings_ModuleSettingsChanged;
    }

    private void ModuleSettings_ModuleSettingsChanged(object sender, BaseModuleSettings.ModuleSettingsChangedEventArgs e)
    {
        switch (e.Name)
        {
            case nameof(this.ModuleSettings.RegisterCornerIcon):
                this.HandleCornerIcon(this.ModuleSettings.RegisterCornerIcon.Value);
                break;
        }
    }

    protected abstract string GetDirectoryName();

    private async Task InitializeServices()
    {
        string directoryName = this.GetDirectoryName();

        string directoryPath = null;
        if (!string.IsNullOrWhiteSpace(directoryName))
        {
            directoryPath = this.DirectoriesManager.GetFullDirectoryPath(directoryName);
        }

        using (await this._servicesLock.LockAsync())
        {
            ServiceConfigurations configurations = new ServiceConfigurations();
            this.ConfigureServices(configurations);

            if (configurations.BlishHUDAPI.Enabled)
            {
                if (this.PasswordManager == null)
                {
                    throw new ArgumentNullException(nameof(this.PasswordManager));
                }

                this.BlishHUDAPIService = new BlishHudApiService(configurations.BlishHUDAPI, this.ModuleSettings.BlishAPIUsername, this.PasswordManager, this.GetFlurlClient(), API_ROOT_URL, this.API_VERSION_NO);
                this._services.Add(this.BlishHUDAPIService);
            }

            if (configurations.Account.Enabled)
            {
                this.AccountService = new AccountService(configurations.Account, this.Gw2ApiManager);
                this._services.Add(this.AccountService);
            }

            this.IconService = new IconService(new APIServiceConfiguration()
            {
                Enabled = true,
                AwaitLoading = false
            }, this.ContentsManager);
            this._services.Add(this.IconService);

            this.TranslationService = new TranslationService(new ServiceConfiguration()
            {
                Enabled = true,
                AwaitLoading = true,
            }, this.GetFlurlClient(), this.MODULE_FILE_URL);
            this._services.Add(this.TranslationService);

            this.SettingEventService = new SettingEventService(new ServiceConfiguration()
            {
                Enabled = true,
                AwaitLoading = false,
                SaveInterval = Timeout.InfiniteTimeSpan
            });
            this._services.Add(this.SettingEventService);

            this.NewsService = new NewsService(new ServiceConfiguration()
            {
                AwaitLoading = true,
                Enabled = true
            }, this.GetFlurlClient(), this.MODULE_FILE_URL);
            this._services.Add(this.NewsService);

            if (configurations.Items.Enabled)
            {
                this.ItemService = new ItemService(configurations.Items, this.Gw2ApiManager, directoryPath);
                this._services.Add(this.ItemService);
            }

            if (configurations.TradingPost.Enabled)
            {
                this.TradingPostService = new TradingPostService(configurations.TradingPost, this.Gw2ApiManager, this.ItemService);
                this._services.Add(this.TradingPostService);
            }

            if (configurations.Worldbosses.Enabled)
            {
                if (configurations.Account.Enabled)
                {
                    this.WorldbossService = new WorldbossService(configurations.Worldbosses, this.Gw2ApiManager, this.AccountService);
                    this._services.Add(this.WorldbossService);
                }
                else
                {
                    this.Logger.Debug($"{typeof(WorldbossService).Name} is not available because {typeof(AccountService).Name} is deactivated.");
                    configurations.Worldbosses.Enabled = false;
                }
            }

            if (configurations.Mapchests.Enabled)
            {
                if (configurations.Account.Enabled)
                {
                    this.MapchestService = new MapchestService(configurations.Mapchests, this.Gw2ApiManager, this.AccountService);
                    this._services.Add(this.MapchestService);
                }
                else
                {
                    this.Logger.Debug($"{typeof(MapchestService).Name} is not available because {typeof(AccountService).Name} is deactivated.");
                    configurations.Mapchests.Enabled = false;
                }
            }

            if (configurations.PointOfInterests.Enabled)
            {
                if (string.IsNullOrWhiteSpace(directoryPath))
                {
                    throw new ArgumentNullException(nameof(directoryPath), "Module directory is not specified.");
                }

                this.PointOfInterestService = new PointOfInterestService(configurations.PointOfInterests, this.Gw2ApiManager, directoryPath);
                this._services.Add(this.PointOfInterestService);
            }

            if (configurations.Skills.Enabled)
            {
                if (string.IsNullOrWhiteSpace(directoryPath))
                {
                    throw new ArgumentNullException(nameof(directoryPath), "Module directory is not specified.");
                }

                this.SkillService = new SkillService(configurations.Skills, this.Gw2ApiManager, this.IconService, directoryPath, this.GetFlurlClient(), FILE_ROOT_URL);
                this._services.Add(this.SkillService);
            }

            if (configurations.ArcDPS.Enabled)
            {
                if (configurations.Skills.Enabled)
                {
                    this.ArcDPSService = new ArcDPSService(configurations.ArcDPS, this.SkillService);
                    this._services.Add(this.ArcDPSService);
                }
                else
                {
                    this.Logger.Debug($"{typeof(ArcDPSService).Name} is not available because {typeof(SkillService).Name} is deactivated.");
                    configurations.ArcDPS.Enabled = false;
                }
            }

            if (configurations.Achievements.Enabled)
            {
                this.AchievementService = new AchievementService(this.Gw2ApiManager, configurations.Achievements, directoryPath);
                this._services.Add(this.AchievementService);
            }

            if (configurations.AccountAchievements.Enabled)
            {
                this.AccountAchievementService = new AccountAchievementService(this.Gw2ApiManager, configurations.AccountAchievements);
                this._services.Add(this.AccountAchievementService);
            }

            Collection<ManagedService> customServices = this.GetAdditionalServices(directoryPath);

            if (customServices != null && customServices.Count > 0)
            {
                foreach (ManagedService customService in customServices)
                {
                    this._services.Add(customService);
                }
            }

            this.OnBeforeServicesStarted();

            // Only start states not already running
            foreach (ManagedService state in this._services.Where(state => !state.Running))
            {
                // Order is important
                if (state.AwaitLoading)
                {
                    try
                    {
                        await state.Start();
                    }
                    catch (Exception ex)
                    {
                        this.Logger.Error(ex, "Failed starting state \"{0}\"", state.GetType().Name);
                    }
                }
                else
                {
                    _ = Task.Run(state.Start).ContinueWith(task =>
                    {
                        if (task.IsFaulted)
                        {
                            this.Logger.Error(task.Exception, "Not awaited state start failed for \"{0}\"", state.GetType().Name);
                        }
                    }).ConfigureAwait(false);
                }
            }
        }
    }

    protected virtual void ConfigureServices(ServiceConfigurations configurations) { }

    protected virtual void OnBeforeServicesStarted() { }

    protected virtual Collection<ManagedService> GetAdditionalServices(string directoryPath)
    {
        return null;
    }

    private void HandleCornerIcon(bool show)
    {
        if (show)
        {
            if (this.CornerIcon == null)
            {
                this.CornerIcon = new CornerIcon()
                {
                    IconName = this.Name,
                    Icon = this.GetCornerIcon(),
                    Priority = 1_289_351_278
                };

                this.OnCornerIconBuild();
            }
        }
        else
        {
            if (this.CornerIcon != null)
            {
                this.OnCornerIconDispose();
                this.CornerIcon.Dispose();
                this.CornerIcon = null;
            }
        }
    }

    protected virtual void OnCornerIconBuild()
    {
        this.CornerIcon.Click += this.CornerIcon_Click;
        this.CornerIcon.RightMouseButtonPressed += this.CornerIcon_RightMouseButtonPressed;
    }

    protected virtual void OnCornerIconDispose()
    {
        this.CornerIcon.Click -= this.CornerIcon_Click;
        this.CornerIcon.RightMouseButtonPressed -= this.CornerIcon_RightMouseButtonPressed;
    }

    private void CornerIcon_Click(object sender, Blish_HUD.Input.MouseEventArgs e)
    {
        switch (this.ModuleSettings.CornerIconLeftClickAction.Value)
        {
            case Models.CornerIconClickAction.Settings:
                this.SettingsWindow.ToggleWindow();
                break;
            case Models.CornerIconClickAction.Visibility:
                this.ModuleSettings.GlobalDrawerVisible.Value = !this.ModuleSettings.GlobalDrawerVisible.Value;
                break;
        }
    }

    private void CornerIcon_RightMouseButtonPressed(object sender, Blish_HUD.Input.MouseEventArgs e)
    {
        switch (this.ModuleSettings.CornerIconRightClickAction.Value)
        {
            case Models.CornerIconClickAction.Settings:
                this.SettingsWindow.ToggleWindow();
                break;
            case Models.CornerIconClickAction.Visibility:
                this.ModuleSettings.GlobalDrawerVisible.Value = !this.ModuleSettings.GlobalDrawerVisible.Value;
                break;
        }
    }

    public override IView GetSettingsView()
    {
        if (this._defaultSettingView == null)
        {
            this._defaultSettingView = new ModuleSettingsView(this.IconService, this.TranslationService);
            this._defaultSettingView.OpenClicked += this.DefaultSettingView_OpenClicked;
            this._defaultSettingView.CreateGithubIssueClicked += this.DefaultSettingView_CreateGithubIssueClicked;
        }

        return this._defaultSettingView;
    }

    private void DefaultSettingView_CreateGithubIssueClicked(object sender, EventArgs e)
    {
        this.GithubHelper.OpenIssueWindow();
    }

    private void DefaultSettingView_OpenClicked(object sender, EventArgs e)
    {
        this.SettingsWindow.ToggleWindow();
    }

    protected override void OnModuleLoaded(EventArgs e)
    {
        // Base handler must be called
        base.OnModuleLoaded(e);

        this.Logger.Debug("Start building settings window.");

        this.SettingsWindow ??= WindowUtil.CreateTabbedWindow(this.Name, this.GetType(), Guid.Parse("6bd04be4-dc19-4914-a2c3-8160ce76818b"), this.IconService, this.GetEmblem());

        this.SettingsWindow.Tabs.Add(new Tab(this.IconService.GetIcon("482926.png"), () => new UI.Views.NewsView(this.GetFlurlClient(), this.Gw2ApiManager, this.IconService, this.TranslationService, this.NewsService, GameService.Content.DefaultFont16) { DefaultColor = this.ModuleSettings.DefaultGW2Color }, "News"));

        this.OnSettingWindowBuild(this.SettingsWindow);

        this.SettingsWindow.Tabs.Add(new Tab(this.IconService.GetIcon("156331.png"), () => new UI.Views.DonationView(this.GetFlurlClient(), this.Gw2ApiManager, this.IconService, this.TranslationService, GameService.Content.DefaultFont16) { DefaultColor = this.ModuleSettings.DefaultGW2Color }, "Donations"));

        if (this.Debug)
        {
            this.SettingsWindow.Tabs.Add(
                new Tab(
                    this.IconService.GetIcon("155052.png"),
                    () => new UI.Views.Settings.ServiceSettingsView(this._services, this.Gw2ApiManager, this.IconService, this.TranslationService, this.SettingEventService, this.Font)
                    {
                        DefaultColor = this.ModuleSettings.DefaultGW2Color
                    },
                    "Debug"));
        }

        this.Logger.Debug("Finished building settings window.");

        this.HandleCornerIcon(this.ModuleSettings.RegisterCornerIcon.Value);
    }

    /// <summary>
    /// Gets the emblem for the settings window.
    /// </summary>
    /// <returns>The emblem as <see cref="AsyncTexture2D"/>.</returns>
    protected abstract AsyncTexture2D GetEmblem();

    /// <summary>
    /// Gets the icon used for corner icons.
    /// </summary>
    /// <returns>The corner icon as <see cref="AsyncTexture2D"/>.</returns>
    protected abstract AsyncTexture2D GetCornerIcon();

    /// <summary>
    /// Gets called after the base settings window has been constructed. Used to add custom tabs.
    /// </summary>
    /// <param name="settingWindow">The settings window.</param>
    protected virtual void OnSettingWindowBuild(TabbedWindow2 settingWindow) { }

    /// <inheritdoc/>
    protected override void Update(GameTime gameTime)
    {
        this.ShowUI = this.CalculateUIVisibility();

        using (this._servicesLock.Lock())
        {
            List<string> stateLoadingTexts = new List<string>();
            foreach (ManagedService state in this._services)
            {
                state.Update(gameTime);

                if (state is APIService apiService)
                {
                    var loading = apiService.Loading;

                    if (loading)
                    {
                        if (!string.IsNullOrWhiteSpace(apiService.ProgressText))
                        {
                            stateLoadingTexts.Add($"{state.GetType().Name}: {apiService.ProgressText?.ToString()}");
                        }
                        else
                        {
                            stateLoadingTexts.Add(state.GetType().Name);
                        }
                    }
                }
            }

            var stateTexts = stateLoadingTexts.Count == 0 ? null : $"Services:\n{new string(' ', 4)}" + string.Join($"\n{new string(' ', 4)}", stateLoadingTexts);
            this.ReportLoading("states", stateTexts);
        }

        var loadingTexts = new StringBuilder();
        foreach (var loadingText in this._loadingTexts)
        {
            if (loadingText.Value == null) continue;

            loadingTexts.AppendLine(loadingText.Value);
        }

        this.HandleLoadingSpinner(loadingTexts.Length > 0, loadingTexts.ToString().Trim());
    }

    /// <summary>
    /// Report a new loading text to display. Report <see cref="null"/> to finish.
    /// </summary>
    /// <param name="loadingText"></param>
    protected void ReportLoading(string group, string loadingText)
    {
        this._loadingTexts.AddOrUpdate(group, loadingText, (key, oldVal) => loadingText);
    }

    /// <summary>
    /// Calculates the ui visibility based on settings or mumble parameters.
    /// </summary>
    /// <returns>The newly calculated ui visibility or the last value of <see cref="ShowUI"/>.</returns>
    protected virtual bool CalculateUIVisibility()
    {
        if (!this.ModuleSettings.GlobalDrawerVisible.Value)
        {
            return false;
        }

        bool show = true;
        if (this.ModuleSettings.HideOnOpenMap.Value)
        {
            show &= !GameService.Gw2Mumble.UI.IsMapOpen;
        }

        if (this.ModuleSettings.HideOnMissingMumbleTicks.Value)
        {
            show &= GameService.Gw2Mumble.TimeSinceTick.TotalSeconds < 0.5;
        }

        if (this.ModuleSettings.HideInCombat.Value)
        {
            show &= !GameService.Gw2Mumble.PlayerCharacter.IsInCombat;
        }

        // All maps not specified as competetive will be treated as open world
        if (this.ModuleSettings.HideInPvE_OpenWorld.Value)
        {
            MapType[] pveOpenWorldMapTypes = new[] { MapType.Public, MapType.Instance, MapType.Tutorial, MapType.PublicMini };

            show &= !(!GameService.Gw2Mumble.CurrentMap.IsCompetitiveMode && pveOpenWorldMapTypes.Any(type => type == GameService.Gw2Mumble.CurrentMap.Type) && !MumbleInfo.Map.MapInfo.MAP_IDS_PVE_COMPETETIVE.Contains(GameService.Gw2Mumble.CurrentMap.Id));
        }

        if (this.ModuleSettings.HideInPvE_Competetive.Value)
        {
            MapType[] pveCompetetiveMapTypes = new[] { MapType.Instance };

            show &= !(!GameService.Gw2Mumble.CurrentMap.IsCompetitiveMode && pveCompetetiveMapTypes.Any(type => type == GameService.Gw2Mumble.CurrentMap.Type) && MumbleInfo.Map.MapInfo.MAP_IDS_PVE_COMPETETIVE.Contains(GameService.Gw2Mumble.CurrentMap.Id));
        }

        if (this.ModuleSettings.HideInWvW.Value)
        {
            MapType[] wvwMapTypes = new[] { MapType.EternalBattlegrounds, MapType.GreenBorderlands, MapType.RedBorderlands, MapType.BlueBorderlands, MapType.EdgeOfTheMists };

            show &= !(GameService.Gw2Mumble.CurrentMap.IsCompetitiveMode && wvwMapTypes.Any(type => type == GameService.Gw2Mumble.CurrentMap.Type));
        }

        if (this.ModuleSettings.HideInPvP.Value)
        {
            MapType[] pvpMapTypes = new[] { MapType.Pvp, MapType.Tournament };

            show &= !(GameService.Gw2Mumble.CurrentMap.IsCompetitiveMode && pvpMapTypes.Any(type => type == GameService.Gw2Mumble.CurrentMap.Type));
        }

        return show;
    }

    protected void HandleLoadingSpinner(bool show, string text = null)
    {
        show &= this.CornerIcon != null;

        this._loadingSpinner ??= new LoadingSpinner()
        {
            Parent = GameService.Graphics.SpriteScreen,
            Size = this.CornerIcon?.Size ?? new Point(0, 0),
            Visible = false
        };

        if (this.CornerIcon != null)
        {
            this._loadingSpinner.Location = new Point(this.CornerIcon.Location.X, this.CornerIcon.Location.Y + this.CornerIcon.Height + 5);
        }

        this._loadingSpinner.BasicTooltipText = text;
        this._loadingSpinner.Visible = show;
    }

    protected async Task ReloadServices()
    {
        var tasks = new List<Task>();

        using (await this._servicesLock.LockAsync())
        {
            tasks.AddRange(this._services.Select(s => s.Reload()));
        }

        await Task.WhenAll(tasks);
    }

    protected override void Unload()
    {
        this.Logger.Debug("Unload settings...");

        if (this.ModuleSettings != null)
        {
            this.ModuleSettings.ModuleSettingsChanged -= this.ModuleSettings_ModuleSettingsChanged;
            this.ModuleSettings.Unload();
            this.ModuleSettings = null;
        }

        this.Logger.Debug("Unloaded settings.");

        this.Logger.Debug("Unload default settings view...");

        if (this._defaultSettingView != null)
        {
            this._defaultSettingView.OpenClicked -= this.DefaultSettingView_OpenClicked;
            this._defaultSettingView.CreateGithubIssueClicked -= this.DefaultSettingView_CreateGithubIssueClicked;
            this._defaultSettingView.DoUnload();
            this._defaultSettingView = null;
        }

        this.Logger.Debug("Unloaded default settings view.");

        this.Logger.Debug("Unload settings window...");

        this.SettingsWindow?.Hide();
        this.SettingsWindow?.Dispose();
        this.SettingsWindow = null;

        this.Logger.Debug("Unloaded settings window.");

        this.Logger.Debug("Unloading states...");

        using (this._servicesLock.Lock())
        {
            this._services?.ToList().ForEach(state => state?.Dispose());
            this._services?.Clear();

            this.AccountService = null;
            this.ArcDPSService = null;
            this.IconService = null;
            this.ItemService = null;
            this.MapchestService = null;
            this.WorldbossService = null;
            this.PointOfInterestService = null;
            this.SettingEventService = null;
            this.SkillService = null;
            this.TradingPostService = null;
            this.TranslationService = null;
        }

        this.Logger.Debug("Unloaded states.");

        this.Logger.Debug("Unload flurl client...");

        this._flurlClient?.Dispose();
        this._flurlClient = null;

        this.Logger.Debug("Unloaded flurl client.");

        this.Logger.Debug("Unload corner icon...");

        this._loadingTexts?.Clear();

        this.HandleCornerIcon(false);
        this._loadingSpinner?.Dispose();
        this._loadingSpinner = null;

        this.Logger.Debug("Unloaded corner icon.");
    }
}
