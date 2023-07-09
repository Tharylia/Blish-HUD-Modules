namespace Estreya.BlishHUD.EventTable.Controls;

using Blish_HUD;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.BitmapFonts;
using Shared.Services;
using Shared.UI.Views;
using Shared.Utils;
using System;
using System.Globalization;
using ColorUtil = Shared.Utils.ColorUtil;

public class Event : IDisposable
{
    private static Logger logger = Logger.GetLogger<Event>();

    private readonly DateTime _endTime;
    private readonly Func<Color[]> _getColorAction;
    private readonly Func<bool> _getDrawBorders;
    private readonly Func<bool> _getDrawCrossout;
    private readonly Func<bool> _getDrawShadowAction;
    private readonly Func<BitmapFont> _getFontAction;
    private readonly Func<DateTime> _getNowAction;
    private readonly Func<Color> _getShadowColor;
    private readonly Func<string> _getAbsoluteTimeFormatStrings;
    private readonly Func<(string DaysFormat, string HoursFormat, string MinutesFormat)> _getTimespanFormatStrings;
    private readonly Func<Color> _getTextColor;
    private readonly DateTime _startTime;

    private Texture2D _backgroundColorTexture;
    private IconService _iconService;
    private TranslationService _translationService;

    public Event(Models.Event ev, IconService iconService, TranslationService translationService,
        Func<DateTime> getNowAction, DateTime startTime, DateTime endTime,
        Func<BitmapFont> getFontAction,
        Func<bool> getDrawBorders,
        Func<bool> getDrawCrossout,
        Func<Color> getTextColor,
        Func<Color[]> getColorAction,
        Func<bool> getDrawShadowAction,
        Func<Color> getShadowColor,
        Func<string> getDateTimeFormatString,
        Func<(string DaysFormat, string HoursFormat, string MinutesFormat)> getTimespanFormatStrings)
    {
        this.Model = ev;
        this._iconService = iconService;
        this._translationService = translationService;
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
        this._getAbsoluteTimeFormatStrings = getDateTimeFormatString;
        this._getTimespanFormatStrings = getTimespanFormatStrings;
    }

    public Models.Event Model { get; private set; }

    public event EventHandler HideRequested;
    public event EventHandler DisableRequested;
    public event EventHandler ToggleFinishRequested;

    public ContextMenuStrip BuildContextMenu()
    {
        ContextMenuStrip menu = new ContextMenuStrip();

        ContextMenuStripItem disableAction = new ContextMenuStripItem(this._translationService.GetTranslation("event-contextMenu-disable-title", "Disable"))
        {
            Parent = menu,
            BasicTooltipText = this._translationService.GetTranslation("event-contextMenu-disable-tooltip", "Disables the event entirely.")
        };
        disableAction.Click += (s, e) =>
        {
            this.DisableRequested?.Invoke(this, EventArgs.Empty);
        };

        ContextMenuStripItem hideAction = new ContextMenuStripItem(this._translationService.GetTranslation("event-contextMenu-hide-title", "Hide"))
        {
            Parent = menu,
            BasicTooltipText = this._translationService.GetTranslation("event-contextMenu-hide-tooltip", "Hides the event until the next reset.")
        };
        hideAction.Click += (s, e) =>
        {
            this.HideRequested?.Invoke(this, EventArgs.Empty);
        };

        ContextMenuStripItem toggleFinishAction = new ContextMenuStripItem(this._translationService.GetTranslation("event-contextMenu-toggleFinish-title", "Toggle Finish"))
        {
            Parent = menu,
            BasicTooltipText = this._translationService.GetTranslation("event-contextMenu-toggleFinish-tooltip", "Toggles the completed state of the event.")
        };
        toggleFinishAction.Click += (s, e) =>
        {
            this.ToggleFinishRequested?.Invoke(this, EventArgs.Empty);
        };

        return menu;
    }

    public Tooltip BuildTooltip()
    {
        DateTime now = this._getNowAction();

        // Relative
        bool isPrev = this._startTime.AddMinutes(this.Model.Duration) < now;
        bool isNext = !isPrev && this._startTime > now;
        bool isCurrent = !isPrev && !isNext;

        string description = $"{this.Model.Location}{(!string.IsNullOrWhiteSpace(this.Model.Location) ? "\n" : string.Empty)}\n";

        if (isPrev)
        {
            TimeSpan finishedSince = now - this._startTime.AddMinutes(this.Model.Duration);
            description += $"{this._translationService.GetTranslation("event-tooltip-finishedSince", "Finished since")}: {this.FormatTimespan(finishedSince)}";
        }
        else if (isNext)
        {
            TimeSpan startsIn = this._startTime - now;
            description += $"{this._translationService.GetTranslation("event-tooltip-startsIn", "Starts in")}: {this.FormatTimespan(startsIn)}";
        }
        else if (isCurrent)
        {
            TimeSpan remaining = this.GetTimeRemaining(now);
            description += $"{this._translationService.GetTranslation("event-tooltip-remaining", "Remaining")}: {this.FormatTimespan(remaining)}";
        }

        // Absolute
        description += $" ({this._translationService.GetTranslation("event-tooltip-startsAt", "Starts at")}: {this.FormatAbsoluteTime(this._startTime)})";

        return new Tooltip(new TooltipView(this.Model.Name, description, this._iconService.GetIcon(this.Model.Icon), this._translationService));
    }

