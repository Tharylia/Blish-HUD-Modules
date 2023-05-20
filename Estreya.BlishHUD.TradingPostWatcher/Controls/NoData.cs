namespace Estreya.BlishHUD.TradingPostWatcher.Controls;

using Estreya.BlishHUD.Shared.Models;
using Estreya.BlishHUD.Shared.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.BitmapFonts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class NoData : IRenderable
{
    private BitmapFont _font;

    public Color TextColor { get; set; } = Color.Red;

    public string Text { get; set; } = "No data!";

    public NoData(BitmapFont font)
    {
        this._font = font;
    }

    public RectangleF Render(SpriteBatch spriteBatch, RectangleF bounds)
    {
        spriteBatch.DrawString(this.Text, this._font, bounds, this.TextColor, horizontalAlignment: Blish_HUD.Controls.HorizontalAlignment.Center, verticalAlignment: Blish_HUD.Controls.VerticalAlignment.Middle);

        return bounds;
    }

    public void Dispose()
    {
        this._font = null;
    }
}
