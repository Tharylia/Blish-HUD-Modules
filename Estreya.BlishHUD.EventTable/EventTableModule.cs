namespace Estreya.BlishHUD.EventTable
{
    using Blish_HUD;
    using Blish_HUD.Content;
    using Blish_HUD.Controls;
    using Blish_HUD.Graphics.UI;
    using Blish_HUD.Modules;
    using Blish_HUD.Modules.Managers;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.EventTable.Controls;
    using Estreya.BlishHUD.EventTable.Models;
    using Estreya.BlishHUD.EventTable.Models.Settings;
    using Estreya.BlishHUD.EventTable.Resources;
    using Estreya.BlishHUD.EventTable.State;
    using Estreya.BlishHUD.Shared.Modules;
    using Estreya.BlishHUD.Shared.Settings;
    using Estreya.BlishHUD.Shared.State;
    using Estreya.BlishHUD.Shared.Utils;
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
    public class EventTableModule : BaseModule<EventTableModule, ModuleSettings>
    {
        public override string WebsiteModuleName => "event-table";

        internal static EventTableModule ModuleInstance => Instance;

        internal WebClient WebClient => base.Webclient;

        private Dictionary<string, EventArea> _areas = new Dictionary<string, EventArea>();

        private AsyncLock _eventCategoryLock = new AsyncLock();
        public List<EventCategory> EventCategories { get; private set; }

        #region States
        public EventState EventState { get; private set; }
        public EventFileState EventFileState { get; private set; }
        #endregion

        internal MapNavigationUtil MapNavigationUtil { get; private set; }

        [ImportingConstructor]
        public EventTableModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters)
        {
        }

        protected override void Initialize()
        {
        }

        protected override async Task LoadAsync()
        {
            await base.LoadAsync();

            Logger.Debug("Load events.");
            await this.LoadEvents();


            this.AddAllAreas();

            await this.SetAreaEvents();

            this.MapNavigationUtil = new MapNavigationUtil(this.ModuleSettings.MapKeybinding.Value);
        }

        private async Task SetAreaEvents()
        {
            foreach (var area in this._areas.Values)
            {
                await area.UpdateAllEvents(this.EventCategories);
            }
        }

        /// <summary>
        /// Reloads all events.
        /// </summary>
        /// <returns></returns>
        public async Task LoadEvents()
        {
            string threadName = $"{Thread.CurrentThread.ManagedThreadId}";
            Logger.Debug("Try loading events from thread: {0}", threadName);

            using (await this._eventCategoryLock.LockAsync())
            {

                Logger.Debug("Thread \"{0}\" started loading", threadName);

                try
                {
                    if (this.EventCategories != null)
                    {
                        foreach (EventCategory ec in this.EventCategories)
                        {
                            ec.Unload();
                        }

                        this.EventCategories.Clear();
                    }

                    EventSettingsFile eventSettingsFile = await this.EventFileState.GetLocalFile();

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

                    //IEnumerable<Task> eventCategoryLoadTasks = categories.Select(ec =>
                    //{
                    //    return ec.LoadAsync(this.EventState, () => this.DateTimeNow);
                    //});

                    //await Task.WhenAll(eventCategoryLoadTasks);

                    this.EventCategories = categories;

                    await this.SetAreaEvents();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed loading events.");
                }
            }
        }

        protected override void OnModuleLoaded(EventArgs e)
        {
            // Base handler must be called
            base.OnModuleLoaded(e);

            if (this.ModuleSettings.GlobalDrawerVisible.Value)
            {
                this.ToggleContainers(true);
            }
        }


        private void ToggleContainers(bool show)
        {

            if (!this.ModuleSettings.GlobalDrawerVisible.Value)
            {
                show = false;
            }

            this._areas.Values.ToList().ForEach(area =>
            {
                // Don't show if disabled.
                var showArea = show && area.Enabled;

                if (showArea)
                {
                    if (!area.Visible)
                    {
                        area.Show();
                    }
                }
                else
                {
                    if (area.Visible)
                    {
                        area.Hide();
                    }
                }
            });
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            this.ToggleContainers(this.ShowUI);

            foreach (var area in this._areas.Values)
            {
                this.ModuleSettings.CheckDrawerSizeAndPosition(area.Configuration);
            }

            using (this._eventCategoryLock.Lock())
            {
                this.EventCategories.ForEach(ec =>
                {
                    //ec.Update(gameTime);
                });
            }
        }

        private void AddAllAreas()
        {
            if (this.ModuleSettings.EventAreaNames.Value.Count == 0)
            {
                this.ModuleSettings.EventAreaNames.Value.Add("main");
            }

            foreach (string areaName in this.ModuleSettings.EventAreaNames.Value)
            {
                this.AddArea(this.ModuleSettings.AddDrawer(areaName, this.EventCategories));
            }
        }

        private void AddArea(EventAreaConfiguration configuration)
        {
            if (!this.ModuleSettings.EventAreaNames.Value.Contains(configuration.Name))
            {
                this.ModuleSettings.EventAreaNames.Value = new List<string>(this.ModuleSettings.EventAreaNames.Value) { configuration.Name };
            }

            var area = new EventArea(configuration, this.IconState,this.EventState, this.WorldbossState, this.MapchestState, () => this.DateTimeNow)
            {
                Parent = GameService.Graphics.SpriteScreen
            };

            this._areas.Add(configuration.Name, area);
        }

        private void RemoveArea(EventAreaConfiguration configuration)
        {
            this.ModuleSettings.EventAreaNames.Value = new List<string>(this.ModuleSettings.EventAreaNames.Value.Where(areaName => areaName != configuration.Name));

            this._areas[configuration.Name]?.Dispose();
            _ = this._areas.Remove(configuration.Name);

            this.ModuleSettings.RemoveDrawer(configuration.Name);
        }

        /// <inheritdoc />
        protected override void Unload()
        {
            Logger.Debug("Unload module.");

            this.Logger.Debug("Unload drawer.");

            foreach (var area in this._areas.Values)
            {
                area.Dispose();
            }

            _areas.Clear();

            this.Logger.Debug("Unloaded drawer.");

            Logger.Debug("Unload event categories.");

            foreach (EventCategory ec in this.EventCategories)
            {
                ec.Unload();
            }

            Logger.Debug("Unloaded event categories.");

            this.EventFileState.NewVersionAvailable -= this.EventFileState_NewVersionAvailable;
            this.EventFileState.Updated -= this.EventFileState_Updated;

            this.Logger.Debug("Unload base.");

            base.Unload();

            this.Logger.Debug("Unloaded base.");
        }

        protected override BaseModuleSettings DefineModuleSettings(SettingCollection settings)
        {
            var moduleSettings = new ModuleSettings(settings);

            return moduleSettings;
        }

        protected override void OnSettingWindowBuild(TabbedWindow2 settingWindow)
        {
            
            //this.SettingsWindow.Tabs.Add(new Tab(this.IconState.GetIcon("156736.png"), () => new UI.Views.Settings.GeneralSettingsView(this.Gw2ApiManager, this.IconState, GameService.Content.DefaultFont16) { DefaultColor = this.ModuleSettings.DefaultGW2Color }, "General Settings"));
            //this.SettingsWindow.Tabs.Add(new Tab(this.IconState.GetIcon("156740.png"), () => new UI.Views.Settings.GraphicsSettingsView() { APIManager = this.Gw2ApiManager, IconState = this.IconState, DefaultColor = this.ModuleSettings.DefaultGW2Color }, "Graphic Settings"));
            var areaSettingsView = new UI.Views.AreaSettingsView(() => this._areas.Values.Select(area => area.Configuration), this.Gw2ApiManager, this.IconState, GameService.Content.DefaultFont16) { DefaultColor = this.ModuleSettings.DefaultGW2Color };
            areaSettingsView.AddArea += (s, e) =>
            {
                this.AddArea(e.AreaConfiguration);
            };

            areaSettingsView.RemoveArea += (s, e) =>
            {
                this.RemoveArea(e);
            };

            this.SettingsWindow.Tabs.Add(new Tab(this.IconState.GetIcon(@"156742.png"), () => areaSettingsView, "Event Areas"));
        }

        protected override string GetDirectoryName() => "events";

        protected override void ConfigureStates(StateConfigurations configurations)
        {
            configurations.Account.Enabled = true;
            configurations.Worldbosses.Enabled = true;
            configurations.Mapchests.Enabled = true;
            configurations.PointOfInterests.Enabled = true;
        }

        protected override void OnBeforeStatesStarted()
        {
        }

        protected override Collection<ManagedState> GetAdditionalStates(string directoryPath)
        {
            var additionalStates = new Collection<ManagedState>();

            this.EventFileState = new EventFileState(new StateConfiguration()
            {
                AwaitLoading = true,
                Enabled = true,
                SaveInterval = Timeout.InfiniteTimeSpan
            }, directoryPath, "events.json");

            this.EventFileState.NewVersionAvailable += this.EventFileState_NewVersionAvailable;
            this.EventFileState.Updated += this.EventFileState_Updated;

            this.EventState = new EventState(new StateConfiguration()
            {
                AwaitLoading = false,
                Enabled = true,
                SaveInterval = TimeSpan.FromSeconds(30)
            }, directoryPath, () => this.DateTimeNow);

            additionalStates.Add(this.EventFileState);
            additionalStates.Add(this.EventState);

            return additionalStates;
        }

        private void EventFileState_Updated(object sender, EventArgs e)
        {
            Task.Run(this.LoadEvents);
        }

        private void EventFileState_NewVersionAvailable(object sender, NewEventFileVersionArgs e)
        {
            if (e.IsSelfUpdate)
            {
                Shared.Controls.ScreenNotification.ShowNotification(new string[]
                {
                    $"The event file got auto updated: {e.OldVersion} -> {e.NewVersion}"
                }, duration: 5);
            }
            else
            {
                if (!e.AlreadyNotified)
                {
                    Shared.Controls.ScreenNotification.ShowNotification(new string[]
                    {
                    $"Version {e.NewVersion} of the event file is available.",
                    $"Please update it from the settings window. You are running {e.OldVersion}"
                    }, duration: 10);
                }
            }
        }

        protected override AsyncTexture2D GetEmblem()
        {
            return this.IconState.GetIcon("102392.png");
        }

        protected override AsyncTexture2D GetCornerIcon()
        {
            return this.IconState.GetIcon("textures/event_boss_grey.png");
        }
    }
}