    public void Render(SpriteBatch spriteBatch, RectangleF bounds)
    {
        BitmapFont font = this._getFontAction();

        this.DrawBackground(spriteBatch, bounds);
        float nameWidth = this.Model.Filler ? 0 : this.DrawName(spriteBatch, bounds, font);
        this.DrawRemainingTime(spriteBatch, bounds, font, nameWidth);
        this.DrawCrossout(spriteBatch, bounds);
    }

    private void DrawBackground(SpriteBatch spriteBatch, RectangleF bounds)
    {
        Color[] colors = this._getColorAction();
        if (colors.Length == 1)
        {
            spriteBatch.DrawRectangle(ContentService.Textures.Pixel, bounds, colors[0], this._getDrawBorders() ? 1 : 0, Color.Black);
        }
        else
        {
            int width = (int)Math.Ceiling(bounds.Width);
            int height = (int)Math.Ceiling(bounds.Height);

            if (this._backgroundColorTexture == null || this._backgroundColorTexture.Height != height || this._backgroundColorTexture.Width != width)
            {
                this._backgroundColorTexture?.Dispose();
                this._backgroundColorTexture = ColorUtil.CreateColorGradientsTexture(colors, width, height);
            }

            spriteBatch.DrawRectangle(this._backgroundColorTexture, bounds, Color.White, this._getDrawBorders() ? 1 : 0, Color.Black);
        }
    }

    private float DrawName(SpriteBatch spriteBatch, RectangleF bounds, BitmapFont font)
    {
        float xOffset = 5;
        float maxWidth = bounds.Width - (xOffset * 2);
        float nameWidth = 0;
        string text = this.Model.Name;
        do
        {
            nameWidth = (float)Math.Ceiling(font.MeasureString(text).Width);

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

        string remainingTimeString = this.FormatTimespan(remainingTime);
        float timeWidth = (float)Math.Ceiling(font.MeasureString(remainingTimeString).Width);
        float maxWidth = bounds.Width - nameWidth;
        float centerX = (maxWidth / 2) - (timeWidth / 2);
        if (centerX < nameWidth)
        {
            centerX = nameWidth + 10;
        }

        if (centerX + timeWidth > bounds.Width)
        {
            return;
        }

        RectangleF timeRect = new RectangleF(centerX + bounds.X, bounds.Y, maxWidth, bounds.Height);

        Color textColor = this._getTextColor();

        spriteBatch.DrawString(remainingTimeString, font, timeRect, textColor, false, this._getDrawShadowAction(), 1, this._getShadowColor());
    }

    private TimeSpan GetTimeRemaining(DateTime now)
    {
        return now <= this._startTime || now >= this._endTime ? TimeSpan.Zero : this._startTime.AddMinutes(this.Model.Duration) - now;
    }

    private void DrawCrossout(SpriteBatch spriteBatch, RectangleF bounds)
    {
        if (!this._getDrawCrossout())
        {
            return;
        }

        spriteBatch.DrawCrossOut(ContentService.Textures.Pixel, bounds, Color.Red);
    }

    private string FormatTimespan(TimeSpan ts)
    {
        var formatStrings = this._getTimespanFormatStrings();

        try
        {
            if (ts.Days > 0)
            {
                return ts.ToString(formatStrings.DaysFormat, CultureInfo.InvariantCulture);
            }

            if (ts.Hours > 0)
            {
                return ts.ToString(formatStrings.HoursFormat, CultureInfo.InvariantCulture);
            }

            return ts.ToString(formatStrings.MinutesFormat, CultureInfo.InvariantCulture);
        }
        catch (Exception ex)
        {
            logger.Warn(ex, $"Failed to format timespan {ts}:");
            return string.Empty;
        }
    }

    private string FormatAbsoluteTime(DateTime dt)
    {
        try
        {
            return dt.ToLocalTime().ToString(this._getAbsoluteTimeFormatStrings(), CultureInfo.InvariantCulture);
        }
        catch (Exception ex)
        {
            logger.Warn(ex, $"Failed to format datetime {dt}:");
            return string.Empty;
        }
    }

    public void Dispose()
    {
        this._iconService = null;
        this._translationService = null;
        this.Model = null;
        this._backgroundColorTexture?.Dispose();
        this._backgroundColorTexture = null;
    }
}