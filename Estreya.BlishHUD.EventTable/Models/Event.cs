namespace Estreya.BlishHUD.EventTable.Models;

using Blish_HUD;
using Gw2Sharp.WebApi.V2.Models;
using Humanizer;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using NodaTime;
using Shared.Attributes;
using Shared.Json.Converter;
using Shared.Services;
using Shared.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

[Serializable]
public class Event : IUpdatable
{
    [IgnoreCopy] private static readonly Logger Logger = Blish_HUD.Logger.GetLogger<Event>();

    [IgnoreCopy] private static Duration _checkForRemindersInterval = Duration.FromMilliseconds(5000);

    [JsonIgnore, IgnoreCopy] private Func<Instant> _getNowAction;
    [JsonIgnore, IgnoreCopy] private TranslationService _translationService;

    [IgnoreCopy] private double _lastCheckForReminders;

    [JsonIgnore] private ConcurrentDictionary<Instant, List<Duration>> _remindedFor = new ConcurrentDictionary<Instant, List<Duration>>();

    [Description("Specifies the key of the event. Should be unique for a event category. Avoid changing it, as it resets saved settings and states.")]
    [JsonProperty("key")]
    public string Key { get; set; }

    /// <summary>
    ///     The name of the event.
    ///     <br />
    ///     Will get overridden with the localized event name if available.
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("offset")]
    //[JsonConverter(typeof(TimeSpanJsonConverter), "dd\\.hh\\:mm\\:ss", new[]
    //{
    //    "dd\\.hh\\:mm\\:ss",
    //    "hh\\:mm\\:ss"
    //})]
    public Duration Offset { get; set; }

    [JsonProperty("repeat")]
    //[JsonConverter(typeof(TimeSpanJsonConverter), "dd\\.hh\\:mm\\:ss", new[]
    //{
    //    "dd\\.hh\\:mm\\:ss",
    //    "hh\\:mm\\:ss"
    //})]
    public Duration Repeat { get; set; }

    [JsonProperty("startingDate")]
    //[JsonConverter(typeof(DateJsonConverter))]
    public LocalDate? StartingDate { get; set; }

    [JsonProperty("location")] public string Location { get; set; }

    [JsonProperty("timers")] public EventTimers[] Timers {  get; set; }

    [JsonProperty("mapIds")] public int[] MapIds { get; set; }

    [JsonProperty("waypoints")] public EventWaypoints Waypoints { get; set; }

    [JsonProperty("wiki")] public string Wiki { get; set; }

    [JsonProperty("duration")] public Duration Duration { get; set; }

    [JsonProperty("icon")] public string Icon { get; set; }

    [JsonProperty("color")] public string BackgroundColorCode { get; set; }

    [JsonProperty("colorGradient")] public string[] BackgroundColorGradientCodes { get; set; }

    [JsonProperty("apiType")] public APICodeType? APICodeType { get; set; }

    [JsonProperty("apiCode")] public string APICode { get; set; }

    [JsonProperty("linkedCompletion")] public bool LinkedCompletion { get; set; }

    /// <summary>
    ///     Sets the keys to also complete if this one completes. Similar to <see cref="APICode"/>.
    /// </summary>
    [JsonProperty("linkedCompletionKeys")] public string[] LinkedCompletionKeys { get; set; }

    [JsonProperty("filler")] public bool Filler { get; set; }

    [JsonProperty("occurrences")] public List<Instant> Occurences { get; set; } = new List<Instant>();

    [JsonIgnore] public string SettingKey { get; private set; }

    [JsonIgnore] public WeakReference<EventCategory> Category { get; private set; }

    [JsonProperty("reminderTimes")]
    //[JsonConverter(typeof(TimeSpanArrayJsonConverter), "hh\\:mm\\:ss", new[]
    //{
    //    "hh\\:mm",
    //    "hh\\:mm\\:ss"
    //}, true)]
    public Duration[] ReminderTimes { get; private set; } =
    {
        Duration.FromMinutes(10)
    };

    public void Update(GameTime gameTime)
    {
        UpdateUtil.Update(this.CheckForReminder, gameTime, _checkForRemindersInterval.TotalMilliseconds, ref this._lastCheckForReminders);
    }

    public event EventHandler<Duration> Reminder;

    public double CalculateXPosition(Instant start, Instant min, double pixelPerMinute)
    {
        double minutesSinceMin = start.Minus(min).TotalMinutes;
        return minutesSinceMin * pixelPerMinute;
    }

    public double CalculateWidth(Instant eventOccurence, Instant min, int maxWidth, double pixelPerMinute)
    {
        double eventWidth = this.Duration.TotalMinutes * pixelPerMinute;

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

    public void Load(EventCategory ec, Func<Instant> getNowAction, TranslationService translationService = null)
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
        this._translationService = translationService;

        if (this._translationService != null)
        {
            this.Name = translationService.GetTranslation($"event-{ec.Key}_{this.Key}-name", this.Name);
        }
    }

    private void CheckForReminder()
    {
        if (this.Filler)
        {
            return;
        }

        Instant nowUTC = this._getNowAction();

        IEnumerable<Instant> occurences = this.Occurences.Where(o => o >= nowUTC);
        foreach (Instant occurence in occurences)
        {
            List<Duration> alreadyRemindedTimes = this._remindedFor.GetOrAdd(occurence, o => new List<Duration>());

            List<(Duration reminderTime, Duration timeLeft)> eligableTimes = new List<(Duration reminderTime, Duration timeLeft)>();

            foreach (Duration time in this.ReminderTimes)
            {
                if (alreadyRemindedTimes.Contains(time))
                {
                    continue;
                }

                var remindAt = occurence - time;
                Duration diff = remindAt - nowUTC;
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

    public void UpdateReminderTimes(Duration[] reminderTimes)
    {
        this.ReminderTimes = reminderTimes;
        this._remindedFor.Clear();
    }

    public Instant GetCurrentOccurrence()
    {
        var now = this._getNowAction();

        return this.Occurences.OrderBy(x => x).FirstOrDefault(x => x <= now && x.Plus(this.Duration) >= now);
    }

    public Instant GetNextOccurrence()
    {
        var now = this._getNowAction();

        return this.Occurences.OrderBy(x => x).FirstOrDefault(x => x >= now);
    }

    public string GetWaypoint(Account account)
    {
        if (this.Waypoints == null) return null;

        if (account is null)
        {
            Logger.Warn("Account is null. Returning EU waypoint.");
            return this.Waypoints.EU;
        }

        return account.World.ToString().First() switch
        {
            '1' => this.Waypoints.NA,
            _ => this.Waypoints.EU,
        };
    }

    public string GetChatText(EventChatFormat format, Instant occurence, Account account)
    {
        var now = this._getNowAction();

        //DateTime occurenceLocal = occurence;

        var endTime = occurence.Plus(this.Duration);
        bool isPrev = endTime < now;
        bool isNext = !isPrev && occurence > now;
        bool isCurrent = !isPrev && !isNext;

        string timeString = occurence.InZone(DateTimeZoneProviders.Tzdb.GetSystemDefault()).ToString("HH:mm zzz", CultureInfo.CurrentUICulture);

        if (isPrev)
        {
            var finishedSince = now - occurence.Plus(this.Duration);
            timeString = $"{this._translationService.GetTranslation("event-chatText-finishedXAgo", "finished {0} ago").FormatWith(finishedSince.ToTimeSpan().Humanize(2, minUnit: Humanizer.Localisation.TimeUnit.Second))}";
        }
        else if (isNext)
        {
            var startsIn = occurence - now;
            timeString = $"{this._translationService.GetTranslation("event-chatText-startsInX", "starts in {0}").FormatWith(startsIn.ToTimeSpan().Humanize(2, minUnit: Humanizer.Localisation.TimeUnit.Second))}";
        }
        else if (isCurrent)
        {
            var remaining = endTime - now;
            timeString = $"{this._translationService.GetTranslation("event-chatText-hasXRemaining", "has {0} remaining").FormatWith(remaining.ToTimeSpan().Humanize(2, minUnit: Humanizer.Localisation.TimeUnit.Second))}";
        }

        var waypoint = this.GetWaypoint(account);

        return format switch
        {
            EventChatFormat.Full => this._translationService.GetTranslation("event-chatText-format-full","Event \"{0}\" {1} in \"{2}\": {3}").FormatWith(this.Name, timeString, this.Location,waypoint),
            EventChatFormat.WithTime => this._translationService.GetTranslation("event-chatText-format-withTime", "Event \"{0}\" {1}: {2}").FormatWith(this.Name, timeString, waypoint),
            EventChatFormat.WithLocation => this._translationService.GetTranslation("event-chatText-format-withLocation", "Event \"{0}\" in \"{1}\": {2}").FormatWith(this.Name, this.Location, waypoint),
            _ => waypoint,
        };
    }

    public override string ToString()
    {
        string[] keySplit = this.SettingKey?.Split('_') ?? new[]
        {
            string.Empty,
            this.Name
        };
        return $"Category: {keySplit[0]} - Name: {keySplit[1]} - Filler {this.Filler}";
    }
}