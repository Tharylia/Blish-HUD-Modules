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
    public class Event : IUpdatable
    {
        [IgnoreCopy]
        private static readonly Logger Logger = Logger.GetLogger<Event>();

        [IgnoreCopy]
        private static TimeSpan _checkForRemindersInterval = TimeSpan.FromMilliseconds(5000);

        [IgnoreCopy]
        private double _lastCheckForReminders = 0;

        private Func<DateTime> _getNowAction;

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

        [JsonProperty("offset"), JsonConverter(typeof(Shared.Json.Converter.TimeSpanJsonConverter), "dd\\.hh\\:mm\\:ss", new string[] { "dd\\.hh\\:mm\\:ss", "hh\\:mm\\:ss" })]
        public TimeSpan Offset { get; set; }

        [JsonProperty("repeat"), JsonConverter(typeof(Shared.Json.Converter.TimeSpanJsonConverter), "dd\\.hh\\:mm\\:ss", new string[] { "dd\\.hh\\:mm\\:ss", "hh\\:mm\\:ss" })]
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
        public WeakReference<EventCategory> Category { get; private set; }

        [JsonProperty("reminderTimes"), JsonConverter(typeof(Shared.Json.Converter.TimeSpanArrayJsonConverter), "hh\\:mm\\:ss", new string[] { "hh\\:mm", "hh\\:mm\\:ss" }, true)]
        public TimeSpan[] ReminderTimes { get; private set; } = new[]
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

        public void Load(EventCategory ec, Func<DateTime> getNowAction, TranslationState translationState = null)
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
            this.Category = new WeakReference<EventCategory>(ec);

            this._getNowAction = getNowAction;

            if (translationState != null)
            {
                this.Name = translationState.GetTranslation($"event-{ec.Key}_{this.Key}-name", this.Name);
            }
        }

        public void Update(GameTime gameTime)
        {
            UpdateUtil.Update(this.CheckForReminder, gameTime, _checkForRemindersInterval.TotalMilliseconds, ref this._lastCheckForReminders);
        }

        private void CheckForReminder()
        {
            if (this.Filler) return;

            var nowUTC = this._getNowAction();

            var occurences = this.Occurences.Where(o => o >= nowUTC);
            foreach (var occurence in occurences)
            {
                var alreadyRemindedTimes = this._remindedFor.GetOrAdd(occurence, o => new List<TimeSpan>());

                var eligableTimes = new List<(TimeSpan reminderTime, TimeSpan timeLeft)>();

                foreach (var time in this.ReminderTimes)
                {
                    if (alreadyRemindedTimes.Contains(time)) continue;

                    var remindAt = occurence - time;
                    var diff = remindAt - nowUTC;
                    if (remindAt < nowUTC || (remindAt >= nowUTC && diff.TotalSeconds <= 0))
                    {
                        eligableTimes.Add((time, occurence - nowUTC));
                        //this.Reminder?.Invoke(this, occurence - nowUTC);
                        //alreadyRemindedTimes.Add(time);
                    }
                }

                if (eligableTimes.Count > 0)
                {
                    eligableTimes.ForEach(et => alreadyRemindedTimes.Add(et.reminderTime));
                    this.Reminder?.Invoke(this, eligableTimes.OrderBy(et => et.reminderTime).First().timeLeft);
                }
            }
        }

        public void UpdateReminderTimes(TimeSpan[] reminderTimes)
        {
            this.ReminderTimes = reminderTimes;
            this._remindedFor.Clear();
        }

        public override string ToString()
        {
            var keySplit = this.SettingKey?.Split('_') ?? new string[] { string.Empty, this.Name };
            return $"Category: {keySplit[0]} - Name: {keySplit[1]} - Filler {this.Filler}";
        }
    }
}
