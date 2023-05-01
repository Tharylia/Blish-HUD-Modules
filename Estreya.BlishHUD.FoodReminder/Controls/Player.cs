namespace Estreya.BlishHUD.FoodReminder.Controls;

using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Estreya.BlishHUD.FoodReminder.Models;
using Estreya.BlishHUD.Shared.Utils;
using Humanizer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.BitmapFonts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Player : TableControl
{
    private TableColumnSizes _columnSizes;
    private readonly Func<BitmapFont> _getFont;
    private readonly Func<int> _getHeight;
    private readonly Func<Color> _getTextColor;

    public Models.Player Model { get; private set; }

    public Player(Models.Player player, TableColumnSizes columnSizes, Func<BitmapFont> getFont, Func<int> getHeight, Func<Color> getTextColor) : base(columnSizes)
    {
        this.Model = player;
        this._columnSizes = columnSizes;
        this._getFont = getFont;
        this._getHeight = getHeight;
        this._getTextColor = getTextColor;

        this.BuildContextMenu();
    }

    protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds)
    {
        var font = this._getFont();
        var color = this._getTextColor();
        var texts = this.GetTexts();

        var x = 0f;
        spriteBatch.DrawStringOnCtrl(this, texts.Name, font, new MonoGame.Extended.RectangleF(x, 0, this._columnSizes.Name.Value, bounds.Height), color, verticalAlignment: VerticalAlignment.Middle, horizontalAlignment: HorizontalAlignment.Center);
        x += this._columnSizes.Name.Value;
        spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new RectangleF(x, 0, 1, bounds.Height), Color.Black);
        spriteBatch.DrawStringOnCtrl(this, texts.Food, font, new MonoGame.Extended.RectangleF(x, 0, this._columnSizes.Food.Value, bounds.Height), color, verticalAlignment: VerticalAlignment.Middle, horizontalAlignment: HorizontalAlignment.Center);
        x += this._columnSizes.Food.Value;
        spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new RectangleF(x, 0, 1, bounds.Height), Color.Black);
        spriteBatch.DrawStringOnCtrl(this, texts.Utility, font, new MonoGame.Extended.RectangleF(x, 0, this._columnSizes.Utility.Value, bounds.Height), color, verticalAlignment: VerticalAlignment.Middle, horizontalAlignment: HorizontalAlignment.Center);
        x += this._columnSizes.Utility.Value;
        spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new RectangleF(x, 0, 1, bounds.Height), Color.Black);
        spriteBatch.DrawStringOnCtrl(this, texts.Reinforced, font, new MonoGame.Extended.RectangleF(x, 0, this._columnSizes.Reinforced.Value, bounds.Height), color, verticalAlignment: VerticalAlignment.Middle, horizontalAlignment: HorizontalAlignment.Center);
        x += this._columnSizes.Reinforced.Value;
        spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new RectangleF(x, 0, 1, bounds.Height), Color.Black);
    }

    private (string Name, string Food, string Utility, string Reinforced) GetTexts()
    {
        var nameText = this.Model.Name;
        var foodText = this.Model.Food?.Display ?? "???";
        var utilityText = this.Model.Utility?.Display ?? "???";
        var reinforcedText = this.Model.Reinforced ? "Yes" : "No";

        return (nameText, foodText, utilityText, reinforcedText);
    }

    public override void DoUpdate(GameTime gameTime)
    {
        base.DoUpdate(gameTime);

        this.Height = this._getHeight();

        var texts = this.GetTexts();

        this.BasicTooltipText = $"{texts.Name}\n\n{this.GetFoodTooltipText()}\n{this.GetUtilityTooltipText()}\nReinforced: {texts.Reinforced}";
    }

    private string GetFoodTooltipText()
    {
        var name = this.Model.Food?.Name ?? "???";
        var stats = this.Model.Food?.Stats == null ? string.Empty : this.Model.Food.Stats.Humanize(",") + "\n";

        return $"Food: {name}\n{stats}";
    }

    private string GetUtilityTooltipText()
    {
        var name = this.Model.Utility?.Name ?? "???";
        var stats = this.Model.Utility?.Stats == null ? string.Empty : this.Model.Utility.Stats.Humanize(",") + "\n";

        return $"Utility: {name}\n{stats}";
    }

    private void BuildContextMenu()
    {
        this.Menu = new ContextMenuStrip(() =>
        {
            var copyFoodId = new ContextMenuStripItem("Copy Food ID");
            copyFoodId.Click += this.CopyFoodId_Click;

            var copyUtilityId = new ContextMenuStripItem("Copy Utility ID");
            copyUtilityId.Click += this.CopyUtilityId_Click;

            return new List<ContextMenuStripItem>()
            {
                copyFoodId,
                copyUtilityId
            };
        });
    }

    private void CopyUtilityId_Click(object sender, MouseEventArgs e)
    {
        if (this.Model?.Utility == null)
        {
            ScreenNotification.ShowNotification("Player has no utility.", ScreenNotification.NotificationType.Warning);
            return;
        }

        ClipboardUtil.WindowsClipboardService.SetTextAsync(this.Model.Utility.ID.ToString())
            .ContinueWith(t =>
            {
                ScreenNotification.ShowNotification("Utility ID copied.");
            });
    }

    private void CopyFoodId_Click(object sender, MouseEventArgs e)
    {
        if (this.Model?.Food == null)
        {
            ScreenNotification.ShowNotification("Player has no food.", ScreenNotification.NotificationType.Warning);
            return;
        }

        ClipboardUtil.WindowsClipboardService.SetTextAsync(this.Model.Food.ID.ToString())
            .ContinueWith(t =>
            {
                ScreenNotification.ShowNotification("Food ID copied.");
            });
    }
}
