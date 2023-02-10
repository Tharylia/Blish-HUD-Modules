namespace Estreya.BlishHUD.EventTable.Controls;

using Blish_HUD;
using Blish_HUD._Extensions;
using Blish_HUD.ArcDps.Models;
using Blish_HUD.Controls;
using Estreya.BlishHUD.EventTable.Models;
using Estreya.BlishHUD.EventTable.State;
using Estreya.BlishHUD.Shared.Extensions;
using Estreya.BlishHUD.Shared.Models;
using Estreya.BlishHUD.Shared.State;
using Estreya.BlishHUD.Shared.Threading;
using Estreya.BlishHUD.Shared.Utils;
using Flurl.Http;
using Gw2Sharp.WebApi.Http;
using Humanizer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.BitmapFonts;
using Newtonsoft.Json;
using SemVer;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Blish_HUD.ContentService;

public class EventArea : Container
{
    private static readonly Logger Logger = Logger.GetLogger<EventArea>();

    private static TimeSpan _updateEventOccurencesInterval = TimeSpan.FromMinutes(15);
    private AsyncRef<double> _lastEventOccurencesUpdate = new AsyncRef<double>(0);

    private static TimeSpan _updateEventInterval = TimeSpan.FromMilliseconds(250);
    private double _lastEventUpdate = 0;

    private static readonly ConcurrentDictionary<FontSize, BitmapFont> _fonts = new ConcurrentDictionary<FontSize, BitmapFont>();
    private IconState _iconState;
    private TranslationState _translationState;
    private EventState _eventState;
    private WorldbossState _worldbossState;
    private MapchestState _mapchestState;
    private PointOfInterestState _pointOfInterestState;
    private MapUtil _mapUtil;
    private IFlurlClient _flurlClient;
    private string _apiRootUrl;
    private Func<DateTime> _getNowAction;
    private readonly Func<SemVer.Version> _getVersion;
    private List<EventCategory> _allEvents = new List<EventCategory>();

    private ConcurrentDictionary<string, List<(DateTime Occurence, Event Event)>> _controlEvents = new ConcurrentDictionary<string, List<(DateTime Occurence, Event Event)>>();

    public new bool Enabled => this.Configuration?.Enabled.Value ?? false;

    private bool _clearing = false;

    private double PixelPerMinute
    {
        get
        {
            int pixels = this.Size.X;

            double pixelPerMinute = pixels / (double)this.Configuration.TimeSpan.Value;

            return pixelPerMinute;
        }
    }

    public EventAreaConfiguration Configuration { get; private set; }

