namespace Estreya.BlishHUD.TradingPostWatcher.Controls
{

    using Blish_HUD;
    using Blish_HUD._Extensions;
    using Blish_HUD.Controls;
    using Estreya.BlishHUD.Shared.Models;
    using Estreya.BlishHUD.Shared.Models.Drawers;
    using Estreya.BlishHUD.Shared.Models.GW2API.Commerce;
    using Estreya.BlishHUD.Shared.Service;
    using Estreya.BlishHUD.TradingPostWatcher.Models;
    using Glide;
    using Microsoft.Xna.Framework;
    using System;
    using System.Collections.Generic;

    public class TransactionDrawer : FlowPanel
    {
        private bool _currentVisibilityDirection = false;
        private Tween _currentVisibilityAnimation { get; set; }

        private List<PlayerTransaction> _transactions = new List<PlayerTransaction>();
        private readonly IconService _iconService;
        private readonly TradingPostService _tradingPostService;
        private readonly TranslationService _translationService;

        public TransactionDrawerConfiguration Configuration { get; private set; }

        public TransactionDrawer(TransactionDrawerConfiguration configuration, IconService iconService, TradingPostService tradingPostService, TranslationService translationService)
        {
            this.Configuration = configuration;
            this._iconService = iconService;
            this._tradingPostService = tradingPostService;
            this._translationService = translationService;

            this.Size_X_SettingChanged(this, new ValueChangedEventArgs<int>(0, this.Configuration.Size.X.Value));
            this.Size_Y_SettingChanged(this, new ValueChangedEventArgs<int>(0, this.Configuration.Size.Y.Value));
            this.Location_X_SettingChanged(this, new ValueChangedEventArgs<int>(0, this.Configuration.Location.X.Value));
            this.Location_Y_SettingChanged(this, new ValueChangedEventArgs<int>(0, this.Configuration.Location.Y.Value));
            this.BackgroundColor_SettingChanged(this, new ValueChangedEventArgs<Gw2Sharp.WebApi.V2.Models.Color>(null, this.Configuration.BackgroundColor.Value));
            this.Opacity_SettingChanged(this, new ValueChangedEventArgs<float>(0f, this.Configuration.Opacity.Value));

            this.Configuration.BackgroundColor.SettingChanged += this.BackgroundColor_SettingChanged;
            this.Configuration.Opacity.SettingChanged += this.Opacity_SettingChanged;
            this.Configuration.Location.X.SettingChanged += this.Location_X_SettingChanged;
            this.Configuration.Location.Y.SettingChanged += this.Location_Y_SettingChanged;
            this.Configuration.Size.X.SettingChanged += this.Size_X_SettingChanged;
            this.Configuration.Size.Y.SettingChanged += this.Size_Y_SettingChanged;
            this.Configuration.MaxTransactions.SettingChanged += this.MaxTransactions_SettingChanged;
            this.Configuration.ShowBuyTransactions.SettingChanged += this.ShowBuyTransactions_SettingChanged;
            this.Configuration.ShowSellTransactions.SettingChanged += this.ShowSellTransactions_SettingChanged;
            this.Configuration.ShowHighestTransactions.SettingChanged += this.ShowHighestTransactions_SettingChanged;
        }

        private void ShowHighestTransactions_SettingChanged(object sender, ValueChangedEventArgs<bool> e)
        {
            this.ReAddControls();
        }

        private void ShowSellTransactions_SettingChanged(object sender, ValueChangedEventArgs<bool> e)
        {
            this.ReAddControls();
        }

        private void ShowBuyTransactions_SettingChanged(object sender, ValueChangedEventArgs<bool> e)
        {
            this.ReAddControls();
        }

        private void MaxTransactions_SettingChanged(object sender, ValueChangedEventArgs<int> e)
        {
            this.ReAddControls();
        }

        private void Size_Y_SettingChanged(object sender, ValueChangedEventArgs<int> e)
        {
            this.Size = new Point(this.Size.X, e.NewValue);
        }

        private void Size_X_SettingChanged(object sender, ValueChangedEventArgs<int> e)
        {
            this.Size = new Point(e.NewValue, this.Size.Y);
        }

        private void Location_Y_SettingChanged(object sender, ValueChangedEventArgs<int> e)
        {
            this.Location = new Point(this.Location.X, e.NewValue);
        }

