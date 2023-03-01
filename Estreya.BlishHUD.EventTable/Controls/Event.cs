namespace Estreya.BlishHUD.EventTable.Controls;

using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Estreya.BlishHUD.Shared.Controls;
using Estreya.BlishHUD.Shared.State;
using Estreya.BlishHUD.Shared.UI.Views;
using Estreya.BlishHUD.Shared.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.BitmapFonts;
using System;
using System.Collections.Generic;
using System.Windows.Media.TextFormatting;

public class Event: IDisposable
{
    public event EventHandler HideRequested;
    public event EventHandler DisableRequested;
    public event EventHandler FinishRequested;

    public Models.Event Ev { get; private set; }
    private IconState _iconState;
    private TranslationState _translationState;
    private readonly Func<DateTime> _getNowAction;
    private readonly DateTime _startTime;
    private readonly DateTime _endTime;
    private readonly Func<BitmapFont> _getFontAction;
    private readonly Func<bool> _getDrawBorders;
    private readonly Func<bool> _getDrawCrossout;
    private readonly Func<Color> _getTextColor;
    private readonly Func<Color> _getColorAction;
    private readonly Func<bool> _getDrawShadowAction;
    private readonly Func<Color> _getShadowColor;

    public Event(Models.Event ev, IconState iconState, TranslationState translationState,
        Func<DateTime> getNowAction, DateTime startTime, DateTime endTime,
        Func<BitmapFont> getFontAction,
        Func<bool> getDrawBorders,
        Func<bool> getDrawCrossout,
        Func<Color> getTextColor,
        Func<Color> getColorAction,
        Func<bool> getDrawShadowAction,
        Func<Color> getShadowColor)
    {
        this.Ev = ev;
        this._iconState = iconState;
        this._translationState = translationState;
        this._getNowAction = getNowAction;
        this._startTime = startTime;
        this._endTime = endTime;
        this._getFontAction = getFontAction;
        this._getDrawBorders = getDrawBorders;
        this._getDrawCrossout = getDrawCrossout;
        this._getTextColor = getTextColor;
        this._getColorAction = getColorAction;
        this._getDrawShadowAction = getDrawShadowAction;
        this._getShadowColor = getShadowColor;
    }

    public ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip();

        var disableAction = new ContextMenuStripItem("Disable")
        {
            Parent = menu,
            BasicTooltipText = "Disables the event entirely."
        };
        disableAction.Click += (s, e) =>
        {
            this.DisableRequested?.Invoke(this, EventArgs.Empty);
        };

        var hideAction = new ContextMenuStripItem("Hide")
        {
            Parent = menu,
            BasicTooltipText = "Hides the event until the next reset."
        };
        hideAction.Click += (s, e) =>
        {
            this.HideRequested?.Invoke(this, EventArgs.Empty);
        };

        var finishAction = new ContextMenuStripItem("Finish")
        {
            Parent = menu,
            BasicTooltipText = "Completes the event until the next reset."
        };
        finishAction.Click += (s, e) =>
        {
            this.FinishRequested?.Invoke(this, EventArgs.Empty);
        };