    public EventArea(EventAreaConfiguration configuration, IconState iconState, TranslationState translationState, EventState eventState, WorldbossState worldbossState, MapchestState mapchestState, PointOfInterestState pointOfInterestState, MapUtil mapUtil, IFlurlClient flurlClient, string apiRootUrl, Func<DateTime> getNowAction, Func<SemVer.Version> getVersion)
    {
        this.Configuration = configuration;

        this.Configuration.EnabledKeybinding.Value.Activated += this.EnabledKeybinding_Activated;
        this.Configuration.Size.X.SettingChanged += this.Size_SettingChanged;
        this.Configuration.Size.Y.SettingChanged += this.Size_SettingChanged;
        this.Configuration.Location.X.SettingChanged += this.Location_SettingChanged;
        this.Configuration.Location.Y.SettingChanged += this.Location_SettingChanged;
        this.Configuration.TimeSpan.SettingChanged += this.TimeSpan_SettingChanged;
        this.Configuration.Opacity.SettingChanged += this.Opacity_SettingChanged;
        this.Configuration.BackgroundColor.SettingChanged += this.BackgroundColor_SettingChanged;
        this.Configuration.UseFiller.SettingChanged += this.UseFiller_SettingChanged;
        this.Configuration.BuildDirection.SettingChanged += this.BuildDirection_SettingChanged;
        this.Configuration.DisabledEventKeys.SettingChanged += this.DisabledEventKeys_SettingChanged;
        this.Configuration.EventOrder.SettingChanged += this.EventOrder_SettingChanged;

        this.Location_SettingChanged(this, null);
        this.Size_SettingChanged(this, null);
        this.Opacity_SettingChanged(this, new ValueChangedEventArgs<float>(0f, this.Configuration.Opacity.Value));
        this.BackgroundColor_SettingChanged(this, new ValueChangedEventArgs<Gw2Sharp.WebApi.V2.Models.Color>(null, this.Configuration.BackgroundColor.Value));

        this._getNowAction = getNowAction;
        this._getVersion = getVersion;
        this._iconState = iconState;
        this._translationState = translationState;
        this._eventState = eventState;
        this._worldbossState = worldbossState;
        this._mapchestState = mapchestState;
        this._pointOfInterestState = pointOfInterestState;
        this._mapUtil = mapUtil;
        this._flurlClient = flurlClient;
        this._apiRootUrl = apiRootUrl;

        //this._eventState.
        if (this._worldbossState != null)
        {
            this._worldbossState.WorldbossCompleted += this.Event_Completed;
            this._worldbossState.WorldbossRemoved += this.Event_Removed;
        }

        if (this._mapchestState != null)
        {
            this._mapchestState.MapchestCompleted += this.Event_Completed;
            this._mapchestState.MapchestRemoved += this.Event_Removed;
        }
    }

    private void TimeSpan_SettingChanged(object sender, ValueChangedEventArgs<int> e)
    {
        this.ReAddEvents();
    }

    private void EventOrder_SettingChanged(object sender, ValueChangedEventArgs<List<string>> e)
    {
        this.ReAddEvents();
    }

    private void EnabledKeybinding_Activated(object sender, EventArgs e)
    {
        this.Configuration.Enabled.Value = !this.Configuration.Enabled.Value;
    }

    private void DisabledEventKeys_SettingChanged(object sender, ValueChangedEventArgs<List<string>> e)
    {
        this.ReAddEvents();
    }

    private void BuildDirection_SettingChanged(object sender, ValueChangedEventArgs<BuildDirection> e)
    {
        this.Location_SettingChanged(this, null);
    }

    private void UseFiller_SettingChanged(object sender, ValueChangedEventArgs<bool> e)
    {
        this.ReAddEvents();
    }

    public void UpdateAllEvents(List<EventCategory> allEvents)
    {
        this._allEvents.Clear();

        this._allEvents.AddRange(JsonConvert.DeserializeObject<List<EventCategory>>(JsonConvert.SerializeObject(allEvents)));

        (DateTime Now, DateTime Min, DateTime Max) times = this.GetTimes();

        this._allEvents.ForEach(ec => ec.Load(this._translationState));

        // Events should have occurences calculated already

        this.ReAddEvents();
    }

    private void Event_Removed(object sender, string apiCode)
    {
        List<Models.Event> events = this._allEvents.SelectMany(ec => ec.Events).Where(ev => ev.APICode == apiCode).ToList();
        events.ForEach(ev =>
        {
            this._eventState.Remove(this.Configuration.Name, ev.SettingKey);
        });
    }

    private void Event_Completed(object sender, string apiCode)
    {
        var until = this.GetNextReset();
        List<Models.Event> events = this._allEvents.SelectMany(ec => ec.Events).Where(ev => ev.APICode == apiCode).ToList();
        events.ForEach(ev =>
        {
            this.FinishEvent(ev, until);
        });
    }

    private void BackgroundColor_SettingChanged(object sender, ValueChangedEventArgs<Gw2Sharp.WebApi.V2.Models.Color> e)
    {
        Color backgroundColor = Color.Transparent;

        if (e.NewValue != null && e.NewValue.Id != 1)
        {
            backgroundColor = e.NewValue.Cloth.ToXnaColor();
        }

        this.BackgroundColor = backgroundColor * this.Configuration.Opacity.Value;
    }

