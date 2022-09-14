namespace Estreya.BlishHUD.EventTable.Models
{
    using Blish_HUD;
    using Estreya.BlishHUD.EventTable.Resources;
    using Estreya.BlishHUD.EventTable.State;
    using Estreya.BlishHUD.Shared.Helpers;
    using Estreya.BlishHUD.Shared.Utils;
    using Microsoft.Xna.Framework;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    [Serializable]
    public class EventCategory
    {
        private static readonly Logger Logger = Logger.GetLogger<EventCategory>();

        private readonly TimeSpan updateInterval = TimeSpan.FromMinutes(15);
        private double timeSinceUpdate = 0;

        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }

        [JsonProperty("showCombined")]
        public bool ShowCombined { get; set; }

        [JsonProperty("events")]
        private List<Event> _originalEvents = new List<Event>();

        [JsonIgnore]
        private List<Event> _fillerEvents = new List<Event>();

        [JsonIgnore]
        private AsyncLock _eventLock = new AsyncLock();

        [JsonIgnore]
        public List<Event> Events
        {
            get => this._originalEvents.Concat(this._fillerEvents).ToList();
            set => this._originalEvents = value;
        }

        [JsonIgnore]
        private bool? _isDisabled;

        [JsonIgnore]
        private EventState _eventState;

        [JsonIgnore]
        private Func<DateTime> _getNowAction;

        [JsonIgnore]
        public bool IsDisabled
        {
            get
            {
                if (_isDisabled == null)
                {
                    this._isDisabled = this._eventState?.Contains(this.Key, EventState.EventStates.Hidden) ?? false;
                }

                return _isDisabled.Value;
            }
        }

        public EventCategory()
        {
            this.timeSinceUpdate = this.updateInterval.TotalMilliseconds;
        }

        public void UpdateEventOccurences(DateTime now, DateTime min, DateTime max)
        {
            lock (this._fillerEvents)
            {
                this._fillerEvents.Clear();
            }

            using (this._eventLock.Lock())
            {
                this.Events.ForEach(ev => ev.UpdateOccurences(now, min, max));
            }

            //var activeEvents = this.Events.Where(e => eventSettings.Find(eventSetting => eventSetting.EntryKey == e.Name).Value).ToList();
            List<Event> activeEvents = this._originalEvents.Where(e => !e.IsDisabled).ToList();

            List<KeyValuePair<DateTime, Event>> activeEventStarts = new List<KeyValuePair<DateTime, Event>>();

            foreach (Event activeEvent in activeEvents)
            {
                List<DateTime> eventOccurences = activeEvent.Occurences.Where(oc => (oc >= min || oc.AddMinutes(activeEvent.Duration) >= min) && oc <= max).ToList();//.GetStartOccurences(now, max, min);

                eventOccurences.ForEach(eo => activeEventStarts.Add(new KeyValuePair<DateTime, Event>(eo, activeEvent)));
            }

            activeEventStarts = activeEventStarts.OrderBy(aes => aes.Key).ToList();

            List<KeyValuePair<DateTime, Event>> modifiedEventStarts = activeEventStarts.ToList();

            for (int i = 0; i < activeEventStarts.Count - 1; i++)
            {
                KeyValuePair<DateTime, Event> currentEvent = activeEventStarts.ElementAt(i);
                KeyValuePair<DateTime, Event> nextEvent = activeEventStarts.ElementAt(i + 1);

                DateTime currentStart = currentEvent.Key;
                DateTime currentEnd = currentStart + TimeSpan.FromMinutes(currentEvent.Value.Duration);

                DateTime nextStart = nextEvent.Key;

                TimeSpan gap = nextStart - currentEnd;
                if (gap > TimeSpan.Zero)
                {
                    Event filler = new Event()
                    {
                        Name = $"{currentEvent.Value.Name} - {nextEvent.Value.Name}",
                        Duration = (int)gap.TotalMinutes,
                        Filler = true
                    };

                    AsyncHelper.RunSync(() => filler.LoadAsync(this, this._eventState, this._getNowAction));

                    modifiedEventStarts.Insert(i + 1, new KeyValuePair<DateTime, Event>(currentEnd, filler));
                }
            }

            if (activeEventStarts.Count > 1)
            {
                KeyValuePair<DateTime, Event> firstEvent = activeEventStarts.FirstOrDefault();
                KeyValuePair<DateTime, Event> lastEvent = activeEventStarts.LastOrDefault();

                // We have a following event
                KeyValuePair<DateTime, Event> nextEvent = activeEvents.Select(ae =>
                {
                    return new KeyValuePair<DateTime, Event>(ae.Occurences.Where(oc => oc > lastEvent.Key && oc < max.AddDays(2))/*GetStartOccurences(now, max.AddDays(2), lastEvent.Key, limitsBetweenRanges: true)*/.FirstOrDefault(), ae);
                }).OrderBy(aeo => aeo.Key).First();
                KeyValuePair<DateTime, Event> nextEventMapping = new KeyValuePair<DateTime, Event>(nextEvent.Key, nextEvent.Value);

                DateTime nextStart = nextEventMapping.Key;
                DateTime nextEnd = nextStart + TimeSpan.FromMinutes(nextEventMapping.Value.Duration);

                if (nextStart - lastEvent.Key > TimeSpan.Zero)
                {
                    Event filler = new Event()
                    {
                        Name = $"{lastEvent.Value.Name} - {nextEventMapping.Value.Name}",
                        Filler = true,
                        Duration = (int)(nextStart - lastEvent.Key).TotalMinutes
                    };

                    AsyncHelper.RunSync(() => filler.LoadAsync(this, this._eventState, this._getNowAction));

                    modifiedEventStarts.Add(new KeyValuePair<DateTime, Event>(lastEvent.Key + TimeSpan.FromMinutes(lastEvent.Value.Duration), filler));
                }

                // We have a previous event
                KeyValuePair<DateTime, Event> prevEvent = activeEvents.Select(ae =>
                {
                    return new KeyValuePair<DateTime, Event>(ae.Occurences.Where(oc => oc > min.AddDays(-2) && oc < firstEvent.Key)/*GetStartOccurences(now, firstEvent.Key, min.AddDays(-2), limitsBetweenRanges: true)*/.LastOrDefault(), ae);
                }).OrderBy(aeo => aeo.Key).Last();

                KeyValuePair<DateTime, Event> prevEventMapping = new KeyValuePair<DateTime, Event>(prevEvent.Key, prevEvent.Value);

                DateTime prevStart = prevEventMapping.Key;
                DateTime prevEnd = prevStart + TimeSpan.FromMinutes(prevEventMapping.Value.Duration);

                if (firstEvent.Key - prevEnd > TimeSpan.Zero)
                {
                    Event filler = new Event()
                    {
                        Name = $"{prevEventMapping.Value.Name} - {firstEvent.Value.Name}",
                        Filler = true,
                        Duration = (int)(firstEvent.Key - prevEnd).TotalMinutes
                    };

                    AsyncHelper.RunSync(() => filler.LoadAsync(this, this._eventState, this._getNowAction));

                    modifiedEventStarts.Add(new KeyValuePair<DateTime, Event>(prevEnd, filler));
                }
            }
            else if (activeEventStarts.Count == 1 && activeEvents.Count >= 1)
            {
                KeyValuePair<DateTime, Event> currentEvent = activeEventStarts.First();
                DateTime currentStart = currentEvent.Key;
                DateTime currentEnd = currentStart + TimeSpan.FromMinutes(currentEvent.Value.Duration);

                // We have a following event
                KeyValuePair<DateTime, Event> nextEvent = activeEvents.Select(ae =>
                {
                    return new KeyValuePair<DateTime, Event>(ae.Occurences.Where(oc => oc > currentEnd && oc < max.AddDays(2))/*GetStartOccurences(now, max.AddDays(2), currentEnd, limitsBetweenRanges: true)*/.FirstOrDefault(), ae);
                }).OrderBy(aeo => aeo.Key).First();
                KeyValuePair<DateTime, Event> nextEventMapping = new KeyValuePair<DateTime, Event>(nextEvent.Key, nextEvent.Value);

                DateTime nextStart = nextEventMapping.Key;
                DateTime nextEnd = nextStart + TimeSpan.FromMinutes(nextEventMapping.Value.Duration);

                if (nextStart - currentEnd > TimeSpan.Zero)
                {
                    Event filler = new Event()
                    {
                        Name = $"{currentEvent.Value.Name} - {nextEventMapping.Value.Name}",
                        Filler = true,
                        Duration = (int)(nextStart - currentEnd).TotalMinutes
                    };

                    AsyncHelper.RunSync(() => filler.LoadAsync(this, this._eventState, this._getNowAction));

                    modifiedEventStarts.Add(new KeyValuePair<DateTime, Event>(currentEnd, filler));
                }

                // We have a previous event
                KeyValuePair<DateTime, Event> prevEvent = activeEvents.Select(ae =>
                {
                    return new KeyValuePair<DateTime, Event>(ae.Occurences.Where(oc => oc > min.AddDays(-2) && oc < currentStart)/*GetStartOccurences(now, currentStart, min.AddDays(-2), limitsBetweenRanges: true)*/.LastOrDefault(), ae);
                }).OrderBy(aeo => aeo.Key).Last();

                KeyValuePair<DateTime, Event> prevEventMapping = new KeyValuePair<DateTime, Event>(prevEvent.Key, prevEvent.Value);

                DateTime prevStart = prevEventMapping.Key;
                DateTime prevEnd = prevStart + TimeSpan.FromMinutes(prevEventMapping.Value.Duration);

                if (currentStart - prevEnd > TimeSpan.Zero)
                {
                    Event filler = new Event()
                    {
                        Name = $"{prevEventMapping.Value.Name} - {currentEvent.Value.Name}",
                        Filler = true,
                        Duration = (int)(currentStart - prevEnd).TotalMinutes
                    };

                    AsyncHelper.RunSync(() => filler.LoadAsync(this, this._eventState, this._getNowAction));

                    modifiedEventStarts.Add(new KeyValuePair<DateTime, Event>(prevEnd, filler));
                }
            }
            else if (activeEventStarts.Count == 0 && activeEvents.Count >= 1)
            {
                KeyValuePair<DateTime, Event> prevEvent = activeEvents.Select(ae =>
                {
                    return new KeyValuePair<DateTime, Event>(ae.Occurences.Where(oc => oc > min.AddDays(-2) && oc < max)/*GetStartOccurences(now, now, min.AddDays(-2), limitsBetweenRanges: true)*/.LastOrDefault(), ae);
                }).OrderBy(aeo => aeo.Key).Last();

                KeyValuePair<DateTime, Event> nextEvent = activeEvents.Select(ae =>
                {
                    return new KeyValuePair<DateTime, Event>(ae.Occurences.Where(oc => oc > min && oc < max.AddDays(2))/*GetStartOccurences(now, max.AddDays(2), now, limitsBetweenRanges: true)*/.FirstOrDefault(), ae);
                }).OrderBy(aeo => aeo.Key).First();

                KeyValuePair<DateTime, Event> prevEventMapping = new KeyValuePair<DateTime, Event>(prevEvent.Key, prevEvent.Value);
                KeyValuePair<DateTime, Event> nextEventMapping = new KeyValuePair<DateTime, Event>(nextEvent.Key, nextEvent.Value);

                DateTime prevStart = prevEventMapping.Key;
                DateTime prevEnd = prevStart + TimeSpan.FromMinutes(prevEventMapping.Value.Duration);
                DateTime nextStart = nextEventMapping.Key;

                Event filler = new Event()
                {
                    Name = $"{prevEventMapping.Value.Name} - {nextEventMapping.Value.Name}",
                    Duration = (int)(nextStart - prevEnd).TotalMinutes,
                    Filler = true
                };

                AsyncHelper.RunSync(() => filler.LoadAsync(this, this._eventState, this._getNowAction));

                modifiedEventStarts.Add(new KeyValuePair<DateTime, Event>(prevEnd, filler));
            }

            lock (this._fillerEvents)
            {
                modifiedEventStarts.Where(e => e.Value.Filler).ToList().ForEach(modEvent => modEvent.Value.Occurences.Add(modEvent.Key));
                IEnumerable<Event> modifiedEvents = modifiedEventStarts.Where(e => e.Value.Filler).Select(e => e.Value);
                this._fillerEvents.AddRange(modifiedEvents);
            }

            //return modifiedEventStarts.OrderBy(mes => mes.Key).ToList();
        }
        public void Hide()
        {
            DateTime now = this._getNowAction().ToUniversalTime();
            DateTime until = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, System.DateTimeKind.Utc).AddDays(1);
            this._eventState?.Add(this.Key, until, EventState.EventStates.Hidden);
        }

        public void Finish()
        {
            DateTime now = this._getNowAction().ToUniversalTime();
            DateTime until = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, System.DateTimeKind.Utc).AddDays(1);
            this._eventState?.Add(this.Key, until, EventState.EventStates.Completed);
        }

        public void Unfinish()
        {
            this._eventState?.Remove(this.Key);
        }

        public bool IsFinished()
        {
            var finished = this._eventState?.Contains(this.Key, EventState.EventStates.Completed) ?? false;

            return finished;
        }

        public async Task LoadAsync(EventState eventState, Func<DateTime> getNowAction)
        {
            Logger.Debug("Load event category: {0}", this.Key);

            this._eventState = eventState;
            this._getNowAction = getNowAction;

            using (await this._eventLock.LockAsync())
            {
                var eventLoadTasks = this.Events.Select(ev =>
                {
                    return ev.LoadAsync(this, eventState, getNowAction);
                });

                await Task.WhenAll(eventLoadTasks);
            }

            Logger.Debug("Loaded event category: {0}", this.Key);
        }

        public void Unload()
        {
            Logger.Debug("Unload event category: {0}", this.Key);

            this.Events.ForEach(ev => ev.Unload());

            Logger.Debug("Unloaded event category: {0}", this.Key);
        }
            }
}
