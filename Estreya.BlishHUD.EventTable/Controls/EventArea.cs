namespace Estreya.BlishHUD.EventTable.Controls;

using Blish_HUD;
using Blish_HUD._Extensions;
using Blish_HUD.ArcDps.Models;
using Blish_HUD.Controls;
using Estreya.BlishHUD.EventTable.Models;
using Estreya.BlishHUD.EventTable.State;
using Estreya.BlishHUD.Shared.Models;
using Estreya.BlishHUD.Shared.State;
using Estreya.BlishHUD.Shared.Threading;
using Estreya.BlishHUD.Shared.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.BitmapFonts;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Blish_HUD.ContentService;

public class EventArea : Container
{
    private static readonly Logger Logger = Logger.GetLogger<EventArea>();

    private const int EVENT_HEIGHT = 30;

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
    private MapNavigationUtil _mapNavigationUtil;
    private Func<DateTime> _getNowAction;

    private List<EventCategory> _allEvents = new List<EventCategory>();

    private ConcurrentDictionary<string, List<(DateTime Occurence, Event Event)>> _controlEvents = new ConcurrentDictionary<string, List<(DateTime Occurence, Event Event)>>();

    public new bool Enabled => this.Configuration?.Enabled.Value ?? false;

    private bool _clearing = false;

    private double PixelPerMinute
    {
        get
        {
            int pixels = this.Size.X;

            double pixelPerMinute = pixels / this.Configuration.TimeSpan.Value;

            return pixelPerMinute;
        }
    }

    public EventAreaConfiguration Configuration { get; private set; }

    public EventArea(EventAreaConfiguration configuration, IconState iconState, TranslationState translationState, EventState eventState, WorldbossState worldbossState, MapchestState mapchestState, PointOfInterestState pointOfInterestState, MapNavigationUtil mapNavigationUtil, Func<DateTime> getNowAction)
    {
        this.Configuration = configuration;
        this.Configuration.Size.X.SettingChanged += this.Size_SettingChanged;
        this.Configuration.Size.Y.SettingChanged += this.Size_SettingChanged;
        this.Configuration.Location.X.SettingChanged += this.Location_SettingChanged;
        this.Configuration.Location.Y.SettingChanged += this.Location_SettingChanged;
        this.Configuration.Opacity.SettingChanged += this.Opacity_SettingChanged;
        this.Configuration.BackgroundColor.SettingChanged += this.BackgroundColor_SettingChanged;
        this.Configuration.UseFiller.SettingChanged += this.UseFiller_SettingChanged;
        this.Configuration.BuildDirection.SettingChanged += this.BuildDirection_SettingChanged;
        this.Configuration.ActiveEventKeys.SettingChanged += this.ActiveEventKeys_SettingChanged;

        this.Location_SettingChanged(this, null);
        this.Size_SettingChanged(this, null);
        this.Opacity_SettingChanged(this, new ValueChangedEventArgs<float>(0f, this.Configuration.Opacity.Value));
        this.BackgroundColor_SettingChanged(this, new ValueChangedEventArgs<Gw2Sharp.WebApi.V2.Models.Color>(null, this.Configuration.BackgroundColor.Value));

        this._getNowAction = getNowAction;

        this._iconState = iconState;
        this._translationState = translationState;
        this._eventState = eventState;
        this._worldbossState = worldbossState;
        this._mapchestState = mapchestState;
        this._pointOfInterestState = pointOfInterestState;
        this._mapNavigationUtil = mapNavigationUtil;

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

    private void ActiveEventKeys_SettingChanged(object sender, ValueChangedEventArgs<List<string>> e)
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

    public async Task UpdateAllEvents(List<EventCategory> allEvents)
    {
        this._allEvents.Clear();

        this._allEvents.AddRange(JsonConvert.DeserializeObject<List<EventCategory>>(JsonConvert.SerializeObject(allEvents)));

        (DateTime Now, DateTime Min, DateTime Max) times = this.GetTimes();

        await Task.WhenAll(this._allEvents.Select(ec => ec.LoadAsync(this._getNowAction, this._translationState)));

        this.ReAddEvents();
    }

    private void Event_Removed(object sender, string apiCode)
    {
        List<Models.Event> events = this._allEvents.SelectMany(ec => ec.Events).Where(ev => ev.APICode == apiCode).ToList();
        events.ForEach(ev =>
        {
            this._eventState.Remove(this.GetEventKey(ev));
        });
    }

    private void Event_Completed(object sender, string apiCode)
    {
        DateTime now = this._getNowAction().ToUniversalTime();
        DateTime until = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, System.DateTimeKind.Utc).AddDays(1);

        List<Models.Event> events = this._allEvents.SelectMany(ec => ec.Events).Where(ev => ev.APICode == apiCode).ToList();
        events.ForEach(ev =>
        {
            string areaEventKey = this.GetEventKey(ev);
            switch (this.Configuration.CompletionAcion.Value)
            {
                case EventCompletedAction.Crossout:
                    this._eventState.Add(areaEventKey, until, EventState.EventStates.Completed);
                    break;
                case EventCompletedAction.Hide:
                    this._eventState.Add(areaEventKey, until, EventState.EventStates.Hidden);
                    break;
            }
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

    private string GetEventKey(Models.Event ev)
    {
        return $"{this.Configuration.Name}_{ev.SettingKey}";
    }

    private List<IGrouping<string, string>> GetActiveEventKeysGroupedByCategory()
    {
        int i = 0;
        var order = this._allEvents.Where(ec => !this.EventCategoryDisabled(ec)).SelectMany(x =>
        {
            var events = x.Events.Where(ev => !ev.Filler).Select(x => x.SettingKey).Distinct();
            return events;
        }).ToDictionary(x =>
        {
            return x;
        }, x => i++);

        if (order == null || order.Count == 0)
        {
            return new List<IGrouping<string, string>>();
        }

        return this.Configuration.ActiveEventKeys.Value.OrderBy(x => order[x]).GroupBy(aek => aek.Split('_')[0]).ToList();
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
        int halveTimespan = this.Configuration.TimeSpan.Value / 2;
        DateTime min = now.AddMinutes(-halveTimespan);
        DateTime max = now.AddMinutes(halveTimespan);

        return (now, min, max);
    }

    private async Task UpdateEventOccurences()
    {
        (DateTime Now, DateTime Min, DateTime Max) times = this.GetTimes();

        List<Task> tasks = new List<Task>();

        foreach (EventCategory ec in this._allEvents)
        {
            tasks.Add(ec.UpdateEventOccurences(times.Now, times.Min, times.Max, this.Configuration.ActiveEventKeys.Value, (ev) => this.EventDisabled(ev)));
        }

        await Task.WhenAll(tasks);
    }

    private bool EventCategoryDisabled(EventCategory ec)
    {
        var finished = this._eventState?.Contains(ec.Key, EventState.EventStates.Completed) ?? false;

        return finished;
    }

    private bool EventDisabled(Models.Event ev)
    {
        var enabled = this.Configuration.ActiveEventKeys.Value.Contains(ev.SettingKey);

        enabled &= !this._eventState.Contains(GetEventStateKey(ev), EventState.EventStates.Hidden);

        return !enabled;
    }

    private string GetEventStateKey(Models.Event ev)
    {
        return $"{this.Configuration.Name}-{ev.SettingKey}";
    }

    private void UpdateEvents()
    {
        if (_clearing) return;

        (DateTime Now, DateTime Min, DateTime Max) times = this.GetTimes();

        // Update and delete existing
        foreach (List<(DateTime Occurence, Event Event)> controlEventPairs in this._controlEvents.Values)
        {
            var toDelete = new List<(DateTime Occurence, Event Event)>();

            foreach ((DateTime Occurence, Event Event) controlEvent in controlEventPairs)
            {
                int x = (int)Math.Ceiling(controlEvent.Event.Ev.CalculateXPosition(controlEvent.Occurence, times.Min, this.PixelPerMinute));
                int width = (int)Math.Ceiling(controlEvent.Event.Ev.CalculateWidth(controlEvent.Occurence, times.Min, this.Width, this.PixelPerMinute));

                controlEvent.Event.Left = x < 0 ? 0 : x;
                controlEvent.Event.Width = width;

                if (width <= 0)
                {
                    // Control can be deleted
                    toDelete.Add(controlEvent);
                }
            }

            foreach (var delete in toDelete)
            {
                Logger.Debug($"Deleted event {delete.Event.Ev.Name}");
                delete.Event.Dispose();
                controlEventPairs.Remove(delete);
            }
        }

        // Add new
        int y = 0;
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

                    if (x > this.Width || width < 0)
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
                        () => this._eventState.Contains(ev.SettingKey, EventState.EventStates.Completed),
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
                            return new Color(colorFromEvent.R, colorFromEvent.G, colorFromEvent.B);
                        })
                    {
                        Parent = this,
                        Top = y,
                        Height = EVENT_HEIGHT,
                        Width = width,
                        Left = x < 0 ? 0 : x
                    };

                    newEventControl.LeftMouseButtonPressed += this.EventControl_LeftMouseButtonPressed;

                    Logger.Debug($"Added event {ev.Name} with occurence {occurence}");

                    _controlEvents[categoryKey].Add((occurence, newEventControl));
                }

                renderedAny = true;

            }

