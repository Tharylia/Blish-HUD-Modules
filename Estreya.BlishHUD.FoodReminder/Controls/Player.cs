namespace Estreya.BlishHUD.FoodReminder.Controls;

using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Humanizer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Models;
using MonoGame.Extended;
using MonoGame.Extended.BitmapFonts;
using Shared.Utils;
using System;
using System.Collections.Generic;

public class Player : TableControl
{
    private readonly Func<BitmapFont> _getFont;
    private readonly Func<int> _getHeight;
    private readonly Func<Color> _getTextColor;
    private readonly TableColumnSizes _columnSizes;

    public Player(Models.Player player, TableColumnSizes columnSizes, Func<BitmapFont> getFont, Func<int> getHeight, Func<Color> getTextColor) : base(columnSizes)
    {
        this.Model = player;
        this._columnSizes = columnSizes;
        this._getFont = getFont;
        this._getHeight = getHeight;
        this._getTextColor = getTextColor;

        this.BuildContextMenu();
    }

    public Models.Player Model { get; }

    protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds)
    {
        BitmapFont font = this._getFont();
        Color color = this._getTextColor();
        (string Name, string Food, string Utility, string Reinforced) texts = this.GetTexts();

        float x = 0f;
        spriteBatch.DrawStringOnCtrl(this, texts.Name, font, new RectangleF(x, 0, this._columnSizes.Name.Value, bounds.Height), color, verticalAlignment: VerticalAlignment.Middle, horizontalAlignment: HorizontalAlignment.Center);
        x += this._columnSizes.Name.Value;
        spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new RectangleF(x, 0, 1, bounds.Height), Color.Black);
        spriteBatch.DrawStringOnCtrl(this, texts.Food, font, new RectangleF(x, 0, this._columnSizes.Food.Value, bounds.Height), color, verticalAlignment: VerticalAlignment.Middle, horizontalAlignment: HorizontalAlignment.Center);
        x += this._columnSizes.Food.Value;
        spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new RectangleF(x, 0, 1, bounds.Height), Color.Black);
        spriteBatch.DrawStringOnCtrl(this, texts.Utility, font, new RectangleF(x, 0, this._columnSizes.Utility.Value, bounds.Height), color, verticalAlignment: VerticalAlignment.Middle, horizontalAlignment: HorizontalAlignment.Center);
        x += this._columnSizes.Utility.Value;
        spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new RectangleF(x, 0, 1, bounds.Height), Color.Black);
        spriteBatch.DrawStringOnCtrl(this, texts.Reinforced, font, new RectangleF(x, 0, this._columnSizes.Reinforced.Value, bounds.Height), color, verticalAlignment: VerticalAlignment.Middle, horizontalAlignment: HorizontalAlignment.Center);
        x += this._columnSizes.Reinforced.Value;
        spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new RectangleF(x, 0, 1, bounds.Height), Color.Black);
    }

    private (string Name, string Food, string Utility, string Reinforced) GetTexts()
    {
        string nameText = this.Model.Name;
        string foodText = this.Model.Food?.Display ?? "???";
        string utilityText = this.Model.Utility?.Display ?? "???";
        string reinforcedText = this.Model.Reinforced ? "Yes" : "No";

        return (nameText, foodText, utilityText, reinforcedText);
    }

    public override void DoUpdate(GameTime gameTime)
    {
        base.DoUpdate(gameTime);

        this.Height = this._getHeight();

        (string Name, string Food, string Utility, string Reinforced) texts = this.GetTexts();

        this.BasicTooltipText = $"{texts.Name}\n\n{this.GetFoodTooltipText()}\n{this.GetUtilityTooltipText()}\nReinforced: {texts.Reinforced}";
    }

    private string GetFoodTooltipText()
    {
        string name = this.Model.Food?.Name ?? "???";
        string stats = this.Model.Food?.Stats == null ? string.Empty : this.Model.Food.Stats.Humanize(",") + "\n";

        return $"Food: {name}\n{stats}";
    }

    private string GetUtilityTooltipText()
    {
        string name = this.Model.Utility?.Name ?? "???";
        string stats = this.Model.Utility?.Stats == null ? string.Empty : this.Model.Utility.Stats.Humanize(",") + "\n";

        return $"Utility: {name}\n{stats}";
    }

    private void BuildContextMenu()
    {
        this.Menu = new ContextMenuStrip(() =>
        {
            ContextMenuStripItem copyFoodId = new ContextMenuStripItem("Copy Food ID");
            copyFoodId.Click += this.CopyFoodId_Click;

            ContextMenuStripItem copyUtilityId = new ContextMenuStripItem("Copy Utility ID");
            copyUtilityId.Click += this.CopyUtilityId_Click;

            return new List<ContextMenuStripItem>
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