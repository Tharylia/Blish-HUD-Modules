namespace Estreya.BlishHUD.EventTable.Managers;

using Blish_HUD;
using Estreya.BlishHUD.EventTable.Contexts;
using Estreya.BlishHUD.Shared.Contexts;
using Estreya.BlishHUD.EventTable.Models;
using Estreya.BlishHUD.Shared.Utils;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Estreya.BlishHUD.EventTable.Services;
using Estreya.BlishHUD.EventTable.Controls;
using Estreya.BlishHUD.Shared.Services;
using Estreya.BlishHUD.Shared.Threading.Events;
using Gw2Sharp.WebApi.V2.Models;
using Estreya.BlishHUD.Shared.Services.Audio;

public class ContextManager : IDisposable, IUpdatable
{
    private static Logger _logger = Logger.GetLogger<ContextManager>();

    private List<EventCategory> _temporaryEventCategories = new List<EventCategory>();

    private AsyncLock _eventLock = new AsyncLock();
    private EventTableContext _context;
    private readonly ModuleSettings _moduleSettings;
    private readonly DynamicEventService _dynamicEventService;
    private readonly IconService _iconService;
    private readonly EventStateService _eventStateService;
    private readonly AudioService _audioService;
    private readonly Func<Task<IEnumerable<Models.Event>>> _getEvents;

    public event AsyncEventHandler ReloadEvents;