            if (renderedAny)
            {
                y += EVENT_HEIGHT; // TODO: Make setting
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
                    var result = await (_mapNavigationUtil?.NavigateToPosition(poi, this.Configuration.AcceptWaypointPrompt.Value) ?? Task.FromResult(new MapNavigationUtil.NavigationResult(false, "Variable null.")));
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
        UpdateUtil.Update(this.UpdateEvents, gameTime, _updateEventInterval.TotalMilliseconds, ref _lastEventUpdate);
    }

    private void ClearEventControls()
    {
        this.Children.ToList().ForEach(child =>
        {
            var eventControl = child as Event;
            eventControl.LeftMouseButtonPressed -= this.EventControl_LeftMouseButtonPressed;
            child.Dispose();
        });

        this.Children.Clear();
        this._controlEvents.Clear();
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
        this._mapNavigationUtil = null;
        this._pointOfInterestState = null;

        this.Configuration.Size.X.SettingChanged -= this.Size_SettingChanged;
        this.Configuration.Size.Y.SettingChanged -= this.Size_SettingChanged;
        this.Configuration.Location.X.SettingChanged -= this.Location_SettingChanged;
        this.Configuration.Location.Y.SettingChanged -= this.Location_SettingChanged;
        this.Configuration.Opacity.SettingChanged -= this.Opacity_SettingChanged;
        this.Configuration.BackgroundColor.SettingChanged -= this.BackgroundColor_SettingChanged;
        this.Configuration.UseFiller.SettingChanged -= this.UseFiller_SettingChanged;
        this.Configuration.BuildDirection.SettingChanged -= this.BuildDirection_SettingChanged;

        this.Configuration = null;
    }
}