    private void ReportNewHeight(int height)
    {
        var oldHeight = this.Height;

        if (oldHeight != height)
        {
            this.Height = height;
            this.Configuration.Size.Y.Value = height; // Update setting to correct setting views
            this.Location_SettingChanged(this, null);
        }
    }

    private void Opacity_SettingChanged(object sender, ValueChangedEventArgs<float> e)
    {
        this.BackgroundColor_SettingChanged(this, new ValueChangedEventArgs<Gw2Sharp.WebApi.V2.Models.Color>(null, this.Configuration.BackgroundColor.Value));
    }

    private void Location_SettingChanged(object sender, ValueChangedEventArgs<int> e)
    {
        bool buildFromBottom = this.Configuration.BuildDirection.Value == BuildDirection.Bottom;

        this.Location = buildFromBottom
            ? new Point(this.Configuration.Location.X.Value, this.Configuration.Location.Y.Value - this.Height)
            : new Point(this.Configuration.Location.X.Value, this.Configuration.Location.Y.Value);
    }

    private void Size_SettingChanged(object sender, ValueChangedEventArgs<int> e)
    {
        this.Size = new Point(this.Configuration.Size.X.Value, this.Height);
    }

    protected override CaptureType CapturesInput()
    {
        return CaptureType.None;
    }

    private List<IGrouping<string, string>> GetActiveEventKeysGroupedByCategory()
    {
        var activeSettingKeys = this.GetActiveEventKeys();
        var order = this.GetEventCategoryOrdering();

        return activeSettingKeys.OrderBy(x => order.IndexOf(x.Split('_')[0])).GroupBy(aek => aek.Split('_')[0]).ToList();
    }

    private List<string> GetEventCategoryOrdering()
    {
        return this.Configuration.EventOrder.Value.ToList();
    }

    private List<string> GetActiveEventKeys()
    {
        var activeSettingKeys = this._allEvents.SelectMany(ae => ae.Events).Where(e => !e.Filler).Select(e => e.SettingKey).Where(sk => !this.Configuration.DisabledEventKeys.Value.Contains(sk));

        return activeSettingKeys.ToList();
    }

    private void ReAddEvents()
    {
        this._clearing = true;
        using IDisposable suspendCtx = this.SuspendLayoutContext();

        this.ClearEventControls();

        _lastEventOccurencesUpdate.Value = _updateEventOccurencesInterval.TotalMilliseconds;
        _lastEventUpdate = _updateEventInterval.TotalMilliseconds;
        this._clearing = false;
    }

    public override void PaintAfterChildren(SpriteBatch spriteBatch, Rectangle bounds)
    {
        float middleLineX = this.Width / 2;
        float width = 2;
        spriteBatch.DrawLineOnCtrl(this, ContentService.Textures.Pixel, new RectangleF(middleLineX - (width / 2), 0, width, this.Height), Color.LightGray);
    }

    private (DateTime Now, DateTime Min, DateTime Max) GetTimes()
    {
        DateTime now = this._getNowAction();
        double halveTimespan = this.Configuration.TimeSpan.Value / 2;
        DateTime min = now.AddMinutes(-halveTimespan);
        DateTime max = now.AddMinutes(halveTimespan);

        return (now, min, max);
    }

    private async Task UpdateEventOccurences()
    {
        (DateTime Now, DateTime Min, DateTime Max) times = this.GetTimes();

        List<Task> tasks = new List<Task>();

        var activeEventKeys = this.GetActiveEventKeys();

        var fillers = await this.GetFillers(times.Now, times.Min, times.Max, activeEventKeys.Where(ev => !this.EventDisabled(ev)).ToList());
        foreach (EventCategory ec in this._allEvents)
        {
                if (fillers.TryGetValue(ec.Key, out var categoryFillers))
                {
                categoryFillers.ForEach(cf => cf.Load(ec, this._translationState));
                }

                ec.UpdateFillers(categoryFillers);
                //await ec.UpdateEventOccurences(categoryFillers, times.Now, times.Min, times.Max, this.Configuration.ActiveEventKeys.Value, (ev) => this.EventDisabled(ev));
        }
    }

