namespace Estreya.BlishHUD.EventTable
{
    using Blish_HUD;
    using Blish_HUD.ArcDps.Models;
    using Blish_HUD.Content;
    using Blish_HUD.Controls;
    using Blish_HUD.Input;
    using Blish_HUD.Modules;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.EventTable.Controls;
    using Estreya.BlishHUD.EventTable.Models;
    using Estreya.BlishHUD.EventTable.State;
    using Estreya.BlishHUD.Shared.Controls;
    using Estreya.BlishHUD.Shared.Helpers;
    using Estreya.BlishHUD.Shared.Modules;
    using Estreya.BlishHUD.Shared.Settings;
    using Estreya.BlishHUD.Shared.State;
    using Estreya.BlishHUD.Shared.Threading;
    using Estreya.BlishHUD.Shared.Utils;
    using Flurl.Http;
    using Microsoft.Xna.Framework;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel.Composition;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    [Export(typeof(Blish_HUD.Modules.Module))]
    public class EventTableModule : BaseModule<EventTableModule, ModuleSettings>
    {
        public override string WebsiteModuleName => "event-table";

        //internal static EventTableModule ModuleInstance => Instance;

        private Dictionary<string, EventArea> _areas = new Dictionary<string, EventArea>();

        private AsyncLock _eventCategoryLock = new AsyncLock();
        public List<EventCategory> EventCategories { get; private set; } = new List<EventCategory>();

        private static TimeSpan _updateEventsInterval = TimeSpan.FromMinutes(30);
        private AsyncRef<double> _lastEventUpdate = new AsyncRef<double>(0);

        #region States
        public EventState EventState { get; private set; }
        public DynamicEventState DynamicEventState { get; private set; }
        #endregion

        internal MapUtil MapUtil { get; private set; }

        protected override string API_VERSION_NO => "1";

        [ImportingConstructor]
        public EventTableModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters)
        {
        }

        protected override async Task LoadAsync()
        {
            await base.LoadAsync();
            this.MapUtil = new MapUtil(this.ModuleSettings.MapKeybinding.Value, this.Gw2ApiManager);

            this.Logger.Debug("Load events.");
            await this.LoadEvents();

            this.AddAllAreas();

            await this.SetAreaEvents();

#if DEBUG
            GameService.Input.Keyboard.KeyPressed += this.Keyboard_KeyPressed;
#endif
        }

        private void Keyboard_KeyPressed(object sender, KeyboardEventArgs e)
        {
            if (e.EventType != Blish_HUD.Input.KeyboardEventType.KeyDown) return;
            if (GameService.Input.Keyboard.TextFieldIsActive()) return;

            if (e.Key == Microsoft.Xna.Framework.Input.Keys.U)
            {
                var notification = new EventNotification(this.EventCategories.First().Events.First(), "Starts in 10 minutes! ajkshdkjahsdkjhaskjdhkajlshdkha", this.IconState);
                notification.Show(TimeSpan.FromSeconds(5), 200, 200);

                //var mapId = GameService.Gw2Mumble.CurrentMap.Id;
                //var ev = this.DynamicEventState.GetEventsByMap(mapId).FirstOrDefault();
                //if (ev != null)
                //{
                //    Task.Run(async () =>
                //    {
                //        var coords = await this.MapUtil.MapCoordinatesToContinentCoordinates(mapId, new double[] { ev.Location.Center[0], ev.Location.Center[1] });
                //        await this.MapUtil.DrawCircle(coords.X, coords.Y, 1);
                //    });
                //}else
                //{
                //    ScreenNotification.ShowNotification("No events on this map", ScreenNotification.NotificationType.Error);
                //}
            }
        }

        private async Task SetAreaEvents()
        {
            foreach (EventArea area in this._areas.Values)
            {
                await this.SetAreaEvents(area);
            }
        }

        private async Task SetAreaEvents(EventArea area)
        {
            await area.UpdateAllEvents(this.EventCategories);
        }