        private void Location_X_SettingChanged(object sender, ValueChangedEventArgs<int> e)
        {
            this.Location = new Point(e.NewValue, this.Location.Y);
        }

        private void Opacity_SettingChanged(object sender, ValueChangedEventArgs<float> e)
        {
            this.Opacity = e.NewValue;
        }

        private void BackgroundColor_SettingChanged(object sender, ValueChangedEventArgs<Gw2Sharp.WebApi.V2.Models.Color> e)
        {
            Color backgroundColor = Color.Transparent;

            if (e.NewValue != null && e.NewValue.Id != 1)
            {
                backgroundColor = e.NewValue.Cloth.ToXnaColor();
            }

            this.BackgroundColor = backgroundColor;
        }

        public new bool Visible
        {
            get
            {
                if (this._currentVisibilityDirection && this._currentVisibilityAnimation != null)
                {
                    return true;
                }

                if (!this._currentVisibilityDirection && this._currentVisibilityAnimation != null)
                {
                    return false;
                }

                return base.Visible;
            }
            set => base.Visible = value;
        }

        public new void Show()
        {
            if (this.Visible && this._currentVisibilityAnimation == null)
            {
                return;
            }

            if (this._currentVisibilityAnimation != null)
            {
                this._currentVisibilityAnimation.Cancel();
            }

            this._currentVisibilityDirection = true;
            this.Visible = true;
            this._currentVisibilityAnimation = Animation.Tweener.Tween(this, new { Opacity = 1f }, 0.2f);
            this._currentVisibilityAnimation.OnComplete(() =>
            {
                this._currentVisibilityAnimation = null;
            });
        }

        public new void Hide()
        {
            if (!this.Visible && this._currentVisibilityAnimation == null)
            {
                return;
            }

            if (this._currentVisibilityAnimation != null)
            {
                this._currentVisibilityAnimation.Cancel();
            }

            this._currentVisibilityDirection = false;
            this._currentVisibilityAnimation = Animation.Tweener.Tween(this, new { Opacity = 0f }, 0.2f);
            this._currentVisibilityAnimation.OnComplete(() =>
            {
                this.Visible = false;
                this._currentVisibilityAnimation = null;
            });
        }

        private bool AddControl(PlayerTransaction transaction)
        {
            if (!this.Configuration.ShowBuyTransactions.Value && transaction.Type == TransactionType.Buy)
            {
                return false;
            }

            if (!this.Configuration.ShowSellTransactions.Value && transaction.Type == TransactionType.Sell)
            {
                return false;
            }

            if (!this.Configuration.ShowHighestTransactions.Value && transaction.IsHighest)
            {
                return false;
            }

            if (this.Children.Count >= this.Configuration.MaxTransactions.Value)
            {
                return false;
            }

            new Transaction(
                transaction,
                this._iconService,
                this._tradingPostService,
                this._translationService,
                this.Configuration.Opacity,
                this.Configuration.ShowPrice,
                this.Configuration.ShowPriceAsTotal,
                this.Configuration.ShowRemaining,
                this.Configuration.ShowCreated,
                this.Configuration.ShowTooltips,
                this.Configuration.FontSize,
                this.Configuration.HighestTransactionColor,
                this.Configuration.OutbidTransactionColor)
            {
                Parent = this,
                DrawInterval = TimeSpan.FromSeconds(1),
                HeightSizingMode = SizingMode.AutoSize,
                WidthSizingMode = SizingMode.AutoSize
            };
            return true;
        }

        private void ReAddControls()
        {
            using var suspendCtx = this.SuspendLayoutContext();

            this.Children.ToList().ForEach(child => child.Dispose());
            this.Children.Clear();

            foreach (var transaction in _transactions)
            {
                _ = this.AddControl(transaction);
            }
        }

        public bool AddTransaction(PlayerTransaction transaction)
        {
            this._transactions.Add(transaction);

            return this.AddControl(transaction);
        }

        public void RemoveTransaction(PlayerTransaction transaction)
        {
            if (this._transactions.Remove(transaction))
            {
                this.ReAddControls();
            }
        }

        public void ClearTransactions()
        {
            this._transactions.Clear();

            this.Children.ToList().ForEach(child => child.Dispose());
            this.Children.Clear();
        }

        protected override CaptureType CapturesInput()
        {
            return CaptureType.None;
        }
    }
}