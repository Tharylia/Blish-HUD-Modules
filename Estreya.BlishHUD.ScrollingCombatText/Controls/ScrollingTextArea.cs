namespace Estreya.BlishHUD.ScrollingCombatText.Controls;

using Blish_HUD;
using Blish_HUD._Extensions;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Models;
using MonoGame.Extended;
using MonoGame.Extended.BitmapFonts;
using Shared.Extensions;
using Shared.Models.ArcDPS;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using static Blish_HUD.ContentService;
using Color = Gw2Sharp.WebApi.V2.Models.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

public class ScrollingTextArea : Control
{
    private const int MAX_CONCURRENT_EVENTS = 1000;
    private static readonly Logger Logger = Logger.GetLogger<ScrollingTextArea>();

    private static readonly ConcurrentDictionary<FontSize, BitmapFont> _fonts = new ConcurrentDictionary<FontSize, BitmapFont>();

    private readonly SynchronizedCollection<ScrollingTextAreaEvent> _activeEvents = new SynchronizedCollection<ScrollingTextAreaEvent>();

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
        this.BackgroundColor_SettingChanged(this, new ValueChangedEventArgs<Color>(null, this.Configuration.BackgroundColor.Value));
    }

    public new bool Enabled => this.Configuration?.Enabled.Value ?? false;

    public ScrollingTextAreaConfiguration Configuration { get; private set; }

    private void BackgroundColor_SettingChanged(object sender, ValueChangedEventArgs<Color> e)
    {
        Microsoft.Xna.Framework.Color backgroundColor = Microsoft.Xna.Framework.Color.Transparent;

        if (e.NewValue != null && e.NewValue.Id != 1)
        {
            backgroundColor = e.NewValue.Cloth.ToXnaColor();
        }

        this.BackgroundColor = backgroundColor * this.Configuration.Opacity.Value;
    }

    private void Opacity_SettingChanged(object sender, ValueChangedEventArgs<float> e)
    {
        this.BackgroundColor_SettingChanged(this, new ValueChangedEventArgs<Color>(null, this.Configuration.BackgroundColor.Value));
    }

    private void Location_SettingChanged(object sender, ValueChangedEventArgs<int> e)
    {
        this.Location = new Point(this.Configuration.Location.X.Value, this.Configuration.Location.Y.Value);
    }

    private void Size_SettingChanged(object sender, ValueChangedEventArgs<int> e)
    {
        this.Size = new Point(this.Configuration.Size.X.Value, this.Configuration.Size.Y.Value);
    }

    public void AddCombatEvent(CombatEvent combatEvent)
    {
        if (!this.Enabled || !this.CheckConfiguration(combatEvent))
        {
            return;
        }

        try
        {
            CombatEventFormatRule rule = this.Configuration.FormatRules.Value.Find(rule => rule.Category == combatEvent.Category && rule.Type == combatEvent.Type && rule.State == combatEvent.State);

            if (!rule.Validate())
            {
                Logger.Warn($"Rule '{rule.Name}' of area '{this.Configuration.Name}' is invalid. Expect possible errors.");
            }

            BitmapFont font = _fonts.GetOrAdd(rule?.FontSize ?? FontSize.Size16, fontSize => GameService.Content.GetFont(FontFace.Menomonia, fontSize, FontStyle.Regular));

            Microsoft.Xna.Framework.Color textColor = Microsoft.Xna.Framework.Color.White;
            if (rule?.TextColor != null && rule.TextColor.Id != 1)
            {
                textColor = rule.TextColor.Cloth.ToXnaColor();
            }

            // Width is currently not respected 
            ScrollingTextAreaEvent scrollingTextAreaEvent = new ScrollingTextAreaEvent(combatEvent, rule, font, this.Width, this.Configuration.EventHeight.Value) { BaseTextColor = textColor };

            scrollingTextAreaEvent.Disposed += this.ScrollingTextAreaEvent_Disposed;

            this._activeEvents.Add(scrollingTextAreaEvent);
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

    private float DistanceToTime(float timeNow, float actualScrollspeed, float distance)
    {
        float x = timeNow - ((distance - (timeNow * actualScrollspeed)) / -actualScrollspeed);
        return x;
    }

    private bool CheckConfiguration(CombatEvent combatEvent)
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
        this.Configuration.Size.X.SettingChanged -= this.Size_SettingChanged;
        this.Configuration.Size.Y.SettingChanged -= this.Size_SettingChanged;
        this.Configuration.Location.X.SettingChanged -= this.Location_SettingChanged;
        this.Configuration.Location.Y.SettingChanged -= this.Location_SettingChanged;
        this.Configuration.Opacity.SettingChanged -= this.Opacity_SettingChanged;
        this.Configuration.BackgroundColor.SettingChanged -= this.BackgroundColor_SettingChanged;

        this.Configuration = null;

        this._activeEvents?.Clear();
    }

    protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds)
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
                return;
            }

            if (activeEvents == null || activeEvents.Count == 0)
            {
                return;
            }

            if (activeEvents.Count >= MAX_CONCURRENT_EVENTS)
            {
                // Fail safe when processing gets laggy
                Logger.Warn($"Area \"{this.Configuration.Name}\" has reached max events of {MAX_CONCURRENT_EVENTS}. Clear for better performance.");

                this._activeEvents.Clear();
                return;
            }

            float now = this.GetNow();
            float actualScrollspeed = this.GetActualScrollspeed();

            if (activeEvents.Count >= 2)
            {
                // Move olders events down, if not enough spacew
                ScrollingTextAreaEvent lastChecked = activeEvents[activeEvents.Count - 1];
                for (int i = activeEvents.Count - 2; i >= 0; i--)
                {
                    ScrollingTextAreaEvent activeEvent = activeEvents[i];
                    // If events seem to overlap, check if activeEvent.Height is -1 for whatever reason.
                    if (lastChecked.Time <= activeEvent.Time + this.DistanceToTime(now, actualScrollspeed, activeEvent.Height))
                    {
                        activeEvent.Time -= this.DistanceToTime(now, actualScrollspeed, lastChecked.Height);
                    }

                    lastChecked = activeEvent;
                }
            }

            float fadeLength = 0.20f;
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

                float alpha = 1f;
                if (percentage > 1f - fadeLength)
                {
                    alpha = 1 - ((percentage - 1f + fadeLength) / fadeLength);
                }
                //activeEvent.Opacity = alpha;

                float x = 0;

                switch (this.Configuration.Curve.Value)
                {
                    case ScrollingTextAreaCurve.Left:
                        if (activeEvent.Width < this.Width)
                        {
                            x += (this.Width - activeEvent.Width) * ((2 * percentage) - 1) * ((2 * percentage) - 1);
                        }

                        break;
                    case ScrollingTextAreaCurve.Right:
                        if (activeEvent.Width < this.Width)
                        {
                            x += (this.Width - activeEvent.Width) * (1 - (((2 * percentage) - 1) * ((2 * percentage) - 1)));
                        }

                        break;
                    case ScrollingTextAreaCurve.Straight:
                        break;
                }

                RectangleF childBounds = new RectangleF(x, animatedHeight, activeEvent.Width, activeEvent.Height);
                activeEvent.Render(spriteBatch, childBounds.ToBounds(this.AbsoluteBounds), alpha);
            }
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, $"{nameof(this.Paint)} failed:");
        }
    }
}