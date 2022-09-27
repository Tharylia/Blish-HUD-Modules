namespace Estreya.BlishHUD.ScrollingCombatText.Controls;

using Blish_HUD;
using Blish_HUD._Extensions;
using Blish_HUD.Controls;
using Estreya.BlishHUD.ScrollingCombatText.Models;
using Estreya.BlishHUD.Shared.Extensions;
using Humanizer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.BitmapFonts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using static Blish_HUD.ContentService;

public class ScrollingTextArea : Container
{
    private static readonly Logger Logger = Logger.GetLogger<ScrollingTextArea>();

    private static readonly ConcurrentDictionary<FontSize, BitmapFont> _fonts = new ConcurrentDictionary<FontSize, BitmapFont>();

    private const int MAX_CONCURRENT_EVENTS = 1000;

    private const int MAX_CONSECUTIVE_FAIL_ITERATIONS = 10;

    private readonly SynchronizedCollection<ScrollingTextAreaEvent> _activeEvents = new SynchronizedCollection<ScrollingTextAreaEvent>();

    private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

    private readonly EventWaitHandle _updateWaitHandle = new EventWaitHandle(true, EventResetMode.ManualReset);

    private readonly Task _updateWorker;


#if DEBUG
    /// <summary>
    /// Last time <see cref="_callsPerInterval"/> was updated.
    /// </summary>
    private DateTime _lastCallUpdate = DateTime.UtcNow;

    /// <summary>
    /// The time interval in which <see cref="_callsPerInterval"/> is calculated.
    /// </summary>
    private TimeSpan _callUpdateInterval = TimeSpan.FromMilliseconds(1000);

    /// <summary>
    /// Temporary amount of calls per interval. Gets swapped to <see cref="_callsPerInterval"/> after the specified interval of <see cref="_callUpdateInterval"/>.
    /// </summary>
    private int _tempCallsPerInterval;

    /// <summary>
    /// The calculated calls per interval specified by <see cref="_callUpdateInterval"/>.
    /// </summary>
    private int _callsPerInterval;

    //private List<KeyValuePair<DateTime, int>> _callsPerIntervalHistory = new List<KeyValuePair<DateTime, int>>();
#endif

    public new bool Enabled => this.Configuration?.Enabled.Value ?? false;

    public ScrollingTextAreaConfiguration Configuration { get; private set; }

    public ScrollingTextArea(ScrollingTextAreaConfiguration configuration)
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

        this._updateWorker = Task.Factory.StartNew(this.HandleUpdate, TaskCreationOptions.LongRunning).Unwrap();
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

    public void AddCombatEvent(Shared.Models.ArcDPS.CombatEvent combatEvent)
    {
        if (!this.Enabled || this._cancellationTokenSource.IsCancellationRequested || !this.CheckConfiguration(combatEvent))
        {
            return;
        }

        try
        {
            CombatEventFormatRule rule = this.Configuration.FormatRules.Value.Find(rule => rule.Category == combatEvent.Category && rule.Type == combatEvent.Type);

            if (!rule.Validate())
            {
                Logger.Warn($"Rule '{rule.Name}' of area '{this.Configuration.Name}' is invalid. Expect possible errors.");
            }

            BitmapFont font = _fonts.GetOrAdd(rule?.FontSize ?? FontSize.Size16, (fontSize) => GameService.Content.GetFont(FontFace.Menomonia, fontSize, FontStyle.Regular));

            Color textColor = Color.White;
            if (rule?.TextColor != null && rule.TextColor.Id != 1)
            {
                textColor = rule.TextColor.Cloth.ToXnaColor();
            }

            ScrollingTextAreaEvent scrollingTextAreaEvent = new ScrollingTextAreaEvent(combatEvent, rule, font)
            {
                Parent = this,
                Height = this.Configuration.EventHeight.Value,
                BaseTextColor = textColor,
                Opacity = 0f,
                Visible = true,
                DrawInterval = TimeSpan.FromSeconds(30)
            };

            scrollingTextAreaEvent.CalculateLayout();
            scrollingTextAreaEvent.CalculateScrollingTexts();

            scrollingTextAreaEvent.Disposed += this.ScrollingTextAreaEvent_Disposed;

            this._activeEvents.Add(scrollingTextAreaEvent);

            // Signal because of new events
            _ = this._updateWaitHandle.Set();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Could not add event to area '{this.Configuration.Name}':");
        }
    }