    private async Task<ConcurrentDictionary<string, List<Models.Event>>> GetFillers(DateTime now, DateTime min, DateTime max, List<string> activeEventKeys)
    {
        try
        {
            if (activeEventKeys == null || activeEventKeys.Count == 0)
            {
                return new ConcurrentDictionary<string, List<Models.Event>>();
            }

            var flurlRequest = this._flurlClient.Request(this._apiRootUrl, "fillers");

            List<Models.Event> activeEvents = this._allEvents.SelectMany(a => a.Events).Where(ev => activeEventKeys.Any(aeg => aeg == ev.SettingKey)).ToList();

            var response = await flurlRequest.PostJsonAsync(new OnlineFillerRequest()
            {
                Module = new OnlineFillerRequest.OnlineFillerRequestModule()
                {
                    Version = this._getVersion().ToString(),
                },
                Times = new OnlineFillerRequest.OnlineFillerRequestTimes()
                {
                    Now_UTC_ISO = now.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'"),
                    Min_UTC_ISO = min.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'"),
                    Max_UTC_ISO = max.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'"),
                },
                EventKeys = activeEvents.Select(a => a.SettingKey).ToArray()
            });

            var fillers = await response.GetJsonAsync<OnlineFillerCategory[]>();

            var fillerList = fillers.ToList();
            var parsedFillers = new ConcurrentDictionary<string, List<Models.Event>>();
            for (int i = 0; i < fillerList.Count; i++)
            {
                var currentCategory = fillerList[i];

                foreach (var fillerItem in currentCategory.Fillers)
                {
                    Models.Event filler = new Models.Event()
                    {
                        Name = $"{fillerItem.Name}",
                        Duration = fillerItem.Duration,
                        Filler = true
                    };

                    fillerItem.Occurences.ToList().ForEach(o => filler.Occurences.Add(/*DateTime.SpecifyKind(o.DateTime, DateTimeKind.Utc).ToLocalTime()*/ o.UtcDateTime));

                    var prevFillers = parsedFillers.GetOrAdd(currentCategory.Key, (key) => new List<Models.Event>() { filler });
                    prevFillers.Add(filler);
                }
            }

            return parsedFillers;
        }
        catch (FlurlHttpException ex)
        {
            var error = await ex.GetResponseStringAsync();
            Logger.Warn($"Could not load fillers from {ex.Call.Request.RequestUri}: {error}");
        }

        return new ConcurrentDictionary<string, List<Models.Event>>();
    }

    private bool EventCategoryDisabled(EventCategory ec)
    {
        var finished = this._eventState?.Contains(this.Configuration.Name, ec.Key, EventState.EventStates.Completed) ?? false;

        return finished;
    }

    private bool EventDisabled(Models.Event ev)
    {
        return !ev.Filler && this.EventDisabled(ev.SettingKey);
    }

    private bool EventDisabled(string settingKey)
    {
        var enabled = !this.Configuration.DisabledEventKeys.Value.Contains(settingKey);

        enabled &= !this._eventState.Contains(this.Configuration.Name, settingKey, EventState.EventStates.Hidden);

        return !enabled;
    }

