namespace Estreya.BlishHUD.EventTable.Models
{
    using Blish_HUD;
    using Blish_HUD._Extensions;
    using Blish_HUD.Controls;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.EventTable.Resources;
    using Estreya.BlishHUD.EventTable.State;
    using Estreya.BlishHUD.Shared.Extensions;
    using Estreya.BlishHUD.Shared.Utils;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using MonoGame.Extended;
    using MonoGame.Extended.BitmapFonts;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;

    [Serializable]
    public class Event
    {
        private static readonly Logger Logger = Logger.GetLogger<Event>();
        private EventState _eventState;
        private Func<DateTime> _getNowAction;

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

        [JsonProperty("api")]
        public string APICode { get; set; }

        [JsonIgnore]
        public bool Filler { get; set; }

        [JsonIgnore]
        public List<DateTime> Occurences { get; private set; }

        [JsonIgnore]
        public string SettingKey { get; private set; }

        [JsonIgnore]
        private bool? _isDisabled;

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

        public void UpdateOccurences(DateTime now, DateTime min, DateTime max)
        {
            this.Occurences = this.GetStartOccurences(now, min, max);
        }

        private List<DateTime> GetStartOccurences(DateTime now, DateTime min, DateTime max)
        {
            List<DateTime> startOccurences = new List<DateTime>();

            DateTime zero = this.StartingDate ?? new DateTime(min.Year, min.Month, min.Day, 0, 0, 0).AddDays(this.Repeat.TotalMinutes == 0 ? 0 : -1);

            TimeSpan offset = this.Offset.Add(TimeZone.CurrentTimeZone.GetUtcOffset(now));

            DateTime eventStart = zero.Add(offset);

            while (eventStart < max)
            {
                bool startAfterMin = eventStart > min;
                bool startBeforeMax = eventStart < max;
                bool endAfterMin = eventStart.AddMinutes(this.Duration) > min;

                bool inRange = (startAfterMin || endAfterMin) && startBeforeMax;

                if (inRange)
                {
                    startOccurences.Add(eventStart);
                }

                eventStart = this.Repeat.TotalMinutes == 0 ? eventStart.Add(TimeSpan.FromDays(1)) : eventStart.Add(this.Repeat);
            }

            return startOccurences;
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

        public Task LoadAsync(EventCategory ec, EventState eventState, Func<DateTime> getNowAction)
        {
            Logger.Debug("Load event: {0}", this.Key);

            this._eventState = eventState;
            this._getNowAction = getNowAction;

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

            Logger.Debug("Loaded event: {0}", this.Key);

            return Task.CompletedTask;
        }

        public void Unload()
        {
            Logger.Debug("Unload event: {0}", this.Key);

            Logger.Debug("Unloaded event: {0}", this.Key);
        }
    }
}