    private float GetNow()
    {
        return (float)DateTime.UtcNow.TimeOfDay.TotalMilliseconds;
    }

    private float GetActualScrollspeed()
    {
        return this.Configuration.ScrollSpeed.Value / 3;
    }

    protected override CaptureType CapturesInput()
    {
        return CaptureType.None;
    }

    private async Task HandleUpdate()
    {
        int currentConsecutiveFails = 0;
        do
        {
            try
            {
                // Only lock once
                List<ScrollingTextAreaEvent> activeEvents = new List<ScrollingTextAreaEvent>();

                try
                {
                    activeEvents.AddRange(this._activeEvents.ToList());
                }
                catch (ArgumentException)
                {
                    // Don't let this be seen.
                    // Return as there are no events to process anyway.
                    continue;
                }

                if (activeEvents == null || activeEvents.Count == 0)
                {
                    Logger.Debug($"Scrolling area \"{this.Configuration.Name}\" has no events. Waiting.");

#if DEBUG
                    this.CalculateUpdateRate(0);
#endif

                    _ = this._updateWaitHandle.Reset();
                    _ = await this._updateWaitHandle.WaitOneAsync(Timeout.InfiniteTimeSpan, this._cancellationTokenSource.Token);

                    if (this._cancellationTokenSource.IsCancellationRequested)
                    {
                        continue;
                    }

                    Logger.Debug($"Scrolling area \"{this.Configuration.Name}\" has events. Continue.");
                }

                if (activeEvents.Count >= MAX_CONCURRENT_EVENTS)
                {
                    // Fail safe when processing gets laggy
                    Logger.Warn($"Area \"{this.Configuration.Name}\" has reached max events of {MAX_CONCURRENT_EVENTS}. Clear for better performance.");

                    this._activeEvents.Clear();
                    continue;
                }

#if DEBUG
                this.CalculateUpdateRate();
#endif

                float now = this.GetNow();
                float actualScrollspeed = this.GetActualScrollspeed();

                if (activeEvents.Count >= 2)
                {
                    // Move olders events down, if not enough space
                    ScrollingTextAreaEvent lastChecked = activeEvents[activeEvents.Count - 1];
                    for (int i = activeEvents.Count - 2; i >= 0; i--)
                    {
                        ScrollingTextAreaEvent activeEvent = activeEvents[i];
                        if (lastChecked.Bottom > activeEvent.Top)
                        {
                            activeEvent.Time -= this.DistanceToTime(now, actualScrollspeed, lastChecked.Bottom - activeEvent.Top);
                        }

                        lastChecked = activeEvent;
                    }
                }

                float fadeLength = 0.10f;
                for (int i = 0; i < activeEvents.Count; i++)
                {
                    ScrollingTextAreaEvent activeEvent = activeEvents[i];

                    float animatedHeight = (float)(now - activeEvent.Time) * actualScrollspeed;
                    float percentage = animatedHeight / this.Height;

                    if (percentage > 1)
                    {
                        activeEvent.Dispose();
                        //i--;
                        continue;
                    }

                    float alpha = 1 - (percentage - 1f + fadeLength) / fadeLength;
                    activeEvent.Opacity = alpha;

                    float x = 0;

                    switch (this.Configuration.Curve.Value)
                    {
                        case ScrollingTextAreaCurve.Left:
                            if (activeEvent.Width < this.Width)
                            {
                                x += (this.Width - activeEvent.Width) * (2 * percentage - 1) * (2 * percentage - 1);
                            }

                            break;
                        case ScrollingTextAreaCurve.Right:
                            if (activeEvent.Width < this.Width)
                            {
                                x += (this.Width - activeEvent.Width) * (1 - (2 * percentage - 1) * (2 * percentage - 1));
                            }

                            break;
                        case ScrollingTextAreaCurve.Straight:
                            break;
                        default:
                            break;
                    }

                    activeEvent.Location = new Point((int)x, (int)animatedHeight);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, $"{nameof(HandleUpdate)} failed:");
                currentConsecutiveFails++;

                if (currentConsecutiveFails >= MAX_CONSECUTIVE_FAIL_ITERATIONS)
                {
                    Logger.Error($"{nameof(HandleUpdate)} failed at least {MAX_CONSECUTIVE_FAIL_ITERATIONS} times. Abort worker.");
                    this._cancellationTokenSource.Cancel();
                }

                continue;
            }

            currentConsecutiveFails = 0; // Reset fail chain
        } while (!this._cancellationTokenSource.IsCancellationRequested);

        Logger.Debug($"{nameof(HandleUpdate)} for area '{this.Configuration.Name}' exited.");
    }

#if DEBUG
    private void CalculateUpdateRate(int overrideUpdateRate = -1)
    {
        var now = DateTime.UtcNow;

        if (overrideUpdateRate != -1)
        {
            this._callsPerInterval = overrideUpdateRate;
            _tempCallsPerInterval = 0;
            _lastCallUpdate = now;
            return;
        }

        _tempCallsPerInterval++;

        if ((now - _lastCallUpdate).TotalMilliseconds >= _callUpdateInterval.TotalMilliseconds)
        {
            _callsPerInterval = _tempCallsPerInterval;

            _tempCallsPerInterval = 0;
            _lastCallUpdate = now;

            //this._callsPerIntervalHistory.Add(new KeyValuePair<DateTime, int>(now, _callsPerInterval));
        }
    }