    private void UpdateEventsOnScreen()
    {
        if (_clearing) return;

        (DateTime Now, DateTime Min, DateTime Max) times = this.GetTimes();

        // Update and delete existing
        int y = 0;
        var order = this.GetEventCategoryOrdering();
        var oderedControlEvents = this._controlEvents.OrderBy(x => order.IndexOf(x.Key)).Select(x => x.Value).ToList();
        foreach (List<(DateTime Occurence, Event Event)> controlEventPairs in oderedControlEvents)
        {
            var toDelete = new List<(DateTime Occurence, Event Event)>();

            foreach ((DateTime Occurence, Event Event) controlEvent in controlEventPairs)
            {
                bool disabled = this.EventDisabled(controlEvent.Event.Ev);
                if (disabled)
                {
                    // Control can be deleted
                    toDelete.Add(controlEvent);
                    continue;
                }

                int x = (int)Math.Ceiling(controlEvent.Event.Ev.CalculateXPosition(controlEvent.Occurence, times.Min, this.PixelPerMinute));
                int width = (int)Math.Ceiling(controlEvent.Event.Ev.CalculateWidth(controlEvent.Occurence, times.Min, this.Width, this.PixelPerMinute));

                controlEvent.Event.Location = new Point(x < 0 ? 0 : x, y);
                controlEvent.Event.Size = new Point(width, this.Configuration.EventHeight.Value);

                if (width <= 0 || disabled)
                {
                    // Control can be deleted
                    toDelete.Add(controlEvent);
                }
            }

            foreach (var delete in toDelete)
            {
                Logger.Debug($"Deleted event {delete.Event.Ev.Name}");
                this.RemoveEventHooks(delete.Event);
                delete.Event.Dispose();
                controlEventPairs.Remove(delete);
            }

            y += this.Configuration.EventHeight.Value;
        }

        // Add new
        y = 0;
        foreach (var activeEventGroup in this.GetActiveEventKeysGroupedByCategory())
        {
            string categoryKey = activeEventGroup.Key;
            EventCategory validCategory = this._allEvents.Find(ec => ec.Key == categoryKey);

            bool renderedAny = false;

            //eventKey == Event.SettingsKey
            List<Models.Event> events = validCategory.Events.Where(ev => activeEventGroup.Any(aeg => aeg == ev.SettingKey) || (this.Configuration.UseFiller.Value && ev.Filler)).ToList();
            if (events.Count == 0)
            {
                continue;
            }

            _ = _controlEvents.TryAdd(categoryKey, new List<(DateTime Occurence, Event Event)>());

            IEnumerable<Models.Event> validEvents = events.Where(ev => ev.Occurences.Any(oc => oc.AddMinutes(ev.Duration) >= times.Min && oc <= times.Max));

            foreach (Models.Event ev in validEvents)
            {
                if (this.EventDisabled(ev))
                {
                    continue;
                }

                IEnumerable<DateTime> validOccurences = ev.Occurences.Where(oc => oc.AddMinutes(ev.Duration) >= times.Min && oc <= times.Max);
                foreach (DateTime occurence in validOccurences)
                {
                    // Check if we got this occurence already added
                    if (_controlEvents[categoryKey].Any(addedEvent => addedEvent.Occurence == occurence))
                    {
                        continue;
                    }

                    int x = (int)Math.Ceiling(ev.CalculateXPosition(occurence, times.Min, this.PixelPerMinute));
                    int width = (int)Math.Ceiling(ev.CalculateWidth(occurence, times.Min, this.Width, this.PixelPerMinute));

                    if (x > this.Width || width <= 0)
                    {
                        continue;
                    }

                    var newEventControl = new Event(ev,
                        this._iconState,
                        this._translationState,
                        this._getNowAction,
                        occurence,
                        occurence.AddMinutes(ev.Duration),
                        () => _fonts.GetOrAdd(this.Configuration.FontSize.Value, fontSize => GameService.Content.GetFont(FontFace.Menomonia, fontSize, FontStyle.Regular)),
                        () => !ev.Filler && this.Configuration.DrawBorders.Value,
                        () => this._eventState.Contains(this.Configuration.Name, ev.SettingKey, EventState.EventStates.Completed),
                        () =>
                        {
                            var defaultTextColor = Color.Black;

                            return ev.Filler
                                ? this.Configuration.FillerTextColor.Value.Id == 1 ? defaultTextColor : this.Configuration.FillerTextColor.Value.Cloth.ToXnaColor()
                                : this.Configuration.TextColor.Value.Id == 1 ? defaultTextColor : this.Configuration.TextColor.Value.Cloth.ToXnaColor();
                        },
                        () =>
                        {
                            if (ev.Filler)
                            {
                                return Color.Transparent;
                            }

                            System.Drawing.Color colorFromEvent = string.IsNullOrWhiteSpace(ev.BackgroundColorCode) ? System.Drawing.Color.White : System.Drawing.ColorTranslator.FromHtml(ev.BackgroundColorCode);
                            return new Color(colorFromEvent.R, colorFromEvent.G, colorFromEvent.B) * this.Configuration.EventOpacity.Value;
                        })
                    {
                        Parent = this,
                        Top = y,
                        Height = this.Configuration.EventHeight.Value,
                        Width = width,
                        Left = x < 0 ? 0 : x
                    };

                    this.AddEventHooks(newEventControl);

                    Logger.Debug($"Added event {ev.Name} with occurence {occurence}");

                    _controlEvents[categoryKey].Add((occurence, newEventControl));
                }

                renderedAny = true;

            }

            if (renderedAny)
            {
                y += this.Configuration.EventHeight.Value; // TODO: Make setting
            }
        }

        this.ReportNewHeight(y);
    }

