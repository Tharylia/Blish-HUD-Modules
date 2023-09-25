namespace Estreya.BlishHUD.EventTable;

using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Blish_HUD.Modules;
using Blish_HUD.Settings;
using Estreya.BlishHUD.Shared.Extensions;
using Controls;
using Flurl.Http;
using Gw2Sharp.Models;
using Humanizer;
using Humanizer.Localisation;
using Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Models;
using MonoGame.Extended.BitmapFonts;
using Services;
using Shared.Modules;
using Shared.MumbleInfo.Map;
using Shared.Services;
using Shared.Settings;
using Shared.Threading;
using Shared.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UI.Views;
using Event = Models.Event;
using ScreenNotification = Shared.Controls.ScreenNotification;
using TabbedWindow = Shared.Controls.TabbedWindow;

/// <summary>
/// The event table module class.
/// </summary>
[Export(typeof(Module))]
public class EventTableModule : BaseModule<EventTableModule, ModuleSettings>
{
    private static TimeSpan _updateEventsInterval = TimeSpan.FromMinutes(30);

    private static TimeSpan _checkDrawerSettingInterval = TimeSpan.FromSeconds(30);

    private ConcurrentDictionary<string, EventArea> _areas;
    private List<EventCategory> _eventCategories;

    private readonly AsyncLock _eventCategoryLock = new AsyncLock();
    private double _lastCheckDrawerSettings;
    private AsyncRef<double> _lastEventUpdate;

