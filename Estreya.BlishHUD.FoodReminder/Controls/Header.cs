namespace Estreya.BlishHUD.FoodReminder.Controls;

using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Models;
using MonoGame.Extended;
using MonoGame.Extended.BitmapFonts;
using Shared.Utils;
using System;
using static Blish_HUD.ContentService;

public class Header : TableControl
{
    private readonly Func<BitmapFont> _getFont;
    private readonly Func<int> _getHeight;
    private readonly Func<Color> _getTextColor;
    private readonly TableColumnSizes _columnSizes;

    public Header(TableColumnSizes columnSizes, Func<BitmapFont> getFont, Func<int> getHeight, Func<Color> getTextColor) : base(columnSizes)
    {
        this._columnSizes = columnSizes;
        this._getFont = getFont;
        this._getHeight = getHeight;
        this._getTextColor = getTextColor;
    }

    public override void DoUpdate(GameTime gameTime)
    {
        base.DoUpdate(gameTime);

        this.Height = this._getHeight();
    }

    protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds)
    {
        BitmapFont font = this._getFont();
        Color color = this._getTextColor();

        float x = 0f;
        spriteBatch.DrawStringOnCtrl(this, "Name", font, new RectangleF(x, 0, this._columnSizes.Name.Value - 1, bounds.Height), color, verticalAlignment: VerticalAlignment.Middle, horizontalAlignment: HorizontalAlignment.Center);
        x += this._columnSizes.Name.Value;
        spriteBatch.DrawOnCtrl(this, Textures.Pixel, new RectangleF(x, 0, 1, bounds.Height), Color.Black);
        spriteBatch.DrawStringOnCtrl(this, "Food", font, new RectangleF(x, 0, this._columnSizes.Food.Value, bounds.Height), color, verticalAlignment: VerticalAlignment.Middle, horizontalAlignment: HorizontalAlignment.Center);
        x += this._columnSizes.Food.Value;
        spriteBatch.DrawOnCtrl(this, Textures.Pixel, new RectangleF(x, 0, 1, bounds.Height), Color.Black);
        spriteBatch.DrawStringOnCtrl(this, "Utility", font, new RectangleF(x, 0, this._columnSizes.Utility.Value, bounds.Height), color, verticalAlignment: VerticalAlignment.Middle, horizontalAlignment: HorizontalAlignment.Center);
        x += this._columnSizes.Utility.Value;
        spriteBatch.DrawOnCtrl(this, Textures.Pixel, new RectangleF(x, 0, 1, bounds.Height), Color.Black);
        spriteBatch.DrawStringOnCtrl(this, "Reinforced", font, new RectangleF(x, 0, this._columnSizes.Reinforced.Value, bounds.Height), color, verticalAlignment: VerticalAlignment.Middle, horizontalAlignment: HorizontalAlignment.Center);
        x += this._columnSizes.Reinforced.Value;
        spriteBatch.DrawOnCtrl(this, Textures.Pixel, new RectangleF(x, 0, 1, bounds.Height), Color.Black);

        spriteBatch.DrawOnCtrl(this, Textures.Pixel, new RectangleF(0, bounds.Height - 2, x, 1), Color.Black);
    }
}