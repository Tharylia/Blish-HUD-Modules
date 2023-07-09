namespace Estreya.BlishHUD.EventTable.Controls;

using Blish_HUD;
using Blish_HUD._Extensions;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Flurl.Http;
using Gw2Sharp.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Models;
using MonoGame.Extended.BitmapFonts;
using Newtonsoft.Json;
using Services;
using Shared.Controls;
using Shared.Extensions;
using Shared.Models;
using Shared.Models.GW2API.PointOfInterest;
using Shared.MumbleInfo.Map;
using Shared.Services;
using Shared.Threading;
using Shared.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using static Blish_HUD.ContentService;
using Color = Gw2Sharp.WebApi.V2.Models.Color;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using RectangleF = MonoGame.Extended.RectangleF;
using ScreenNotification = Shared.Controls.ScreenNotification;
using Version = SemVer.Version;

public class EventArea : RenderTarget2DControl
{
    private static readonly Logger Logger = Logger.GetLogger<EventArea>();

    private static TimeSpan _updateEventOccurencesInterval = TimeSpan.FromMinutes(15);

    private static TimeSpan _checkForNewEventsInterval = TimeSpan.FromMilliseconds(1000);

    private static readonly ConcurrentDictionary<FontSize, BitmapFont> _fonts = new ConcurrentDictionary<FontSize, BitmapFont>();
    private readonly Func<string> _getAccessToken;
    private readonly Func<DateTime> _getNowAction;
    private readonly Func<Version> _getVersion;
    private Event _activeEvent;
    private List<EventCategory> _allEvents = new List<EventCategory>();
    private string _apiRootUrl;

    private bool _clearing;
    private readonly ConcurrentDictionary<string, List<(DateTime Occurence, Event Event)>> _controlEvents = new ConcurrentDictionary<string, List<(DateTime Occurence, Event Event)>>();

    private readonly AsyncLock _controlLock = new AsyncLock();

    private int _drawXOffset;

    private List<string> _eventCategoryOrdering;

    private readonly AsyncLock _eventLock = new AsyncLock();
    private EventStateService _eventService;
    private IFlurlClient _flurlClient;

    private int _heightFromLastDraw = 1; // Blish does not render controls at y 0 with 0 height
    private IconService _iconService;

    private Event _lastActiveEvent;
    private double _lastCheckForNewEventsUpdate;
    private readonly AsyncRef<double> _lastEventOccurencesUpdate = new AsyncRef<double>(0);
    private MouseEventType _lastMouseEventType;
    private MapchestService _mapchestService;
    private MapUtil _mapUtil;

    private List<List<(DateTime Occurence, Event Event)>> _orderedControlEvents;
    private PointOfInterestService _pointOfInterestService;
    private TimeSpan? _savedDrawInterval;

    private int _tempHistorySplit = -1;
    private TranslationService _translationService;
    private WorldbossService _worldbossService;

    public EventArea(EventAreaConfiguration configuration, IconService iconService, TranslationService translationService, EventStateService eventService, WorldbossService worldbossService, MapchestService mapchestService, PointOfInterestService pointOfInterestService, MapUtil mapUtil, IFlurlClient flurlClient, string apiRootUrl, Func<DateTime> getNowAction, Func<Version> getVersion, Func<string> getAccessToken)
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
        this.Configuration.DrawInterval.SettingChanged += this.DrawInterval_SettingChanged;
        this.Configuration.LimitToCurrentMap.SettingChanged += this.LimitToCurrentMap_SettingChanged;
        this.Configuration.AllowUnspecifiedMap.SettingChanged += this.AllowUnspecifiedMap_SettingChanged;
        GameService.Gw2Mumble.CurrentMap.MapChanged += this.CurrentMap_MapChanged;

        this.Click += this.OnLeftMouseButtonPressed;
        this.MouseLeft += this.OnMouseLeft;
        this.MouseWheelScrolled += this.OnMouseWheelScrolled;

        this.Location_SettingChanged(this, null);
        this.Size_SettingChanged(this, null);
        this.Opacity_SettingChanged(this, new ValueChangedEventArgs<float>(0f, this.Configuration.Opacity.Value));
        this.BackgroundColor_SettingChanged(this, new ValueChangedEventArgs<Color>(null, this.Configuration.BackgroundColor.Value));
        this.DrawInterval_SettingChanged(this, new ValueChangedEventArgs<DrawInterval>(Models.DrawInterval.INSTANT, this.Configuration.DrawInterval.Value));

        this._getNowAction = getNowAction;
        this._getVersion = getVersion;
        this._getAccessToken = getAccessToken;
        this._iconService = iconService;
        this._translationService = translationService;
        this._eventService = eventService;
        this._worldbossService = worldbossService;
        this._mapchestService = mapchestService;
        this._pointOfInterestService = pointOfInterestService;
        this._mapUtil = mapUtil;
        this._flurlClient = flurlClient;
        this._apiRootUrl = apiRootUrl;

        if (this._worldbossService != null)
        {
            this._worldbossService.WorldbossCompleted += this.Event_Completed;
            this._worldbossService.WorldbossRemoved += this.Event_Removed;
        }

