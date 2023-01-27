namespace Estreya.BlishHUD.TradingPostWatcher.Controls
{

    using Blish_HUD;
    using Blish_HUD._Extensions;
    using Blish_HUD.Content;
    using Blish_HUD.Controls;
    using Blish_HUD.Input;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.Shared.Controls;
    using Estreya.BlishHUD.Shared.Models;
    using Estreya.BlishHUD.Shared.Models.GW2API.Commerce;
    using Estreya.BlishHUD.Shared.State;
    using Estreya.BlishHUD.Shared.Threading;
    using Estreya.BlishHUD.Shared.UI.Views;
    using Estreya.BlishHUD.Shared.Utils;
    using Estreya.BlishHUD.TradingPostWatcher.UI.Views;
    using Humanizer;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using MonoGame.Extended;
    using MonoGame.Extended.BitmapFonts;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class Transaction : RenderTargetControl
    {
        private static Logger Logger = Logger.GetLogger<Transaction>();
        private static TimeSpan _updateTooltipInterval = TimeSpan.FromSeconds(30);
        private AsyncRef<double> _timeSinceLastTooltipUpdate = new AsyncRef<double>(_updateTooltipInterval.TotalMilliseconds);

        private readonly PlayerTransaction _currentTransaction;

        private readonly IconState _iconState;
        private readonly TradingPostState _tradingPostState;
        private readonly TranslationState _translationState;
        private readonly SettingEntry<float> _opacitySetting;
        private readonly SettingEntry<bool> _showPriceSetting;
        private readonly SettingEntry<bool> _showPriceAsTotalSetting;
        private readonly SettingEntry<bool> _showRemainingSetting;
        private readonly SettingEntry<bool> _showCreatedDateSetting;
        private readonly SettingEntry<bool> _showTooltipsSetting;
        private readonly SettingEntry<ContentService.FontSize> _fontSizeSetting;
        private readonly SettingEntry<Gw2Sharp.WebApi.V2.Models.Color> _highestTransactionColorSetting;
        private readonly SettingEntry<Gw2Sharp.WebApi.V2.Models.Color> _outbidTransactionColorSetting;
        private static readonly ConcurrentDictionary<ContentService.FontSize, BitmapFont> _fontCache = new ConcurrentDictionary<ContentService.FontSize, BitmapFont>();

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
        //private Tooltip _tooltip;

        /// <summary>
        /// Determines how the height of this
        /// container should be handled.
        /// </summary>
        public virtual SizingMode HeightSizingMode
        {
            get => this._heightSizingMode;
            set => this.SetProperty(ref this._heightSizingMode, value);
        }

        public Transaction(PlayerTransaction commerceTransaction, IconState iconState, TradingPostState tradingPostState, TranslationState translationState,
            SettingEntry<float> opacity, SettingEntry<bool> showPrice, SettingEntry<bool> showPriceAsTotal,
            SettingEntry<bool> showRemaining, SettingEntry<bool> showCreatedDate, SettingEntry<bool> showTooltips,
            SettingEntry<ContentService.FontSize> fontSize, SettingEntry<Gw2Sharp.WebApi.V2.Models.Color> highestTransactionColorSetting,
            SettingEntry<Gw2Sharp.WebApi.V2.Models.Color> outbidTransactionColorSetting) : base()
        {
            this._currentTransaction = commerceTransaction;
            this._iconState = iconState;
            this._tradingPostState = tradingPostState;
            this._translationState = translationState;
            this._opacitySetting = opacity;
            this._showPriceSetting = showPrice;
            this._showPriceAsTotalSetting = showPriceAsTotal;
            this._showRemainingSetting = showRemaining;
            this._showCreatedDateSetting = showCreatedDate;
            this._showTooltipsSetting = showTooltips;
            this._fontSizeSetting = fontSize;
            this._highestTransactionColorSetting = highestTransactionColorSetting;
            this._outbidTransactionColorSetting = outbidTransactionColorSetting;
            this._transactionTexture = iconState.GetIcon(this._currentTransaction?.Item?.Icon);

            this._showTooltipsSetting.SettingChanged += this.ShowTooltips_SettingChanged;
        }

        private void ShowTooltips_SettingChanged(object sender, ValueChangedEventArgs<bool> e)
        {
            if (!e.NewValue)
            {
                this.Tooltip?.Dispose();
                return;
            }

            _timeSinceLastTooltipUpdate = _updateTooltipInterval.TotalMilliseconds;
        }

        protected override CaptureType CapturesInput()
        {
            return CaptureType.Mouse | CaptureType.DoNotBlock;
        }

        protected override void DoPaint(SpriteBatch spriteBatch, Rectangle bounds)
        {
            float opacity = this._opacitySetting?.Value ?? 1;

            RectangleF iconBounds = RectangleF.Empty;
            if (this._transactionTexture != null && this._transactionTexture.HasSwapped)
            {
                Size iconSize = this.GetIconSize();
                iconBounds.Size = new Size2(iconSize.Width, iconSize.Height);

                spriteBatch.Draw(this._transactionTexture, iconBounds, Color.White * opacity);
            }

            int textMaxWidth = (int)(this.Width - (iconBounds.Width + SPACING_X * 3));
            string text = this.GetWrappedText(textMaxWidth);

            RectangleF textRectangle = new RectangleF(iconBounds.Width + SPACING_X, 0, textMaxWidth, this.Height);

            spriteBatch.DrawString(text, this.GetFont(), textRectangle, this.GetColor() * opacity);
        }

        private Color GetColor()
        {
            if (this._currentTransaction?.IsHighest ?? false)
            {
                return this._highestTransactionColorSetting?.Value.Cloth.ToXnaColor() ?? Color.Green;
            }

            return this._outbidTransactionColorSetting?.Value.Cloth.ToXnaColor() ?? Color.Red;
        }

        private string GetText()
        {
            if (this._currentTransaction == null)
            {
                return string.Empty;
            }

            string text = $"{this._currentTransaction.Type.Humanize()}: {this._currentTransaction.Item?.Name ?? "Unknown"}";

            List<string> additionalInfos = new List<string>();

            if (this._showPriceSetting?.Value ?? false)
            {
                int price = this._currentTransaction.Price;

                if (this._showPriceAsTotalSetting?.Value ?? false)
                {
                    price *= this._currentTransaction.Quantity;
                }

                additionalInfos.Add($"Price: {GW2Utils.FormatCoins(price)}");
            }

            if (this._showRemainingSetting?.Value ?? false)
            {
                additionalInfos.Add($"Remaining: {this._currentTransaction.Quantity}");
            }

            if (this._showCreatedDateSetting?.Value ?? false)
            {
                additionalInfos.Add($"Created: {this._currentTransaction.Created.ToLocalTime():dd.MM.yyyy HH:mm:ss}");
            }

            if (additionalInfos.Count > 0)
            {
                text += $" ({string.Join(", ", additionalInfos)})";
            }

            return text;
        }

        private string GetWrappedText(int maxSize)
        {
            return DrawUtil.WrapText(this.GetFont(), this.GetText(), maxSize);
        }

        private Size GetIconSize()
        {
            return new Size(MathHelper.Clamp(this._transactionTexture.Width, 0, 24), MathHelper.Clamp(this._transactionTexture.Height, 0, 24));
        }

        protected override void InternalUpdate(GameTime gameTime)
        {
            if (this._showTooltipsSetting.Value)
            {
                _ = UpdateUtil.UpdateAsync(this.BuildTooltip, gameTime, _updateTooltipInterval.TotalMilliseconds, _timeSinceLastTooltipUpdate);
            }

            BitmapFont font = this.GetFont();

            Size iconSize = this.GetIconSize();
            Size2 textSize = font.MeasureString(this.GetText());

            // Update our size based on the sizing mode
            var parent = this.Parent;
            if (parent != null)
            {
                int width = this.GetUpdatedSizing(this.WidthSizingMode,
                                                      this.Width,
                                                      MathHelper.Clamp((int)Math.Ceiling(iconSize.Width + (SPACING_X * 3) + textSize.Width), 0, parent.Width),
                                                      parent.ContentRegion.Width - this.Left);

                Size2 wrappedTextSize = font.MeasureString(this.GetWrappedText(width - iconSize.Width - (SPACING_X * 3)));

                this.Size = new Point(width,
                                      this.GetUpdatedSizing(this.HeightSizingMode,
                                                      this.Height,
                                                      MathHelper.Clamp((int)Math.Ceiling(Math.Max(iconSize.Height, wrappedTextSize.Height)), 0, parent.Height),
                                                      parent.ContentRegion.Height - this.Top));
            }
        }

        private BitmapFont GetFont()
        {
            return _fontCache.GetOrAdd(this._fontSizeSetting?.Value ?? ContentService.FontSize.Size14,
                fontSize => GameService.Content.GetFont(ContentService.FontFace.Menomonia, fontSize, ContentService.FontStyle.Regular));
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

        private async Task BuildTooltip()
        {
            if (this._currentTransaction?.Item != null)
            {
                var itemPrice = await _tradingPostState.GetPriceForItem(this._currentTransaction.ItemId, this._currentTransaction.Type);
                var priceNote = this._showPriceAsTotalSetting?.Value ?? false ? $"You have enabled combined price display!" : null;
                this.Tooltip = new Tooltip(new PriceTooltipView(this._currentTransaction.Item.Name, this._currentTransaction.Item.Description, itemPrice, priceNote, this._transactionTexture, null, this._iconState, this._translationState));
            }
        }

        protected override void InternalDispose()
        {
            this._showTooltipsSetting.SettingChanged -= this.ShowTooltips_SettingChanged;

            this._transactionTexture = null; // Don't dispose
            this.Tooltip?.Dispose();
        }
    }
}