    [ImportingConstructor]
    public EventTableModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters)
    {
    }

    public override string UrlModuleName => "event-table";

    protected override bool FailIfBackendDown => true;

    /// <summary>
    ///     Gets the current time in utc.
    /// </summary>
    private DateTime NowUTC => DateTime.UtcNow;

    /// <summary>
    ///     Gets or sets the map util used to interact with the in-game (mini-)map.
    /// </summary>
    private MapUtil MapUtil { get; set; }

    /// <summary>
    ///     Gets or sets a handler for dynamic events.
    /// </summary>
    private DynamicEventHandler DynamicEventHandler { get; set; }

    protected override string API_VERSION_NO => "1";

    protected override void Initialize()
    {
        base.Initialize();

        this._areas = new ConcurrentDictionary<string, EventArea>();
        this._eventCategories = new List<EventCategory>();

        this._lastEventUpdate = new AsyncRef<double>(0);
        this._lastCheckDrawerSettings = 0;
    }

    protected override async Task LoadAsync()
    {
        Stopwatch sw = Stopwatch.StartNew();
        await base.LoadAsync();

        this.BlishHUDAPIService.NewLogin += this.BlishHUDAPIService_NewLogin;
        this.BlishHUDAPIService.RefreshedLogin += this.BlishHUDAPIService_RefreshedLogin; ;
        this.BlishHUDAPIService.LoggedOut += this.BlishHUDAPIService_LoggedOut;

        this.MapUtil = new MapUtil(this.ModuleSettings.MapKeybinding.Value, this.Gw2ApiManager);
        this.DynamicEventHandler = new DynamicEventHandler(this.MapUtil, this.DynamicEventService, this.Gw2ApiManager, this.ModuleSettings);
        this.DynamicEventHandler.FoundLostEntities += this.DynamicEventHandler_FoundLostEntities;

        await this.DynamicEventHandler.AddDynamicEventsToMap();
        await this.DynamicEventHandler.AddDynamicEventsToWorld();

        await this.LoadEvents();

        this.AddAllAreas();

        this.SetAreaEvents();

        sw.Stop();
        this.Logger.Debug($"Loaded in {sw.Elapsed.TotalMilliseconds.ToString(CultureInfo.InvariantCulture)}ms");
    }

    /// <summary>
    ///     Handles the event of lost entities of the <see cref="DynamicEventHandler"/>.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">The event arguments.</param>
    private void DynamicEventHandler_FoundLostEntities(object sender, EventArgs e)
    {
        string[] messages = new[]
        {
            this.TranslationService.GetTranslation("dynamicEventHandler-foundLostEntities1", "GameService.Graphics.World.Entities has lost references."),
            this.TranslationService.GetTranslation("dynamicEventHandler-foundLostEntities2", "Expect dynamic event boundaries on screen.")
        };

        ScreenNotification.ShowNotification(
            messages,
            ScreenNotification.NotificationType.Warning);
    }

    /// <summary>
    ///     Updates the events in all registered areas.
    /// </summary>
    private void SetAreaEvents()
    {
        foreach (EventArea area in this._areas.Values)
        {
            this.SetAreaEvents(area);
        }
    }

    /// <summary>
    ///     Updates the events for a given area.
    /// </summary>
    /// <param name="area">The area which should receive the current loaded events.</param>
    private void SetAreaEvents(EventArea area)
    {
        area.UpdateAllEvents(this._eventCategories);
    }

    /// <summary>
    ///     Reloads all events.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task LoadEvents()
    {
        this.Logger.Info("Load events...");
        using (await this._eventCategoryLock.LockAsync())
        {
            this.Logger.Debug("Acquired lock.");
            try
            {
                this._eventCategories?.SelectMany(ec => ec.Events).ToList().ForEach(this.RemoveEventHooks);
                this._eventCategories?.Clear();

                IFlurlRequest request = this.GetFlurlClient().Request(this.MODULE_API_URL, "events");

                if (!string.IsNullOrWhiteSpace(this.BlishHUDAPIService.AccessToken))
                {
                    this.Logger.Info("Include custom events...");
                    request.WithOAuthBearerToken(this.BlishHUDAPIService.AccessToken);
                }

                List<EventCategory> categories = await request.GetJsonAsync<List<EventCategory>>();

                int eventCategoryCount = categories.Count;
                int eventCount = categories.Sum(ec => ec.Events.Count);

                this.Logger.Info($"Loaded {eventCategoryCount} Categories with {eventCount} Events.");

                categories.ForEach(ec =>
                {
                    ec.Load(() => this.NowUTC, this.TranslationService);
                });

                this.Logger.Debug("Loaded all event categories.");

                this.AssignEventReminderTimes(categories);

                this._eventCategories = categories;

                foreach (Event ev in this._eventCategories.SelectMany(ec => ec.Events))
                {
                    this.AddEventHooks(ev);
                }

                this._lastCheckDrawerSettings = _checkDrawerSettingInterval.TotalMilliseconds;

                this.SetAreaEvents();

                this.Logger.Debug("Updated events in all areas.");
            }
            catch (FlurlHttpException ex)
            {
                string message = await ex.GetResponseStringAsync();
                this.Logger.Warn(ex, $"Failed loading events: {message}");
            }
            catch (Exception ex)
            {
                this.Logger.Error(ex, "Failed loading events.");
            }
        }
    }

    /// <summary>
    ///     Assigns all saved time overrides the to corresponding events.
    /// </summary>
    /// <param name="categories"></param>
    private void AssignEventReminderTimes(List<EventCategory> categories)
    {
        IEnumerable<Event> events = categories.SelectMany(ec => ec.Events).Where(ev => !ev.Filler);
        foreach (Event ev in events)
        {
            if (!this.ModuleSettings.ReminderTimesOverride.Value.ContainsKey(ev.SettingKey)) continue;

            List<TimeSpan> times = this.ModuleSettings.ReminderTimesOverride.Value[ev.SettingKey];
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
                foreach (KeyValuePair<string, EventArea> area in this._areas)
                {
                    this.ModuleSettings.CheckDrawerSettings(area.Value.Configuration, this._eventCategories);
                }
            }
        }
    }

    /// <summary>
    ///     Toggles all areas based on ui visibility calculations.
    /// </summary>
    private void ToggleContainers()
    {
        bool show = this.ShowUI && this.ModuleSettings.GlobalDrawerVisible.Value;

        foreach (var area in this._areas.Values)
        {
            // Don't show if disabled.
            bool showArea = show && area.Enabled && area.CalculateUIVisibility();

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
        }
    }

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        this.ToggleContainers();

        this.ModuleSettings.CheckGlobalSizeAndPosition();

        foreach (EventArea area in this._areas.Values)
        {
            this.ModuleSettings.CheckDrawerSizeAndPosition(area.Configuration);
        }

        // Dont block update when we need to wait, can cause slight delays when skipping update
        if (this._eventCategoryLock.IsFree())
        {
            using (this._eventCategoryLock.Lock())
            {
                foreach (Event ev in this._eventCategories.SelectMany(ec => ec.Events))
                {
                    ev.Update(gameTime);
                }
            }
        }

        this.DynamicEventHandler.Update(gameTime);

        UpdateUtil.Update(this.CheckDrawerSettings, gameTime, _checkDrawerSettingInterval.TotalMilliseconds, ref this._lastCheckDrawerSettings);
        _ = UpdateUtil.UpdateAsync(this.LoadEvents, gameTime, _updateEventsInterval.TotalMilliseconds, this._lastEventUpdate);
    }

    /// <summary>
    ///     Calculates the ui visibility of reminders based on settings or mumble parameters.
    /// </summary>
    /// <returns>The newly calculated ui visibility.</returns>
    private bool CalculateReminderUIVisibility()
    {
        bool show = true;
        if (this.ModuleSettings.HideRemindersOnOpenMap.Value)
        {
            show &= !GameService.Gw2Mumble.UI.IsMapOpen;
        }

        if (this.ModuleSettings.HideRemindersOnMissingMumbleTicks.Value)
        {
            show &= GameService.Gw2Mumble.TimeSinceTick.TotalSeconds < 0.5;
        }

        if (this.ModuleSettings.HideRemindersInCombat.Value)
        {
            show &= !GameService.Gw2Mumble.PlayerCharacter.IsInCombat;
        }

        // All maps not specified as competetive will be treated as open world
        if (this.ModuleSettings.HideRemindersInPvE_OpenWorld.Value)
        {
            MapType[] pveOpenWorldMapTypes =
            {
                MapType.Public,
                MapType.Instance,
                MapType.Tutorial,
                MapType.PublicMini
            };

            show &= !(!GameService.Gw2Mumble.CurrentMap.IsCompetitiveMode && pveOpenWorldMapTypes.Any(type => type == GameService.Gw2Mumble.CurrentMap.Type) && !MapInfo.MAP_IDS_PVE_COMPETETIVE.Contains(GameService.Gw2Mumble.CurrentMap.Id));
        }

        if (this.ModuleSettings.HideRemindersInPvE_Competetive.Value)
        {
            MapType[] pveCompetetiveMapTypes =
            {
                MapType.Instance
            };

            show &= !(!GameService.Gw2Mumble.CurrentMap.IsCompetitiveMode && pveCompetetiveMapTypes.Any(type => type == GameService.Gw2Mumble.CurrentMap.Type) && MapInfo.MAP_IDS_PVE_COMPETETIVE.Contains(GameService.Gw2Mumble.CurrentMap.Id));
        }

        if (this.ModuleSettings.HideRemindersInWvW.Value)
        {
            MapType[] wvwMapTypes =
            {
                MapType.EternalBattlegrounds,
                MapType.GreenBorderlands,
                MapType.RedBorderlands,
                MapType.BlueBorderlands,
                MapType.EdgeOfTheMists
            };

            show &= !(GameService.Gw2Mumble.CurrentMap.IsCompetitiveMode && wvwMapTypes.Any(type => type == GameService.Gw2Mumble.CurrentMap.Type));
        }

        if (this.ModuleSettings.HideRemindersInPvP.Value)
        {
            MapType[] pvpMapTypes =
            {
                MapType.Pvp,
                MapType.Tournament
            };

            show &= !(GameService.Gw2Mumble.CurrentMap.IsCompetitiveMode && pvpMapTypes.Any(type => type == GameService.Gw2Mumble.CurrentMap.Type));
        }

        return show;
    }

    /// <summary>
    ///     Adds all event hooks to the specified event.
    /// </summary>
    /// <param name="ev">The event to which the event hooks should be added.</param>
    private void AddEventHooks(Event ev)
    {
        ev.Reminder += this.Ev_Reminder;
    }

    /// <summary>
    ///     Removes all event hooks from the specified event.
    /// </summary>
    /// <param name="ev">The event from which the event hooks should be removed.</param>
    private void RemoveEventHooks(Event ev)
    {
        ev.Reminder -= this.Ev_Reminder;
    }

    /// <summary>
    ///     Handles the event of an event reminder.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">The timespan until the event start.</param>
    private void Ev_Reminder(object sender, TimeSpan e)
    {
        Event ev = sender as Event;

        if (!this.ModuleSettings.RemindersEnabled.Value || this.ModuleSettings.ReminderDisabledForEvents.Value.Contains(ev.SettingKey))
        {
            return;
        }

        if (!this.CalculateReminderUIVisibility())
        {
            this.Logger.Debug($"Reminder {ev.SettingKey} was not displayed due to UI Visibility settings.");
            return;
        }

        string startsInTranslation = this.TranslationService.GetTranslation("reminder-startsIn", "Starts in");
        EventNotification notification = new EventNotification(ev, 
            $"{startsInTranslation} {e.Humanize(2, minUnit: TimeUnit.Second)}!",
            this.ModuleSettings.ReminderPosition.X.Value,
            this.ModuleSettings.ReminderPosition.Y.Value, 
            this.ModuleSettings.ReminderStackDirection.Value,
            this.IconService,
            this.ModuleSettings.ReminderLeftClickAction.Value != LeftClickAction.None)
        { BackgroundOpacity = this.ModuleSettings.ReminderOpacity.Value };
        notification.Click += this.EventNotification_Click;
        notification.Disposed += this.EventNotification_Disposed;
        notification.Show(TimeSpan.FromSeconds(this.ModuleSettings.ReminderDuration.Value));
    }

    private void EventNotification_Disposed(object sender, EventArgs e)
    {
        var notification = sender as EventNotification;
        notification.Click -= this.EventNotification_Click;
        notification.Disposed -= this.EventNotification_Disposed;
    }

    private void EventNotification_Click(object sender, MouseEventArgs e)
    {
        var notification = sender as EventNotification;
        switch (this.ModuleSettings.ReminderLeftClickAction.Value)
        {
            case LeftClickAction.CopyWaypoint:
                if (!string.IsNullOrWhiteSpace(notification.Model.Waypoint))
                {
                    ClipboardUtil.WindowsClipboardService.SetTextAsync(notification.Model.Waypoint);
                    ScreenNotification.ShowNotification(new[]
                    {
                        notification.Model.Name,
                        "Copied to clipboard!"
                    });
                }
                break;
            case LeftClickAction.NavigateToWaypoint:
                if (string.IsNullOrWhiteSpace(notification.Model.Waypoint))
                {
                    return;
                }

                if (this.PointOfInterestService.Loading)
                {
                    ScreenNotification.ShowNotification($"{nameof(PointOfInterestService)} is still loading!", ScreenNotification.NotificationType.Error);
                    return;
                }

                Shared.Models.GW2API.PointOfInterest.PointOfInterest poi = this.PointOfInterestService.GetPointOfInterest(notification.Model.Waypoint);
                if (poi == null)
                {
                    ScreenNotification.ShowNotification($"{notification.Model.Waypoint} not found!", ScreenNotification.NotificationType.Error);
                    return;
                }

                _ = Task.Run(async () =>
                {
                    MapUtil.NavigationResult result = await (this.MapUtil?.NavigateToPosition(poi, this.ModuleSettings.AcceptWaypointPrompt.Value) ?? Task.FromResult(new MapUtil.NavigationResult(false, "Variable null.")));
                    if (!result.Success)
                    {
                        ScreenNotification.ShowNotification($"Navigation failed: {result.Message ?? "Unknown"}", ScreenNotification.NotificationType.Error);
                    }
                });
                break;
        }
    }

    /// <summary>
    ///     Adds all saved areas.
    /// </summary>
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

    /// <summary>
    ///     Adds a new area
    /// </summary>
    /// <param name="name">The name of the new area</param>
    /// <returns>The created area configuration.</returns>
    private EventAreaConfiguration AddArea(string name)
    {
        EventAreaConfiguration config = this.ModuleSettings.AddDrawer(name, this._eventCategories);
        this.AddArea(config);

        return config;
    }

    /// <summary>
    ///     Adds a new area.
    /// </summary>
    /// <param name="configuration">The configuration of the new area.</param>
    private void AddArea(EventAreaConfiguration configuration)
    {
        if (!this.ModuleSettings.EventAreaNames.Value.Contains(configuration.Name))
        {
            this.ModuleSettings.EventAreaNames.Value = new List<string>(this.ModuleSettings.EventAreaNames.Value) { configuration.Name };
        }

        this.ModuleSettings.UpdateDrawerLocalization(configuration, this.TranslationService);

        EventArea area = new EventArea(
            configuration,
            this.IconService,
            this.TranslationService,
            this.EventStateService,
            this.WorldbossService,
            this.MapchestService,
            this.PointOfInterestService,
            this.MapUtil,
            this.GetFlurlClient(),
            this.MODULE_API_URL,
            () => this.NowUTC,
            () => this.Version,
            () => this.BlishHUDAPIService.AccessToken,
            this.ContentsManager)
        { Parent = GameService.Graphics.SpriteScreen };

        _ = this._areas.AddOrUpdate(configuration.Name, area, (name, prev) => area);
    }

    /// <summary>
    ///     Removes the specified area.
    /// </summary>
    /// <param name="configuration">The configuration of the area which should be removed.</param>
    private void RemoveArea(EventAreaConfiguration configuration)
    {
        this.ModuleSettings.EventAreaNames.Value = new List<string>(this.ModuleSettings.EventAreaNames.Value.Where(areaName => areaName != configuration.Name));

        this._areas[configuration.Name]?.Dispose();
        _ = this._areas.TryRemove(configuration.Name, out _);

        this.ModuleSettings.RemoveDrawer(configuration.Name);
    }

    protected override BaseModuleSettings DefineModuleSettings(SettingCollection settings) => new ModuleSettings(settings);

    protected override void OnSettingWindowBuild(TabbedWindow settingWindow)
    {
        settingWindow.SavesSize = true;
        settingWindow.CanResize = true;
        settingWindow.RebuildViewAfterResize = true;
        settingWindow.UnloadOnRebuild = false;
        settingWindow.MinSize = settingWindow.Size;
        settingWindow.MaxSize = new Point(settingWindow.Width * 2, settingWindow.Height * 3);
        settingWindow.RebuildDelay = 500;
        // Reorder Icon: 605018

        this.SettingsWindow.Tabs.Add(new Tab(
            this.IconService.GetIcon("156736.png"),
            () => new GeneralSettingsView(this.ModuleSettings, this.Gw2ApiManager, this.IconService, this.TranslationService, this.SettingEventService) { DefaultColor = this.ModuleSettings.DefaultGW2Color },
            this.TranslationService.GetTranslation("generalSettingsView-title", "General")));

        //this.SettingsWindow.Tabs.Add(new Tab(this.IconService.GetIcon("156740.png"), () => new UI.Views.Settings.GraphicsSettingsView() { APIManager = this.Gw2ApiManager, IconService = this.IconService, DefaultColor = this.ModuleSettings.DefaultGW2Color }, "Graphic Settings"));
        AreaSettingsView areaSettingsView = new AreaSettingsView(
            () => this._areas.Values.Select(area => area.Configuration),
            () => this._eventCategories,
            this.ModuleSettings,
            this.Gw2ApiManager,
            this.IconService,
            this.TranslationService,
            this.SettingEventService,
            this.EventStateService)
        { DefaultColor = this.ModuleSettings.DefaultGW2Color };
        areaSettingsView.AddArea += (s, e) =>
        {
            e.AreaConfiguration = this.AddArea(e.Name);
            if (e.AreaConfiguration != null)
            {
                EventArea newArea = this._areas.Values.Where(x => x.Configuration.Name == e.Name).First();
                this.SetAreaEvents(newArea);
            }
        };

        areaSettingsView.RemoveArea += (s, e) =>
        {
            this.RemoveArea(e);
        };

        this.SettingsWindow.Tabs.Add(new Tab(
            this.IconService.GetIcon("605018.png"),
            () => areaSettingsView,
            this.TranslationService.GetTranslation("areaSettingsView-title", "Event Areas")));

        this.SettingsWindow.Tabs.Add(new Tab(
            this.IconService.GetIcon("1466345.png"),
            () => new ReminderSettingsView(this.ModuleSettings, () => this._eventCategories, this.Gw2ApiManager, this.IconService, this.TranslationService, this.SettingEventService) { DefaultColor = this.ModuleSettings.DefaultGW2Color },
            this.TranslationService.GetTranslation("reminderSettingsView-title", "Reminders")));

        this.SettingsWindow.Tabs.Add(new Tab(
            this.IconService.GetIcon("759448.png"),
            () => new DynamicEventsSettingsView(this.DynamicEventService, this.ModuleSettings, this.GetFlurlClient(), this.Gw2ApiManager, this.IconService, this.TranslationService, this.SettingEventService) { DefaultColor = this.ModuleSettings.DefaultGW2Color },
            this.TranslationService.GetTranslation("dynamicEventsSettingsView-title", "Dynamic Events")));

        this.SettingsWindow.Tabs.Add(new Tab(
            this.IconService.GetIcon("156764.png"),
            () => new CustomEventView(this.Gw2ApiManager, this.IconService, this.TranslationService, this.BlishHUDAPIService) { DefaultColor = this.ModuleSettings.DefaultGW2Color },
            this.TranslationService.GetTranslation("customEventView-title", "Custom Events")));

        this.SettingsWindow.Tabs.Add(new Tab(
            this.IconService.GetIcon("157097.png"),
            () => new HelpView(() => this._eventCategories, this.MODULE_API_URL, this.Gw2ApiManager, this.IconService, this.TranslationService) { DefaultColor = this.ModuleSettings.DefaultGW2Color },
            this.TranslationService.GetTranslation("helpView-title", "Help")));
    }

    protected override string GetDirectoryName() => "events";

    protected override void ConfigureServices(ServiceConfigurations configurations)
    {
        configurations.BlishHUDAPI.Enabled = true;
        configurations.Account.Enabled = true;
        configurations.Account.AwaitLoading = true;
        configurations.Worldbosses.Enabled = true;
        configurations.Mapchests.Enabled = true;
        configurations.PointOfInterests.Enabled = true;
    }

    /// <summary>
    ///     Handles the event of a login on the api backend.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">The empty event arguments.</param>
    private void BlishHUDAPIService_NewLogin(object sender, EventArgs e)
    {
        this._lastEventUpdate.Value = _updateEventsInterval.TotalMilliseconds;
    }

    /// <summary>
    ///     Handles the event of a refreshed login on the api backend.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">The empty event arguments.</param>
    private void BlishHUDAPIService_RefreshedLogin(object sender, EventArgs e)
    {
        this._lastEventUpdate.Value = _updateEventsInterval.TotalMilliseconds;
    }

    /// <summary>
    ///     Handles the event of a logout from the api backend.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">The empty event arguments.</param>
    private void BlishHUDAPIService_LoggedOut(object sender, EventArgs e)
    {
        this._lastEventUpdate.Value = _updateEventsInterval.TotalMilliseconds;
    }

    protected override Collection<ManagedService> GetAdditionalServices(string directoryPath)
    {
        Collection<ManagedService> additionalServices = new Collection<ManagedService>();

        this.EventStateService = new EventStateService(new ServiceConfiguration
        {
            AwaitLoading = false,
            Enabled = true,
            SaveInterval = TimeSpan.FromSeconds(30)
        }, directoryPath, () => this.NowUTC);

        this.DynamicEventService = new DynamicEventService(new APIServiceConfiguration
        {
            AwaitLoading = false,
            Enabled = true,
            SaveInterval = Timeout.InfiniteTimeSpan
        }, this.Gw2ApiManager, this.GetFlurlClient(), API_ROOT_URL, directoryPath);

        additionalServices.Add(this.EventStateService);
        additionalServices.Add(this.DynamicEventService);

        return additionalServices;
    }

    protected override AsyncTexture2D GetEmblem()
    {
        return this.IconService.GetIcon(this.IsPrerelease ? "textures/emblem_demo.png" : "102392.png");
    }

    protected override AsyncTexture2D GetCornerIcon()
    {
        return this.IconService.GetIcon($"textures/event_boss_grey{(this.IsPrerelease ? "_demo" : "")}.png");
    }

    private BitmapFont _defaultFont;

    public override BitmapFont Font
    {
        get
        {
            if (this._defaultFont == null)
            {
                using var defaultFontStream = this.ContentsManager.GetFileStream("fonts\\Anonymous.ttf");

                // Default size 16 is same as loaded size 18
                this._defaultFont = defaultFontStream is not null ? FontUtils.FromTrueTypeFont(defaultFontStream.ToByteArray(), 18, 256, 256).ToBitmapFont() : GameService.Content.DefaultFont16;
            }

            return this._defaultFont;
        }
    }

    protected override void Unload()
    {
        this.Logger.Debug("Unload module.");

        if (this.DynamicEventHandler != null)
        {
            this.DynamicEventHandler.FoundLostEntities -= this.DynamicEventHandler_FoundLostEntities;
            this.DynamicEventHandler.Dispose();
            this.DynamicEventHandler = null;
        }

        this.MapUtil?.Dispose();
        this.MapUtil = null;

        this.Logger.Debug("Unload drawer.");

        if (this._areas != null)
        {
            foreach (EventArea area in this._areas.Values)
            {
                area?.Dispose();
            }

            this._areas?.Clear();
        }

        this.Logger.Debug("Unloaded drawer.");

        this.Logger.Debug("Unload events.");

        using (this._eventCategoryLock.Lock())
        {
            foreach (EventCategory ec in this._eventCategories)
            {
                ec.Events.ForEach(ev => this.RemoveEventHooks(ev));
            }

            this._eventCategories?.Clear();
        }

        this.Logger.Debug("Unloaded events.");

        if (this.BlishHUDAPIService != null)
        {
            this.BlishHUDAPIService.NewLogin -= this.BlishHUDAPIService_NewLogin;
            this.BlishHUDAPIService.LoggedOut -= this.BlishHUDAPIService_LoggedOut;
        }

        this.Logger.Debug("Unload base.");

        base.Unload();

        this.Logger.Debug("Unloaded base.");
    }

    protected override int CornerIconPriority => 1_289_351_278;

    #region Services

    public EventStateService EventStateService { get; private set; }
    public DynamicEventService DynamicEventService { get; private set; }

    #endregion
}