    public ContextManager(EventTableContext context, ModuleSettings moduleSettings, DynamicEventService dynamicEventService, IconService iconService, EventStateService eventStateService,
        AudioService audioService, Func<Task<IEnumerable<Models.Event>>> getEvents)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));
        if (moduleSettings is null) throw new ArgumentNullException(nameof(moduleSettings));
        if (dynamicEventService is null) throw new ArgumentNullException(nameof(dynamicEventService));
        if (iconService is null) throw new ArgumentNullException(nameof(iconService));

        this._context = context;
        this._moduleSettings = moduleSettings;
        this._dynamicEventService = dynamicEventService;
        this._iconService = iconService;
        this._eventStateService = eventStateService;
        this._audioService = audioService;
        this._getEvents = getEvents;
        this._context.RequestAddCategory += this.RequestAddCategory;
        this._context.RequestAddEvent += this.RequestAddEvent;
        this._context.RequestRemoveCategory += this.RequestRemoveCategory;
        this._context.RequestRemoveEvent += this.RequestRemoveEvent;
        this._context.RequestReloadEvents += this.RequestReloadEvents;
        this._context.RequestShowReminder += this.RequestShowReminder;
        this._context.RequestAddDynamicEvent += this.RequestAddDynamicEvent;
        this._context.RequestRemoveDynamicEvent += this.RequestRemoveDynamicEvent;
        this._context.RequestEventSettingKeys += this.RequestEventSettingKeys;
        this._context.RequestAreaNames += this.RequestAreaNames;
        this._context.RequestAddEventState += this.RequestAddEventState;
        this._context.RequestRemoveEventState += this.RequestRemoveEventState;
    }

    public List<EventCategory> GetContextCategories()
    {
        using (this._eventLock.Lock())
        {
            return this._temporaryEventCategories;
        }
    }

    private Task RequestRemoveEventState(object sender, ContextEventArgs<RemoveEventState> e)
    {
        this._eventStateService.Remove(e.Content.AreaName, e.Content.EventKey);
        return Task.CompletedTask;
    }

    private Task RequestAddEventState(object sender, ContextEventArgs<AddEventState> e)
    {
        this._eventStateService.Add(e.Content.AreaName, e.Content.EventKey, e.Content.Until, e.Content.State);
        return Task.CompletedTask;
    }

    private async Task<IEnumerable<string>> RequestEventSettingKeys(object sender, ContextEventArgs e)
    {
        if (this._getEvents is null) throw new ArgumentNullException(nameof(this._getEvents), "Method to get events is null.");

        var events = await this._getEvents();

        return events is null ? Enumerable.Empty<string>() : events.Select(e => e.SettingKey);
    }

    private Task<IEnumerable<string>> RequestAreaNames(object sender, ContextEventArgs e)
    {
        var areaNames = this._moduleSettings.EventAreaNames.Value;

        return Task.FromResult(areaNames is null ? Enumerable.Empty<string>() : areaNames);
    }

    private async Task RequestRemoveDynamicEvent(object sender, ContextEventArgs<Guid> e)
    {
        await this._dynamicEventService.RemoveCustomEvent(e.Content.ToString());
        await this._dynamicEventService.NotifyCustomEventsUpdated();
    }

    private async Task RequestAddDynamicEvent(object sender, ContextEventArgs<AddDynamicEvent> e)
    {
        var eArgsContent = e.Content;
        await this._dynamicEventService.AddCustomEvent(new DynamicEvent()
        {
            ID = eArgsContent.Id.ToString(),
            Name = eArgsContent.Name,
            MapId = eArgsContent.MapId,
            ColorCode = eArgsContent.ColorCode,
            Flags = eArgsContent.Flags,
            Icon = !eArgsContent.Icon.HasValue ? null : new DynamicEvent.DynamicEventIcon()
            {
                FileID = eArgsContent.Icon.Value.FileID,
                Signature = eArgsContent.Icon.Value.Signature,
            },
            Level = eArgsContent.Level,
            Location = !eArgsContent.Location.HasValue ? null : new DynamicEvent.DynamicEventLocation()
            {
                Center = eArgsContent.Location.Value.Center,
                Height = eArgsContent.Location.Value.Height,
                Points = eArgsContent.Location.Value.Points,
                Radius = eArgsContent.Location.Value.Radius,
                Rotation = eArgsContent.Location.Value.Rotation,
                Type = eArgsContent.Location.Value.Type,
                ZRange = eArgsContent.Location.Value.ZRange
            }
        });

        await this._dynamicEventService.NotifyCustomEventsUpdated();
    }

    private async Task RequestShowReminder(object sender, ContextEventArgs<ShowReminder> e)
    {
        ShowReminder eArgsContent = e.Content;
        var icon = !string.IsNullOrWhiteSpace(eArgsContent.Icon) ? this._iconService.GetIcon(eArgsContent.Icon) : null;

        if (this._moduleSettings.ReminderType.Value is Models.Reminders.ReminderType.Control or Models.Reminders.ReminderType.Both)
        {
            _ = EventNotification.ShowAsControl(null, eArgsContent.Title, eArgsContent.Message, icon, this._iconService, this._moduleSettings);
            await EventNotification.PlaySound(this._audioService);
        }

        if (this._moduleSettings.ReminderType.Value is Models.Reminders.ReminderType.Windows or Models.Reminders.ReminderType.Both)
        {
#if !WINE
            await EventNotification.ShowAsWindowsNotification(eArgsContent.Title, eArgsContent.Message, icon);
#else
            Shared.Controls.ScreenNotification.ShowNotification("OS Notifications are not supported in WINE", Shared.Controls.ScreenNotification.NotificationType.Error, duration: 5);
#endif
        }
    }

    private async Task RequestReloadEvents(object sender, ContextEventArgs e)
    {
        await (this.ReloadEvents?.Invoke(this) ?? Task.FromException(new NotImplementedException()));
    }

    private async Task RequestRemoveEvent(object sender, ContextEventArgs<RemoveEvent> e)
    {
        RemoveEvent eArgsContent = e.Content;
        using (await this._eventLock.LockAsync())
        {
            var category = this._temporaryEventCategories.FirstOrDefault(ec => ec.Key == eArgsContent.CategoryKey)
                ?? throw new ArgumentException($"Category with key \"{eArgsContent.CategoryKey}\" does not exist.");

            if (!category.Events.Any(ev => ev.Key == eArgsContent.EventKey)) throw new ArgumentException($"Event with the key \"{eArgsContent.EventKey}\" does not exist.");

            category.UpdateOriginalEvents(category.OriginalEvents.Where(ev => ev.Key != eArgsContent.EventKey).ToList());
            category.UpdateFillers(category.FillerEvents.Where(ev => ev.Key != eArgsContent.EventKey).ToList());

            _logger.Info($"Event \"{eArgsContent.EventKey}\" of category \"{eArgsContent.CategoryKey}\" was removed via context.");
        }
    }

    private async Task RequestRemoveCategory(object sender, ContextEventArgs<string> e)
    {
        using (await this._eventLock.LockAsync())
        {
            var category = this._temporaryEventCategories.FirstOrDefault(ec => ec.Key == e.Content)
                ?? throw new ArgumentException($"Category with key \"{e.Content}\" does not exist.");

            this._temporaryEventCategories.Remove(category);

            _logger.Info($"Category \"{category.Name}\" ({category.Key}) was removed via context.");
        }
    }

    private async Task RequestAddEvent(object sender, ContextEventArgs<AddEvent> e)
    {
        AddEvent eArgsContent = e.Content;
        using (await this._eventLock.LockAsync())
        {
            var category = this._temporaryEventCategories.FirstOrDefault(ec => ec.Key == eArgsContent.CategoryKey)
                ?? throw new ArgumentException($"Category with key \"{eArgsContent.Key}\" does not exist.");

            if (category.Events.Any(ev => ev.Key == eArgsContent.Key)) throw new ArgumentException($"Event with the key \"{eArgsContent.Key}\" already exists.");

            var newEvent = new Models.Event()
            {
                Key = eArgsContent.Key,
                Name = eArgsContent.Name,
                APICode = eArgsContent.APICode,
                APICodeType = eArgsContent.APICodeType,
                BackgroundColorCode = eArgsContent.BackgroundColorCode,
                BackgroundColorGradientCodes = eArgsContent.BackgroundColorGradientCodes,
                Duration = eArgsContent.Duration,
                Filler = eArgsContent.Filler,
                Icon = eArgsContent.Icon,
                Locations = eArgsContent.Locations,
                MapIds = eArgsContent.MapIds,
                Offset = eArgsContent.Offset,
                Repeat = eArgsContent.Repeat,
                StartingDate = eArgsContent.StartingDate,
                Waypoints = eArgsContent.Waypoints,
                Wiki = eArgsContent.Wiki,
            };

            if (eArgsContent.Occurences != null)
            {
                newEvent.Occurences.AddRange(eArgsContent.Occurences);
            }

            if (eArgsContent.ReminderTimes != null)
            {
                newEvent.UpdateReminderTimes(eArgsContent.ReminderTimes);
            }

            // Event is loaded in LoadEvents

            if (newEvent.Filler)
            {
                category.UpdateFillers(new List<Models.Event>(category.FillerEvents) { newEvent });
            }
            else
            {
                category.UpdateOriginalEvents(new List<Models.Event>(category.OriginalEvents) { newEvent });
            }

            _logger.Info($"Event \"{eArgsContent.Name}\" ({eArgsContent.Key}) of category \"{category.Name}\" ({category.Key}) was registered via context.");
        }
    }

    private async Task RequestAddCategory(object sender, ContextEventArgs<AddCategory> e)
    {
        AddCategory eArgsContent = e.Content;
        using (await this._eventLock.LockAsync())
        {
            if (this._temporaryEventCategories.Any(ec => ec.Key == eArgsContent.Key)) throw new ArgumentException($"Category with key \"{eArgsContent.Key}\" already exists.");

            this._temporaryEventCategories.Add(new EventCategory()
            {
                Key = eArgsContent.Key,
                Name = eArgsContent.Name,
                Icon = eArgsContent.Icon,
                ShowCombined = eArgsContent.ShowCombined,
                FromContext = true
            });

            _logger.Info($"Category \"{eArgsContent.Name}\" ({eArgsContent.Key}) was registered via context.");
        }
    }

    public void Update(GameTime gameTime)
    {

    }

    public void Dispose()
    {
        using (this._eventLock.Lock())
        {
            this._temporaryEventCategories.Clear();
            this._temporaryEventCategories = null;
        }

        if (this._context != null)
        {
            this._context.RequestAddCategory -= this.RequestAddCategory;
            this._context.RequestAddEvent -= this.RequestAddEvent;
            this._context.RequestRemoveCategory -= this.RequestRemoveCategory;
            this._context.RequestRemoveEvent -= this.RequestRemoveEvent;
            this._context.RequestReloadEvents -= this.RequestReloadEvents;
            this._context.RequestShowReminder -= this.RequestShowReminder;
            this._context.RequestAddDynamicEvent -= this.RequestAddDynamicEvent;
            this._context.RequestRemoveDynamicEvent -= this.RequestRemoveDynamicEvent;
            this._context.RequestEventSettingKeys -= this.RequestEventSettingKeys;
            this._context.RequestAreaNames -= this.RequestAreaNames;
            this._context.RequestAddEventState -= this.RequestAddEventState;
            this._context.RequestRemoveEventState -= this.RequestRemoveEventState;
        }

        this._context = null;
    }
}
