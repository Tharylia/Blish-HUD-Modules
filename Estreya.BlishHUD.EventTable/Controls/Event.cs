namespace Estreya.BlishHUD.EventTable.Controls;

using Estreya.BlishHUD.Shared.Controls;
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
    private readonly Models.Event _ev;
    private readonly DateTime _startUTC;
    private readonly DateTime _endUTC;
    private readonly BitmapFont _font;
    private readonly Color _textColor;

    public Event(Models.Event ev, DateTime startUTC, DateTime endUTC, BitmapFont font, Color textColor)
    {
        this._ev = ev;
        this._startUTC = startUTC;
        this._endUTC = endUTC;
        this._font = font;
        this._textColor = textColor;
    }

    protected override void DoPaint(SpriteBatch spriteBatch, Rectangle bounds)
    {
        var sizeRect = new Rectangle(0, 0, this.Width, this.Height);

        spriteBatch.DrawString(this._ev.Name, this._font, sizeRect, this._textColor);
    }
}
