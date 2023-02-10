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
using MonoGame.Extended.BitmapFonts;
using System;
using System.Collections.Generic;

public class Event : RenderTargetControl
{
    public event EventHandler HideRequested;
    public event EventHandler DisableRequested;
    public event EventHandler FinishRequested;

    public Models.Event Ev { get; private set; }
    private readonly IconState _iconState;
    private readonly TranslationState _translationState;
    private readonly Func<DateTime> _getNowAction;
    private readonly DateTime _startTime;
    private readonly DateTime _endTime;
    private readonly Func<BitmapFont> _getFontAction;
    private readonly Func<bool> _getDrawBorders;
    private readonly Func<bool> _getDrawCrossout;
    private readonly Func<Color> _getTextColor;
    private readonly Func<Color> _getColorAction;

    private Tooltip _tooltip;

    public Event(Models.Event ev, IconState iconState, TranslationState translationState,
        Func<DateTime> getNowAction, DateTime startTime, DateTime endTime,
        Func<BitmapFont> getFontAction,
        Func<bool> getDrawBorders,
        Func<bool> getDrawCrossout,
        Func<Color> getTextColor,
        Func<Color> getColorAction)
    {
        this.ClipsBounds = false;

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

        this.BuildContextMenu();
    }

    private void BuildContextMenu()
    {
        this.Menu = new ContextMenuStrip();

        var disableAction = new ContextMenuStripItem("Disable")
        {
            Parent = this.Menu,
            BasicTooltipText = "Disables the event entirely."
        };
        disableAction.Click += (s, e) =>
        {
            this.DisableRequested?.Invoke(this, EventArgs.Empty);
        };

        var hideAction = new ContextMenuStripItem("Hide")
        {
            Parent = this.Menu,
            BasicTooltipText = "Hides the event until the next reset."
        };
        hideAction.Click += (s, e) =>
        {
            this.HideRequested?.Invoke(this, EventArgs.Empty);
        };

        var finishAction = new ContextMenuStripItem("Finish")
        {
            Parent = this.Menu,
            BasicTooltipText = "Completes the event until the next reset."
        };
        finishAction.Click += (s, e) =>
        {
            this.FinishRequested?.Invoke(this, EventArgs.Empty);
        };
    }

    protected override void OnMouseEntered(MouseEventArgs e)
    {
        base.OnMouseEntered(e);

        if (!this.Ev.Filler)
        {
            this.BuildOrUpdateTooltip();

            this.Tooltip = this._tooltip;
        }
    }

    private void BuildOrUpdateTooltip()
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
            TimeSpan remaining = this.Ev.GetTimeRemaining(now);
            description += $"{this._translationState.GetTranslation("event-tooltip-remaining", "Remaining")}: {this.FormatTime(remaining)}";
        }

        // Absolute
        description += $" ({this._translationState.GetTranslation("event-tooltip-startsAt", "Starts at")}: {this.FormatTime(this._startTime.ToLocalTime())})";

        this._tooltip = new Tooltip(new TooltipView(this.Ev.Name, description, this._iconState.GetIcon(this.Ev.Icon), this._translationState));
    }

    protected override void DoPaint(SpriteBatch spriteBatch, Rectangle bounds)
    {
        BitmapFont font = this._getFontAction();

        this.DrawBackground(spriteBatch);
        int nameWidth = this.Ev.Filler ? 0 : this.DrawName(spriteBatch, font);
        this.DrawRemainingTime(spriteBatch, font, nameWidth);
        this.DrawCrossout(spriteBatch);
    }

    private void DrawBackground(SpriteBatch spriteBatch)
    {
        Rectangle backgroundRect = new Rectangle(0, 0, this.Width, this.Height);
        spriteBatch.DrawRectangle(ContentService.Textures.Pixel, backgroundRect, this._getColorAction(), this._getDrawBorders() ? 1 : 0, Color.Black);
    }

    private int DrawName(SpriteBatch spriteBatch, BitmapFont font)
    {
        int nameWidth = MathHelper.Clamp((int)Math.Ceiling(font.MeasureString(this.Ev.Name).Width) + 10, 0, this.Width - 10);
        Rectangle nameRect = new Rectangle(5, 0, nameWidth, this.Height);

        Color textColor = this._getTextColor();

        spriteBatch.DrawString(this.Ev.Name, font, nameRect, textColor);

        return nameRect.Width;
    }
    private void DrawRemainingTime(SpriteBatch spriteBatch, BitmapFont font, int x)
    {
        if (x > this.Width)
        {
            return;
        }

        TimeSpan remainingTime = this.Ev.GetTimeRemaining(this._getNowAction());
        if (remainingTime == TimeSpan.Zero)
        {
            return;
        }

        string remainingTimeString = this.FormatTimeRemaining(remainingTime);
        int timeWidth = (int)Math.Ceiling(font.MeasureString(remainingTimeString).Width) + 10;
        int maxWidth = this.Width - x;
        int centerX = (maxWidth / 2) - (timeWidth / 2);
        if (centerX < x)
        {
            centerX = x + 10;
        }

        if (centerX + timeWidth > this.Width)
        {
            return;
        }

        Rectangle timeRect = new Rectangle(centerX, 0, maxWidth, this.Height);

        Color textColor = this._getTextColor();

        spriteBatch.DrawString(remainingTimeString, font, timeRect, textColor);
    }

    private void DrawCrossout(SpriteBatch spriteBatch)
    {
        if (!this._getDrawCrossout())
        {
            return;
        }

        Rectangle fullRect = new Rectangle(0, 0, this.Width, this.Height);
        spriteBatch.DrawCrossOut(ContentService.Textures.Pixel, fullRect, Color.Red);
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
}