        if (this._mapchestService != null)
        {
            this._mapchestService.MapchestCompleted += this.Event_Completed;
            this._mapchestService.MapchestRemoved += this.Event_Removed;
        }

        if (this._eventService != null)
        {
            this._eventService.StateAdded += this.EventService_ServiceAdded;
            this._eventService.StateRemoved += this.EventService_ServiceRemoved;
        }
    }

    private int DrawXOffset
    {
        get => this.Configuration.ShowCategoryNames.Value ? this._drawXOffset : 0;
        set => this._drawXOffset = value;
    }

    private List<string> EventCategoryOrdering
    {
        get
        {
            this._eventCategoryOrdering ??= this.GetEventCategoryOrdering();

            return this._eventCategoryOrdering;
        }
    }

    private List<List<(DateTime Occurence, Event Event)>> OrderedControlEvents
    {
        get
        {
            List<string> order = this.EventCategoryOrdering;

            using (this._controlLock.Lock())
            {
                this._orderedControlEvents ??= this._controlEvents.OrderBy(x => order.IndexOf(x.Key)).Select(x => x.Value).ToList();
            }

            return this._orderedControlEvents;
        }
    }

    public new bool Enabled => this.Configuration?.Enabled.Value ?? false;

    private double PixelPerMinute
    {
        get
        {
            int pixels = this.GetWidth();

            double pixelPerMinute = pixels / (double)this.Configuration.TimeSpan.Value;

            return pixelPerMinute;
        }
    }

    public EventAreaConfiguration Configuration { get; private set; }

    private void OnMouseWheelScrolled(object sender, MouseEventArgs e)
    {
        if (!this.Configuration.EnableHistorySplitScrolling.Value)
        {
            return;
        }

        if (this._tempHistorySplit == -1)
        {
            this._tempHistorySplit = this.Configuration.HistorySplit.Value;
        }

        this._savedDrawInterval ??= this.DrawInterval;
        this.DrawInterval = TimeSpan.FromMilliseconds((int)Models.DrawInterval.INSTANT);

        int scrollDistance = GameService.Input.Mouse.State.ScrollWheelValue;

        int scrollValue = this.Configuration.HistorySplitScrollingSpeed.Value * (scrollDistance >= 0 ? 1 : -1);

        (float Min, float Max)? range = this.Configuration.HistorySplit.GetRange();

        if (range == null || !range.HasValue)
        {
            return;
        }

        if (this._tempHistorySplit + scrollValue > range.Value.Max)
        {
            this._tempHistorySplit = (int)range.Value.Max;
        }
        else if (this._tempHistorySplit + scrollValue < range.Value.Min)
        {
            this._tempHistorySplit = (int)range.Value.Min;
        }
        else
        {
            this._tempHistorySplit += scrollValue;
        }
    }

    private void OnMouseLeft(object sender, MouseEventArgs e)
    {
        this._tempHistorySplit = -1;
        if (this._savedDrawInterval != null)
        {
            this.DrawInterval = this._savedDrawInterval.Value;
            this._savedDrawInterval = null;
        }
    }

    private void EventService_ServiceAdded(object sender, ValueEventArgs<EventStateService.VisibleStateInfo> e)
    {
        if (e.Value.AreaName == this.Configuration.Name && e.Value.State == EventStateService.EventStates.Hidden)
        {
            this.ReAddEvents();
        }
    }

    private void EventService_ServiceRemoved(object sender, ValueEventArgs<EventStateService.VisibleStateInfo> e)
    {
        if (e.Value.AreaName == this.Configuration.Name && e.Value.State == EventStateService.EventStates.Hidden)
        {
            this.ReAddEvents();
        }
    }

    private void CurrentMap_MapChanged(object sender, ValueEventArgs<int> e)
    {
        if (this.Configuration.LimitToCurrentMap.Value)
        {
            this.ReAddEvents();
        }
    }

    private void AllowUnspecifiedMap_SettingChanged(object sender, ValueChangedEventArgs<bool> e)
    {
        if (this.Configuration.LimitToCurrentMap.Value)
        {
            this.ReAddEvents();
        }
    }

    private void LimitToCurrentMap_SettingChanged(object sender, ValueChangedEventArgs<bool> e)
    {
        this.ReAddEvents();
    }

    private void DrawInterval_SettingChanged(object sender, ValueChangedEventArgs<DrawInterval> e)
    {
        this.DrawInterval = TimeSpan.FromMilliseconds((int)e.NewValue);
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
        using (this._eventLock.Lock())
        {
            this._allEvents.Clear();

            this._allEvents.AddRange(JsonConvert.DeserializeObject<List<EventCategory>>(JsonConvert.SerializeObject(allEvents)));

            (DateTime Now, DateTime Min, DateTime Max) times = this.GetTimes();

            this._allEvents.ForEach(ec => ec.Load(this._getNowAction, this._translationService));
            // Events should have occurences calculated already
        }

        this.ReAddEvents();
    }

    private void Event_Removed(object sender, string apiCode)
    {
        List<Models.Event> events = new List<Models.Event>();
        using (this._eventLock.Lock())
        {
            events.AddRange(this._allEvents.SelectMany(ec => ec.Events).Where(ev => ev.APICode == apiCode));
        }

        events.ForEach(ev =>
        {
            this._eventService.Remove(this.Configuration.Name, ev.SettingKey);
        });
    }

    private void Event_Completed(object sender, string apiCode)
    {
        List<Models.Event> events = new List<Models.Event>();
        using (this._eventLock.Lock())
        {
            events.AddRange(this._allEvents.SelectMany(ec => ec.Events).Where(ev => ev.APICode == apiCode));
        }

        events.ForEach(ev =>
        {
            DateTime until = this.GetNextReset(ev);
            this.ToggleFinishEvent(ev, until);
        });
    }

    private void BackgroundColor_SettingChanged(object sender, ValueChangedEventArgs<Color> e)
    {
        Microsoft.Xna.Framework.Color backgroundColor = Microsoft.Xna.Framework.Color.Transparent;

        if (e.NewValue != null && e.NewValue.Id != 1)
        {
            backgroundColor = e.NewValue.Cloth.ToXnaColor();
        }

        this.BackgroundColor = backgroundColor * this.Configuration.Opacity.Value;
    }

    private void ReportNewHeight(int height)
    {
        int oldHeight = this.Height;

        if (oldHeight != height)
        {
            this.Height = height;
            this.Configuration.Size.Y.Value = height; // Update setting to correct setting views
            this.Location_SettingChanged(this, null);
        }
    }

    private void Opacity_SettingChanged(object sender, ValueChangedEventArgs<float> e)
    {
        this.BackgroundColor_SettingChanged(this, new ValueChangedEventArgs<Color>(null, this.Configuration.BackgroundColor.Value));
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
        //var type = this._lastMouseEventType switch
        //{
        //    MouseEventType.MouseWheelScrolled => CaptureType.MouseWheel,
        //    _ => CaptureType.Mouse | CaptureType.DoNotBlock,
        //};

        //Logger.Debug($"CaptureType: {type.GetFlags().Humanize()}");

        return CaptureType.Mouse | CaptureType.MouseWheel | CaptureType.DoNotBlock;
    }

    //public override Control TriggerMouseInput(MouseEventType mouseEventType, MouseState ms)
    //{
    //    this._lastMouseEventType = mouseEventType;
    //    return base.TriggerMouseInput(mouseEventType, ms); // This calls CapturesInput() where we get the last type saved prior.
    //}

    private List<IGrouping<string, string>> GetActiveEventKeysGroupedByCategory()
    {
        List<string> activeSettingKeys = this.GetActiveEventKeys();
        List<string> order = this.GetEventCategoryOrdering();

        return activeSettingKeys.OrderBy(x => order.IndexOf(x.Split('_')[0])).GroupBy(aek => aek.Split('_')[0]).ToList();
    }

    private List<string> GetEventCategoryOrdering()
    {
        return this.Configuration.EventOrder.Value.ToList();
    }

    private List<string> GetActiveEventKeys()
    {
        using (this._eventLock.Lock())
        {
            IEnumerable<string> activeSettingKeys = this._allEvents.SelectMany(ae => ae.Events).Where(e => !e.Filler && !this.EventDisabled(e)).Select(e => e.SettingKey).Where(sk => !this.Configuration.DisabledEventKeys.Value.Contains(sk));

            return activeSettingKeys.ToList();
        }
    }

    private int GetWidth()
    {
        return this.Width - this.DrawXOffset;
    }

    private BitmapFont GetFont()
    {
        return _fonts.GetOrAdd(this.Configuration.FontSize.Value, fontSize => GameService.Content.GetFont(FontFace.Menomonia, fontSize, ContentService.FontStyle.Regular));
    }

    private void ReAddEvents()
    {
        this._clearing = true;
        using IDisposable suspendCtx = this.SuspendLayoutContext();

        this.ClearEventControls();

        this._eventCategoryOrdering = null;
        this._lastEventOccurencesUpdate.Value = _updateEventOccurencesInterval.TotalMilliseconds;
        this._lastCheckForNewEventsUpdate = _checkForNewEventsInterval.TotalMilliseconds;
        this._clearing = false;
    }

    private (DateTime Now, DateTime Min, DateTime Max) GetTimes()
    {
        DateTime now = this._getNowAction();

        DateTime min = now.Subtract(TimeSpan.FromMinutes(this.Configuration.TimeSpan.Value * this.GetTimeSpanRatio()));
        DateTime max = now.Add(TimeSpan.FromMinutes(this.Configuration.TimeSpan.Value * (1f - this.GetTimeSpanRatio())));

        return (now, min, max);
    }

    private float GetTimeSpanRatio()
    {
        int historySplit = this._tempHistorySplit != -1 ? this._tempHistorySplit : this.Configuration.HistorySplit.Value;

        float ratio = 0.5f + ((historySplit / 100f) - 0.5f);
        return ratio;
    }

    private async Task UpdateEventOccurences()
    {
        (DateTime Now, DateTime Min, DateTime Max) times = this.GetTimes();

        List<Task> tasks = new List<Task>();

        List<string> activeEventKeys = this.GetActiveEventKeys();

        ConcurrentDictionary<string, List<Models.Event>> fillers = await this.GetFillers(times.Now, times.Min, times.Max, activeEventKeys);
        using (await this._eventLock.LockAsync())
        {
            foreach (EventCategory ec in this._allEvents)
            {
                if (fillers.TryGetValue(ec.Key, out List<Models.Event> categoryFillers))
                {
                    categoryFillers.ForEach(cf => cf.Load(ec, this._getNowAction, this._translationService));
                }

                ec.UpdateFillers(categoryFillers);
            }
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

            Logger.Info("Load fillers...");

            IFlurlRequest flurlRequest = this._flurlClient.Request(this._apiRootUrl, "fillers");

            string accessToken = this._getAccessToken();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                Logger.Info("Include custom event fillers...");
                flurlRequest.WithOAuthBearerToken(accessToken);
            }

            List<Models.Event> activeEvents = new List<Models.Event>();

            using (this._eventLock.Lock())
            {
                activeEvents.AddRange(this._allEvents.SelectMany(a => a.Events).Where(ev => activeEventKeys.Any(aeg => aeg == ev.SettingKey)).ToList());
            }

            IEnumerable<string> eventKeys = activeEvents.Select(a => a.SettingKey).Distinct();
            Logger.Debug($"Fetch fillers with active keys: {string.Join(", ", eventKeys.ToArray())}");

            HttpResponseMessage response = await flurlRequest.PostJsonAsync(new OnlineFillerRequest
            {
                Module = new OnlineFillerRequest.OnlineFillerRequestModule { Version = this._getVersion().ToString() },
                Times = new OnlineFillerRequest.OnlineFillerRequestTimes
                {
                    Now_UTC_ISO = now.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'"),
                    Min_UTC_ISO = min.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'"),
                    Max_UTC_ISO = max.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'")
                },
                EventKeys = activeEvents.Select(a => a.SettingKey).ToArray()
            });

            OnlineFillerCategory[] fillers = await response.GetJsonAsync<OnlineFillerCategory[]>();

            List<OnlineFillerCategory> fillerList = fillers.ToList();
            ConcurrentDictionary<string, List<Models.Event>> parsedFillers = new ConcurrentDictionary<string, List<Models.Event>>();
            for (int i = 0; i < fillerList.Count; i++)
            {
                OnlineFillerCategory currentCategory = fillerList[i];

                foreach (OnlineFillerEvent fillerItem in currentCategory.Fillers)
                {
                    Models.Event filler = new Models.Event
                    {
                        Name = $"{fillerItem.Name}",
                        Duration = fillerItem.Duration,
                        Filler = true
                    };

                    fillerItem.Occurences.ToList().ForEach(o => filler.Occurences.Add( /*DateTime.SpecifyKind(o.DateTime, DateTimeKind.Utc).ToLocalTime()*/ o.UtcDateTime));

                    List<Models.Event> prevFillers = parsedFillers.GetOrAdd(currentCategory.Key, key => new List<Models.Event> { filler });
                    prevFillers.Add(filler);
                }
            }

            return parsedFillers;
        }
        catch (FlurlHttpException ex)
        {
            string error = await ex.GetResponseStringAsync();
            Logger.Warn($"Could not load fillers from {ex.Call.Request.RequestUri}: {error}");
        }

        return new ConcurrentDictionary<string, List<Models.Event>>();
    }

    private bool EventCategoryDisabled(EventCategory ec)
    {
        bool finished = this._eventService?.Contains(this.Configuration.Name, ec.Key, EventStateService.EventStates.Completed) ?? false;

        return finished;
    }

    private bool EventDisabled(Models.Event ev)
    {
        bool disabled = !ev.Filler && this.EventDisabled(ev.SettingKey);

        disabled |= this.EventTemporaryDisabled(ev);

        return disabled;
    }

    private bool EventTemporaryDisabled(Models.Event ev)
    {
        bool disabled = false;
        if (!ev.Filler && this.Configuration.LimitToCurrentMap.Value && GameService.Gw2Mumble.IsAvailable)
        {
            int mapId = GameService.Gw2Mumble.CurrentMap.Id;
            if (!ev.MapIds.Contains(mapId) && !(this.Configuration.AllowUnspecifiedMap.Value && ev.MapIds.Length == 0))
            {
                disabled = true;
            }
        }

        return disabled;
    }

    private bool EventDisabled(string settingKey)
    {
        bool enabled = !this.Configuration.DisabledEventKeys.Value.Contains(settingKey);

        enabled &= !this._eventService.Contains(this.Configuration.Name, settingKey, EventStateService.EventStates.Hidden);

        return !enabled;
    }

    private void UpdateEventsOnScreen(SpriteBatch spriteBatch)
    {
        if (this._clearing)
        {
            return;
        }

        (DateTime Now, DateTime Min, DateTime Max) times = this.GetTimes();

        // Update and delete existing
        this._activeEvent = null;

        int y = 0;
        this._drawXOffset = 0;
        List<List<(DateTime Occurence, Event Event)>> orderedControlEvents = this.OrderedControlEvents;

        if (this.Configuration.ShowCategoryNames.Value)
        {
            foreach (List<(DateTime Occurence, Event Event)> controlEventPairs in orderedControlEvents)
            {
                if (controlEventPairs.Count > 0 && controlEventPairs.First().Event.Model.Category.TryGetTarget(out EventCategory eventCategory))
                {
                    this._drawXOffset = Math.Max((int)this.GetFont().MeasureString(eventCategory.Name).Width + 5, this._drawXOffset);
                }
            }
        }

        foreach (List<(DateTime Occurence, Event Event)> controlEventPairs in orderedControlEvents)
        {
            if (controlEventPairs.Count == 0)
            {
                continue; // We dont have anything to render here
            }

            if (this.Configuration.ShowCategoryNames.Value && controlEventPairs.First().Event.Model.Category.TryGetTarget(out EventCategory eventCategory))
            {
                Microsoft.Xna.Framework.Color color = this.Configuration.CategoryNameColor.Value.Id == 1 ? Microsoft.Xna.Framework.Color.Black : this.Configuration.CategoryNameColor.Value.Cloth.ToXnaColor();
                spriteBatch.DrawString(this.GetFont(), eventCategory.Name, new Vector2(0, y), color);
            }

            List<(DateTime Occurence, Event Event)> toDelete = new List<(DateTime Occurence, Event Event)>();

            foreach ((DateTime Occurence, Event Event) controlEvent in controlEventPairs)
            {
                bool disabled = this.EventDisabled(controlEvent.Event.Model);
                if (disabled)
                {
                    // Control can be deleted
                    toDelete.Add(controlEvent);
                    continue;
                }

                float width = (float)controlEvent.Event.Model.CalculateWidth(controlEvent.Occurence, times.Min, this.GetWidth(), this.PixelPerMinute);

                if (width <= 0)
                {
                    // Control can be deleted
                    toDelete.Add(controlEvent);
                }
                else
                {
                    // We are good to render
                    float x = (float)controlEvent.Event.Model.CalculateXPosition(controlEvent.Occurence, times.Min, this.PixelPerMinute);
                    x = (x < 0 ? 0 : x) + this.DrawXOffset;
                    RectangleF renderRect = new RectangleF(x, y, width, this.Configuration.EventHeight.Value);
                    controlEvent.Event.Render(spriteBatch, renderRect);
                    if (renderRect.ToBounds(this.AbsoluteBounds).Contains(GameService.Input.Mouse.Position))
                    {
                        this._activeEvent = controlEvent.Event;
                    }
                }
            }

            foreach ((DateTime Occurence, Event Event) delete in toDelete)
            {
                Logger.Debug($"Deleted event {delete.Event.Model.Name}");
                this.RemoveEventHooks(delete.Event);
                delete.Event.Dispose();
                controlEventPairs.Remove(delete);
            }

            y += this.Configuration.EventHeight.Value;
        }

        this._heightFromLastDraw = y == 0 ? 1 : y;

        if (this._activeEvent != null && this._lastActiveEvent?.Model?.Key != this._activeEvent.Model.Key)
        {
            // Active event changed
            bool isFiller = this._activeEvent?.Model?.Filler ?? false;
            this.Tooltip?.Dispose();
            this.Tooltip = null;
            if (!this.Menu?.Visible ?? false)
            {
                this.Menu?.Dispose();
                this.Menu = null;
            }

            if (!isFiller)
            {
                this.Tooltip = this.Configuration.ShowTooltips.Value ? this._activeEvent?.BuildTooltip() : null;
                this.Menu = this._activeEvent?.BuildContextMenu();
            }

            this._lastActiveEvent = this._activeEvent;
        }
        else if (this._activeEvent == null)
        {
            this._lastActiveEvent = null;
            this.Tooltip?.Dispose();
            this.Tooltip = null;
            if (!this.Menu?.Visible ?? false)
            {
                this.Menu?.Dispose();
                this.Menu = null;
            }
        }
    }

    private void CheckForNewEventsForScreen()
    {
        if (this._clearing)
        {
            return;
        }

        (DateTime Now, DateTime Min, DateTime Max) times = this.GetTimes();
        foreach (IGrouping<string, string> activeEventGroup in this.GetActiveEventKeysGroupedByCategory())
        {
            string categoryKey = activeEventGroup.Key;
            EventCategory validCategory = null;

            if (this._eventLock.IsFree())
            {
                using (this._eventLock.Lock())
                {
                    validCategory = this._allEvents.Find(ec => ec.Key == categoryKey);
                }
            }
            else
            {
                Logger.Debug($"Event lock is busy. Can't update category {categoryKey}");
            }

            //eventKey == Event.SettingsKey
            List<Models.Event> events = validCategory?.Events.Where(ev => activeEventGroup.Any(aeg => aeg == ev.SettingKey) || (this.Configuration.UseFiller.Value && ev.Filler)).ToList();
            if (events == null || events.Count == 0)
            {
                continue;
            }

            using (this._controlLock.Lock())
            {
                bool added = this._controlEvents.TryAdd(categoryKey, new List<(DateTime Occurence, Event Event)>());
                if (added)
                {
                    this._orderedControlEvents = null; // Refresh cache
                }
            }

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
                    using (this._controlLock.Lock())
                    {
                        if (this._controlEvents[categoryKey].Any(addedEvent => addedEvent.Occurence == occurence))
                        {
                            continue;
                        }
                    }

                    float x = (float)ev.CalculateXPosition(occurence, times.Min, this.PixelPerMinute);
                    float width = (float)ev.CalculateWidth(occurence, times.Min, this.GetWidth(), this.PixelPerMinute);

                    if (x > this.GetWidth() || width <= 0)
                    {
                        continue;
                    }

                    Event newEventControl = new Event(ev,
                        this._iconService,
                        this._translationService,
                        this._getNowAction,
                        occurence,
                        occurence.AddMinutes(ev.Duration),
                        this.GetFont,
                        () => !ev.Filler && this.Configuration.DrawBorders.Value,
                        () => this.Configuration.CompletionAction.Value is EventCompletedAction.Crossout or EventCompletedAction.CrossoutAndChangeOpacity && this._eventService.Contains(this.Configuration.Name, ev.SettingKey, EventStateService.EventStates.Completed),
                        () =>
                        {
                            Microsoft.Xna.Framework.Color defaultTextColor = Microsoft.Xna.Framework.Color.Black;
                            Microsoft.Xna.Framework.Color color = ev.Filler
                                ? this.Configuration.FillerTextColor.Value.Id == 1 ? defaultTextColor : this.Configuration.FillerTextColor.Value.Cloth.ToXnaColor()
                                : this.Configuration.TextColor.Value.Id == 1
                                    ? defaultTextColor
                                    : this.Configuration.TextColor.Value.Cloth.ToXnaColor();
                            float alpha = ev.Filler ? this.Configuration.FillerTextOpacity.Value : this.Configuration.EventTextOpacity.Value;

                            if (this.Configuration.CompletionAction.Value is EventCompletedAction.ChangeOpacity or EventCompletedAction.CrossoutAndChangeOpacity && this._eventService.Contains(this.Configuration.Name, ev.SettingKey, EventStateService.EventStates.Completed))
                            {
                                if (this.Configuration.CompletedEventsInvertTextColor.Value)
                                {
                                    color = new Microsoft.Xna.Framework.Color(color.PackedValue ^ 0xffffff);
                                }

                                alpha = this.Configuration.CompletedEventsTextOpacity.Value;
                            }

                            return color * alpha;
                        },
                        () =>
                        {
                            if (ev.Filler)
                            {
                                return new[]
                                {
                                    Microsoft.Xna.Framework.Color.Transparent
                                };
                            }

                            float alpha = this.Configuration.EventBackgroundOpacity.Value;

                            if (this.Configuration.CompletionAction.Value is EventCompletedAction.ChangeOpacity or EventCompletedAction.CrossoutAndChangeOpacity && this._eventService.Contains(this.Configuration.Name, ev.SettingKey, EventStateService.EventStates.Completed))
                            {
                                alpha = this.Configuration.CompletedEventsBackgroundOpacity.Value;
                            }

                            if (this.Configuration.EnableColorGradients.Value && ev.BackgroundColorGradientCodes != null && ev.BackgroundColorGradientCodes.Length > 0)
                            {
                                IEnumerable<Microsoft.Xna.Framework.Color> colorCodes = ev.BackgroundColorGradientCodes.Select(cc =>
                                {
                                    System.Drawing.Color parsedColor = ColorTranslator.FromHtml(cc);
                                    return new Microsoft.Xna.Framework.Color(parsedColor.R, parsedColor.G, parsedColor.B) * alpha;
                                });

                                return colorCodes.ToArray();
                            }

                            if (!string.IsNullOrWhiteSpace(ev.BackgroundColorCode))
                            {
                                System.Drawing.Color tempColor = ColorTranslator.FromHtml(ev.BackgroundColorCode);
                                return new[]
                                {
                                    new Microsoft.Xna.Framework.Color(tempColor.R, tempColor.G, tempColor.B) * alpha
                                };
                            }

                            return new[]
                            {
                                Microsoft.Xna.Framework.Color.White * alpha
                            };
                        },
                        () => ev.Filler ? this.Configuration.DrawShadowsForFiller.Value : this.Configuration.DrawShadows.Value,
                        () =>
                        {
                            return ev.Filler
                                ? (this.Configuration.FillerShadowColor.Value.Id == 1 ? Microsoft.Xna.Framework.Color.Black : this.Configuration.FillerShadowColor.Value.Cloth.ToXnaColor()) * this.Configuration.FillerShadowOpacity.Value
                                : (this.Configuration.ShadowColor.Value.Id == 1 ? Microsoft.Xna.Framework.Color.Black : this.Configuration.ShadowColor.Value.Cloth.ToXnaColor()) * this.Configuration.ShadowOpacity.Value;
                        },
                        () => this.Configuration.EventAbsoluteTimeFormatString.Value,
                        () => (this.Configuration.EventTimespanDaysFormatString.Value, this.Configuration.EventTimespanHoursFormatString.Value, this.Configuration.EventTimespanMinutesFormatString.Value));

                    this.AddEventHooks(newEventControl);

                    Logger.Debug($"Added event {ev.Name} with occurence {occurence}");

                    using (this._controlLock.Lock())
                    {
                        this._controlEvents[categoryKey].Add((occurence, newEventControl));
                    }
                }
            }
        }
    }

    private void OnLeftMouseButtonPressed(object sender, MouseEventArgs e)
    {
        if (this._activeEvent == null || this._activeEvent.Model.Filler)
        {
            return;
        }

        switch (this.Configuration.LeftClickAction.Value)
        {
            case LeftClickAction.CopyWaypoint:
                if (!string.IsNullOrWhiteSpace(this._activeEvent.Model.Waypoint))
                {
                    ClipboardUtil.WindowsClipboardService.SetTextAsync(this._activeEvent.Model.Waypoint);
                    ScreenNotification.ShowNotification(new[]
                    {
                        this._activeEvent.Model.Name,
                        "Copied to clipboard!"
                    });
                }

                break;
            case LeftClickAction.NavigateToWaypoint:
                if (string.IsNullOrWhiteSpace(this._activeEvent.Model.Waypoint))
                {
                    return;
                }

                if (this._pointOfInterestService.Loading)
                {
                    ScreenNotification.ShowNotification($"{nameof(PointOfInterestService)} is still loading!", ScreenNotification.NotificationType.Error);
                    return;
                }

                PointOfInterest poi = this._pointOfInterestService.GetPointOfInterest(this._activeEvent.Model.Waypoint);
                if (poi == null)
                {
                    ScreenNotification.ShowNotification($"{this._activeEvent.Model.Waypoint} not found!", ScreenNotification.NotificationType.Error);
                    return;
                }

                _ = Task.Run(async () =>
                {
                    MapUtil.NavigationResult result = await (this._mapUtil?.NavigateToPosition(poi, this.Configuration.AcceptWaypointPrompt.Value) ?? Task.FromResult(new MapUtil.NavigationResult(false, "Variable null.")));
                    if (!result.Success)
                    {
                        ScreenNotification.ShowNotification($"Navigation failed: {result.Message ?? "Unknown"}", ScreenNotification.NotificationType.Error);
                    }
                });

                break;
        }
    }

    /// <summary>
    ///     Calculates the ui visibility based on settings or mumble parameters.
    /// </summary>
    /// <returns>The newly calculated ui visibility.</returns>
    public bool CalculateUIVisibility()
    {
        bool show = true;
        if (this.Configuration.HideOnOpenMap.Value)
        {
            show &= !GameService.Gw2Mumble.UI.IsMapOpen;
        }

        if (this.Configuration.HideOnMissingMumbleTicks.Value)
        {
            show &= GameService.Gw2Mumble.TimeSinceTick.TotalSeconds < 0.5;
        }

        if (this.Configuration.HideInCombat.Value)
        {
            show &= !GameService.Gw2Mumble.PlayerCharacter.IsInCombat;
        }

        // All maps not specified as competetive will be treated as open world
        if (this.Configuration.HideInPvE_OpenWorld.Value)
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

        if (this.Configuration.HideInPvE_Competetive.Value)
        {
            MapType[] pveCompetetiveMapTypes =
            {
                MapType.Instance
            };

            show &= !(!GameService.Gw2Mumble.CurrentMap.IsCompetitiveMode && pveCompetetiveMapTypes.Any(type => type == GameService.Gw2Mumble.CurrentMap.Type) && MapInfo.MAP_IDS_PVE_COMPETETIVE.Contains(GameService.Gw2Mumble.CurrentMap.Id));
        }

        if (this.Configuration.HideInWvW.Value)
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

        if (this.Configuration.HideInPvP.Value)
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

    protected override void InternalUpdate(GameTime gameTime)
    {
        _ = UpdateUtil.UpdateAsync(this.UpdateEventOccurences, gameTime, _updateEventOccurencesInterval.TotalMilliseconds, this._lastEventOccurencesUpdate);
        UpdateUtil.Update(this.CheckForNewEventsForScreen, gameTime, _checkForNewEventsInterval.TotalMilliseconds, ref this._lastCheckForNewEventsUpdate);
        this.ReportNewHeight(this._heightFromLastDraw);
    }

    protected override void DoPaint(SpriteBatch spriteBatch, Rectangle bounds)
    {
        this.UpdateEventsOnScreen(spriteBatch);
        this.DrawTimeLine(spriteBatch);
    }

    private void DrawTimeLine(SpriteBatch spriteBatch)
    {
        float middleLineX = (this.GetWidth() * this.GetTimeSpanRatio()) + this.DrawXOffset;
        float width = 2;
        spriteBatch.DrawLine(Textures.Pixel, new RectangleF(middleLineX - (width / 2), 0, width, this.Height), Microsoft.Xna.Framework.Color.LightGray * this.Configuration.TimeLineOpacity.Value);
    }

    private void ClearEventControls()
    {
        using (this._eventLock.Lock())
        {
            this._allEvents?.ForEach(a => a.UpdateFillers(new List<Models.Event>()));
        }

        using (this._controlLock.Lock())
        {
            this._controlEvents?.Clear();
        }

        this._orderedControlEvents = null;
    }

    private void AddEventHooks(Event ev)
    {
        ev.HideRequested += this.Ev_HideRequested;
        ev.ToggleFinishRequested += this.Ev_ToggleFinishRequested;
        ev.DisableRequested += this.Ev_DisableRequested;
    }

    private void RemoveEventHooks(Event ev)
    {
        ev.HideRequested -= this.Ev_HideRequested;
        ev.ToggleFinishRequested -= this.Ev_ToggleFinishRequested;
        ev.DisableRequested -= this.Ev_DisableRequested;
    }

    private void Ev_ToggleFinishRequested(object sender, EventArgs e)
    {
        Event ev = sender as Event;

        this.ToggleFinishEvent(ev.Model, this.GetNextReset(ev.Model));
    }

    private void ToggleFinishEvent(Models.Event ev, DateTime until)
    {
        switch (this.Configuration.CompletionAction.Value)
        {
            case EventCompletedAction.Crossout:
            case EventCompletedAction.ChangeOpacity:
            case EventCompletedAction.CrossoutAndChangeOpacity:
                if (this._eventStateService.Contains(this.Configuration.Name, ev.SettingKey, EventStateService.EventStates.Completed))
                {
                    this._eventStateService.Remove(this.Configuration.Name, ev.SettingKey);
                }
                else
                {
                    this._eventStateService.Add(this.Configuration.Name, ev.SettingKey, until, EventStateService.EventStates.Completed);
                }

                break;
            case EventCompletedAction.Hide:
                this.HideEvent(ev, until);
                break;
        }
    }

    private void HideEvent(Models.Event ev, DateTime until)
    {
        this._eventService.Add(this.Configuration.Name, ev.SettingKey, until, EventStateService.EventStates.Hidden);
    }

    private void Ev_HideRequested(object sender, EventArgs e)
    {
        Event ev = sender as Event;
        this.HideEvent(ev.Model, this.GetNextReset(ev.Model));
    }

    private void Ev_DisableRequested(object sender, EventArgs e)
    {
        Event ev = sender as Event;
        if (!this.Configuration.DisabledEventKeys.Value.Contains(ev.Model.SettingKey))
        {
            this.Configuration.DisabledEventKeys.Value = new List<string>(this.Configuration.DisabledEventKeys.Value) { ev.Model.SettingKey };
        }
    }

    private DateTime GetNextReset(Models.Event ev)
    {
        DateTime nowUTC = this._getNowAction().ToUniversalTime();

        var addDuration = TimeSpan.FromDays(1);

        if (ev.Duration >= addDuration.TotalMinutes)
        {
            // No idea yet
        }

        return new DateTime(nowUTC.Year, nowUTC.Month, nowUTC.Day, 0, 0, 0, DateTimeKind.Utc).AddDays(Math.Ceiling(addDuration.TotalDays));
    }

    protected override void InternalDispose()
    {
        this.ClearEventControls();

        if (this._worldbossService != null)
        {
            this._worldbossService.WorldbossCompleted -= this.Event_Completed;
            this._worldbossService.WorldbossRemoved -= this.Event_Removed;
        }

        if (this._mapchestService != null)
        {
            this._mapchestService.MapchestCompleted -= this.Event_Completed;
            this._mapchestService.MapchestRemoved -= this.Event_Removed;
        }

        if (this._eventService != null)
        {
            this._eventService.StateAdded -= this.EventService_ServiceAdded;
            this._eventService.StateRemoved -= this.EventService_ServiceRemoved;
        }

        this._iconService = null;
        this._worldbossService = null;
        this._mapchestService = null;
        this._eventService = null;
        this._translationService = null;
        this._mapUtil = null;
        this._pointOfInterestService = null;

        this._flurlClient = null;
        this._apiRootUrl = null;

        this.Click -= this.OnLeftMouseButtonPressed;
        this.MouseLeft -= this.OnMouseLeft;
        this.MouseWheelScrolled -= this.OnMouseWheelScrolled;

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
        this.Configuration.DrawInterval.SettingChanged -= this.DrawInterval_SettingChanged;
        this.Configuration.LimitToCurrentMap.SettingChanged -= this.LimitToCurrentMap_SettingChanged;
        this.Configuration.AllowUnspecifiedMap.SettingChanged -= this.AllowUnspecifiedMap_SettingChanged;
        GameService.Gw2Mumble.CurrentMap.MapChanged -= this.CurrentMap_MapChanged;

        this.Configuration = null;

        this._allEvents?.Clear();
        this._allEvents = null;
    }
}