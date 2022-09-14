namespace Estreya.BlishHUD.TradingPostWatcher.Controls;

using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Estreya.BlishHUD.Shared.Controls;
using Estreya.BlishHUD.Shared.Models;
using Estreya.BlishHUD.Shared.Models.GW2API.Commerce;
using Estreya.BlishHUD.Shared.State;
using Estreya.BlishHUD.Shared.Utils;
using Humanizer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.BitmapFonts;
using System;
using System.Collections.Generic;

public class Transaction : RenderTargetControl
{
    private readonly CurrentTransaction _currentTransaction;
    private readonly Func<float> _getOpacityAction;
    private readonly Func<bool> _getShowPrice;
    private readonly Func<bool> _getShowPriceAsTotal;
    private readonly Func<bool> _getShowRemaining;
    private readonly Func<bool> _getShowCreatedDate;
    private readonly Func<BitmapFont> _getFont;

    private AsyncTexture2D _transactionTexture;

    private const int SPACING_X = 10;

    private SizingMode _widthSizingMode = SizingMode.Standard;

    /// <summary>
    /// Determines how the width of this
    /// container should be handled.
    /// </summary>
    public virtual SizingMode WidthSizingMode
    {
        get => this._widthSizingMode;
        set => this.SetProperty(ref this._widthSizingMode, value);
    }

    private SizingMode _heightSizingMode = SizingMode.Standard;

    /// <summary>
    /// Determines how the height of this
    /// container should be handled.
    /// </summary>
    public virtual SizingMode HeightSizingMode
    {
        get => this._heightSizingMode;
        set => this.SetProperty(ref this._heightSizingMode, value);
    }

    public Transaction(CurrentTransaction commerceTransaction, IconState iconState, Func<float> getOpacity, Func<bool> getShowPrice, Func<bool> getShowPriceAsTotal, Func<bool> getShowRemaining, Func<bool> getShowCreatedDate, Func<BitmapFont> getFont)
    {
        this._currentTransaction = commerceTransaction;
        this._getOpacityAction = getOpacity;
        this._getShowPrice = getShowPrice;
        this._getShowPriceAsTotal = getShowPriceAsTotal;
        this._getShowRemaining = getShowRemaining;
        this._getShowCreatedDate = getShowCreatedDate;
        this._getFont = getFont;

        this._transactionTexture = iconState.GetIcon(this._currentTransaction?.Item?.Icon);
    }

    protected override void DoPaint(SpriteBatch spriteBatch, Rectangle bounds)
    {
        float opacity = this._getOpacityAction?.Invoke() ?? 1;

        RectangleF iconBounds = RectangleF.Empty;
        if (this._transactionTexture != null && this._transactionTexture.HasSwapped)
        {
            Size iconSize = this.GetIconSize();
            iconBounds.Size = new Size2(iconSize.Width, iconSize.Height);

            spriteBatch.Draw(this._transactionTexture, iconBounds, Color.White * opacity);
        }

        int textMaxWidth = (int)(this.Width - (iconBounds.Width + SPACING_X * 5));
        string text = this.GetWrappedText(textMaxWidth);

        RectangleF textRectangle = new RectangleF(iconBounds.Width + SPACING_X, 0, textMaxWidth, this.Height);

        spriteBatch.DrawString(text, this._getFont(), textRectangle, (this._currentTransaction.IsHighest ? Color.Green : Color.Red) * opacity);
    }

    private string GetText()
    {
        string text = $"{this._currentTransaction.Type.Humanize()}: {this._currentTransaction.Item.Name}";

        List<string> additionalInfos = new List<string>();

        if (this._getShowPrice?.Invoke() ?? false)
        {
            int price = this._currentTransaction.Price;

            if (this._getShowPriceAsTotal?.Invoke() ?? false)
            {
                price *= this._currentTransaction.Quantity;
            }

            additionalInfos.Add($"Price: {GW2Utils.FormatCoins(price)}");
        }

        if (this._getShowRemaining?.Invoke() ?? false)
        {
            additionalInfos.Add($"Remaining: {this._currentTransaction.Quantity}");
        }

        if (this._getShowCreatedDate?.Invoke() ?? false)
        {
            additionalInfos.Add($"Created: {this._currentTransaction.Created.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss")}");
        }

        if (additionalInfos.Count > 0)
        {
            text += $" ({string.Join(", ", additionalInfos)})";
        }

        return text;
    }

    private string GetWrappedText(int maxSize)
    {
        return DrawUtil.WrapText(this._getFont(), this.GetText(), maxSize);
    }

    private Size GetIconSize()
    {
        return new Size(MathHelper.Clamp(this._transactionTexture.Width, 0, 24), MathHelper.Clamp(this._transactionTexture.Height, 0, 24));
    }

    protected override void InternalUpdate(GameTime gameTime)
    {
        Size iconSize = this.GetIconSize();
        Size2 textSize = this._getFont().MeasureString(this.GetText());

        // Update our size based on the sizing mode
        var parent = this.Parent;
        if (parent != null)
        {
            int width = this.GetUpdatedSizing(this.WidthSizingMode,
                                                  this.Width,
                                                  (int)Math.Ceiling(iconSize.Width + textSize.Width),
                                                  parent.ContentRegion.Width - this.Left);

            Size2 wrappedTextSize = this._getFont().MeasureString(this.GetWrappedText(width - iconSize.Width - (SPACING_X * 5)));

            this.Size = new Point(width,
                                  this.GetUpdatedSizing(this.HeightSizingMode,
                                                  this.Height,
                                                  (int)Math.Ceiling(Math.Max(iconSize.Height, wrappedTextSize.Height)),
                                                  parent.ContentRegion.Height - this.Top));
        }
    }

    private int GetUpdatedSizing(SizingMode sizingMode, int currentSize, int maxSize, int fillSize)
    {
        return sizingMode switch
        {
            SizingMode.AutoSize => maxSize,
            SizingMode.Fill => fillSize,
            _ => currentSize,
        };
    }
}
