namespace Estreya.BlishHUD.EventTable.Controls;

using Blish_HUD;
using Blish_HUD._Extensions;
using Blish_HUD.Controls;
using Estreya.BlishHUD.EventTable.Models;
using Estreya.BlishHUD.EventTable.State;
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

    private static TimeSpan _updateEventInterval = TimeSpan.FromMinutes(15);
    private AsyncRef<double> _lastEventUpdate = new AsyncRef<double>(0);

    private static readonly ConcurrentDictionary<FontSize, BitmapFont> _fonts = new ConcurrentDictionary<FontSize, BitmapFont>();
    private IconState _iconState;
    private EventState _eventState;
    private WorldbossState _worldbossState;
    private MapchestState _mapchestState;

    private Func<DateTime> _getNowAction;

    private List<EventCategory> _allEvents = new List<EventCategory>();

    private ConcurrentDictionary<string, List<(DateTime Occurence, Event Event)>> _controlEvents = new ConcurrentDictionary<string, List<(DateTime Occurence, Event Event)>>();

    public new bool Enabled => this.Configuration?.Enabled.Value ?? false;

    private bool _readding = false;

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

    public EventArea(EventAreaConfiguration configuration, IconState iconState, EventState eventState, WorldbossState worldbossState, MapchestState mapchestState, Func<DateTime> getNowAction)
    {
        this.Configuration = configuration;
        this.Configuration.Size.X.SettingChanged += this.Size_SettingChanged;
        this.Configuration.Size.Y.SettingChanged += this.Size_SettingChanged;
        this.Configuration.Location.X.SettingChanged += this.Location_SettingChanged;
        this.Configuration.Location.Y.SettingChanged += this.Location_SettingChanged;
        this.Configuration.Opacity.SettingChanged += this.Opacity_SettingChanged;
        this.Configuration.BackgroundColor.SettingChanged += this.BackgroundColor_SettingChanged;

        this.Location_SettingChanged(this, null);
        this.Size_SettingChanged(this, null);
        this.Opacity_SettingChanged(this, new ValueChangedEventArgs<float>(0f, this.Configuration.Opacity.Value));
        this.BackgroundColor_SettingChanged(this, new ValueChangedEventArgs<Gw2Sharp.WebApi.V2.Models.Color>(null, this.Configuration.BackgroundColor.Value));

        this._getNowAction = getNowAction;

        this._iconState = iconState;
        this._eventState = eventState;
        this._worldbossState = worldbossState;
        this._mapchestState = mapchestState;

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

    public async Task UpdateAllEvents(List<EventCategory> allEvents)
    {
        this._allEvents.Clear();

        this._allEvents.AddRange(JsonConvert.DeserializeObject<List<EventCategory>>(JsonConvert.SerializeObject(allEvents)));

        (DateTime Now, DateTime Min, DateTime Max) times = this.GetTimes();

        await Task.WhenAll(this._allEvents.Select(ec => ec.LoadAsync(this._eventState, this._getNowAction)));

        await this.UpdateEventOccurences();

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

    private void Opacity_SettingChanged(object sender, ValueChangedEventArgs<float> e)
    {
        this.BackgroundColor_SettingChanged(this, new ValueChangedEventArgs<Gw2Sharp.WebApi.V2.Models.Color>(null, this.Configuration.BackgroundColor.Value));
    }

    private void Location_SettingChanged(object sender, ValueChangedEventArgs<int> e)
    {
        this.Location = new Point(this.Configuration.Location.X.Value, this.Configuration.Location.Y.Value);
    }

    private void Size_SettingChanged(object sender, ValueChangedEventArgs<int> e)
    {
        this.Size = new Point(this.Configuration.Size.X.Value, this.Height);
    }

    protected override CaptureType CapturesInput()
    {
        return CaptureType.None;
    }

    protected override void DisposeControl()
    {
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

        this.Configuration.Size.X.SettingChanged -= this.Size_SettingChanged;
        this.Configuration.Size.Y.SettingChanged -= this.Size_SettingChanged;
        this.Configuration.Location.X.SettingChanged -= this.Location_SettingChanged;
        this.Configuration.Location.Y.SettingChanged -= this.Location_SettingChanged;
        this.Configuration.Opacity.SettingChanged -= this.Opacity_SettingChanged;
        this.Configuration.BackgroundColor.SettingChanged -= this.BackgroundColor_SettingChanged;

        this.Configuration = null;
    }

    private string GetEventKey(Models.Event ev)
    {
        return $"{this.Configuration.Name}_{ev.SettingKey}";
    }

    private void ReAddEvents()
    {
        _readding = true;
        using IDisposable suspendCtx = this.SuspendLayoutContext();

        this.Children.ToList().ForEach(child => child.Dispose());
        this.Children.Clear();

        (DateTime Now, DateTime Min, DateTime Max) times = this.GetTimes();

        int y = 0;
        foreach (IGrouping<string, string> activeEventGroup in this.Configuration.ActiveEventKeys.Value.GroupBy(aek => aek.Split('_')[0]))
        {
            string categoryKey = activeEventGroup.Key;
            EventCategory validCategory = this._allEvents.Find(ec => ec.Key == categoryKey);

            bool renderedAny = false;

            this._controlEvents.TryRemove(categoryKey, out List<(DateTime Occurence, Event Event)> _);

            //eventKey == Event.SettingsKey
            List<Models.Event> events = validCategory.Events.Where(ev => activeEventGroup.Any(aeg => aeg == ev.SettingKey) || ev.Filler).ToList();
            if (events.Count == 0)
            {
                continue;
            }

            List<(DateTime Occurence, Event Event)> controlEvents = new List<(DateTime Occurence, Event Event)>();

            IEnumerable<Models.Event> validEvents = events.Where(ev => ev.Occurences.Any(oc => oc.AddMinutes(ev.Duration) >= times.Min && oc <= times.Max));

            foreach (Models.Event ev in validEvents)
            {
                IEnumerable<DateTime> validOccurences = ev.Occurences.Where(oc => oc.AddMinutes(ev.Duration) >= times.Min && oc <= times.Max);
                foreach (DateTime occurence in validOccurences)
                {
                    int x = (int)Math.Floor(ev.CalculateXPosition(occurence, times.Min, this.PixelPerMinute));
                    int width = (int)Math.Ceiling(ev.CalculateWidth(occurence, times.Min, this.Width, this.PixelPerMinute));

                    if (x > this.Width || width < 0)
                    {
                        continue;
                    }

                    controlEvents.Add((occurence, new Event(ev,
                        this._iconState,
                        this._getNowAction,
                        occurence,
                        occurence.AddMinutes(ev.Duration),
                        () => _fonts.GetOrAdd(this.Configuration.FontSize.Value, fontSize => GameService.Content.GetFont(FontFace.Menomonia, fontSize, FontStyle.Regular)),
                        this.Configuration.TextColor,
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
                    }));
                }


                renderedAny = true;

                this._controlEvents[categoryKey] = controlEvents;
            }

            if (renderedAny)
            {
                y += EVENT_HEIGHT; // TODO: Make setting
            }

            this.Height = y;
        }

        _readding = false;
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
            tasks.Add(ec.UpdateEventOccurences(times.Now, times.Min, times.Max));
        }

        await Task.WhenAll(tasks);
    }

    private void UpdateEvents()
    {
        if (_readding) return;

        (DateTime Now, DateTime Min, DateTime Max) times = this.GetTimes();

        foreach (List<(DateTime Occurence, Event Event)> controlEventPairs in this._controlEvents.Values)
        {
            var toDelete = new List<(DateTime Occurence, Event Event)>();
            foreach ((DateTime Occurence, Event Event) controlEvent in controlEventPairs)
            {
                int x = (int)Math.Floor(controlEvent.Event.Ev.CalculateXPosition(controlEvent.Occurence, times.Min, this.PixelPerMinute));
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
                delete.Event.Dispose();
                controlEventPairs.Remove(delete);
            }
        }
    }

    public override void UpdateContainer(GameTime gameTime)
    {
        _ = UpdateUtil.UpdateAsync(this.UpdateEventOccurences, gameTime, _updateEventInterval.TotalMilliseconds, this._lastEventUpdate);
        this.UpdateEvents();
    }
}
