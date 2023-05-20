namespace Estreya.BlishHUD.TradingPostWatcher.Controls
{

    using Blish_HUD;
    using Blish_HUD.Content;
    using Blish_HUD.Controls;
    using Estreya.BlishHUD.Shared.Models;
    using Estreya.BlishHUD.Shared.Models.GW2API.Commerce;
    using Estreya.BlishHUD.Shared.Services;
    using Estreya.BlishHUD.Shared.Utils;
    using Estreya.BlishHUD.TradingPostWatcher.UI.Views;
    using Humanizer;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using MonoGame.Extended;
    using MonoGame.Extended.BitmapFonts;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class Transaction : IRenderable
    {
        private static Logger Logger = Logger.GetLogger<Transaction>();

        public PlayerTransaction Model;
        private IconService _iconService;
        private TranslationService _translationService;
        private AsyncTexture2D _texture;

        private readonly Func<BitmapFont> _getFont;
        private readonly Func<float> _getOpacity;
        private readonly Func<Color> _getTextColor;
        private readonly Func<bool> _showPrice;
        private readonly Func<bool> _showPriceAsTotal;
        private readonly Func<bool> _showQuantity;
        private readonly Func<bool> _showCreatedDate;
        private readonly Func<Task<int>> _getItemPrice;
        private const int SPACING_X = 10;

        public Transaction(PlayerTransaction transaction, IconService iconService, TranslationService translationService, Func<BitmapFont> getFont, Func<float> getOpacity, Func<Color> getTextColor, Func<bool> showPrice, Func<bool> showPriceAsTotal, Func<bool> showQuantity, Func<bool> showCreatedDate, Func<Task<int>> getItemPrice)
        {
            this.Model = transaction;
            this._iconService = iconService;
            this._translationService = translationService;
            this._getFont = getFont;
            this._getOpacity = getOpacity;
            this._getTextColor = getTextColor;
            this._showPrice = showPrice;
            this._showPriceAsTotal = showPriceAsTotal;
            this._showQuantity = showQuantity;
            this._showCreatedDate = showCreatedDate;
            this._getItemPrice = getItemPrice;

            this._texture = this._iconService.GetIcon(this.Model.Item.Icon);
        }

        /// <summary>
        /// Renders the transaction.
        /// </summary>
        /// <param name="spriteBatch"></param>
        /// <param name="bounds"></param>
        /// <returns>The actual bounds of the transaction control.</returns>
        public RectangleF Render(SpriteBatch spriteBatch, RectangleF bounds)
        {
            float opacity = this._getOpacity();
            BitmapFont font = this._getFont();

            RectangleF iconBounds = RectangleF.Empty;
            if (this._texture != null)
            {
                Size iconSize = this.GetIconSize();
                iconBounds = new RectangleF(bounds.X, bounds.Y, iconSize.Width, iconSize.Height);

                spriteBatch.Draw(this._texture, iconBounds, Color.White * opacity);
            }

            int textMaxWidth = (int)(bounds.Width - (iconBounds.Width + (SPACING_X * 3)));
            string text = this.GetWrappedText(textMaxWidth);

            float height = MathHelper.Clamp(font.MeasureString(text).Height, bounds.Height, float.MaxValue);

            RectangleF textRectangle = new RectangleF(iconBounds.Width + SPACING_X, bounds.Y, textMaxWidth, height);

            spriteBatch.DrawString(text, font, textRectangle, this._getTextColor() * opacity);

            return new RectangleF(bounds.X, bounds.Y, bounds.Width, height);
        }

        private string GetText()
        {
            if (this.Model == null)
            {
                return string.Empty;
            }

            string text = $"{this.Model.Type.Humanize()}: {this.Model.Item?.Name ?? "Unknown"}";

            List<string> additionalInfos = new List<string>();

            if (this._showPrice())
            {
                int price = this.Model.Price;

                if (this._showPriceAsTotal())
                {
                    price *= this.Model.Quantity;
                }

                additionalInfos.Add($"Price: {GW2Utils.FormatCoins(price)}");
            }

            if (this._showQuantity())
            {
                additionalInfos.Add($"Remaining: {this.Model.Quantity}");
            }

            if (this._showCreatedDate())
            {
                additionalInfos.Add($"Created: {this.Model.Created.ToLocalTime():dd.MM.yyyy HH:mm:ss}");
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
            return new Size(MathHelper.Clamp(this._texture.Width, 0, 32), MathHelper.Clamp(this._texture.Height, 0, 32));
        }

        public async Task<Tooltip> BuildTooltip()
        {
            if (this.Model?.Item != null)
            {
                int itemPrice = await this._getItemPrice();
                string priceNote = this.Model.Quantity > 1 && this._showPriceAsTotal() ? $"You have enabled combined price display!" : null;
                return new Tooltip(new PriceTooltipView(this.Model.Item.Name, this.Model.Item.Description, itemPrice, priceNote, this._texture, null, this._iconService, this._translationService));
            }

            return null;
        }

        public void Dispose()
        {
            this.Model = null;

            this._iconService = null;
            this._translationService = null;
            this._texture = null;
        }
    }
}