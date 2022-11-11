namespace Estreya.BlishHUD.EventTable.Controls;

using Blish_HUD;
using Blish_HUD._Extensions;
using Blish_HUD.Controls;
using Estreya.BlishHUD.EventTable.Models;
using Estreya.BlishHUD.EventTable.State;
using Estreya.BlishHUD.Shared.Extensions;
using Estreya.BlishHUD.Shared.State;
using Estreya.BlishHUD.Shared.Utils;
using Humanizer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.BitmapFonts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using static Blish_HUD.ContentService;

public class EventArea : Container
{
    private static readonly Logger Logger = Logger.GetLogger<EventArea>();

    private static TimeSpan _updateEventInterval = TimeSpan.FromMinutes(15);
    private double _lastEventUpdate = 0;

    private static readonly ConcurrentDictionary<FontSize, BitmapFont> _fonts = new ConcurrentDictionary<FontSize, BitmapFont>();
    private EventState _eventState;
    private WorldbossState _worldbossState;
    private MapchestState _mapchestState;

    private Func<DateTime> _getNowAction;

    private List<EventCategory> _allEvents = new List<EventCategory>();

    private ConcurrentDictionary<string, List<Event>> _controlEvents = new ConcurrentDictionary<string, List<Event>>();

    public new bool Enabled => this.Configuration?.Enabled.Value ?? false;

    public EventAreaConfiguration Configuration { get; private set; }

    public EventArea(EventAreaConfiguration configuration, EventState eventState, WorldbossState worldbossState, MapchestState mapchestState, Func<DateTime> getNowAction)
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

        this._eventState = eventState;
        this._worldbossState = worldbossState;
        this._mapchestState = mapchestState;

        //this._eventState.

        this._worldbossState.WorldbossCompleted += this.Event_Completed;
        this._worldbossState.WorldbossRemoved += this.Event_Removed;

        this._mapchestState.MapchestCompleted += this.Event_Completed;
        this._mapchestState.MapchestRemoved += this.Event_Removed;
    }

    public void UpdateAllEvents(List<EventCategory> allEvents)
    {
        this._allEvents.Clear();

        this._allEvents.AddRange(allEvents.Copy());

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
            var areaEventKey = this.GetEventKey(ev);
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
        this.Size = new Point(this.Configuration.Size.X.Value, this.Configuration.Size.Y.Value);
    }

    protected override CaptureType CapturesInput()
    {
        return CaptureType.None;
    }

    protected override void DisposeControl()
    {
        this._worldbossState.WorldbossCompleted -= this.Event_Completed;
        this._worldbossState.WorldbossRemoved -= this.Event_Removed;

        this._mapchestState.MapchestCompleted -= this.Event_Completed;
        this._mapchestState.MapchestRemoved -= this.Event_Removed;

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
        using var suspendCtx = this.SuspendLayoutContext();

        this.Children.ToList().ForEach(child => child.Dispose());
        this.Children.Clear();

        var times = this.GetTimes();

        foreach (var eventKey in this.Configuration.ActiveEventKeys.Value)
        {
            this._controlEvents.TryRemove(eventKey, out var _);

            //eventKey == Event.SettingsKey
            var events = this._allEvents.SelectMany(ec => ec.Events.Where(ev => ev.SettingKey == eventKey)).ToList();
            if (events.Count == 0)
            {
                continue;
            }

            var controlEvents= new List<Event>();

            foreach (Models.Event ev in events)
            {
                foreach (var occurence in ev.Occurences)
                {
                    controlEvents.Add(new Event(ev, occurence, occurence.AddMinutes(ev.Duration), _fonts.GetOrAdd(this.Configuration.FontSize.Value, fontSize => GameService.Content.GetFont(FontFace.Menomonia, fontSize, FontStyle.Regular)), this.Configuration.TextColor.Value.Cloth.ToXnaColor())
                    {
                        Parent = this,
                    });
                }
            }

            this._controlEvents[eventKey] = controlEvents;
        }
    }

    private (DateTime Now, DateTime Min, DateTime Max) GetTimes()
    {
        var now = this._getNowAction();
        var halveTimespan = this.Configuration.TimeSpan.Value / 2;
        var min = now.AddMinutes(-halveTimespan);
        var max = now.AddMinutes(halveTimespan);

        return (now, min, max);
    }

    private void UpdateEvents()
    {
        var times = this.GetTimes();

        this._allEvents.ForEach(ec =>
        {
            ec.UpdateEventOccurences(times.Now, times.Min, times.Max);
            // Called in EventCategory.UpdateEventOccurences
            //ec.Events.ForEach(ev => ev.UpdateOccurences(times.Now, times.Min, times.Max));
        });
    }

    public override void UpdateContainer(GameTime gameTime)
    {
        UpdateUtil.Update(this.UpdateEvents, gameTime, _updateEventInterval.TotalMilliseconds, ref _lastEventUpdate);
    }
}