    private void EventControl_LeftMouseButtonPressed(object sender, Blish_HUD.Input.MouseEventArgs e)
    {
        var eventControl = sender as Event;

        switch (this.Configuration.LeftClickAction.Value)
        {
            case LeftClickAction.CopyWaypoint:
                if (!string.IsNullOrWhiteSpace(eventControl.Ev.Waypoint))
                {
                    ClipboardUtil.WindowsClipboardService.SetTextAsync(eventControl.Ev.Waypoint);
                    Shared.Controls.ScreenNotification.ShowNotification(new string[]
                    {
                        eventControl.Ev.Name,
                        "Copied to clipboard!"
                    });
                }

                break;
            case LeftClickAction.NavigateToWaypoint:
                if (string.IsNullOrWhiteSpace(eventControl.Ev.Waypoint))
                {
                    return;
                }

                if (_pointOfInterestState.Loading)
                {
                    Shared.Controls.ScreenNotification.ShowNotification($"{nameof(PointOfInterestState)} is still loading!", Shared.Controls.ScreenNotification.NotificationType.Error);
                    return;
                }

                var poi = this._pointOfInterestState.GetPointOfInterest(eventControl.Ev.Waypoint);
                if (poi == null)
                {
                    Shared.Controls.ScreenNotification.ShowNotification($"{eventControl.Ev.Waypoint} not found!", Shared.Controls.ScreenNotification.NotificationType.Error);
                    return;
                }

                _ = Task.Run(async () =>
                {
                    var result = await (_mapUtil?.NavigateToPosition(poi, this.Configuration.AcceptWaypointPrompt.Value) ?? Task.FromResult(new MapUtil.NavigationResult(false, "Variable null.")));
                    if (!result.Success)
                    {
                        Shared.Controls.ScreenNotification.ShowNotification($"Navigation failed: {result.Message ?? "Unknown"}", Shared.Controls.ScreenNotification.NotificationType.Error);
                    }
                });

                break;
        }
    }

    public override void UpdateContainer(GameTime gameTime)
    {
        _ = UpdateUtil.UpdateAsync(this.UpdateEventOccurences, gameTime, _updateEventOccurencesInterval.TotalMilliseconds, this._lastEventOccurencesUpdate);
        UpdateUtil.Update(this.UpdateEventsOnScreen, gameTime, _updateEventInterval.TotalMilliseconds, ref _lastEventUpdate);
    }

    private void ClearEventControls()
    {
        this.Children.ToList().ForEach(child =>
        {
            var eventControl = child as Event;
            this.RemoveEventHooks(eventControl);
            child.Dispose();
        });

        this._allEvents.ForEach(a => a.UpdateFillers(new List<Models.Event>()));

        this.Children.Clear();
        this._controlEvents.Clear();
    }