        /// <summary>
        /// Reloads all events.
        /// </summary>
        /// <returns></returns>
        public async Task LoadEvents()
        {
            using (await this._eventCategoryLock.LockAsync())
            {
                try
                {
                    this.EventCategories?.Clear();

                    List<EventCategory> categories = await this.GetFlurlClient().Request(this.API_URL, "events").GetJsonAsync<List<EventCategory>>();

                    int eventCategoryCount = categories.Count;
                    int eventCount = categories.Sum(ec => ec.Events.Count);

                    this.Logger.Info($"Loaded {eventCategoryCount} Categories with {eventCount} Events.");

                    IEnumerable<Task> eventCategoryLoadTasks = categories.Select(ec =>
                    {
                        return ec.LoadAsync(() => this.DateTimeNow.ToUniversalTime(), this.TranslationState);
                    });

                    await Task.WhenAll(eventCategoryLoadTasks);

                    this.EventCategories = categories;

                    await this.SetAreaEvents();
                }
                catch (Exception ex)
                {
                    this.Logger.Warn(ex, "Failed loading events.");
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
                bool showArea = show && area.Enabled;

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

            foreach (EventArea area in this._areas.Values)
            {
                this.ModuleSettings.CheckDrawerSizeAndPosition(area.Configuration);
            }

            _ = UpdateUtil.UpdateAsync(this.LoadEvents, gameTime, _updateEventsInterval.TotalMilliseconds, this._lastEventUpdate);
        }

        private void AddAllAreas()
        {
            if (this.ModuleSettings.EventAreaNames.Value.Count == 0)
            {
                this.ModuleSettings.EventAreaNames.Value.Add("Main");
            }

            foreach (string areaName in this.ModuleSettings.EventAreaNames.Value)
            {
                this.AddArea(areaName);
            }
        }

        private EventAreaConfiguration AddArea(string name)
        {
            var config = this.ModuleSettings.AddDrawer(name, this.EventCategories);
            this.AddArea(config);

            return config;
        }

        private void AddArea(EventAreaConfiguration configuration)
        {
            if (!this.ModuleSettings.EventAreaNames.Value.Contains(configuration.Name))
            {
                this.ModuleSettings.EventAreaNames.Value = new List<string>(this.ModuleSettings.EventAreaNames.Value) { configuration.Name };
            }

            this.ModuleSettings.UpdateDrawerLocalization(configuration, this.TranslationState);

            EventArea area = new EventArea(
                configuration,
                this.IconState,
                this.TranslationState,
                this.EventState,
                this.WorldbossState,
                this.MapchestState,
                this.PointOfInterestState,
                this.MapUtil,
                this.GetFlurlClient(),
                this.API_URL,
                () => /*DateTime.Parse("2023-01-11T16:00:00"))*/ this.DateTimeNow.ToUniversalTime(),
                () => this.Version)
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

        protected override BaseModuleSettings DefineModuleSettings(SettingCollection settings)
        {
            ModuleSettings moduleSettings = new ModuleSettings(settings);

            return moduleSettings;
        }

        protected override void OnSettingWindowBuild(TabbedWindow2 settingWindow)
        {
            // Reorder Icon: 605018

            this.SettingsWindow.Tabs.Add(new Tab(this.IconState.GetIcon("156736.png"), () => new UI.Views.GeneralSettingsView(this.ModuleSettings, this.Gw2ApiManager, this.IconState, this.TranslationState, GameService.Content.DefaultFont16) { DefaultColor = this.ModuleSettings.DefaultGW2Color }, "General"));
            //this.SettingsWindow.Tabs.Add(new Tab(this.IconState.GetIcon("156740.png"), () => new UI.Views.Settings.GraphicsSettingsView() { APIManager = this.Gw2ApiManager, IconState = this.IconState, DefaultColor = this.ModuleSettings.DefaultGW2Color }, "Graphic Settings"));
            UI.Views.AreaSettingsView areaSettingsView = new UI.Views.AreaSettingsView(
                () => this._areas.Values.Select(area => area.Configuration),
                () => this.EventCategories,
                this.Gw2ApiManager,
                this.IconState,
                this.TranslationState,
                this.EventState,
                GameService.Content.DefaultFont16)
            { DefaultColor = this.ModuleSettings.DefaultGW2Color };
            areaSettingsView.AddArea += (s, e) =>
            {
                e.AreaConfiguration = this.AddArea(e.Name);
                if (e.AreaConfiguration != null)
                {
                    var newArea = this._areas.Values.Where(x => x.Configuration.Name == e.Name).First();
                    AsyncHelper.RunSync(async () => await this.SetAreaEvents(newArea));
                }
            };

            areaSettingsView.RemoveArea += (s, e) =>
            {
                this.RemoveArea(e);
            };

            this.SettingsWindow.Tabs.Add(new Tab(this.IconState.GetIcon(@"605018.png"), () => areaSettingsView, "Event Areas"));
        }

        protected override string GetDirectoryName()
        {
            return "events";
        }

        protected override void ConfigureStates(StateConfigurations configurations)
        {
            configurations.Account.Enabled = true;
            configurations.Worldbosses.Enabled = true;
            configurations.Mapchests.Enabled = true;
            configurations.PointOfInterests.Enabled = true;
        }

        protected override Collection<ManagedState> GetAdditionalStates(string directoryPath)
        {
            Collection<ManagedState> additionalStates = new Collection<ManagedState>();

            this.EventState = new EventState(new StateConfiguration()
            {
                AwaitLoading = false,
                Enabled = true,
                SaveInterval = TimeSpan.FromSeconds(30)
            }, directoryPath, () => this.DateTimeNow);

            this.DynamicEventState = new DynamicEventState(new StateConfiguration()
            {
                AwaitLoading = false,
                Enabled = true,
                SaveInterval = Timeout.InfiniteTimeSpan
            }, this.GetFlurlClient());

            additionalStates.Add(this.EventState);
            additionalStates.Add(this.DynamicEventState);

            return additionalStates;
        }

        protected override AsyncTexture2D GetEmblem()
        {
            return this.IconState.GetIcon(this.IsPrerelease ? "textures/emblem_demo.png" : "102392.png");
        }

        protected override AsyncTexture2D GetCornerIcon()
        {
            return this.IconState.GetIcon($"textures/event_boss_grey{(this.IsPrerelease ? "_demo" : "")}.png");
        }

        /// <inheritdoc />
        protected override void Unload()
        {
            this.Logger.Debug("Unload module.");

            this.Logger.Debug("Unload drawer.");

            foreach (EventArea area in this._areas.Values)
            {
                area?.Dispose();
            }

            this._areas?.Clear();

            this.Logger.Debug("Unloaded drawer.");

            this.Logger.Debug("Unload base.");

            base.Unload();

            this.Logger.Debug("Unloaded base.");
        }
    }
}

