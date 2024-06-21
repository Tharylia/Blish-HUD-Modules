namespace Estreya.BlishHUD.EventTable.Controls.Map;

using Blish_HUD;
using Blish_HUD.ArcDps.Models;
using Estreya.BlishHUD.EventTable.Models;
using Estreya.BlishHUD.Shared.Controls.Map;
using Estreya.BlishHUD.Shared.Extensions;
using Estreya.BlishHUD.Shared.Services;
using Estreya.BlishHUD.Shared.Utils;
using Humanizer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using SharpDX.Direct3D9;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class EventMapTimer : MapEntity
{
    private readonly Event _ev;
    private readonly Models.EventMapTimer _mapTimer;
    private readonly Color _color;
    private readonly float _thickness;
    private readonly Func<DateTime> _getNow;
    private readonly TranslationService _translationService;

    private float X => this._mapTimer.X;
    private float Y => this._mapTimer.Y;

    private float Radius => this._mapTimer.Radius * (1 / 24f);

    public EventMapTimer(Models.Event ev, Models.EventMapTimer mapTimer, Color color, Func<DateTime> getNow, TranslationService translationService, float thickness = 1)
    {
        this._ev = ev;
        this._mapTimer = mapTimer;
        this._color = color;
        this._thickness = thickness;
        this._getNow = getNow;
        this._translationService = translationService;
    }

    public override RectangleF? RenderToMiniMap(SpriteBatch spriteBatch, Rectangle bounds, double offsetX, double offsetY, double scale, float opacity)
    {
        Vector2 location = this.GetScaledLocation(this.X, this.Y, scale, offsetX, offsetY);

        float radius = this.Radius / (float)scale;

        //Logger.Debug($"Location: {location} - OffsetX: {offsetX} - OffsetY: {offsetY} - Scale: {scale}");

        CircleF circle = new CircleF(new Point2(location.X, location.Y), radius);
        spriteBatch.DrawCircle(circle, 50, this._color * opacity, this._thickness);

        var now = this._getNow();
        var startTime = this._ev.Occurences.Where(o => o >= now || o.AddMinutes(this._ev.Duration) >= now).OrderBy(x => x).First();
        var endTime = startTime.AddMinutes(this._ev.Duration);

        var occurenceIndex = this._ev.Occurences.IndexOf(startTime);
        var remainingTime = this.GetTime(now, startTime, endTime, occurenceIndex is -1 or 0 ? null : this._ev.Occurences[occurenceIndex - 1]);

        var degree = remainingTime.calculatedTime.TotalSeconds.Remap( 0, remainingTime.maxTime.TotalSeconds, 0,360) * -1;
        var angle = Math.PI * (degree - 90f) / 180.0;
        var angleX = (radius - this._thickness) * (float)Math.Cos(angle);
        var angleY = (radius - this._thickness)* (float)Math.Sin(angle);
        var angleLineThickness = 3;
        //spriteBatch.DrawAngledLine(ContentService.Textures.Pixel, circle.Center, circle.Center + new Vector2(angleX, angleY), Color.Red, angleLineThickness);

        // Draw top line
        var topLineThickness = 3;
        //spriteBatch.DrawAngledLine(ContentService.Textures.Pixel, circle.Center, circle.Center + new Vector2(0, - (circle.Radius - this._thickness)), Color.Gray, topLineThickness);

        if (!GameService.Gw2Mumble.UI.IsMapOpen || GameService.Gw2Mumble.UI.MapScale <= 1f)
        {
            var text = $"{this._ev.Name}\n{this.GetEventDescription(now, startTime, endTime)}";
            var font = ContentService.Content.DefaultFont18;
            var textSize = font.MeasureString(text);
            var circleBottomCenter = circle.Center + new Vector2(0, circle.Radius);
            var textLocation = new RectangleF(circleBottomCenter.X - textSize.Width / 2f, circleBottomCenter.Y + 10, textSize.Width +5, textSize.Height + 5 );
            spriteBatch.DrawString(text, font, textLocation, Color.Red, scale: 1, horizontalAlignment:Blish_HUD.Controls.HorizontalAlignment.Center, verticalAlignment: Blish_HUD.Controls.VerticalAlignment.Top);
        }

        return circle.ToRectangleF();
    }

    private (TimeSpan maxTime, TimeSpan calculatedTime) GetTime(DateTime now,DateTime startTime, DateTime endTime, DateTime? prevEndTime)
    {
        // Relative
        bool isPrev = endTime < now;
        bool isNext = !isPrev && startTime > now;
        bool isCurrent = !isPrev && !isNext;

        if (isPrev)
        {
            TimeSpan finishedSince = now - endTime;
            return (endTime - startTime, finishedSince);
        }
        else if (isNext)
        {
            TimeSpan startsIn = startTime - now;
            return (prevEndTime != null ? startTime - prevEndTime.Value : startsIn, startsIn);
        }
        else if (isCurrent)
        {
            TimeSpan remaining = this.GetTimeRemaining(now, startTime, endTime);
            return (TimeSpan.FromMinutes( this._ev.Duration), remaining);
        }

        return (TimeSpan.Zero, TimeSpan.Zero);
    }

    private string GetEventDescription(DateTime now, DateTime startTime, DateTime endTime)
    {
        // Relative
        bool isPrev = endTime < now;
        bool isNext = !isPrev && startTime > now;
        bool isCurrent = !isPrev && !isNext;

        string description = $"";

        if (isPrev)
        {
            TimeSpan finishedSince = now - endTime;
            description += $"{this._translationService.GetTranslation("event-tooltip-finishedSince", "Finished since")}: {this.FormatTimespan(finishedSince)}";
        }
        else if (isNext)
        {
            TimeSpan startsIn = startTime - now;
            description += $"{this._translationService.GetTranslation("event-tooltip-startsIn", "Starts in")}: {this.FormatTimespan(startsIn)}";
        }
        else if (isCurrent)
        {
            TimeSpan remaining = this.GetTimeRemaining(now, startTime, endTime);
            description += $"{this._translationService.GetTranslation("event-tooltip-remaining", "Remaining")}: {this.FormatTimespan(remaining)}";
        }

        // Absolute
        description += $" ({this._translationService.GetTranslation("event-tooltip-startsAt", "Starts at")}: {this.FormatAbsoluteTime(startTime)})";

        return description;
    }

    private string FormatTimespan(TimeSpan ts)
    {
        var formatStrings = (DaysFormat: "dd\\.hh\\:mm\\:ss", HoursFormat: "hh\\:mm\\:ss", MinutesFormat: "mm\\:ss");

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
            //logger.Warn(ex, $"Failed to format timespan {ts}:");
            return string.Empty;
        }
    }

    private string FormatAbsoluteTime(DateTime dt)
    {
        try
        {
            return dt.ToLocalTime().ToString("HH\\:mm", CultureInfo.InvariantCulture);
        }
        catch (Exception ex)
        {
            //logger.Warn(ex, $"Failed to format datetime {dt}:");
            return string.Empty;
        }
    }

    private TimeSpan GetTimeRemaining(DateTime now, DateTime startTime, DateTime endTime)
    {
        return now <= startTime || now >= endTime ? TimeSpan.Zero : startTime.AddMinutes(this._ev.Duration) - now;
    }
}
