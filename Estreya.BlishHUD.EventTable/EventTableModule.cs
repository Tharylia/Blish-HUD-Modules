namespace Estreya.BlishHUD.EventTable
{
    using Blish_HUD;
    using Blish_HUD.Content;
    using Blish_HUD.Controls;
    using Blish_HUD.Entities;
    using Blish_HUD.Input;
    using Blish_HUD.Modules;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.EventTable.Controls;
    using Estreya.BlishHUD.EventTable.Managers;
    using Estreya.BlishHUD.EventTable.Models;
    using Estreya.BlishHUD.EventTable.State;
    using Estreya.BlishHUD.Shared.Controls.World;
    using Estreya.BlishHUD.Shared.Extensions;
    using Estreya.BlishHUD.Shared.Helpers;
    using Estreya.BlishHUD.Shared.Modules;
    using Estreya.BlishHUD.Shared.Settings;
    using Estreya.BlishHUD.Shared.State;
    using Estreya.BlishHUD.Shared.Threading;
    using Estreya.BlishHUD.Shared.Utils;
    using Flurl.Http;
    using Humanizer;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Newtonsoft.Json;
    using Octokit;
    using SharpDX.MediaFoundation;
    using SharpDX.X3DAudio;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    [Export(typeof(Blish_HUD.Modules.Module))]
    public class EventTableModule : BaseModule<EventTableModule, ModuleSettings>
    {
        public override string WebsiteModuleName => "event-table";

        private ConcurrentDictionary<string, EventArea> _areas = new ConcurrentDictionary<string, EventArea>();

        private AsyncLock _eventCategoryLock = new AsyncLock();
        private List<EventCategory> _eventCategories = new List<EventCategory>();

        private static TimeSpan _updateEventsInterval = TimeSpan.FromMinutes(30);
        private AsyncRef<double> _lastEventUpdate = new AsyncRef<double>(0);

        private static TimeSpan _checkDrawerSettingInterval = TimeSpan.FromSeconds(30);
        private double _lastCheckDrawerSettings = 0;

        private DateTime NowUTC => DateTime.UtcNow;

        #region States
        public EventState EventState { get; private set; }
        public DynamicEventState DynamicEventState { get; private set; }
        #endregion

        private MapUtil MapUtil { get; set; }

        private DynamicEventHandler DynamicEventHandler { get; set; }

        protected override string API_VERSION_NO => "1";

        [ImportingConstructor]
        public EventTableModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters)
        {
        }

        protected override async Task LoadAsync()
        {
            await base.LoadAsync();

            this.BlishHUDAPIState.NewLogin += this.BlishHUDAPIState_NewLogin;
            this.BlishHUDAPIState.LoggedOut += this.BlishHUDAPIState_LoggedOut;

            this.MapUtil = new MapUtil(this.ModuleSettings.MapKeybinding.Value, this.Gw2ApiManager);
            this.DynamicEventHandler = new DynamicEventHandler(this.MapUtil, this.DynamicEventState, this.Gw2ApiManager, this.ModuleSettings);

            await this.DynamicEventHandler.AddDynamicEventsToMap();
            await this.DynamicEventHandler.AddDynamicEventsToWorld();

            await this.LoadEvents();

            this.AddAllAreas();

            this.SetAreaEvents();

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
            }
        }

        private void SetAreaEvents()
        {
            foreach (EventArea area in this._areas.Values)
            {
                this.SetAreaEvents(area);
            }
        }

        private void SetAreaEvents(EventArea area)
        {
            area.UpdateAllEvents(this._eventCategories);
        }

        /// <summary>
        /// Reloads all events.
        /// </summary>
        /// <returns></returns>
        public async Task LoadEvents()
        {
            this.Logger.Info("Load events...");
            using (await this._eventCategoryLock.LockAsync())
            {
                this.Logger.Debug("Acquired lock.");
                try
                {
                    this._eventCategories?.SelectMany(ec => ec.Events).ToList().ForEach(ev => this.RemoveEventHooks(ev));
                    this._eventCategories?.Clear();

                    var request = this.GetFlurlClient().Request(this.API_URL, "events");

                    if (!string.IsNullOrWhiteSpace(this.BlishHUDAPIState.AccessToken))
                    {
                        this.Logger.Info("Include custom events...");
                        request.WithOAuthBearerToken(this.BlishHUDAPIState.AccessToken);
                    }

                    List<EventCategory> categories = await request.GetJsonAsync<List<EventCategory>>();

                    int eventCategoryCount = categories.Count;
                    int eventCount = categories.Sum(ec => ec.Events.Count);

                    this.Logger.Info($"Loaded {eventCategoryCount} Categories with {eventCount} Events.");

                    categories.ForEach(ec =>
                    {
                        ec.Load(() => this.NowUTC, this.TranslationState);
                    });

                    this.Logger.Debug($"Loaded all event categories.");

                    this.AssignEventReminderTimes(categories);

                    this._eventCategories = categories;

                    foreach (var ev in this._eventCategories.SelectMany(ec => ec.Events))
                    {
                        this.AddEventHooks(ev);
                    }

                    this._lastCheckDrawerSettings = _checkDrawerSettingInterval.TotalMilliseconds;

                    this.SetAreaEvents();

                    this.Logger.Debug($"Updated events in all areas.");
                }
                catch (FlurlHttpException ex)
                {
                    var message = await ex.GetResponseStringAsync();
                    this.Logger.Warn(ex, $"Failed loading events: {message}");
                }
                catch (Exception ex)
                {
                    this.Logger.Error(ex, "Failed loading events.");
                }
            }
        }

        private void AssignEventReminderTimes(List<EventCategory> categories)
        {
            var events = categories.SelectMany(ec => ec.Events).Where(ev => !ev.Filler);
            foreach (var ev in events)
            {
                if (!this.ModuleSettings.ReminderTimesOverride.Value.ContainsKey(ev.SettingKey)) continue;

                var times = this.ModuleSettings.ReminderTimesOverride.Value[ev.SettingKey];
                ev.UpdateReminderTimes(times.ToArray());
            }
        }

        private void CheckDrawerSettings()
        {
            // Don't lock when it would freeze
            if (this._eventCategoryLock.IsFree())
            {
                using (this._eventCategoryLock.Lock())
                {
                    foreach (var area in this._areas)
                    {
                        this.ModuleSettings.CheckDrawerSettings(area.Value.Configuration, this._eventCategories);
                    }
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

            this.ModuleSettings.CheckGlobalSizeAndPosition();

            foreach (EventArea area in this._areas.Values)
            {
                this.ModuleSettings.CheckDrawerSizeAndPosition(area.Configuration);
            }

            // Dont block update when we need to wait
            if (this._eventCategoryLock.IsFree())
            {
                using (this._eventCategoryLock.Lock())
                {
                    foreach (var ev in this._eventCategories.SelectMany(ec => ec.Events))
                    {
                        ev.Update(gameTime);
                    }
                }
            }

            this.DynamicEventHandler.Update(gameTime);

            UpdateUtil.Update(this.CheckDrawerSettings, gameTime, _checkDrawerSettingInterval.TotalMilliseconds, ref this._lastCheckDrawerSettings);
            _ = UpdateUtil.UpdateAsync(this.LoadEvents, gameTime, _updateEventsInterval.TotalMilliseconds, this._lastEventUpdate);
        }

        private void AddEventHooks(Models.Event ev)
        {
            ev.Reminder += this.Ev_Reminder;
        }

        private void RemoveEventHooks(Models.Event ev)
        {
            ev.Reminder -= this.Ev_Reminder;
        }

        private void Ev_Reminder(object sender, TimeSpan e)
        {
            var ev = sender as Models.Event;

            if (!this.ModuleSettings.RemindersEnabled.Value || this.ModuleSettings.ReminderDisabledForEvents.Value.Contains(ev.SettingKey)) return;

            var startsInTranslation = this.TranslationState.GetTranslation("eventArea-reminder-startsIn", "Starts in");
            var notification = new EventNotification(ev, $"{startsInTranslation} {e.Humanize(2, minUnit: Humanizer.Localisation.TimeUnit.Second)}!", this.ModuleSettings.ReminderPosition.X.Value, this.ModuleSettings.ReminderPosition.Y.Value, this.IconState)
            {
                BackgroundOpacity = this.ModuleSettings.ReminderOpacity.Value
            };
            notification.Show(TimeSpan.FromSeconds(this.ModuleSettings.ReminderDuration.Value));
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
            var config = this.ModuleSettings.AddDrawer(name, this._eventCategories);
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
                () => this.NowUTC,
                () => this.Version,
                () => this.BlishHUDAPIState.AccessToken)
            {
                Parent = GameService.Graphics.SpriteScreen
            };

            _ = this._areas.AddOrUpdate(configuration.Name, area, (name, prev) => area);
        }

        private void RemoveArea(EventAreaConfiguration configuration)
        {
            this.ModuleSettings.EventAreaNames.Value = new List<string>(this.ModuleSettings.EventAreaNames.Value.Where(areaName => areaName != configuration.Name));

            this._areas[configuration.Name]?.Dispose();
            _ = this._areas.TryRemove(configuration.Name, out _);

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

            this.SettingsWindow.Tabs.Add(new Tab(this.IconState.GetIcon("156736.png"), () => new UI.Views.GeneralSettingsView(this.ModuleSettings, this.Gw2ApiManager, this.IconState, this.TranslationState, this.SettingEventState, GameService.Content.DefaultFont16) { DefaultColor = this.ModuleSettings.DefaultGW2Color }, "General"));
            //this.SettingsWindow.Tabs.Add(new Tab(this.IconState.GetIcon("156740.png"), () => new UI.Views.Settings.GraphicsSettingsView() { APIManager = this.Gw2ApiManager, IconState = this.IconState, DefaultColor = this.ModuleSettings.DefaultGW2Color }, "Graphic Settings"));
            UI.Views.AreaSettingsView areaSettingsView = new UI.Views.AreaSettingsView(
                () => this._areas.Values.Select(area => area.Configuration),
                () => this._eventCategories,
                this.ModuleSettings,
                this.Gw2ApiManager,
                this.IconState,
                this.TranslationState,
                this.SettingEventState,
                this.EventState,
                GameService.Content.DefaultFont16)
            { DefaultColor = this.ModuleSettings.DefaultGW2Color };
            areaSettingsView.AddArea += (s, e) =>
            {
                e.AreaConfiguration = this.AddArea(e.Name);
                if (e.AreaConfiguration != null)
                {
                    var newArea = this._areas.Values.Where(x => x.Configuration.Name == e.Name).First();
                    this.SetAreaEvents(newArea);
                }
            };

            areaSettingsView.RemoveArea += (s, e) =>
            {
                this.RemoveArea(e);
            };

            this.SettingsWindow.Tabs.Add(new Tab(this.IconState.GetIcon("605018.png"), () => areaSettingsView, "Event Areas"));
            this.SettingsWindow.Tabs.Add(new Tab(this.IconState.GetIcon("1466345.png"), () => new UI.Views.ReminderSettingsView(this.ModuleSettings, () => this._eventCategories, this.Gw2ApiManager, this.IconState, this.TranslationState, this.SettingEventState, GameService.Content.DefaultFont16) { DefaultColor = this.ModuleSettings.DefaultGW2Color }, "Reminders"));
            this.SettingsWindow.Tabs.Add(new Tab(this.IconState.GetIcon("759448.png"), () => new UI.Views.DynamicEventsSettingsView(this.DynamicEventState, this.ModuleSettings, this.Gw2ApiManager, this.IconState, this.TranslationState, this.SettingEventState, GameService.Content.DefaultFont16) { DefaultColor = this.ModuleSettings.DefaultGW2Color }, "Dynamic Events"));
            this.SettingsWindow.Tabs.Add(new Tab(this.IconState.GetIcon("156764.png"), () => new UI.Views.CustomEventView(this.Gw2ApiManager, this.IconState, this.TranslationState, this.BlishHUDAPIState) { DefaultColor = this.ModuleSettings.DefaultGW2Color }, "Custom Events"));

            this.SettingsWindow.Tabs.Add(new Tab(this.IconState.GetIcon("157097.png"), () => new UI.Views.HelpView(() => this._eventCategories, this.API_URL, this.Gw2ApiManager, this.IconState, this.TranslationState, GameService.Content.DefaultFont16) { DefaultColor = this.ModuleSettings.DefaultGW2Color }, "Help"));

        }

        protected override string GetDirectoryName()
        {
            return "events";
        }

        protected override void ConfigureStates(StateConfigurations configurations)
        {
            configurations.BlishHUDAPI.Enabled = true;
            configurations.Account.Enabled = true;
            configurations.Worldbosses.Enabled = true;
            configurations.Mapchests.Enabled = true;
            configurations.PointOfInterests.Enabled = true;
        }

        private void BlishHUDAPIState_NewLogin(object sender, EventArgs e)
        {
            this._lastEventUpdate.Value = _updateEventsInterval.TotalMilliseconds;
        }

        private void BlishHUDAPIState_LoggedOut(object sender, EventArgs e)
        {
            this._lastEventUpdate.Value = _updateEventsInterval.TotalMilliseconds;
        }

        protected override Collection<ManagedState> GetAdditionalStates(string directoryPath)
        {
            Collection<ManagedState> additionalStates = new Collection<ManagedState>();

            this.EventState = new EventState(new StateConfiguration()
            {
                AwaitLoading = false,
                Enabled = true,
                SaveInterval = TimeSpan.FromSeconds(30)
            }, directoryPath, () => this.NowUTC);
            this.DynamicEventState = new DynamicEventState(new APIStateConfiguration()
            {
                AwaitLoading = false,
                Enabled = true,
                SaveInterval = Timeout.InfiniteTimeSpan
            }, this.Gw2ApiManager, this.GetFlurlClient(), this.API_ROOT_URL);

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

            this.MapUtil?.Dispose();
            this.MapUtil = null;

            this.Logger.Debug("Unload drawer.");

            foreach (EventArea area in this._areas.Values)
            {
                area?.Dispose();
            }

            this._areas?.Clear();

            this.Logger.Debug("Unloaded drawer.");

            this.Logger.Debug("Unload events.");

            using (this._eventCategoryLock.Lock())
            {
                foreach (var ec in _eventCategories)
                {
                    ec.Events.ForEach(ev => this.RemoveEventHooks(ev));
                }

                this._eventCategories?.Clear();
            }

            this.Logger.Debug("Unloaded events.");

            this.Logger.Debug("Unload base.");

            base.Unload();

            this.Logger.Debug("Unloaded base.");
        }
    }
}