    private void AddEventHooks(Event ev)
    {
        ev.LeftMouseButtonPressed += this.EventControl_LeftMouseButtonPressed;
        ev.HideRequested += this.Ev_HideRequested;
        ev.FinishRequested += this.Ev_FinishRequested;
        ev.DisableRequested += this.Ev_DisableRequested;
    }

    private void RemoveEventHooks(Event ev)
    {
        ev.LeftMouseButtonPressed -= this.EventControl_LeftMouseButtonPressed;
        ev.HideRequested -= this.Ev_HideRequested;
        ev.FinishRequested -= this.Ev_FinishRequested;
        ev.DisableRequested -= this.Ev_DisableRequested;
    }

    private void Ev_FinishRequested(object sender, EventArgs e)
    {
        var ev = sender as Event;
        this.FinishEvent(ev.Ev, this.GetNextReset());
    }

    private void FinishEvent(Models.Event ev, DateTime until)
    {
        switch (this.Configuration.CompletionAcion.Value)
        {
            case EventCompletedAction.Crossout:
                this._eventState.Add(this.Configuration.Name, ev.SettingKey, until, EventState.EventStates.Completed);
                break;
            case EventCompletedAction.Hide:
                this.HideEvent(ev, until);
                break;
        }
    }

    private void HideEvent(Models.Event ev, DateTime until)
    {
        this._eventState.Add(this.Configuration.Name, ev.SettingKey, until, EventState.EventStates.Hidden);
        this.ReAddEvents();
    }

    private void Ev_HideRequested(object sender, EventArgs e)
    {
        var ev = sender as Event;
        this.HideEvent(ev.Ev, this.GetNextReset());
    }

    private void Ev_DisableRequested(object sender, EventArgs e)
    {
        var ev = sender as Event;
        if (!this.Configuration.DisabledEventKeys.Value.Contains(ev.Ev.SettingKey))
        {
            this.Configuration.DisabledEventKeys.Value = new List<string>(this.Configuration.DisabledEventKeys.Value) { ev.Ev.SettingKey };
        }
    }

    private DateTime GetNextReset()
    {
        var nowUTC = this._getNowAction().ToUniversalTime();

        return new DateTime(nowUTC.Year, nowUTC.Month, nowUTC.Day, 0, 0, 0, DateTimeKind.Utc).AddDays(1);
    }

    protected override void DisposeControl()
    {
        this.ClearEventControls();

        if (this._worldbossState != null)
        {
            this._worldbossState.WorldbossCompleted -= this.Event_Completed;
            this._worldbossState.WorldbossRemoved -= this.Event_Removed;
        }

        if (this._mapchestState != null)
        {
            this._mapchestState.MapchestCompleted -= this.Event_Completed;
            this._mapchestState.MapchestRemoved -= this.Event_Removed;
        }

        this._iconState = null;
        this._worldbossState = null;
        this._mapchestState = null;
        this._eventState = null;
        this._mapUtil = null;
        this._pointOfInterestState = null;

        this._flurlClient = null;
        this._apiRootUrl = null;

        this.Configuration.EnabledKeybinding.Value.Activated -= this.EnabledKeybinding_Activated;
        this.Configuration.Size.X.SettingChanged -= this.Size_SettingChanged;
        this.Configuration.Size.Y.SettingChanged -= this.Size_SettingChanged;
        this.Configuration.Location.X.SettingChanged -= this.Location_SettingChanged;
        this.Configuration.Location.Y.SettingChanged -= this.Location_SettingChanged;
        this.Configuration.Opacity.SettingChanged -= this.Opacity_SettingChanged;
        this.Configuration.BackgroundColor.SettingChanged -= this.BackgroundColor_SettingChanged;
        this.Configuration.UseFiller.SettingChanged -= this.UseFiller_SettingChanged;
        this.Configuration.BuildDirection.SettingChanged -= this.BuildDirection_SettingChanged;
        this.Configuration.EventOrder.SettingChanged -= this.EventOrder_SettingChanged;

        this.Configuration = null;
    }
}
