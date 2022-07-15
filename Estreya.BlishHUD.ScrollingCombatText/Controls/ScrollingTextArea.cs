namespace Estreya.BlishHUD.ScrollingCombatText.Controls;

using Blish_HUD;
using Blish_HUD._Extensions;
using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.ScrollingCombatText.Models;
using Estreya.BlishHUD.Shared.State;
using Estreya.BlishHUD.Shared.Utils;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.BitmapFonts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;

public class ScrollingTextArea : Container
{
    private static readonly Logger Logger = Logger.GetLogger<ScrollingTextArea>();

    private readonly BitmapFont _font;
    private int _eventHeight;
    private Color _textColor;

    private readonly SynchronizedCollection<ScrollingTextAreaEvent> _activeEvents = new SynchronizedCollection<ScrollingTextAreaEvent>();

    public ScrollingTextAreaConfiguration Configuration { get; }

    public ScrollingTextArea(ScrollingTextAreaConfiguration configuration)
    {
        this.Configuration = configuration;

        this._font = GameService.Content.GetFont(ContentService.FontFace.Menomonia, this.Configuration.FontSize.Value, ContentService.FontStyle.Regular);

        this._eventHeight = this.Configuration.EventHeight.Value != -1 ? this.Configuration.EventHeight.Value : this._font.LineHeight;

        this.Configuration.Size.X.SettingChanged += this.Size_SettingChanged;
        this.Configuration.Size.Y.SettingChanged += this.Size_SettingChanged;
        this.Configuration.Location.X.SettingChanged += this.Location_SettingChanged;
        this.Configuration.Location.Y.SettingChanged += this.Location_SettingChanged;
        this.Configuration.Opacity.SettingChanged += this.Opacity_SettingChanged;
        this.Configuration.BackgroundColor.SettingChanged += this.BackgroundColor_SettingChanged;
        this.Configuration.EventHeight.SettingChanged += this.EventHeight_SettingChanged;
        this.Configuration.TextColor.SettingChanged += this.TextColor_SettingChanged;

        this.Location_SettingChanged(this, null);
        this.Size_SettingChanged(this, null);
        this.Opacity_SettingChanged(this, new ValueChangedEventArgs<float>(0f, this.Configuration.Opacity.Value));
        this.BackgroundColor_SettingChanged(this, new ValueChangedEventArgs<Gw2Sharp.WebApi.V2.Models.Color>(null, this.Configuration.BackgroundColor.Value));
        this.EventHeight_SettingChanged(this, new ValueChangedEventArgs<int>(0, this.Configuration.EventHeight.Value));
        this.TextColor_SettingChanged(this, new ValueChangedEventArgs<Gw2Sharp.WebApi.V2.Models.Color>(null, this.Configuration.TextColor.Value));
    }

    private void TextColor_SettingChanged(object sender, ValueChangedEventArgs<Gw2Sharp.WebApi.V2.Models.Color> e)
    {
        Color textColor = Color.Black;

        if (e.NewValue != null && e.NewValue.Id != 1)
        {
            textColor = e.NewValue.Cloth.ToXnaColor();
        }

        this._textColor = textColor;
    }

    private void EventHeight_SettingChanged(object sender, ValueChangedEventArgs<int> e)
    {
        this._eventHeight = e.NewValue;
    }

    private void BackgroundColor_SettingChanged(object sender, ValueChangedEventArgs<Gw2Sharp.WebApi.V2.Models.Color> e)
    {
        Color backgroundColor = Color.Transparent;

        if (e.NewValue != null && e.NewValue.Id != 1)
        {
            backgroundColor = e.NewValue.Cloth.ToXnaColor();
        }

        this.BackgroundColor = backgroundColor;
    }

    private void Opacity_SettingChanged(object sender, ValueChangedEventArgs<float> e)
    {
        this.Opacity = e.NewValue;
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
        if (!this.CheckConfiguration(combatEvent))
        {
            return;
        }

        var rule = this.Configuration.FormatRules.Value.Find(rule => rule.Category == combatEvent.Category && rule.Type == combatEvent.Type);

        ScrollingTextAreaEvent scrollingTextAreaEvent = new ScrollingTextAreaEvent(combatEvent, rule, this._font)
        {
            Parent = this,
            Width = this.Width,
            Height = this._eventHeight,
            TextColor = this._textColor,
            Opacity = 0f,
        };

        scrollingTextAreaEvent.Disposed += this.ScrollingTextAreaEvent_Disposed;

        _activeEvents.Add(scrollingTextAreaEvent);

        scrollingTextAreaEvent.Show();
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

    public override void UpdateContainer(GameTime gameTime)
    {
        if (_activeEvents == null || _activeEvents.Count == 0)
        {
            return;
        }

        var now = this.GetNow();
        var actualScrollspeed = this.GetActualScrollspeed();

        if (_activeEvents.Count >= 2)
        {
            // Move olders events down, if not enough space
            ScrollingTextAreaEvent lastChecked = _activeEvents[_activeEvents.Count - 1];
            for (int i = _activeEvents.Count - 2; i >= 0; i--)
            {
                var activeEvent = _activeEvents[i];
                if (lastChecked.Bottom > activeEvent.Top)
                {
                    activeEvent.Time -= this.DistanceToTime(now, actualScrollspeed, lastChecked.Bottom - activeEvent.Top);
                }

                lastChecked = activeEvent;
            }
        }

        float fadeLength = 0.2f;
        for (int i = 0; i < _activeEvents.Count; i++)
        {
            var activeEvent = _activeEvents[i];

            float animatedHeight = (float)(now - activeEvent.Time) * actualScrollspeed;
            float percentage = animatedHeight / this.Height;

            if (percentage > 1)
            {
                activeEvent.Dispose();
                i--;
                continue;
            }

            var alpha = 1 - (percentage - 1f + fadeLength) / fadeLength;
            activeEvent.Opacity = alpha;

            float x = 0;

            switch (this.Configuration.Curve.Value)
            {
                case ScrollingTextAreaCurve.Left:
                    x += this.Width * (2 * percentage - 1) * (2 * percentage - 1);
                    break;
                case ScrollingTextAreaCurve.Right:
                    x += this.Width * (1 - (2 * percentage - 1) * (2 * percentage - 1));
                    activeEvent.Visible = x < this.Width * 0.90;
                    break;
                case ScrollingTextAreaCurve.Straight:
                    break;
                default:
                    break;
            }

            activeEvent.Location = new Point((int)x, (int)animatedHeight);
        }
    }

    private float DistanceToTime(float timeNow, float actualScrollspeed, float distance)
    {
        var x = timeNow - (distance - (timeNow * actualScrollspeed)) / -actualScrollspeed;
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

        _activeEvents.Remove(scrollingTextAreaEvent).ToString();
    }
    protected override void DisposeControl()
    {
        this.Configuration.Size.X.SettingChanged -= this.Size_SettingChanged;
        this.Configuration.Size.Y.SettingChanged -= this.Size_SettingChanged;
        this.Configuration.Location.X.SettingChanged -= this.Location_SettingChanged;
        this.Configuration.Location.Y.SettingChanged -= this.Location_SettingChanged;
        this.Configuration.Opacity.SettingChanged -= this.Opacity_SettingChanged;
        this.Configuration.BackgroundColor.SettingChanged -= this.BackgroundColor_SettingChanged;
        this.Configuration.EventHeight.SettingChanged -= this.EventHeight_SettingChanged;

        this._activeEvents?.Clear();
    }
}
