﻿namespace Estreya.BlishHUD.EventTable.Controls;

using Blish_HUD;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.BitmapFonts;
using NodaTime;
using Shared.Services;
using Shared.UI.Views;
using Shared.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using ColorUtil = Shared.Utils.ColorUtil;

public class Event : IDisposable
{
    private static Logger logger = Logger.GetLogger<Event>();

    private readonly Instant _endTime;
    private readonly Func<Color[]> _getColorAction;
    private readonly Func<bool> _getDrawBorders;
    private readonly Func<bool> _getDrawCrossout;
    private readonly Func<bool> _getDrawShadowAction;
    private readonly Func<BitmapFont> _getFontAction;
    private readonly Func<Instant> _getNowAction;
    private readonly Func<Color> _getShadowColor;
    private readonly Func<string> _getAbsoluteTimeFormatStrings;
    private readonly Func<(string DaysFormat, string HoursFormat, string MinutesFormat)> _getTimespanFormatStrings;
    private readonly Func<Color> _getTextColor;
    public Instant StartTime { get; private set; }

    private Texture2D _backgroundColorTexture;
    private IconService _iconService;
    private TranslationService _translationService;

    public Event(Models.Event ev, IconService iconService, TranslationService translationService,
        Func<Instant> getNowAction, Instant startTime, Instant endTime,
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
        this.StartTime = startTime;
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

    public event EventHandler HideClicked;
    public event EventHandler DisableClicked;
    public event EventHandler ToggleFinishClicked;
    public event EventHandler<string> MoveToAreaClicked;
    public event EventHandler<string> CopyToAreaClicked;
    public event EventHandler EnableReminderClicked;
    public event EventHandler DisableReminderClicked;

    public ContextMenuStrip BuildContextMenu(Func<List<string>> getAreaNames, string currentAreaName, Func<List<string>> getDisabledReminderKeys)
    {
        ContextMenuStrip menu = new ContextMenuStrip();

        ContextMenuStripItem disableAction = new ContextMenuStripItem(this._translationService.GetTranslation("event-contextMenu-disable-title", "Disable"))
        {
            Parent = menu,
            BasicTooltipText = this._translationService.GetTranslation("event-contextMenu-disable-tooltip", "Disables the event entirely.")
        };
        disableAction.Click += (s, e) =>
        {
            this.DisableClicked?.Invoke(this, EventArgs.Empty);
        };

        ContextMenuStripItem hideAction = new ContextMenuStripItem(this._translationService.GetTranslation("event-contextMenu-hide-title", "Hide"))
        {
            Parent = menu,
            BasicTooltipText = this._translationService.GetTranslation("event-contextMenu-hide-tooltip", "Hides the event until the next reset.")
        };
        hideAction.Click += (s, e) =>
        {
            this.HideClicked?.Invoke(this, EventArgs.Empty);
        };

        ContextMenuStripItem toggleFinishAction = new ContextMenuStripItem(this._translationService.GetTranslation("event-contextMenu-toggleFinish-title", "Toggle Finish"))
        {
            Parent = menu,
            BasicTooltipText = this._translationService.GetTranslation("event-contextMenu-toggleFinish-tooltip", "Toggles the completed state of the event.")
        };
        toggleFinishAction.Click += (s, e) =>
        {
            this.ToggleFinishClicked?.Invoke(this, EventArgs.Empty);
        };

        var isReminderEnabled = !getDisabledReminderKeys().Contains(this.Model.SettingKey);

        var reminderMenu = new ContextMenuStrip();
        ContextMenuStripItem reminderMenuAction = new ContextMenuStripItem(this._translationService.GetTranslation("event-contextMenu-reminderMenu-title", "Reminders..."))
        {
            Parent = menu,
            //BasicTooltipText = this._translationService.GetTranslation("event-contextMenu-reminderMenu-tooltip", "Moves the selected event to the selected area and disables it in the current area."),
            Submenu = reminderMenu,
        };

        ContextMenuStripItem enableReminderAction = new ContextMenuStripItem(this._translationService.GetTranslation("event-contextMenu-reminderMenu-enable-title", "Enable Reminder"))
        {
            Parent = reminderMenu,
            BasicTooltipText = this._translationService.GetTranslation("event-contextMenu-reminderMenu-enable-tooltip", "Enables the corresponding reminder for this event."),
            Enabled = !isReminderEnabled,
        };

        enableReminderAction.Click += (s, e) =>
        {
            this.EnableReminderClicked?.Invoke(this, EventArgs.Empty);
        };

        ContextMenuStripItem disableReminderAction = new ContextMenuStripItem(this._translationService.GetTranslation("event-contextMenu-reminderMenu-disable-title", "Disable Reminder"))
        {
            Parent = reminderMenu,
            BasicTooltipText = this._translationService.GetTranslation("event-contextMenu-reminderMenu-disable-tooltip", "Disables the corresponding reminder for this event."),
            Enabled = isReminderEnabled,
        };

        disableReminderAction.Click += (s, e) =>
        {
            this.DisableReminderClicked?.Invoke(this, EventArgs.Empty);
        };

        if (getAreaNames != null && currentAreaName != null)
        {
            var areaNames = getAreaNames.Invoke();

            if (areaNames.Count > 1)
            {
                var moveToAreaMenu = new ContextMenuStrip();
                ContextMenuStripItem moveToAreaAction = new ContextMenuStripItem(this._translationService.GetTranslation("event-contextMenu-moveToArea-title", "Move to Area..."))
                {
                    Parent = menu,
                    BasicTooltipText = this._translationService.GetTranslation("event-contextMenu-moveToArea-tooltip", "Moves the selected event to the selected area and disables it in the current area."),
                    Submenu = moveToAreaMenu,
                };

                var copyToAreaMenu = new ContextMenuStrip();
                ContextMenuStripItem copyToAreaAction = new ContextMenuStripItem(this._translationService.GetTranslation("event-contextMenu-copyToArea-title", "Copy to Area..."))
                {
                    Parent = menu,
                    BasicTooltipText = this._translationService.GetTranslation("event-contextMenu-copyToArea-tooltip", "Copies the selected event to the selected area."),
                    Submenu = copyToAreaMenu,
                };

                foreach (var areaName in areaNames)
                {
                    ContextMenuStripItem moveToAreaClickAction = new ContextMenuStripItem(areaName)
                    {
                        Parent = moveToAreaMenu,
                        BasicTooltipText = this._translationService.GetTranslation("event-contextMenu-moveToArea-tooltip", "Moves the selected event to the selected area and disables it in the current area."),
                        Enabled = areaName != currentAreaName
                    };

                    moveToAreaClickAction.Click += (s, e) =>
                    {
                        this.MoveToAreaClicked?.Invoke(this, areaName);
                    };

                    ContextMenuStripItem copyToAreaClickAction = new ContextMenuStripItem(areaName)
                    {
                        Parent = copyToAreaMenu,
                        BasicTooltipText = this._translationService.GetTranslation("event-contextMenu-copyToArea-tooltip", "Copies the selected event to the selected area."),
                        Enabled = areaName != currentAreaName
                    };

                    copyToAreaClickAction.Click += (s, e) =>
                    {
                        this.CopyToAreaClicked?.Invoke(this, areaName);
                    };
                }
            }
        }

        return menu;
    }

    public Tooltip BuildTooltip()
    {
        Instant now = this._getNowAction();

        // Relative
        bool isPrev = this.StartTime.Plus(this.Model.Duration) < now;
        bool isNext = !isPrev && this.StartTime > now;
        bool isCurrent = !isPrev && !isNext;

        string description = $"{this.Model.Location}{(!string.IsNullOrWhiteSpace(this.Model.Location) ? "\n" : string.Empty)}\n";

        if (isPrev)
        {
            Duration finishedSince = now - this.StartTime.Plus(this.Model.Duration);
            description += $"{this._translationService.GetTranslation("event-tooltip-finishedSince", "Finished since")}: {this.FormatDuration(finishedSince)}";
        }
        else if (isNext)
        {
            Duration startsIn = this.StartTime - now;
            description += $"{this._translationService.GetTranslation("event-tooltip-startsIn", "Starts in")}: {this.FormatDuration(startsIn)}";
        }
        else if (isCurrent)
        {
            Duration remaining = this.GetTimeRemaining(now);
            description += $"{this._translationService.GetTranslation("event-tooltip-remaining", "Remaining")}: {this.FormatDuration(remaining)}";
        }

        // Absolute
        description += $" ({this._translationService.GetTranslation("event-tooltip-startsAt", "Starts at")}: {this.FormatAbsoluteTime(this.StartTime)})";

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

        Duration remainingTime = this.GetTimeRemaining(this._getNowAction());
        if (remainingTime == Duration.Zero)
        {
            return;
        }

        string remainingTimeString = this.FormatDuration(remainingTime);
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

    private Duration GetTimeRemaining(Instant now)
    {
        return now <= this.StartTime || now >= this._endTime ? Duration.Zero : this.StartTime.Plus(this.Model.Duration) - now;
    }

    private void DrawCrossout(SpriteBatch spriteBatch, RectangleF bounds)
    {
        if (!this._getDrawCrossout())
        {
            return;
        }

        spriteBatch.DrawCrossOut(ContentService.Textures.Pixel, bounds, Color.Red);
    }

    private string FormatDuration(Duration ts)
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

    private string FormatAbsoluteTime(Instant dt)
    {
        try
        {
            return dt.ToDateTimeUtc().ToLocalTime().ToString(this._getAbsoluteTimeFormatStrings(), CultureInfo.InvariantCulture);
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