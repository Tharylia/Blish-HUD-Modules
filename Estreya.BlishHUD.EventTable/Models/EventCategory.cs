namespace Estreya.BlishHUD.EventTable.Models;

using Blish_HUD;
using Newtonsoft.Json;
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

    [JsonIgnore] private List<Event> _fillerEvents = new List<Event>();

    [JsonProperty("events")] private List<Event> _originalEvents = new List<Event>();

    [JsonProperty("key")] public string Key { get; set; }

    [JsonProperty("name")] public string Name { get; set; }

    [JsonProperty("icon")] public string Icon { get; set; }

    [JsonProperty("showCombined")] public bool ShowCombined { get; set; }

    [JsonIgnore]
    public List<Event> Events
    {
        get => this._originalEvents.Concat(this._fillerEvents).ToList();
        set => this._originalEvents = value;
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

    public void Load(Func<DateTime> getNowAction, TranslationService translationService = null)
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