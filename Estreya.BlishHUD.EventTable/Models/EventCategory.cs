namespace Estreya.BlishHUD.EventTable.Models;

using Blish_HUD;
using Newtonsoft.Json;
using NodaTime;
using Shared.Attributes;
using Shared.Services;
using Shared.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class EventCategory
{
    [IgnoreCopy] private static readonly Logger Logger = Logger.GetLogger<EventCategory>();

    [JsonIgnore] private AsyncLock _eventLock = new AsyncLock();

    [JsonProperty("fillers")] public List<Event> FillerEvents { get; private set; } = new List<Event>();

    [JsonProperty("events")] public List<Event> OriginalEvents { get; private set; } = new List<Event>();

    [JsonProperty("key")] public string Key { get; set; }

    [JsonProperty("name")] public string Name { get; set; }

    [JsonProperty("icon")] public string Icon { get; set; }

    [JsonProperty("showCombined")] public bool ShowCombined { get; set; }

    [JsonProperty("fromContext")] internal bool FromContext { get; set; }

    /// <summary>
    /// Gets original and filler events or sets original events. Adding to the getted list does nothing.
    /// </summary>
    [JsonIgnore]
    public List<Event> Events
    {
        get => this.OriginalEvents.Concat(this.FillerEvents).ToList();
    }

    public void UpdateFillers(List<Event> fillers)
    {
        lock (this.FillerEvents)
        {
            this.FillerEvents.Clear();

            if (fillers != null)
            {
                this.FillerEvents.AddRange(fillers);
            }
        }
    }

    public void UpdateOriginalEvents(List<Event> events)
    {
        lock (this.OriginalEvents)
        {
            this.OriginalEvents.Clear();

            if (events != null)
            {
                this.OriginalEvents.AddRange(events);
            }
        }
    }

    public void Load(Func<Instant> getNowAction, TranslationService translationService = null)
    {
        if (translationService != null)
        {
            this.Name = translationService.GetTranslation($"eventCategory-{this.Key}-name", this.Name);
        }

        using (this._eventLock.Lock())
        {
            this.Events.ForEach(ev =>
            {
                ev.Load(this, getNowAction, translationService);
            });
        }
    }
}