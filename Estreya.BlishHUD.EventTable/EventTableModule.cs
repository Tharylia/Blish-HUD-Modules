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
using Estreya.BlishHUD.Shared.Helpers;
using Blish_HUD.ArcDps.Models;
using Estreya.BlishHUD.Shared.Contexts;
using Estreya.BlishHUD.EventTable.Contexts;
using Windows.UI.WindowManagement;
using Microsoft.Xna.Framework.Audio;

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

    private EventTableContext _eventTableContext;
    private ContextManager _contextManager;
    private ContextsService.ContextHandle<EventTableContext> _eventTableContextHandle;

    [ImportingConstructor]
    public EventTableModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters)
    {
    }

    protected override string UrlModuleName => "event-table";

    protected override bool NotifyIfBackendDown => true;

    protected override bool EnableMetrics => true;

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
        this.ModuleSettings.ValidateAndTryFixSettings();
        await base.LoadAsync();

        this.BlishHUDAPIService.NewLogin += this.BlishHUDAPIService_NewLogin;
        this.BlishHUDAPIService.RefreshedLogin += this.BlishHUDAPIService_RefreshedLogin;
        this.BlishHUDAPIService.LoggedOut += this.BlishHUDAPIService_LoggedOut;

        this.MapUtil = new MapUtil(this.ModuleSettings.MapKeybinding.Value, this.Gw2ApiManager);
        this.DynamicEventHandler = new DynamicEventHandler(this.MapUtil, this.DynamicEventService, this.Gw2ApiManager, this.ModuleSettings);
        this.DynamicEventHandler.FoundLostEntities += this.DynamicEventHandler_FoundLostEntities;

        await this.DynamicEventHandler.AddDynamicEventsToMap();
        await this.DynamicEventHandler.AddDynamicEventsToWorld();

        this.ModuleSettings.IncludeSelfHostedEvents.SettingChanged += this.IncludeSelfHostedEvents_SettingChanged;

        this.AddAllAreas();

        await this.LoadEvents();

        sw.Stop();
        this.Logger.Debug($"Loaded in {sw.Elapsed.TotalMilliseconds.ToString(CultureInfo.InvariantCulture)}ms");
    }

    private async void IncludeSelfHostedEvents_SettingChanged(object sender, ValueChangedEventArgs<bool> e)
    {
        await this.ReloadEvents();
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

    protected override void OnModuleLoaded(EventArgs e)
    {
        base.OnModuleLoaded(e);
        this.RegisterContext();
    }

    private void RegisterContext()
    {
        if (!this.ModuleSettings.RegisterContext.Value)
        {
            this.Logger.Info("Event Table context was not registered due to user preferences.");
            return;
        }

        this._eventTableContext = new EventTableContext();
        this._contextManager = new ContextManager(this._eventTableContext, this.ModuleSettings, this.DynamicEventService,
            this.IconService,
            this.EventStateService,
            async () =>
            {
                using (await this._eventCategoryLock.LockAsync())
                {
                    return this._eventCategories.SelectMany(ec => ec.Events);
                }
            });

        this._contextManager.ReloadEvents += this.ContextManager_ReloadEvents;

        this._eventTableContextHandle = GameService.Contexts.RegisterContext(this._eventTableContext);
        this.Logger.Info("Event Table context registered.");
    }

    private async Task ContextManager_ReloadEvents(object sender)
    {
        await this.ReloadEvents();
    }

    private async Task ReloadEvents()
    {
        this._lastEventUpdate.Value = _updateEventsInterval.TotalMilliseconds;

        await AsyncHelper.WaitUntil(() => this._lastEventUpdate.Value < _updateEventsInterval.TotalMilliseconds, TimeSpan.FromSeconds(15));
    }

    /// <summary>
    ///     Reloads all events.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task LoadEvents()
    {
        if (this.ModuleState == Shared.Modules.ModuleState.Error) return;

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

                var contextEvents = this._contextManager?.GetContextCategories();
                if (contextEvents is not null && contextEvents.Count > 0)
                {
                    this.Logger.Info($"Include {contextEvents.Count} context categories with {contextEvents.Sum(ec => ec.Events?.Count ?? 0)} events.");

                    categories.AddRange(contextEvents);
                }

                if (this.ModuleSettings.IncludeSelfHostedEvents.Value)
                {
                    var selfHostedEvents = await this.LoadSelfHostedEvents();

                    if (selfHostedEvents is not null)
                    {
                        foreach (var selfHostedCategory in selfHostedEvents)
                        {
                            if (!categories.Any(c => c.Key == selfHostedCategory.Key)) continue;

                            var category = categories.Find(c => c.Key == selfHostedCategory.Key);
                            foreach (var selfHostedEvent in selfHostedCategory.Value)
                            {
                                var ev = new Event()
                                {
                                    Key = selfHostedEvent.EventKey,
                                    Name = selfHostedEvent.EventName ?? selfHostedEvent.EventKey,
                                    Duration = selfHostedEvent.Duration,
                                    HostedBySystem = false
                                };

                                ev.Occurences.Add(selfHostedEvent.StartTime.UtcDateTime);

                                category.OriginalEvents.Add(ev);
                            }
                        }
                    }
                }

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

    private async Task<Dictionary<string, List<SelfHostedEventEntry>>> LoadSelfHostedEvents()
    {
        try
        {
            IFlurlRequest request = this.GetFlurlClient().Request(this.MODULE_API_URL, "self-hosting");

            var selfhostedEntries = await request.GetJsonAsync<Dictionary<string, List<SelfHostedEventEntry>>>();

            int eventCategoryCount = selfhostedEntries.Count;
            int eventCount = selfhostedEntries.Sum(ec => ec.Value.Count);

            this.Logger.Info($"Loaded {eventCategoryCount} self hosted categories with {eventCount} events.");

            return selfhostedEntries;
        }
        catch (FlurlHttpException ex)
        {
            string message = await ex.GetResponseStringAsync();
            this.Logger.Warn(ex, $"Failed loading self hosted events: {message}");
        }
        catch (Exception ex)
        {
            this.Logger.Warn(ex, "Failed loading self hosted events.");
        }

        return null;
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

        this.DynamicEventHandler?.Update(gameTime);
        this._contextManager?.Update(gameTime);

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
    private async void Ev_Reminder(object sender, TimeSpan e)
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

        if (this.ModuleSettings.DisableRemindersWhenEventFinished.Value)
        {
            var areaName = this.ModuleSettings.DisableRemindersWhenEventFinishedArea.Value;
            var completed = areaName switch
            {
                ModuleSettings.ANY_AREA_NAME => this.EventStateService.Contains(ev.SettingKey),
                _ => this.EventStateService.Contains(areaName, ev.SettingKey),
            };

            if (completed)
            {
                this.Logger.Debug($"Reminder {ev.SettingKey} was not displayed due to being completed/hidden in the area \"{areaName}\".");
                return;
            }
        }

        try
        {
            string startsInTranslation = this.TranslationService.GetTranslation("reminder-startsIn", "Starts in");
            var title = ev.Name;
            var message = $"{startsInTranslation} {e.Humanize(6, minUnit: this.ModuleSettings.ReminderMinTimeUnit.Value)}!";
            var icon = string.IsNullOrWhiteSpace(ev.Icon) ? new AsyncTexture2D() : this.IconService.GetIcon(ev.Icon);

            if (this.ModuleSettings.ReminderType.Value is Models.Reminders.ReminderType.Control or Models.Reminders.ReminderType.Both)
            {
                EventNotification notification = new EventNotification(
                    ev,
                    title,
                    message,
                    icon,
                    this.ModuleSettings.ReminderPosition.X.Value,
                    this.ModuleSettings.ReminderPosition.Y.Value,
                    this.ModuleSettings.ReminderSize.X.Value,
                    this.ModuleSettings.ReminderSize.Y.Value,
                    this.ModuleSettings.ReminderSize.Icon.Value,
                    this.ModuleSettings.ReminderStackDirection.Value,
                    this.ModuleSettings.ReminderOverflowStackDirection.Value,
                    this.ModuleSettings.ReminderFonts.TitleSize.Value,
                    this.ModuleSettings.ReminderFonts.MessageSize.Value,
                    this.IconService,
                    this.ModuleSettings.ReminderLeftClickAction.Value != LeftClickAction.None
                    || this.ModuleSettings.ReminderRightClickAction.Value != Models.Reminders.EventReminderRightClickAction.None)
                { BackgroundOpacity = this.ModuleSettings.ReminderOpacity.Value };
                notification.Click += this.EventNotification_Click;
                notification.RightMouseButtonPressed += this.EventNotification_RightMouseButtonPressed;
                notification.Disposed += this.EventNotification_Disposed;
                notification.Show(TimeSpan.FromSeconds(this.ModuleSettings.ReminderDuration.Value));
            }

            if (this.ModuleSettings.ReminderType.Value is Models.Reminders.ReminderType.Windows or Models.Reminders.ReminderType.Both)
            {
                await EventNotification.ShowAsWindowsNotification(title, message, icon);
            }

            this.AudioService.PlaySoundFromFile("reminder", true);
        }
        catch (Exception ex)
        {
            this.Logger.Warn(ex, $"Failed to show reminder for event \"{ev.SettingKey}\"");
        }
    }

    private void EventNotification_Disposed(object sender, EventArgs e)
    {
        var notification = sender as EventNotification;
        notification.Click -= this.EventNotification_Click;
        notification.RightMouseButtonPressed -= this.EventNotification_RightMouseButtonPressed;
        notification.Disposed -= this.EventNotification_Disposed;
    }

    private void EventNotification_Click(object sender, MouseEventArgs e)
    {
        var notification = sender as EventNotification;
        switch (this.ModuleSettings.ReminderLeftClickAction.Value)
        {
            case LeftClickAction.CopyWaypoint:
                if (notification is not null && notification.Model is not null && !string.IsNullOrWhiteSpace(notification.Model.Waypoint))
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
                if (notification is null || notification.Model is null || string.IsNullOrWhiteSpace(notification.Model.Waypoint) || this.PointOfInterestService is null)
                {
                    break;
                }

                if (this.PointOfInterestService.Loading)
                {
                    ScreenNotification.ShowNotification($"{nameof(this.PointOfInterestService)} is still loading!", ScreenNotification.NotificationType.Error);
                    break;
                }

                Shared.Models.GW2API.PointOfInterest.PointOfInterest poi = this.PointOfInterestService.GetPointOfInterest(notification.Model.Waypoint);
                if (poi == null)
                {
                    ScreenNotification.ShowNotification($"{notification.Model.Waypoint} not found!", ScreenNotification.NotificationType.Error);
                    break;
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

    private void EventNotification_RightMouseButtonPressed(object sender, MouseEventArgs e)
    {
        var notification = sender as EventNotification;
        switch (this.ModuleSettings.ReminderRightClickAction.Value)
        {
            case Models.Reminders.EventReminderRightClickAction.Dismiss:
                notification?.Dispose();
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
            () => this.ModuleSettings.EventAreaNames.Value.ToArray().ToList(),
            this.ContentsManager)
        { Parent = GameService.Graphics.SpriteScreen };

        area.CopyToAreaClicked += this.EventArea_CopyToAreaClicked;
        area.MoveToAreaClicked += this.EventArea_MoveToAreaClicked;
        area.Disposed += this.EventArea_Disposed;

        _ = this._areas.AddOrUpdate(configuration.Name, area, (name, prev) => area);
    }

    private void EventArea_MoveToAreaClicked(object sender, (string EventSettingKey, string DestinationArea) e)
    {
        var sourceArea = sender as EventArea;
        var destArea = this._areas.First(a => a.Key == e.DestinationArea).Value;

        sourceArea.DisableEvent(e.EventSettingKey);
        destArea.EnableEvent(e.EventSettingKey);
    }

    private void EventArea_CopyToAreaClicked(object sender, (string EventSettingKey, string DestinationArea) e)
    {
        var sourceArea = sender as EventArea;
        var destArea = this._areas.First(a => a.Key == e.DestinationArea).Value;

        destArea.EnableEvent(e.EventSettingKey);
    }

    private void EventArea_Disposed(object sender, EventArgs e)
    {
        var area = sender as EventArea;
        area.CopyToAreaClicked -= this.EventArea_CopyToAreaClicked;
        area.MoveToAreaClicked -= this.EventArea_MoveToAreaClicked;
        area.Disposed -= this.EventArea_Disposed;
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
            () => new GeneralSettingsView(this.ModuleSettings, this.Gw2ApiManager, this.IconService, this.TranslationService, this.SettingEventService, this.MetricsService) { DefaultColor = this.ModuleSettings.DefaultGW2Color },
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

        areaSettingsView.SyncEnabledEventsToReminders += (s, e) =>
        {
            this.ModuleSettings.ReminderDisabledForEvents.Value = new List<string>(e.DisabledEventKeys.Value);
            return Task.CompletedTask;
        };

        areaSettingsView.SyncEnabledEventsFromReminders += (s, e) =>
        {
            e.DisabledEventKeys.Value = new List<string>(this.ModuleSettings.ReminderDisabledForEvents.Value);
            return Task.CompletedTask;
        };

        areaSettingsView.SyncEnabledEventsToOtherAreas += (s, e) =>
        {
            if (this._areas == null) throw new ArgumentNullException(nameof(this._areas), "Areas are not available.");

            foreach (EventArea area in this._areas.Values)
            {
                if (area.Configuration.Name == e.Name) continue;

                area.Configuration.DisabledEventKeys.Value = new List<string>(e.DisabledEventKeys.Value);
            }

            return Task.CompletedTask;
        };

        this.SettingsWindow.Tabs.Add(new Tab(
            this.IconService.GetIcon("605018.png"),
            () => areaSettingsView,
            this.TranslationService.GetTranslation("areaSettingsView-title", "Event Areas")));

        var reminderSettingsView = new ReminderSettingsView(this.ModuleSettings, () => this._eventCategories, () => this._areas.Keys.ToList(), this.Gw2ApiManager, this.IconService, this.TranslationService, this.SettingEventService) { DefaultColor = this.ModuleSettings.DefaultGW2Color };
        reminderSettingsView.SyncEnabledEventsToAreas += (s) =>
        {
            if (this._areas == null) throw new ArgumentNullException(nameof(this._areas), "Areas are not available.");

            foreach (EventArea area in this._areas.Values)
            {
                area.Configuration.DisabledEventKeys.Value = new List<string>(this.ModuleSettings.ReminderDisabledForEvents.Value);
            }

            return Task.CompletedTask;
        };

        this.SettingsWindow.Tabs.Add(new Tab(
            this.IconService.GetIcon("1466345.png"),
            () => reminderSettingsView,
            this.TranslationService.GetTranslation("reminderSettingsView-title", "Reminders")));

        this.SettingsWindow.Tabs.Add(new Tab(
            this.IconService.GetIcon("759448.png"),
            () => new DynamicEventsSettingsView(this.DynamicEventService, this.ModuleSettings, this.GetFlurlClient(), this.Gw2ApiManager, this.IconService, this.TranslationService, this.SettingEventService) { DefaultColor = this.ModuleSettings.DefaultGW2Color },
            this.TranslationService.GetTranslation("dynamicEventsSettingsView-title", "Dynamic Events")));

        this.SettingsWindow.Tabs.Add(new Tab(
            this.IconService.GetIcon("156764.png"),
            () => new Shared.UI.Views.BlishHUDAPIView(this.Gw2ApiManager, this.IconService, this.TranslationService, this.BlishHUDAPIService, this.GetFlurlClient()) { DefaultColor = this.ModuleSettings.DefaultGW2Color },
            "Estreya BlishHUD API"));

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
        configurations.Audio.Enabled = true;
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
        }, this.Gw2ApiManager, this.GetFlurlClient(), this.API_ROOT_URL, directoryPath);

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

    protected override AsyncTexture2D GetErrorCornerIcon()
    {
        return this.IconService.GetIcon($"textures/event_boss_grey_error.png");
    }

    private BitmapFont _defaultFont;

    protected override BitmapFont Font
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

    private void UnloadContext()
    {
        this._eventTableContextHandle?.Expire();
        this.Logger.Info("Event Table context expired.");

        if (this._contextManager != null)
        {
            this._contextManager.Dispose();
            this._contextManager.ReloadEvents -= this.ContextManager_ReloadEvents;
            this._contextManager = null;
        }

        this._eventTableContext = null;
        this._eventTableContextHandle = null;
    }

    protected override void Unload()
    {
        this.Logger.Debug("Unload module.");

        this.Logger.Debug("Unload events.");

        using (this._eventCategoryLock.Lock())
        {
            foreach (EventCategory ec in this._eventCategories)
            {
                ec.Events.ForEach(ev => this.RemoveEventHooks(ev));
            }

            this._eventCategories?.Clear();
        }

        if (this.DynamicEventHandler != null)
        {
            this.DynamicEventHandler.FoundLostEntities -= this.DynamicEventHandler_FoundLostEntities;
            this.DynamicEventHandler.Dispose();
            this.DynamicEventHandler = null;
        }

        if (this.BlishHUDAPIService != null)
        {
            this.BlishHUDAPIService.NewLogin -= this.BlishHUDAPIService_NewLogin;
            this.BlishHUDAPIService.LoggedOut -= this.BlishHUDAPIService_LoggedOut;
        }

        this.ModuleSettings.IncludeSelfHostedEvents.SettingChanged -= this.IncludeSelfHostedEvents_SettingChanged;

        this.Logger.Debug("Unloaded events.");

        this.UnloadContext();

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