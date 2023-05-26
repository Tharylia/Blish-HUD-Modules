namespace Estreya.BlishHUD.TradingPostWatcher.Controls;

using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.BitmapFonts;
using Shared.Models;
using Shared.Utils;

public class NoData : IRenderable
{
    private BitmapFont _font;

    public NoData(BitmapFont font)
    {
        this._font = font;
    }

    public Color TextColor { get; set; } = Color.Red;

    public string Text { get; set; } = "No data!";

    public RectangleF Render(SpriteBatch spriteBatch, RectangleF bounds)
    {
        spriteBatch.DrawString(this.Text, this._font, bounds, this.TextColor, horizontalAlignment: HorizontalAlignment.Center, verticalAlignment: VerticalAlignment.Middle);

        return bounds;
    }

    public void Dispose()
    {
        this._font = null;
    }
}