        return menu;
    }

    public Tooltip BuildTooltip()
    {
        DateTime now = this._getNowAction();

        // Relative
        bool isPrev = this._startTime.AddMinutes(this.Ev.Duration) < now;
        bool isNext = !isPrev && this._startTime > now;
        bool isCurrent = !isPrev && !isNext;

        string description = $"{this.Ev.Location}{(!string.IsNullOrWhiteSpace(this.Ev.Location) ? "\n" : string.Empty)}\n";

        if (isPrev)
        {
            TimeSpan finishedSince = now - this._startTime.AddMinutes(this.Ev.Duration);
            description += $"{this._translationState.GetTranslation("event-tooltip-finishedSince", "Finished since")}: {this.FormatTime(finishedSince)}";
        }
        else if (isNext)
        {
            TimeSpan startsIn = this._startTime - now;
            description += $"{this._translationState.GetTranslation("event-tooltip-startsIn", "Starts in")}: {this.FormatTime(startsIn)}";
        }
        else if (isCurrent)
        {
            TimeSpan remaining = this.GetTimeRemaining(now);
            description += $"{this._translationState.GetTranslation("event-tooltip-remaining", "Remaining")}: {this.FormatTime(remaining)}";
        }

        // Absolute
        description += $" ({this._translationState.GetTranslation("event-tooltip-startsAt", "Starts at")}: {this.FormatTime(this._startTime.ToLocalTime())})";

        return new Tooltip(new TooltipView(this.Ev.Name, description, this._iconState.GetIcon(this.Ev.Icon), this._translationState));
    }

    public void Render(SpriteBatch spriteBatch, RectangleF bounds)
    {
        BitmapFont font = this._getFontAction();

        this.DrawBackground(spriteBatch, bounds);
        float nameWidth = this.Ev.Filler ? 0 : this.DrawName(spriteBatch,bounds, font);
        this.DrawRemainingTime(spriteBatch,bounds, font, nameWidth);
        this.DrawCrossout(spriteBatch, bounds);
    }

    private void DrawBackground(SpriteBatch spriteBatch, RectangleF bounds)
    {
        spriteBatch.DrawRectangle(ContentService.Textures.Pixel, bounds, this._getColorAction(), this._getDrawBorders() ? 1 : 0, Color.Black);
    }

    private float DrawName(SpriteBatch spriteBatch, RectangleF bounds, BitmapFont font)
    {
        float xOffset = 5;
        float maxWidth = bounds.Width - xOffset * 2;
        float nameWidth = 0;
        string text = this.Ev.Name;
        do {
            nameWidth = (float)Math.Ceiling(font.MeasureString(text).Width) ;

            if (string.IsNullOrWhiteSpace(text))
            {
                return 0;
            }

            if (nameWidth > maxWidth)
            {
                text = text.Substring(0, text.Length - 1);
            }
        } while (nameWidth > maxWidth);

        RectangleF nameRect = new RectangleF(bounds.X + xOffset, bounds.Y, nameWidth, bounds.Height);

        spriteBatch.DrawString(text, font, nameRect, this._getTextColor(), false, this._getDrawShadowAction(), 1, this._getShadowColor());

        return nameRect.Width;
    }
    private void DrawRemainingTime(SpriteBatch spriteBatch, RectangleF bounds, BitmapFont font, float nameWidth)
    {
        if (nameWidth > bounds.Width)
        {
            return;
        }

        TimeSpan remainingTime = this.GetTimeRemaining(this._getNowAction());
        if (remainingTime == TimeSpan.Zero)
        {
            return;
        }

        string remainingTimeString = this.FormatTimeRemaining(remainingTime);
        float timeWidth = (float)Math.Ceiling(font.MeasureString(remainingTimeString).Width);
        float maxWidth = bounds.Width - nameWidth;
        float centerX =(maxWidth / 2) - (timeWidth / 2);
        if (centerX < nameWidth)
        {
            centerX = nameWidth + 10;
        }

        if (centerX + timeWidth > bounds.Width)
        {
            return;
        }

        RectangleF timeRect = new RectangleF(centerX+ bounds.X, bounds.Y, maxWidth, bounds.Height);

        Color textColor = this._getTextColor();

        spriteBatch.DrawString(remainingTimeString, font, timeRect, textColor, false, this._getDrawShadowAction(), 1, this._getShadowColor());
    }

    private TimeSpan GetTimeRemaining(DateTime now)
    {
        return now <= _startTime || now >= _endTime ? TimeSpan.Zero : _startTime.AddMinutes(this.Ev.Duration) - now;
    }

    private void DrawCrossout(SpriteBatch spriteBatch, RectangleF bounds)
    {
        if (!this._getDrawCrossout())
        {
            return;
        }

        spriteBatch.DrawCrossOut(ContentService.Textures.Pixel, bounds, Color.Red);
    }

    private string FormatTimeRemaining(TimeSpan ts)
    {
        if (ts.Days > 0)
        {
            return ts.ToString("dd\\.hh\\:mm\\:ss");
        }
        else if (ts.Hours > 0)
        {
            return ts.ToString("hh\\:mm\\:ss");
        }
        else
        {
            return ts.ToString("mm\\:ss");
        }
    }

    private string FormatTime(DateTime dt)
    {
        return this.FormatTime(dt.TimeOfDay);
    }

    private string FormatTime(TimeSpan ts)
    {
        if (ts.Days > 0)
        {
            return ts.ToString("dd\\.hh\\:mm\\:ss");
        }
        else
        {
            return ts.ToString("hh\\:mm\\:ss");
        }
    }

    public void Dispose()
    {
        this._iconState = null;
        this._translationState = null;
        this.Ev = null;
    }
}
