namespace Estreya.BlishHUD.EventTable.Controls;

using Blish_HUD;
using Blish_HUD._Extensions;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Blish_HUD.Settings;
using Estreya.BlishHUD.EventTable.Resources;
using Estreya.BlishHUD.Shared.Controls;
using Estreya.BlishHUD.Shared.State;
using Estreya.BlishHUD.Shared.UI.Views;
using Estreya.BlishHUD.Shared.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.BitmapFonts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Event : RenderTargetControl
{
    public Models.Event Ev { get; private set; }
    private readonly IconState _iconState;
    private readonly Func<DateTime> _getNowAction;
    private readonly DateTime _startTime;
    private readonly DateTime _endTime;
    private readonly Func<BitmapFont> _getFontAction;
    private readonly SettingEntry<Gw2Sharp.WebApi.V2.Models.Color> _textColorSetting;
    private readonly Func<Color> _getColorAction;

    private Tooltip _tooltip;

    public Event(Models.Event ev, IconState iconState, Func<DateTime> getNowAction, DateTime startTime, DateTime endTime, Func<BitmapFont> getFontAction, SettingEntry<Gw2Sharp.WebApi.V2.Models.Color> textColorSetting, Func<Color> getColorAction)
    {
        this.Ev = ev;
        this._iconState = iconState;
        this._getNowAction = getNowAction;
        this._startTime = startTime;
        this._endTime = endTime;
        this._getFontAction = getFontAction;
        this._textColorSetting = textColorSetting;
        this._getColorAction = getColorAction;
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
        var now = _getNowAction();

        // Relative
        bool isPrev = _startTime.AddMinutes(this.Ev.Duration) < now;
        bool isNext = !isPrev && _startTime > now;
        bool isCurrent = !isPrev && !isNext;

       string description = $"{this.Ev.Location}{(!string.IsNullOrWhiteSpace(this.Ev.Location) ? "\n" : string.Empty)}\n";

        if (isPrev)
        {
            var finishedSince = now - _startTime.AddMinutes(this.Ev.Duration);
            description += $"{Strings.Event_Tooltip_FinishedSince}: {finishedSince:hh\\:mm\\:ss}";
        }
        else if (isNext)
        {
            var startsIn = _startTime - now;
            description += $"{Strings.Event_Tooltip_StartsIn}: {startsIn:hh\\:mm\\:ss}";
        }
        else if (isCurrent)
        {
            var remaining = this.Ev.GetTimeRemaining(now);
            description += $"{Strings.Event_Tooltip_Remaining}: {remaining:hh\\:mm\\:ss}";
        }

        // Absolute
        description += $" ({Strings.Event_Tooltip_StartsAt}: {_startTime:hh\\:mm\\:ss})";

        _tooltip = new Tooltip(new TooltipView(this.Ev.Name, description, this._iconState.GetIcon(this.Ev.Icon)));
    }

    protected override void DoPaint(SpriteBatch spriteBatch, Rectangle bounds)
    {
        var font = this._getFontAction();

        this.DrawBackground(spriteBatch);
        var nameWidth = this.Ev.Filler ? 0 : this.DrawName(spriteBatch, font);
        this.DrawRemainingTime(spriteBatch, font, nameWidth);
    }

    private void DrawBackground(SpriteBatch spriteBatch)
    {
        var backgroundRect = new Rectangle(0, 0, this.Width, this.Height);
        spriteBatch.DrawRectangle(ContentService.Textures.Pixel, backgroundRect, _getColorAction());
    }

    private int DrawName(SpriteBatch spriteBatch, BitmapFont font)
    {
        var nameWidth = (int)Math.Ceiling(font.MeasureString(this.Ev.Name).Width) + 10;
        var nameRect = new Rectangle(0, 0, nameWidth, this.Height);

        var textColor = this._textColorSetting.Value.Id != 1 ? this._textColorSetting.Value.Cloth.ToXnaColor() : Color.Black;

        spriteBatch.DrawString(this.Ev.Name, font, nameRect, textColor);

        return nameRect.Width;
    }
    private void DrawRemainingTime(SpriteBatch spriteBatch, BitmapFont font, int x)
    {
        if (x > this.Width)
        {
            return;
        }

        var remainingTime = this.Ev.GetTimeRemaining(_getNowAction());
        if (remainingTime == TimeSpan.Zero)
        {
            return;
        }

        var remainingTimeString = this.FormatTimeRemaining(remainingTime);
        var timeWidth = (int)Math.Ceiling(font.MeasureString(remainingTimeString).Width) + 10;
        var maxWidth = this.Width - x;
        var centerX = (maxWidth / 2) - (timeWidth / 2);
        if (centerX < x) centerX = x + 10;

        if (centerX + timeWidth > this.Width) return;
        var timeRect = new Rectangle(centerX, 0,  maxWidth, this.Height);

        var textColor = this._textColorSetting.Value.Id != 1 ? this._textColorSetting.Value.Cloth.ToXnaColor() : Color.Black;

        spriteBatch.DrawString(remainingTimeString, font, timeRect, textColor);
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
}
