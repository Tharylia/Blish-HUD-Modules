namespace Estreya.BlishHUD.EventTable
{
    using Blish_HUD;
    using Blish_HUD.Controls;
    using Blish_HUD.Graphics.UI;
    using Blish_HUD.Modules;
    using Blish_HUD.Modules.Managers;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.EventTable.Controls;
    using Estreya.BlishHUD.EventTable.Helpers;
    using Estreya.BlishHUD.EventTable.Models;
    using Estreya.BlishHUD.EventTable.Models.Settings;
    using Estreya.BlishHUD.EventTable.Resources;
    using Estreya.BlishHUD.EventTable.State;
    using Estreya.BlishHUD.EventTable.Utils;
    using Gw2Sharp.Models;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using MonoGame.Extended.BitmapFonts;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel.Composition;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    [Export(typeof(Blish_HUD.Modules.Module))]
    public class EventTableModule : Blish_HUD.Modules.Module
    {
        private static readonly Logger Logger = Logger.GetLogger<EventTableModule>();

        public const string WEBSITE_ROOT_URL = "https://blishhud.estreya.de";
        public const string WEBSITE_FILE_ROOT_URL = "https://files.blishhud.estreya.de";
        public const string WEBSITE_MODULE_URL = $"{WEBSITE_ROOT_URL}/modules/event-table";

        internal static EventTableModule ModuleInstance;

        public bool IsPrerelease => !string.IsNullOrWhiteSpace(this.Version?.PreRelease);

        private WebClient _webclient;

        private EventTableDrawer Drawer { get; set; }

        #region Service Managers
        internal SettingsManager SettingsManager => this.ModuleParameters.SettingsManager;
        internal ContentsManager ContentsManager => this.ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => this.ModuleParameters.DirectoriesManager;
        internal Gw2ApiManager Gw2ApiManager => this.ModuleParameters.Gw2ApiManager;
        #endregion

        internal ModuleSettings ModuleSettings { get; private set; }

        private CornerIcon CornerIcon { get; set; }

        internal TabbedWindow2 SettingsWindow { get; private set; }

        internal bool Debug => this.ModuleSettings.DebugEnabled.Value;

        private BitmapFont _font;

        internal BitmapFont Font
        {
            get
            {
                if (this._font == null)
                {
                    this._font = GameService.Content.GetFont(ContentService.FontFace.Menomonia, this.ModuleSettings.EventFontSize.Value, ContentService.FontStyle.Regular);
                }

                return this._font;
            }
        }

        internal int EventHeight => this.ModuleSettings?.EventHeight?.Value ?? 30;

        internal DateTime DateTimeNow => DateTime.Now;

        private TimeSpan _eventTimeSpan = TimeSpan.Zero;

        internal TimeSpan EventTimeSpan
        {
            get
            {
                if (this._eventTimeSpan == TimeSpan.Zero)
                {
                    int timespan = this.ModuleSettings.EventTimeSpan.Value;
                    if (timespan > 1440)
                    {
                        timespan = 1440;
                        Logger.Warn($"Event Timespan over 1440. Cap at 1440 for performance reasons.");
                    }

                    this._eventTimeSpan = TimeSpan.FromMinutes(timespan);
                }

                return this._eventTimeSpan;
            }
        }

        internal float EventTimeSpanRatio
        {
            get
            {
                float ratio = 0.5f + ((this.ModuleSettings.EventHistorySplit.Value / 100f) - 0.5f);
                return ratio;
            }
        }

        internal DateTime EventTimeMin
        {
            get
            {
                double millis = this.EventTimeSpan.TotalMilliseconds * (this.EventTimeSpanRatio);
                TimeSpan timespan = TimeSpan.FromMilliseconds(millis);
                DateTime min = EventTableModule.ModuleInstance.DateTimeNow.Subtract(timespan);
                return min;
            }
        }

        internal DateTime EventTimeMax
        {
            get
            {
                double millis = this.EventTimeSpan.TotalMilliseconds * (1f - this.EventTimeSpanRatio);
                TimeSpan timespan = TimeSpan.FromMilliseconds(millis);
                DateTime max = EventTableModule.ModuleInstance.DateTimeNow.Add(timespan);
                return max;
            }
        }

        private SemaphoreSlim _eventCategorySemaphore = new SemaphoreSlim(1, 1);

        private List<EventCategory> _eventCategories = new List<EventCategory>();

        public List<EventCategory> EventCategories => this._eventCategories.Where(ec => !ec.IsDisabled).ToList();

        #region States
        private readonly AsyncLock _stateLock = new AsyncLock();
        internal Collection<ManagedState> States { get; set; } = new Collection<ManagedState>();

        public EventState EventState { get; private set; }
        public AccountState AccountState { get;private set; }
        public WorldbossState WorldbossState { get; private set; }
        public MapchestState MapchestState { get; private set; }
        public EventFileState EventFileState { get; private set; }
        public IconState IconState { get; private set; }
        public PointOfInterestState PointOfInterestState { get; private set; }
        #endregion

        internal MapNavigationUtil MapNavigationUtil { get; private set; }

        [ImportingConstructor]
        public EventTableModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters)
        {
            ModuleInstance = this;
        }

        protected override void DefineSettings(SettingCollection settings)
        {
            this.ModuleSettings = new ModuleSettings(settings);
        }

        protected override void Initialize()
        {
            this.Drawer = new EventTableDrawer()
            {
                Parent = GameService.Graphics.SpriteScreen,
                BackgroundColor = Color.Transparent,
                Opacity = 0f,
                Visible = false
            };

            GameService.Overlay.UserLocaleChanged += (s, e) =>
            {
                AsyncHelper.RunSync(this.LoadEvents);
            };
        }

        protected override async Task LoadAsync()
        {
            Logger.Debug("Load module settings.");
            await this.ModuleSettings.LoadAsync();

            Logger.Debug("Initialize states (before event file loading)");
            await this.InitializeStates(true);

            Logger.Debug("Load events.");
            await this.LoadEvents();

            Logger.Debug("Initialize states (after event file loading)");
            await this.InitializeStates(false);

            await this.Drawer.LoadAsync();

            this.ModuleSettings.ModuleSettingsChanged += (sender, eventArgs) =>
            {
                switch (eventArgs.Name)
                {
                    case nameof(this.ModuleSettings.Width):
                        this.Drawer.UpdateSize(this.ModuleSettings.Width.Value, -1);
                        break;
                    case nameof(this.ModuleSettings.GlobalEnabled):
                        this.ToggleContainer(this.ModuleSettings.GlobalEnabled.Value);
                        break;
                    case nameof(this.ModuleSettings.EventTimeSpan):
                        this._eventTimeSpan = TimeSpan.Zero;
                        break;
                    case nameof(this.ModuleSettings.EventFontSize):
                        this._font = null;
                        break;
                    case nameof(this.ModuleSettings.RegisterCornerIcon):
                        this.HandleCornerIcon(this.ModuleSettings.RegisterCornerIcon.Value);
                        break;
                    case nameof(this.ModuleSettings.BackgroundColor):
                    case nameof(this.ModuleSettings.BackgroundColorOpacity):
                        this.Drawer.UpdateBackgroundColor();
                        break;
                    default:
                        break;
                }
            };


            this.MapNavigationUtil = new MapNavigationUtil(this.ModuleSettings.MapKeybinding.Value);
        }

        /// <summary>
        /// Reloads all events.
        /// </summary>
        /// <returns></returns>
        public async Task LoadEvents()
        {
            string threadName = $"{Thread.CurrentThread.ManagedThreadId}";
            Logger.Debug("Try loading events from thread: {0}", threadName);

            await this._eventCategorySemaphore.WaitAsync();

            Logger.Debug("Thread \"{0}\" started loading", threadName);

            try
            {
                if (this._eventCategories != null)
                {
                    lock (this._eventCategories)
                    {
                        foreach (EventCategory ec in this._eventCategories)
                        {
                            ec.Unload();
                        }

                        this._eventCategories.Clear();
                    }
                }

                EventSettingsFile eventSettingsFile = await this.EventFileState.GetExternalFile();

                if (eventSettingsFile == null)
                {
                    Logger.Error($"Failed to load event file.");
                    return;
                }

                Logger.Info($"Loaded event file version: {eventSettingsFile.Version}");

                List<EventCategory> categories = eventSettingsFile.EventCategories ?? new List<EventCategory>();

                int eventCategoryCount = categories.Count;
                int eventCount = categories.Sum(ec => ec.Events.Count);

                Logger.Info($"Loaded {eventCategoryCount} Categories with {eventCount} Events.");

                IEnumerable<Task> eventCategoryLoadTasks = categories.Select(ec =>
                {
                    return ec.LoadAsync();
                });

                await Task.WhenAll(eventCategoryLoadTasks);

                categories.ForEach(ec => ec.Events.ForEach(ev =>
                {
                    if (ev.Filler) return;

                    ev.Edited += this.EventEdited;
                }));

                lock (this._eventCategories)
                {
                    Logger.Debug("Overwrite current categories with newly loaded.");
                    this._eventCategories = categories;

                    // Add newly added events to settings
                    this.ModuleSettings.InitializeEventSettings(this._eventCategories);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed loading events.");
                throw ex;
            }
            finally
            {
                this._eventCategorySemaphore.Release();
                Logger.Debug("Thread \"{0}\" released loading lock", threadName);
            }
        }

        private void EventEdited(object sender, EventArgs e)
        {
            Event ev = sender as Event;
            Logger.Debug($"Event \"{ev.Key}\" edited.");
            lock (this._eventCategories)
            {
                EventSettingsFile eventSettingsFile = AsyncHelper.RunSync(this.EventFileState.GetExternalFile);
                eventSettingsFile.EventCategories = this._eventCategories;
                Logger.Debug("Export updated file.");
                AsyncHelper.RunSync(() => this.EventFileState.ExportFile(eventSettingsFile));
            }
        }

        private async Task InitializeStates(bool beforeFileLoaded = false)
        {
            string eventsDirectory = this.DirectoriesManager.GetFullDirectoryPath("events");

            using (await this._stateLock.LockAsync())
            {
                if (!beforeFileLoaded)
                {
                    this.AccountState = new AccountState(this.Gw2ApiManager);

                    this.PointOfInterestState = new PointOfInterestState(this.Gw2ApiManager, eventsDirectory);

                    this.WorldbossState = new WorldbossState(this.Gw2ApiManager, this.AccountState);
                    this.WorldbossState.WorldbossCompleted += this.State_EventCompleted;
                    this.WorldbossState.WorldbossRemoved += this.State_EventRemoved;

                    this.MapchestState = new MapchestState(this.Gw2ApiManager, this.AccountState);
                    this.MapchestState.MapchestCompleted += this.State_EventCompleted;
                    this.MapchestState.MapchestRemoved += this.State_EventRemoved;
                }
                else
                {
                    this.EventFileState = new EventFileState(this.ContentsManager, eventsDirectory, "events.json");
                    this.EventState = new EventState(eventsDirectory);
                    this.IconState = new IconState(this.ContentsManager, eventsDirectory);
                }

                if (!beforeFileLoaded)
                {
                    this.States.Add(this.AccountState);
                    this.States.Add(this.PointOfInterestState);
                    this.States.Add(this.WorldbossState);
                    this.States.Add(this.MapchestState);
                }
                else
                {
                    this.States.Add(this.EventFileState);
                    this.States.Add(this.EventState);
                    this.States.Add(this.IconState);
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
                                    Logger.Error(task.Exception, "Not awaited state start failed for \"{0}\"", state.GetType().Name);
                                }
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Failed starting state \"{0}\"", state.GetType().Name);
                    }
                }
            }
        }

        private void State_EventRemoved(object sender, string apiCode)
        {
            lock (this._eventCategories)
            {
                List<Event> events = this._eventCategories.SelectMany(ec => ec.Events).Where(ev => ev.APICode == apiCode).ToList();
                events.ForEach(ev =>
                {
                    this.EventState.Remove(ev.SettingKey);
                });
            }
        }

        private void State_EventCompleted(object sender, string apiCode)
        {
            lock (this._eventCategories)
            {
                List<Event> events = this._eventCategories.SelectMany(ec => ec.Events).Where(ev => ev.APICode == apiCode).ToList();
                events.ForEach(ev =>
                {
                    switch (this.ModuleSettings.EventCompletedAcion.Value)
                    {
                        case EventCompletedAction.Crossout:
                            ev.Finish();
                            break;
                        case EventCompletedAction.Hide:
                            ev.Hide();
                            break;
                        default:
                            Logger.Warn("Unsupported event completion action: {0}", this.ModuleSettings.EventCompletedAcion.Value);
                            break;
                    }
                });
            }
        }

        private void HandleCornerIcon(bool show)
        {
            if (show)
            {
                this.CornerIcon = new CornerIcon()
                {
                    IconName = "Event Table",
                    Icon = this.ContentsManager.GetTexture(@"images\event_boss_grey.png"),
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

        private void ToggleContainer(bool show)
        {
            if (this.Drawer == null)
            {
                return;
            }

            if (!this.ModuleSettings.GlobalEnabled.Value)
            {
                if (this.Drawer.Visible)
                {
                    this.Drawer.Hide();
                }

                return;
            }

            if (show)
            {
                if (!this.Drawer.Visible)
                {
                    this.Drawer.Show();
                }
            }
            else
            {
                if (this.Drawer.Visible)
                {
                    this.Drawer.Hide();
                }
            }
        }

        public override IView GetSettingsView()
        {
            return new UI.Views.ModuleSettingsView();
        }

        protected override void OnModuleLoaded(EventArgs e)
        {
            // Base handler must be called
            base.OnModuleLoaded(e);

            this.Drawer.UpdatePosition(this.ModuleSettings.LocationX.Value, this.ModuleSettings.LocationY.Value);
            this.Drawer.UpdateSize(this.ModuleSettings.Width.Value, -1);

            //this.ManageEventTab = GameService.Overlay.BlishHudWindow.AddTab("Event Table", this.ContentsManager.GetIcon(@"images\event_boss.png"), () => new UI.Views.ManageEventsView(this._eventCategories, this.ModuleSettings.AllEvents));

            Logger.Debug("Start building settings window.");

            Texture2D windowBackground = this.IconState.GetIcon(@"images\502049.png", false);

            Rectangle settingsWindowSize = new Rectangle(35, 26, 1100, 714);
            int contentRegionPaddingY = settingsWindowSize.Y - 15;
            int contentRegionPaddingX = settingsWindowSize.X + 46;
            Rectangle contentRegion = new Rectangle(contentRegionPaddingX, contentRegionPaddingY, settingsWindowSize.Width - 52, settingsWindowSize.Height - contentRegionPaddingY);

            this.SettingsWindow = new TabbedWindow2(windowBackground, settingsWindowSize, contentRegion)
            {
                Parent = GameService.Graphics.SpriteScreen,
                Title = Strings.SettingsWindow_Title,
                Emblem = this.IconState.GetIcon(@"images\event_boss.png"),
                Subtitle = Strings.SettingsWindow_Subtitle,
                SavesPosition = true,
                Id = $"{nameof(EventTableModule)}_6bd04be4-dc19-4914-a2c3-8160ce76818b"
            };

            this.SettingsWindow.Tabs.Add(new Tab(this.IconState.GetIcon(@"images\event_boss_grey.png"), () => new UI.Views.ManageEventsView(), Strings.SettingsWindow_ManageEvents_Title));
            this.SettingsWindow.Tabs.Add(new Tab(this.IconState.GetIcon(@"images\bars.png"), () => new UI.Views.ReorderEventsView(), Strings.SettingsWindow_ReorderSettings_Title));
            this.SettingsWindow.Tabs.Add(new Tab(this.IconState.GetIcon(@"156736"), () => new UI.Views.Settings.GeneralSettingsView(this.ModuleSettings), Strings.SettingsWindow_GeneralSettings_Title));
            this.SettingsWindow.Tabs.Add(new Tab(this.IconState.GetIcon(@"images\graphics_settings.png"), () => new UI.Views.Settings.GraphicsSettingsView(this.ModuleSettings), Strings.SettingsWindow_GraphicSettings_Title));
            this.SettingsWindow.Tabs.Add(new Tab(this.IconState.GetIcon(@"155052"), () => new UI.Views.Settings.EventSettingsView(this.ModuleSettings), Strings.SettingsWindow_EventSettings_Title));
#if DEBUG
            this.SettingsWindow.Tabs.Add(new Tab(this.IconState.GetIcon(@"155052"), () => new UI.Views.Settings.DebugSettingsView(this.ModuleSettings), "Debug"));
#endif

            Logger.Debug("Finished building settings window.");

            this.HandleCornerIcon(this.ModuleSettings.RegisterCornerIcon.Value);

            if (this.ModuleSettings.GlobalEnabled.Value)
            {
                this.ToggleContainer(true);
            }
        }

        protected override void Update(GameTime gameTime)
        {
            this.CheckMumble();
            this.Drawer.UpdatePosition(this.ModuleSettings.LocationX.Value, this.ModuleSettings.LocationY.Value); // Handle windows resize

            this.CheckContainerSizeAndPosition();

            using (this._stateLock.Lock())
            {
                foreach (ManagedState state in this.States)
                {
                    state.Update(gameTime);
                }
            }

            lock (this._eventCategories)
            {
                this._eventCategories.ForEach(ec =>
                {
                    ec.Update(gameTime);
                });
            }
        }

        private void CheckContainerSizeAndPosition()
        {
            bool buildFromBottom = this.ModuleSettings.BuildDirection.Value == BuildDirection.Bottom;
            int maxResX = (int)(GameService.Graphics.Resolution.X / GameService.Graphics.UIScaleMultiplier);
            int maxResY = (int)(GameService.Graphics.Resolution.Y / GameService.Graphics.UIScaleMultiplier);

            int minLocationX = 0;
            int maxLocationX = maxResX - this.Drawer.Width;
            int minLocationY = buildFromBottom ? this.Drawer.Height : 0;
            int maxLocationY = buildFromBottom ? maxResY : maxResY - this.Drawer.Height;
            int minWidth = 0;
            int maxWidth = maxResX - this.ModuleSettings.LocationX.Value;

            this.ModuleSettings.LocationX.SetRange(minLocationX, maxLocationX);
            this.ModuleSettings.LocationY.SetRange(minLocationY, maxLocationY);
            this.ModuleSettings.Width.SetRange(minWidth, maxWidth);

            /*
            if (this.ModuleSettings.LocationX.Value < minLocationX)
            {
                Logger.Debug($"LocationX unter min, set to: {minLocationX}");
                this.ModuleSettings.LocationX.Value = minLocationX;
            }

            if (this.ModuleSettings.LocationX.Value > maxLocationX)
            {
                Logger.Debug($"LocationX over max, set to: {maxLocationX}");
                this.ModuleSettings.LocationX.Value = maxLocationX;
            }

            if (this.ModuleSettings.LocationY.Value < minLocationY)
            {
                Logger.Debug($"LocationY unter min, set to: {minLocationY}");
                this.ModuleSettings.LocationY.Value = minLocationY;
            }

            if (this.ModuleSettings.LocationY.Value > maxLocationY)
            {
                Logger.Debug($"LocationY over max, set to: {maxLocationY}");
                this.ModuleSettings.LocationY.Value = maxLocationY;
            }

            if (this.ModuleSettings.Width.Value < minWidth)
            {
                Logger.Debug($"Width under min, set to: {minWidth}");
                this.ModuleSettings.Width.Value = minWidth;
            }

            if (this.ModuleSettings.Width.Value > maxWidth)
            {
                Logger.Debug($"Width over max, set to: {maxWidth}");
                this.ModuleSettings.Width.Value = maxWidth;
            }
            */
        }

        private void CheckMumble()
        {
            if (GameService.Gw2Mumble.IsAvailable)
            {
                if (this.Drawer != null)
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

                    //show &= GameService.Gw2Mumble.CurrentMap.Type != MapType.CharacterCreate;

                    this.ToggleContainer(show);
                }
            }
        }

        public WebClient GetWebClient()
        {
            if (this._webclient == null)
            {
                this._webclient = new WebClient();

                this._webclient.Headers.Add("user-agent", $"Event Table {this.Version}");
            }

            return this._webclient;
        }

        /// <inheritdoc />
        protected override void Unload()
        {
            Logger.Debug("Unload module.");

            Logger.Debug("Unload base.");

            base.Unload();

            Logger.Debug("Unload event categories.");

            foreach (EventCategory ec in this._eventCategories)
            {
                ec.Events.ForEach(ev =>
                {
                    if (ev.Filler) return;

                    ev.Edited -= this.EventEdited;
                });

                ec.Unload();
            }

            Logger.Debug("Unloaded event categories.");

            Logger.Debug("Unload event container.");

            if (this.Drawer != null)
            {
                this.Drawer.Dispose();
            }

            Logger.Debug("Unloaded event container.");

            Logger.Debug("Unload settings window.");

            if (this.SettingsWindow != null)
            {
                this.SettingsWindow.Hide();
            }

            Logger.Debug("Unloaded settings window.");

            Logger.Debug("Unload corner icon.");

            this.HandleCornerIcon(false);

            Logger.Debug("Unloaded corner icon.");

            Logger.Debug("Unloading states...");

            using (this._stateLock.Lock())
            {
                this.WorldbossState.WorldbossCompleted -= this.State_EventCompleted;
                this.MapchestState.MapchestCompleted -= this.State_EventCompleted;

                this.WorldbossState.WorldbossRemoved -= this.State_EventRemoved;
                this.MapchestState.MapchestRemoved -= this.State_EventRemoved;

                this.States.ToList().ForEach(state => state.Dispose());
            }

            Logger.Debug("Finished unloading states.");
        }

        internal async Task ReloadStates()
        {
            using (await this._stateLock.LockAsync())
            {
                await Task.WhenAll(this.States.Select(state => state.Reload()));
            }
        }

        internal async Task ClearStates()
        {
            using (await this._stateLock.LockAsync())
            {
                await Task.WhenAll(this.States.Select(state => state.Clear()));
            }
        }
    }
}

