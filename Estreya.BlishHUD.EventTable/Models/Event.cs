namespace Estreya.BlishHUD.EventTable.Models
{
    using Blish_HUD;
    using Blish_HUD._Extensions;
    using Blish_HUD.ArcDps.Models;
    using Blish_HUD.Controls;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.EventTable.State;
    using Estreya.BlishHUD.Shared.Attributes;
    using Estreya.BlishHUD.Shared.Extensions;
    using Estreya.BlishHUD.Shared.State;
    using Estreya.BlishHUD.Shared.Utils;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using MonoGame.Extended;
    using MonoGame.Extended.BitmapFonts;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;

    [Serializable]
    public class Event
    {
        [IgnoreCopy]
        private static readonly Logger Logger = Logger.GetLogger<Event>();

        public event EventHandler<TimeSpan> Reminder;

        [Description("Specifies the key of the event. Should be unique for a event category. Avoid changing it, as it resets saved settings and states.")]
        [JsonProperty("key")]
        public string Key { get; set; }

        /// <summary>
        /// The name of the event.
        /// <br/>
        /// Will get overridden with the localized event name if available.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("offset"), JsonConverter(typeof(Shared.Json.Converter.TimeSpanJsonConverter), "dd\\.hh\\:mm", new string[] { "dd\\.hh\\:mm", "hh\\:mm" })]
        public TimeSpan Offset { get; set; }

        [JsonProperty("repeat"), JsonConverter(typeof(Shared.Json.Converter.TimeSpanJsonConverter), "dd\\.hh\\:mm", new string[] { "dd\\.hh\\:mm", "hh\\:mm" })]
        public TimeSpan Repeat { get; set; }

        [JsonProperty("startingDate"), JsonConverter(typeof(Shared.Json.Converter.DateJsonConverter))]
        public DateTime? StartingDate { get; set; }

        [JsonProperty("location")]
        public string Location { get; set; }

        [JsonProperty("mapIds")]
        public int[] MapIds { get; set; }

        [JsonProperty("waypoint")]
        public string Waypoint { get; set; }

        [JsonProperty("wiki")]
        public string Wiki { get; set; }

        [JsonProperty("duration")]
        public int Duration { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }

        [JsonProperty("color")]
        public string BackgroundColorCode { get; set; }

        [JsonProperty("apiType")]
        public APICodeType? APICodeType { get; set; }

        [JsonProperty("apiCode")]
        public string APICode { get; set; }

        [JsonIgnore]
        public bool Filler { get; set; }

        [JsonProperty("occurences")]
        public List<DateTime> Occurences { get; private set; } = new List<DateTime>();

        [JsonIgnore]
        public string SettingKey { get; private set; }

        [JsonIgnore]
        public TimeSpan[] ReminderTimes = new[]
        {
            TimeSpan.FromMinutes(10)
        };

        [JsonIgnore]
        private ConcurrentDictionary<DateTime, List<TimeSpan>> _remindedFor = new ConcurrentDictionary<DateTime, List<TimeSpan>>();
               

        public double CalculateXPosition(DateTime start, DateTime min, double pixelPerMinute)
        {
            double minutesSinceMin = start.Subtract(min).TotalMinutes;
            return minutesSinceMin * pixelPerMinute;
        }

        public double CalculateWidth(DateTime eventOccurence, DateTime min, int maxWidth, double pixelPerMinute)
        {
            double eventWidth = this.Duration * pixelPerMinute;

            double x = this.CalculateXPosition(eventOccurence, min, pixelPerMinute);

            if (x < 0)
            {
                eventWidth -= Math.Abs(x);
            }

            // Only draw event until end of form
            if ((x > 0 ? x : 0) + eventWidth > maxWidth)
            {
                eventWidth = maxWidth - (x > 0 ? x : 0);
            }

            //eventWidth = Math.Min(eventWidth, bounds.Width/* - x*/);

            return eventWidth;
        }

        public void Load(EventCategory ec, TranslationState translationState = null)
        {
            // Prevent crash on older events.json files
            if (string.IsNullOrWhiteSpace(this.Key))
            {
                this.Key = this.Name;
            }

            if (string.IsNullOrWhiteSpace(this.Icon))
            {
                this.Icon = ec.Icon;
            }

            this.SettingKey = $"{ec.Key}_{this.Key}";

            if (translationState != null)
            {
                this.Name = translationState.GetTranslation($"event-{ec.Key}_{this.Key}-name", this.Name);
            }
        }

        public void Update(DateTime nowUTC)
        {
            if (this.Filler) return;

            var occurences = this.Occurences.Where(o => o >= nowUTC);
            foreach (var occurence in occurences)
            {
                var alreadyRemindedTimes = this._remindedFor.GetOrAdd(occurence, o => new List<TimeSpan>());

                foreach (var time in this.ReminderTimes)
                {
                    if (alreadyRemindedTimes.Contains(time)) continue;

                    var remindAt = occurence - time;
                    var diff = nowUTC - remindAt;
                    if (remindAt <= nowUTC && Math.Abs(diff.TotalSeconds) <= 1)
                    {
                        this.Reminder?.Invoke(this, time);
                        alreadyRemindedTimes.Add(time);
                    }
                }
            }
        }

        public override string ToString()
        {
            var keySplit = this.SettingKey?.Split('_') ?? new string[] { string.Empty, this.Name };
            return $"Category: {keySplit[0]} - Name: {keySplit[1]} - Filler {this.Filler}";
        }
    }
}
