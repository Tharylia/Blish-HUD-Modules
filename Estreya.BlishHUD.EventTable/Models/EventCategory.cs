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

        public void Load(Func<DateTime> getNowAction, TranslationState translationState = null)
        {
            if (translationState != null)
            {
                this.Name = translationState.GetTranslation($"eventCategory-{this.Key}-name", this.Name);
            }

            using (this._eventLock.Lock())
            {
                this.Events.ForEach(ev =>
                {
                    ev.Load(this, getNowAction, translationState);
                });
            }
        }
    }
}
