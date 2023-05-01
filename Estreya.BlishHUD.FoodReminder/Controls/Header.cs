namespace Estreya.BlishHUD.FoodReminder.Controls;

using Blish_HUD;
using Blish_HUD.Controls;
using Estreya.BlishHUD.FoodReminder.Models;
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
using static Blish_HUD.ContentService;

public class Header : TableControl
{
    private TableColumnSizes _columnSizes;
    private readonly Func<BitmapFont> _getFont;
    private readonly Func<int> _getHeight;
    private readonly Func<Color> _getTextColor;

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
        var font = this._getFont();
        var color = this._getTextColor();

        var x = 0f;
        spriteBatch.DrawStringOnCtrl(this, "Name", font, new MonoGame.Extended.RectangleF(x, 0, this._columnSizes.Name.Value - 1, bounds.Height), color, verticalAlignment: VerticalAlignment.Middle, horizontalAlignment: HorizontalAlignment.Center);
        x += this._columnSizes.Name.Value;
        spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new RectangleF(x, 0, 1, bounds.Height),Color.Black);
        spriteBatch.DrawStringOnCtrl(this, "Food", font, new MonoGame.Extended.RectangleF(x, 0, this._columnSizes.Food.Value, bounds.Height), color, verticalAlignment: VerticalAlignment.Middle, horizontalAlignment: HorizontalAlignment.Center);
        x += this._columnSizes.Food.Value;
        spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new RectangleF(x, 0, 1, bounds.Height), Color.Black);
        spriteBatch.DrawStringOnCtrl(this, "Utility", font, new MonoGame.Extended.RectangleF(x, 0, this._columnSizes.Utility.Value, bounds.Height), color, verticalAlignment: VerticalAlignment.Middle, horizontalAlignment: HorizontalAlignment.Center);
        x += this._columnSizes.Utility.Value;
        spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new RectangleF(x, 0, 1, bounds.Height), Color.Black);
        spriteBatch.DrawStringOnCtrl(this, "Reinforced", font, new MonoGame.Extended.RectangleF(x, 0, this._columnSizes.Reinforced.Value, bounds.Height), color, verticalAlignment: VerticalAlignment.Middle, horizontalAlignment: HorizontalAlignment.Center);
        x += this._columnSizes.Reinforced.Value;
        spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new RectangleF(x, 0, 1, bounds.Height), Color.Black);


        spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new RectangleF(0, bounds.Height - 2, x , 1), Color.Black);
    }
}
