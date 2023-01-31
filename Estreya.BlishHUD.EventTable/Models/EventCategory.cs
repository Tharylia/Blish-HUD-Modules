namespace Estreya.BlishHUD.EventTable.Models
{
    using Blish_HUD;
    using Estreya.BlishHUD.EventTable.State;
    using Estreya.BlishHUD.Shared.Attributes;
    using Estreya.BlishHUD.Shared.Helpers;
    using Estreya.BlishHUD.Shared.State;
    using Estreya.BlishHUD.Shared.Utils;
    using Gw2Sharp.WebApi.V2.Models;
    using Humanizer;
    using Microsoft.Xna.Framework;
    using Newtonsoft.Json;
    using SharpDX.Text;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using static Humanizer.On;

    [Serializable]
    public class EventCategory
    {
        [IgnoreCopy]
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
        private Func<DateTime> _getNowAction;

        public EventCategory()
        {
            this.timeSinceUpdate = this.updateInterval.TotalMilliseconds;
        }

        public void UpdateFillers(List<Event> fillers)
        {
            lock (this._fillerEvents)
            {
                this._fillerEvents.Clear();

                if (fillers != null)
                {
                    this._fillerEvents.AddRange(fillers);
                }
            }
        }

            public async Task UpdateEventOccurences(List<Event> fillers, DateTime now, DateTime min, DateTime max, List<string> activeEventKeys, Func<Event, bool> isDisabledFunc)
        {
            lock (this._fillerEvents)
            {
                this._fillerEvents.Clear();

                if (fillers != null)
                {
                    this._fillerEvents.AddRange(fillers);
                }
            }

            /*
            using (this._eventLock.Lock())
            {
                this._originalEvents.ForEach(ev => ev.UpdateOccurences(now, min, max));
            }
            */
            /*
            var activeEvents = this._originalEvents.Where(e => !isDisabledFunc(e) && activeEventKeys.Contains(e.SettingKey)).ToList();
            //List<Event> activeEvents = this._originalEvents.Where(e => !e.IsDisabled).ToList();

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

                    await filler.LoadAsync(this, this._getNowAction);

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
                    return new KeyValuePair<DateTime, Event>(ae.Occurences.Where(oc => oc > lastEvent.Key && oc < max.AddDays(2)).FirstOrDefault(), ae);
                }).OrderBy(aeo => aeo.Key).First();


                if (nextEvent.Key != default)
                {
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

                        await filler.LoadAsync(this, this._getNowAction);

                        modifiedEventStarts.Add(new KeyValuePair<DateTime, Event>(lastEvent.Key + TimeSpan.FromMinutes(lastEvent.Value.Duration), filler));
                    }
                }

                // We have a previous event
                KeyValuePair<DateTime, Event> prevEvent = activeEvents.Select(ae =>
                {
                    return new KeyValuePair<DateTime, Event>(ae.Occurences.Where(oc => oc > min.AddDays(-2) && oc < firstEvent.Key).LastOrDefault(), ae);
                }).OrderBy(aeo => aeo.Key).Last();

                if (prevEvent.Key != default)
                {
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

                        await filler.LoadAsync(this, this._getNowAction);

                        modifiedEventStarts.Add(new KeyValuePair<DateTime, Event>(prevEnd, filler));
                    }
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
                    return new KeyValuePair<DateTime, Event>(ae.Occurences.Where(oc => oc > currentEnd && oc < max.AddDays(2)).FirstOrDefault(), ae);
                }).OrderBy(aeo => aeo.Key).First();

                if (nextEvent.Key != default)
                {
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

                        await filler.LoadAsync(this, this._getNowAction);

                        modifiedEventStarts.Add(new KeyValuePair<DateTime, Event>(currentEnd, filler));
                    }
                }

                // We have a previous event
                KeyValuePair<DateTime, Event> prevEvent = activeEvents.Select(ae =>
                {
                    return new KeyValuePair<DateTime, Event>(ae.Occurences.Where(oc => oc > min.AddDays(-2) && oc < currentStart).LastOrDefault(), ae);
                }).OrderBy(aeo => aeo.Key).Last();

                if (prevEvent.Key != default)
                {
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

                        await filler.LoadAsync(this, this._getNowAction);

                        modifiedEventStarts.Add(new KeyValuePair<DateTime, Event>(prevEnd, filler));
                    }
                }
            }
            else if (activeEventStarts.Count == 0 && activeEvents.Count >= 1)
            {
                KeyValuePair<DateTime, Event> prevEvent = activeEvents.Select(ae =>
                {
                    return new KeyValuePair<DateTime, Event>(ae.Occurences.Where(oc => oc > min.AddDays(-2) && oc < max).LastOrDefault(), ae);
                }).OrderBy(aeo => aeo.Key).Last();

                KeyValuePair<DateTime, Event> nextEvent = activeEvents.Select(ae =>
                {
                    return new KeyValuePair<DateTime, Event>(ae.Occurences.Where(oc => oc > min && oc < max.AddDays(2)).FirstOrDefault(), ae);
                }).OrderBy(aeo => aeo.Key).First();

                if (prevEvent.Key != default && nextEvent.Key != default)
                {

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

                    await filler.LoadAsync(this, this._getNowAction);

                    modifiedEventStarts.Add(new KeyValuePair<DateTime, Event>(prevEnd, filler));
                }
            }

            lock (this._fillerEvents)
            {
                modifiedEventStarts.Where(e => e.Value.Filler).ToList().ForEach(modEvent => modEvent.Value.Occurences.Add(DateTime.SpecifyKind(modEvent.Key, DateTimeKind.Utc)));
                IEnumerable<Event> modifiedEvents = modifiedEventStarts.Where(e => e.Value.Filler).Select(e => e.Value);
                this._fillerEvents.AddRange(modifiedEvents);
            }
            */


            //return modifiedEventStarts.OrderBy(mes => mes.Key).ToList();
        }
        /*
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
        */

        public async Task LoadAsync(Func<DateTime> getNowAction, TranslationState translationState = null)
        {
            this._getNowAction = getNowAction;

            if (translationState != null)
            {
                this.Name = translationState.GetTranslation($"eventCategory-{this.Key}-name", this.Name);
            }

            using (await this._eventLock.LockAsync())
            {
                var eventLoadTasks = this.Events.Select(ev =>
                {
                    return ev.LoadAsync(this, getNowAction, translationState);
                });

                await Task.WhenAll(eventLoadTasks);
            }
        }
    }
}
