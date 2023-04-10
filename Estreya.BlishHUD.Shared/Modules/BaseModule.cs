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
using Estreya.BlishHUD.Shared.State;
using Estreya.BlishHUD.Shared.UI.Views;
using Estreya.BlishHUD.Shared.Utils;
using Flurl.Http;
using Gw2Sharp.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.BitmapFonts;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public abstract class BaseModule<TModule, TSettings> : Module where TSettings : Settings.BaseModuleSettings where TModule : class
{
    protected Logger Logger { get; }

    protected const string FILE_ROOT_URL = "https://files.estreya.de";
    protected const string FILE_BLISH_ROOT_URL = $"{FILE_ROOT_URL}/blish-hud";
    protected string API_ROOT_URL = "https://blish-hud.api.estreya.de";

    protected const string GITHUB_OWNER = "Tharylia";
    protected const string GITHUB_REPOSITORY = "Blish-HUD-Modules";
    private const string GITHUB_CLIENT_ID = "Iv1.9e4dc29d43243704";

    protected GitHubHelper GithubHelper { get; private set; }

    protected PasswordManager PasswordManager { get; private set; }

    public string WEBSITE_MODULE_FILE_URL => $"{FILE_BLISH_ROOT_URL}/{this.WebsiteModuleName}";
    public string API_URL => $"{API_ROOT_URL}/v{this.API_VERSION_NO}/{this.WebsiteModuleName}";
    public abstract string WebsiteModuleName { get; }

    /// <summary>
    /// Specifies the api version to use with <see cref="API_ROOT_URL"/>
    /// </summary>
    protected abstract string API_VERSION_NO { get; }

    protected static TModule Instance;

    public bool IsPrerelease => !string.IsNullOrWhiteSpace(this.Version?.PreRelease);

    private ModuleSettingsView _defaultSettingView;

    private FlurlClient _flurlClient;

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

    #region States
    private readonly AsyncLock _stateLock = new AsyncLock();
    private Collection<ManagedState> _states = new Collection<ManagedState>();

    public IconState IconState { get; private set; }
    public TranslationState TranslationState { get; private set; }
    public SettingEventState SettingEventState { get; private set; }
    public NewsState NewsState { get; private set; }
    public WorldbossState WorldbossState { get; private set; }
    public MapchestState MapchestState { get; private set; }
    public PointOfInterestState PointOfInterestState { get; private set; }
    public AccountState AccountState { get; private set; }
    public SkillState SkillState { get; private set; }
    public TradingPostState TradingPostState { get; private set; }
    public ItemState ItemState { get; private set; }
    public ArcDPSState ArcDPSState { get; private set; }
    public BlishHudApiState BlishHUDAPIState { get; private set; }
    #endregion

    public BaseModule(ModuleParameters moduleParameters) : base(moduleParameters)
    {
        this.Logger = Logger.GetLogger(this.GetType());
        Instance = this as TModule;
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
        await Task.Factory.StartNew(this.InitializeStates, TaskCreationOptions.LongRunning).Unwrap();

        this.GithubHelper = new GitHubHelper(GITHUB_OWNER, GITHUB_REPOSITORY, GITHUB_CLIENT_ID, this.Name, this.PasswordManager, this.IconState, this.TranslationState);

        this.ModuleSettings.UpdateLocalization(this.TranslationState);

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

    private async Task InitializeStates()
    {
        string directoryName = this.GetDirectoryName();

        string directoryPath = null;
        if (!string.IsNullOrWhiteSpace(directoryName))
        {
            directoryPath = this.DirectoriesManager.GetFullDirectoryPath(directoryName);
        }

        using (await this._stateLock.LockAsync())
        {
            StateConfigurations configurations = new StateConfigurations();
            this.ConfigureStates(configurations);

            if (configurations.BlishHUDAPI.Enabled)
            {
                if (this.PasswordManager == null)
                {
                    throw new ArgumentNullException(nameof(this.PasswordManager));
                }

                this.BlishHUDAPIState = new BlishHudApiState(configurations.BlishHUDAPI, this.ModuleSettings.BlishAPIUsername, this.PasswordManager, this.GetFlurlClient(), API_ROOT_URL, this.API_VERSION_NO);
                this._states.Add(this.BlishHUDAPIState);
            }

            if (configurations.Account.Enabled)
            {
                this.AccountState = new AccountState(configurations.Account, this.Gw2ApiManager);
                this._states.Add(this.AccountState);
            }

            this.IconState = new IconState(new StateConfiguration()
            {
                Enabled = true,
                AwaitLoading = false
            }, this.ContentsManager);
            this._states.Add(this.IconState);

            this.TranslationState = new TranslationState(new StateConfiguration()
            {
                Enabled = true,
                AwaitLoading = true,
            }, this.GetFlurlClient(), this.WEBSITE_MODULE_FILE_URL);
            this._states.Add(this.TranslationState);

            this.SettingEventState = new SettingEventState(new StateConfiguration()
            {
                Enabled = true,
                AwaitLoading = false,
                SaveInterval = Timeout.InfiniteTimeSpan
            });
            this._states.Add(this.SettingEventState);

            this.NewsState = new NewsState(new StateConfiguration()
            {
                AwaitLoading = true,
                Enabled = true
            }, this.GetFlurlClient(), this.WEBSITE_MODULE_FILE_URL);
            this._states.Add(this.NewsState);

            if (configurations.Items.Enabled)
            {
                this.ItemState = new ItemState(configurations.Items, this.Gw2ApiManager, directoryPath);
                this._states.Add(this.ItemState);
            }

            if (configurations.TradingPost.Enabled)
            {
                this.TradingPostState = new TradingPostState(configurations.TradingPost, this.Gw2ApiManager, this.ItemState);
                this._states.Add(this.TradingPostState);
            }

            if (configurations.Worldbosses.Enabled)
            {
                if (configurations.Account.Enabled)
                {
                    this.WorldbossState = new WorldbossState(configurations.Worldbosses, this.Gw2ApiManager, this.AccountState);
                    this._states.Add(this.WorldbossState);
                }
                else
                {
                    this.Logger.Debug($"{typeof(WorldbossState).Name} is not available because {typeof(AccountState).Name} is deactivated.");
                    configurations.Worldbosses.Enabled = false;
                }
            }

            if (configurations.Mapchests.Enabled)
            {
                if (configurations.Account.Enabled)
                {
                    this.MapchestState = new MapchestState(configurations.Mapchests, this.Gw2ApiManager, this.AccountState);
                    this._states.Add(this.MapchestState);
                }
                else
                {
                    this.Logger.Debug($"{typeof(MapchestState).Name} is not available because {typeof(AccountState).Name} is deactivated.");
                    configurations.Mapchests.Enabled = false;
                }
            }

            if (configurations.PointOfInterests.Enabled)
            {
                if (string.IsNullOrWhiteSpace(directoryPath))
                {
                    throw new ArgumentNullException(nameof(directoryPath), "Module directory is not specified.");
                }

                this.PointOfInterestState = new PointOfInterestState(configurations.PointOfInterests, this.Gw2ApiManager, directoryPath);
                this._states.Add(this.PointOfInterestState);
            }

            if (configurations.Skills.Enabled)
            {
                if (string.IsNullOrWhiteSpace(directoryPath))
                {
                    throw new ArgumentNullException(nameof(directoryPath), "Module directory is not specified.");
                }

                this.SkillState = new SkillState(configurations.Skills, this.Gw2ApiManager, this.IconState, directoryPath, this.GetFlurlClient(), FILE_BLISH_ROOT_URL);
                this._states.Add(this.SkillState);
            }

            if (configurations.ArcDPS.Enabled)
            {
                if (configurations.Skills.Enabled)
                {
                    this.ArcDPSState = new ArcDPSState(configurations.ArcDPS, this.SkillState);
                    this._states.Add(this.ArcDPSState);
                }
                else
                {
                    this.Logger.Debug($"{typeof(ArcDPSState).Name} is not available because {typeof(SkillState).Name} is deactivated.");
                    configurations.ArcDPS.Enabled = false;
                }
            }

            Collection<ManagedState> customStates = this.GetAdditionalStates(directoryPath);

            if (customStates != null && customStates.Count > 0)
            {
                foreach (ManagedState customState in customStates)
                {
                    this._states.Add(customState);
                }
            }

            this.OnBeforeStatesStarted();

            // Only start states not already running
            foreach (ManagedState state in this._states.Where(state => !state.Running))
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

    protected virtual void ConfigureStates(StateConfigurations configurations) { }

    protected virtual void OnBeforeStatesStarted() { }

    protected virtual Collection<ManagedState> GetAdditionalStates(string directoryPath)
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
                    Priority = this.Name.GetHashCode()
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
            this._defaultSettingView = new ModuleSettingsView(this.IconState, this.TranslationState);
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

        this.SettingsWindow ??= WindowUtil.CreateTabbedWindow(this.Name, this.GetType(), Guid.Parse("6bd04be4-dc19-4914-a2c3-8160ce76818b"), this.IconState, this.GetEmblem());

        this.SettingsWindow.Tabs.Add(new Tab(this.IconState.GetIcon("482926.png"), () => new UI.Views.NewsView(this.GetFlurlClient(), this.Gw2ApiManager, this.IconState, this.TranslationState, this.NewsState, GameService.Content.DefaultFont16) { DefaultColor = this.ModuleSettings.DefaultGW2Color }, "News"));

        this.OnSettingWindowBuild(this.SettingsWindow);

        this.SettingsWindow.Tabs.Add(new Tab(this.IconState.GetIcon("156331.png"), () => new UI.Views.DonationView(this.GetFlurlClient(), this.Gw2ApiManager, this.IconState, this.TranslationState, GameService.Content.DefaultFont16) { DefaultColor = this.ModuleSettings.DefaultGW2Color }, "Donations"));
        
        if (this.Debug)
        {
            this.SettingsWindow.Tabs.Add(
                new Tab(
                    this.IconState.GetIcon("155052.png"),
                    () => new UI.Views.Settings.StateSettingsView(this._states, this.Gw2ApiManager, this.IconState, this.TranslationState, this.SettingEventState, this.Font)
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

        using (this._stateLock.Lock())
        {
            bool anyStateLoading = false;
            string loadingText = null;
            foreach (ManagedState state in this._states)
            {
                state.Update(gameTime);

                if (state is APIState apiState)
                {
                    var loading = apiState.Loading;

                    if (loading)
                    {
                        anyStateLoading = true;
                        if (!string.IsNullOrWhiteSpace(apiState.ProgressText))
                        {
                            loadingText ??= $"{state.GetType().Name}: {apiState.ProgressText?.ToString()}";
                        }
                        else
                        {
                            loadingText ??= state.GetType().Name;
                        }
                    }
                }
            }

            this.HandleLoadingSpinner(anyStateLoading, loadingText);
        }
    }

    /// <summary>
    /// Calculates the ui visibility based on settings or mumble parameters.
    /// </summary>
    /// <returns>The newly calculated ui visibility or the last value of <see cref="ShowUI"/>.</returns>
    protected virtual bool CalculateUIVisibility()
    {
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

    /// <inheritdoc />
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

        this.Logger.Debug("Unload corner icon...");

        this.HandleCornerIcon(false);
        this._loadingSpinner?.Dispose();
        this._loadingSpinner = null;

        this.Logger.Debug("Unloaded corner icon.");

        this.Logger.Debug("Unloading states...");

        using (this._stateLock.Lock())
        {
            this._states.ToList().ForEach(state => state?.Dispose());
            this._states.Clear();

            this.AccountState = null;
            this.ArcDPSState = null;
            this.IconState = null;
            this.ItemState = null;
            this.MapchestState = null;
            this.WorldbossState = null;
            this.PointOfInterestState = null;
            this.SettingEventState = null;
            this.SkillState = null;
            this.TradingPostState = null;
            this.TranslationState = null;
        }

        this.Logger.Debug("Unloaded states.");

        this.Logger.Debug("Unload flurl client...");

        this._flurlClient?.Dispose();
        this._flurlClient = null;

        this.Logger.Debug("Unloaded flurl client.");

        this.Logger.Debug("Unload module instance...");

        Instance = null;

        this.Logger.Debug("Unloaded module instance.");
    }
}