    public override void PaintBeforeChildren(SpriteBatch spriteBatch, Rectangle bounds)
    {
        spriteBatch.DrawStringOnCtrl(this, $"Calls per {_callUpdateInterval.Humanize()}: {_callsPerInterval:n0}", GameService.Content.DefaultFont16, new Rectangle(0, 0, this.Width, GameService.Content.DefaultFont16.LineHeight), Color.White);
    }
#endif

    private float DistanceToTime(float timeNow, float actualScrollspeed, float distance)
    {
        float x = timeNow - (distance - (timeNow * actualScrollspeed)) / -actualScrollspeed;
        return x;
    }

    private bool CheckConfiguration(Shared.Models.ArcDPS.CombatEvent combatEvent)
    {
        if (this.Configuration.Categories != null && !this.Configuration.Categories.Value.Contains(combatEvent.Category))
        {
            return false;
        }

        if (this.Configuration.Types != null && !this.Configuration.Types.Value.Contains(combatEvent.Type))
        {
            return false;
        }

        return true;
    }

    private void ScrollingTextAreaEvent_Disposed(object sender, EventArgs e)
    {
        ScrollingTextAreaEvent scrollingTextAreaEvent = sender as ScrollingTextAreaEvent;

        scrollingTextAreaEvent.Disposed -= this.ScrollingTextAreaEvent_Disposed;

        _ = this._activeEvents.Remove(scrollingTextAreaEvent);
    }

    protected override void DisposeControl()
    {
        // Cancel token to trigger worker stopping.
        this._cancellationTokenSource.Cancel();

        // Wait until worker has stopped.
        this._updateWorker.Wait();
        this._updateWorker.Dispose();

        this.Configuration.Size.X.SettingChanged -= this.Size_SettingChanged;
        this.Configuration.Size.Y.SettingChanged -= this.Size_SettingChanged;
        this.Configuration.Location.X.SettingChanged -= this.Location_SettingChanged;
        this.Configuration.Location.Y.SettingChanged -= this.Location_SettingChanged;
        this.Configuration.Opacity.SettingChanged -= this.Opacity_SettingChanged;
        this.Configuration.BackgroundColor.SettingChanged -= this.BackgroundColor_SettingChanged;

        this.Configuration = null;

        this._activeEvents.Clear();
    }
}
