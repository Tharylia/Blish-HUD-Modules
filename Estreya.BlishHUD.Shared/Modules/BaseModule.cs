namespace Estreya.BlishHUD.Shared.Modules;

using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Graphics.UI;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Estreya.BlishHUD.Shared.Resources;
using Estreya.BlishHUD.Shared.Settings;
using Estreya.BlishHUD.Shared.State;
using Estreya.BlishHUD.Shared.Utils;
using Gw2Sharp.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.BitmapFonts;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

[Export(typeof(Blish_HUD.Modules.Module))]
public abstract class BaseModule<TModule, TSettings> : Module where TSettings : Settings.BaseModuleSettings where TModule : class
{
    protected Logger Logger { get; }

    public const string WEBSITE_ROOT_URL = "https://blishhud.estreya.de";
    public const string WEBSITE_FILE_ROOT_URL = "https://files.blishhud.estreya.de";
    public string WEBSITE_MODULE_URL => $"{WEBSITE_ROOT_URL}/modules/{this.WebsiteModuleName}";
    public abstract string WebsiteModuleName { get; }

    protected static TModule Instance;

    public bool IsPrerelease => !string.IsNullOrWhiteSpace(this.Version?.PreRelease);

    private WebClient _webclient;

    protected WebClient Webclient
    {
        get
        {
            if (this._webclient == null)
            {
                this._webclient = new WebClient();

                this._webclient.Headers.Add("user-agent", $"{this.Name} {this.Version}");
            }

            return this._webclient;
        }
    }

    #region Service Managers
    protected SettingsManager SettingsManager => this.ModuleParameters.SettingsManager;
    protected ContentsManager ContentsManager => this.ModuleParameters.ContentsManager;
    protected DirectoriesManager DirectoriesManager => this.ModuleParameters.DirectoriesManager;
    protected Gw2ApiManager Gw2ApiManager => this.ModuleParameters.Gw2ApiManager;
    #endregion

    //protected bool Debug => this.ModuleSettings?.DebugEnabled.Value ?? false;
#if DEBUG
    protected bool Debug => true;
#else
    protected bool Debug => false;
#endif

    protected bool ShowUI { get; private set; } = true;

    public TSettings ModuleSettings { get; private set; }

    protected CornerIcon CornerIcon { get; set; }

    protected TabbedWindow2 SettingsWindow { get; private set; }

    private BitmapFont _font;

    public BitmapFont Font
    {
        get
        {
            if (this._font == null)
            {
                this._font = GameService.Content.GetFont(ContentService.FontFace.Menomonia, this.ModuleSettings.FontSize.Value, ContentService.FontStyle.Regular);
            }

            return this._font;
        }
    }

    internal DateTime DateTimeNow => DateTime.Now;

    #region States
    private readonly AsyncLock _stateLock = new AsyncLock();
    private Collection<ManagedState> States { get; } = new Collection<ManagedState>();

    public IconState IconState { get; private set; }
    public WorldbossState WorldbossState { get; private set; }
    public MapchestState MapchestState { get; private set; }
    public PointOfInterestState PointOfInterestState { get; private set; }
    public AccountState AccountState { get; private set; }
    public SkillState SkillState { get; private set; }
    public TradingPostState TradingPostState { get; private set; }
    public ArcDPSState ArcDPSState { get; private set; }

    //public TrackedTransactionState TrackedTransactionState { get; private set; }
    #endregion

    [ImportingConstructor]
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
        GameService.Overlay.UserLocaleChanged += (s, e) =>
        {
        };
    }

    protected override async Task LoadAsync()
    {
        this.Logger.Debug("Initialize states");
        await this.InitializeStates();

        this.ModuleSettings.ModuleSettingsChanged += (sender, eventArgs) =>
        {
            switch (eventArgs.Name)
            {
                case nameof(this.ModuleSettings.FontSize):
                    this._font = null;
                    break;
                case nameof(this.ModuleSettings.RegisterCornerIcon):
                    this.HandleCornerIcon(this.ModuleSettings.RegisterCornerIcon.Value);
                    break;
                default:
                    break;
            }
        };
    }

    protected abstract string GetDirectoryName();

    private async Task InitializeStates()
    {
        string directoryName = this.GetDirectoryName();

        if (string.IsNullOrWhiteSpace(directoryName))
        {
            throw new ArgumentNullException(nameof(directoryName), "Module directory is not specified.");
        }

        string directoryPath = this.DirectoriesManager.GetFullDirectoryPath(directoryName);

        using (await this._stateLock.LockAsync())
        {
            StateConfigurations configurations = new StateConfigurations();
            this.ConfigureStates(configurations);

            if (configurations.Account)
            {
                this.AccountState = new AccountState(this.Gw2ApiManager);
                this.States.Add(this.AccountState);
            }

            this.IconState = new IconState(this.ContentsManager, directoryPath);
            this.States.Add(this.IconState);

            if (configurations.TradingPost)
            {
                this.TradingPostState = new TradingPostState(this.Gw2ApiManager);
                this.States.Add(this.TradingPostState);
            }

            if (configurations.Worldbosses)
            {
                if (configurations.Account)
                {
                    this.WorldbossState = new WorldbossState(this.Gw2ApiManager, this.AccountState);
                    this.States.Add(this.WorldbossState);
                }
                else
                {
                    this.Logger.Debug($"{typeof(WorldbossState).Name} is not available because {typeof(AccountState).Name} is deactivated.");
                    configurations.Worldbosses = false;
                }
            }

            if (configurations.Mapchests)
            {
                if (configurations.Account)
                {
                    this.MapchestState = new MapchestState(this.Gw2ApiManager, this.AccountState);
                    this.States.Add(this.MapchestState);
                }
                else
                {
                    this.Logger.Debug($"{typeof(MapchestState).Name} is not available because {typeof(AccountState).Name} is deactivated.");
                    configurations.Mapchests = false;
                }
            }

            if (configurations.PointOfInterests)
            {
                this.PointOfInterestState = new PointOfInterestState(this.Gw2ApiManager, directoryPath);
                this.States.Add(this.PointOfInterestState);
            }

            if (configurations.Skills)
            {
                this.SkillState = new SkillState(this.Gw2ApiManager, this.IconState, directoryPath);
                this.States.Add(this.SkillState);
            }

            if (configurations.ArcDPS)
            {
                if (configurations.Skills)
                {
                    this.ArcDPSState = new ArcDPSState(this.SkillState);
                    this.States.Add(this.ArcDPSState);
                }
                else
                {
                    this.Logger.Debug($"{typeof(ArcDPSState).Name} is not available because {typeof(SkillState).Name} is deactivated.");
                    configurations.ArcDPS = false;
                }
            }

            this.HandleDefaultStates();

            Collection<ManagedState> customStates = this.GetAdditionalStates(directoryPath);

            if (customStates != null && customStates.Count > 0)
            {
                foreach (ManagedState customState in customStates)
                {
                    this.States.Add(customState);
                }
            }

            // Only start states not already running
            foreach (ManagedState state in this.States.Where(state => !state.Running))
            {
                try
                {
                    // Order is important
                    if (state.AwaitLoad)
                    {
                        await state.Start();
                    }
                    else
                    {
                        _ = state.Start().ContinueWith(task =>
                        {
                            if (task.IsFaulted)
                            {
                                this.Logger.Error(task.Exception, "Not awaited state start failed for \"{0}\"", state.GetType().Name);
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    this.Logger.Error(ex, "Failed starting state \"{0}\"", state.GetType().Name);
                }
            }
        }
    }

    protected abstract void ConfigureStates(StateConfigurations configurations);

    protected abstract void HandleDefaultStates();

    protected virtual Collection<ManagedState> GetAdditionalStates(string directoryPath)
    {
        return new Collection<ManagedState>();
    }

    private void HandleCornerIcon(bool show)
    {
        if (show)
        {
            this.CornerIcon = new CornerIcon()
            {
                IconName = this.Name,
                Icon = this.GetCornerIcon(),
            };

            this.CornerIcon.Click += (s, ea) =>
            {
                this.SettingsWindow.ToggleWindow();
            };
        }
        else
        {
            if (this.CornerIcon != null)
            {
                this.CornerIcon.Dispose();
                this.CornerIcon = null;
            }
        }
    }

    public sealed override IView GetSettingsView()
    {
        Shared.UI.Views.ModuleSettingsView view = new Shared.UI.Views.ModuleSettingsView(Strings.SettingsView_OpenSettings);
        view.OpenClicked += (s, e) => this.SettingsWindow.ToggleWindow();

        return view;
    }

    protected override void OnModuleLoaded(EventArgs e)
    {
        // Base handler must be called
        base.OnModuleLoaded(e);

        this.Logger.Debug("Start building settings window.");

        Texture2D windowBackground = this.IconState.GetIcon(@"textures\setting_window_background.png");

        Rectangle settingsWindowSize = new Rectangle(35, 26, 1100, 714);
        int contentRegionPaddingY = settingsWindowSize.Y - 15;
        int contentRegionPaddingX = settingsWindowSize.X + 46;
        Rectangle contentRegion = new Rectangle(contentRegionPaddingX, contentRegionPaddingY, settingsWindowSize.Width - 52, settingsWindowSize.Height - contentRegionPaddingY);

        this.SettingsWindow = new TabbedWindow2(windowBackground, settingsWindowSize, contentRegion)
        {
            Parent = GameService.Graphics.SpriteScreen,
            Title = this.Name,
            Subtitle = Strings.SettingsWindow_Subtitle,
            SavesPosition = true,
            Id = $"{this.GetType().Name}_6bd04be4-dc19-4914-a2c3-8160ce76818b"
        };

        AsyncTexture2D emblem = this.GetEmblem();

        if (emblem.HasSwapped)
        {
            this.SettingsWindow.Emblem = emblem;
        }
        else
        {
            emblem.TextureSwapped += (s, e) =>
            {
                this.SettingsWindow.Emblem = e.NewValue;
            };
        }

        //this.SettingsWindow.Tabs.Add(new Tab(this.IconState.GetIcon(@"images\tradingpost.png"), () =>
        //{
        //    var trackedTransactionView = new UI.Views.TrackedTransactionView(this.TrackedTransactionState.TrackedTransactions)
        //    {
        //        APIManager = this.Gw2ApiManager,
        //        IconState = this.IconState,
        //        DefaultColor = this.ModuleSettings.DefaultGW2Color
        //    };

        //    trackedTransactionView.AddTracking += (s, e) =>
        //    {
        //        AsyncHelper.RunSync(async () =>
        //        {
        //            var added = await this.TrackedTransactionState.Add(e.ItemId, e.WishPrice, e.Type);
        //        });
        //    };
        //    trackedTransactionView.RemoveTracking += (s, e) =>
        //    {
        //        this.TrackedTransactionState.Remove(e.ItemId, e.Type);
        //    };

        //    return trackedTransactionView;
        //}, "Tracked Transactions"));

        //this.SettingsWindow.Tabs.Add(new Tab(this.IconState.GetIcon(@"156736"), () => new UI.Views.Settings.GeneralSettingsView() { APIManager = this.Gw2ApiManager, IconState = this.IconState, DefaultColor = this.ModuleSettings.DefaultGW2Color }, Strings.SettingsWindow_GeneralSettings_Title));
        //this.SettingsWindow.Tabs.Add(new Tab(this.IconState.GetIcon(@"images\tradingpost.png"), () => new UI.Views.Settings.TransactionSettingsView() { APIManager = this.Gw2ApiManager, IconState = this.IconState, DefaultColor = this.ModuleSettings.DefaultGW2Color }, "Transactions"));
        //this.SettingsWindow.Tabs.Add(new Tab(this.IconState.GetIcon(@"images\graphics_settings.png"), () => new UI.Views.Settings.GraphicsSettingsView() { APIManager = this.Gw2ApiManager, IconState = this.IconState, DefaultColor = this.ModuleSettings.DefaultGW2Color }, Strings.SettingsWindow_GraphicSettings_Title));

        this.OnSettingWindowBuild(this.SettingsWindow);

        if (this.Debug)
        {
            this.SettingsWindow.Tabs.Add(new Tab(this.IconState.GetIcon("155052.png"), () => new UI.Views.Settings.StateSettingsView(this.States) { APIManager = this.Gw2ApiManager, IconState = this.IconState, DefaultColor = this.ModuleSettings.DefaultGW2Color }, "Debug"));
        }

        this.Logger.Debug("Finished building settings window.");

        this.HandleCornerIcon(this.ModuleSettings.RegisterCornerIcon.Value);
    }

    protected abstract AsyncTexture2D GetEmblem();

    protected abstract AsyncTexture2D GetCornerIcon();

    protected virtual void OnSettingWindowBuild(TabbedWindow2 settingWindow)
    {

    }

    protected override void Update(GameTime gameTime)
    {
        this.ShowUI = this.CalculateUIVisibility();

        //this.ModuleSettings.CheckDrawerSizeAndPosition(this.Drawer.Width, this.Drawer.Height);

        using (this._stateLock.Lock())
        {
            foreach (ManagedState state in this.States)
            {
                state.Update(gameTime);
            }
        }
    }

    protected virtual bool CalculateUIVisibility()
    {
        if (GameService.Gw2Mumble.IsAvailable)
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

        return this.ShowUI;
    }


    /// <inheritdoc />
    protected override void Unload()
    {
        this.Logger.Debug("Unload module.");

        this.Logger.Debug("Unload base.");

        base.Unload();

        this.Logger.Debug("Unloaded base.");

        this.Logger.Debug("Unload settings");

        if (this.ModuleSettings != null)
        {
            this.ModuleSettings.Unload();
        }

        this.Logger.Debug("Unloaded settings.");

        this.Logger.Debug("Unload settings window.");

        if (this.SettingsWindow != null)
        {
            this.SettingsWindow.Hide();
            this.SettingsWindow.Dispose();
        }

        this.Logger.Debug("Unloaded settings window.");

        this.Logger.Debug("Unload corner icon.");

        this.HandleCornerIcon(false);

        this.Logger.Debug("Unloaded corner icon.");

        this.Logger.Debug("Unloading states...");

        using (this._stateLock.Lock())
        {
            this.States.ToList().ForEach(state => state.Dispose());
        }

        this.Logger.Debug("Finished unloading states.");
    }

    protected async Task ReloadStates()
    {
        using (await this._stateLock.LockAsync())
        {
            await Task.WhenAll(this.States.Select(state => state.Reload()));
        }
    }

    protected async Task ClearStates()
    {
        using (await this._stateLock.LockAsync())
        {
            await Task.WhenAll(this.States.Select(state => state.Clear()));
        }
    }
}
