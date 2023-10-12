namespace Estreya.BlishHUD.TradingPostWatcher.Controls;

using Blish_HUD;
using Blish_HUD._Extensions;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Models;
using MonoGame.Extended;
using MonoGame.Extended.BitmapFonts;
using Shared.Controls;
using Shared.Extensions;
using Shared.Models;
using Shared.Models.GW2API.Commerce;
using Shared.Services;
using Shared.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using static Blish_HUD.ContentService;
using Color = Gw2Sharp.WebApi.V2.Models.Color;

public class TransactionArea : RenderTarget2DControl, IVisibilityChanging
{
    private static readonly Logger Logger = Logger.GetLogger<Transaction>();

    private static readonly ConcurrentDictionary<FontSize, BitmapFont> _fonts = new ConcurrentDictionary<FontSize, BitmapFont>();
    private readonly IconService _iconService;
    private readonly TradingPostService _tradingPostService;
    private readonly TranslationService _translationService;

    private int _heightFromLastDraw = 1;

    private Transaction _hoveredTransaction;

    private NoData _noDataControl;
    private readonly AsyncLock _transactionLock = new AsyncLock();

    private readonly List<Transaction> _transactions = new List<Transaction>();

    public TransactionArea(TransactionAreaConfiguration configuration, IconService iconService, TradingPostService tradingPostService, TranslationService translationService)
    {
        this.Configuration = configuration;
        this._iconService = iconService;
        this._tradingPostService = tradingPostService;
        this._translationService = translationService;

        this.Size_X_SettingChanged(this, new ValueChangedEventArgs<int>(0, this.Configuration.Size.X.Value));
        this.Location_X_SettingChanged(this, new ValueChangedEventArgs<int>(0, this.Configuration.Location.X.Value));
        this.Location_Y_SettingChanged(this, new ValueChangedEventArgs<int>(0, this.Configuration.Location.Y.Value));
        this.BackgroundColor_SettingChanged(this, new ValueChangedEventArgs<Color>(null, this.Configuration.BackgroundColor.Value));
        this.Opacity_SettingChanged(this, new ValueChangedEventArgs<float>(0f, this.Configuration.Opacity.Value));

        this.Configuration.EnabledKeybinding.Value.Activated += this.EnabledKeybinding_Activated;
        this.Configuration.BackgroundColor.SettingChanged += this.BackgroundColor_SettingChanged;
        this.Configuration.Opacity.SettingChanged += this.Opacity_SettingChanged;
        this.Configuration.Location.X.SettingChanged += this.Location_X_SettingChanged;
        this.Configuration.Location.Y.SettingChanged += this.Location_Y_SettingChanged;
        this.Configuration.Size.X.SettingChanged += this.Size_X_SettingChanged;
        //this.Configuration.Size.Y.SettingChanged += this.Size_Y_SettingChanged;
        this.Configuration.ShowBuyTransactions.SettingChanged += this.ShowBuyTransactions_SettingChanged;
        this.Configuration.ShowSellTransactions.SettingChanged += this.ShowSellTransactions_SettingChanged;
        this.Configuration.ShowHighestTransactions.SettingChanged += this.ShowHighestTransactions_SettingChanged;

        this._noDataControl = new NoData(GameService.Content.DefaultFont18);

        this.Height = 1;
    }

    private void EnabledKeybinding_Activated(object sender, EventArgs e)
    {
        this.Configuration.Enabled.Value = !this.Configuration.Enabled.Value;
    }

    public TransactionAreaConfiguration Configuration { get; }

    public new bool Enabled => this.Configuration.Enabled.Value;

    public bool CalculateVisibility()
    {
        using (this._transactionLock.Lock())
        {
            return this._transactions.Count > 0 || this.Configuration.ShowNoDataInfo.Value;
        }
    }

    public event EventHandler RequestedNewData;

    private void ShowSellTransactions_SettingChanged(object sender, ValueChangedEventArgs<bool> e)
    {
        this.RequestNewData();
    }

    private void ShowBuyTransactions_SettingChanged(object sender, ValueChangedEventArgs<bool> e)
    {
        this.RequestNewData();
    }

    private void ShowHighestTransactions_SettingChanged(object sender, ValueChangedEventArgs<bool> e)
    {
        this.RequestNewData();
    }

    private void RequestNewData()
    {
        this.RequestedNewData?.Invoke(this, EventArgs.Empty);
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

    private void BackgroundColor_SettingChanged(object sender, ValueChangedEventArgs<Color> e)
    {
        Microsoft.Xna.Framework.Color backgroundColor = Microsoft.Xna.Framework.Color.Transparent;

        if (e.NewValue != null && e.NewValue.Id != 1)
        {
            backgroundColor = e.NewValue.Cloth.ToXnaColor();
        }

        this.BackgroundColor = backgroundColor;
    }

    private void AddTransaction(PlayerTransaction transaction)
    {
        if (transaction == null)
        {
            return;
        }

        if (!this.Configuration.ShowBuyTransactions.Value && transaction.Type is TransactionType.Buy)
        {
            return;
        }

        if (!this.Configuration.ShowSellTransactions.Value && transaction.Type is TransactionType.Sell)
        {
            return;
        }

        if (!this.Configuration.ShowHighestTransactions.Value && transaction.IsHighest) return;

        using (this._transactionLock.Lock())
        {
            this._transactions?.Add(new Transaction(
                transaction,
                this._iconService,
                this._translationService,
                this.GetFont,
                () => this.Configuration.Opacity.Value,
                () => this.GetTextColor(transaction),
                () => this.Configuration.ShowPrice.Value,
                () => this.Configuration.ShowPriceAsTotal.Value,
                () => this.Configuration.ShowRemaining.Value,
                () => this.Configuration.ShowCreated.Value,
                async () => await this._tradingPostService.GetPriceForItem(transaction.ItemId, transaction.Type)));
        }

        Logger.Debug($"Added new transaction: {transaction}");
    }

    private Microsoft.Xna.Framework.Color GetTextColor(PlayerTransaction transaction)
    {
        Microsoft.Xna.Framework.Color defaultTextColor = this.Configuration.TextColor.Value.Id != 1
            ? this.Configuration.TextColor.Value.Cloth.ToXnaColor()
            : Microsoft.Xna.Framework.Color.Black;

        return transaction.IsHighest
            ? this.Configuration.HighestTransactionColor.Value.Id != 1 ? this.Configuration.HighestTransactionColor.Value.Cloth.ToXnaColor() : defaultTextColor
            : this.Configuration.OutbidTransactionColor.Value.Id != 1
                ? this.Configuration.OutbidTransactionColor.Value.Cloth.ToXnaColor()
                : defaultTextColor;
    }

    private BitmapFont GetFont()
    {
        return _fonts.GetOrAdd(this.Configuration.FontSize.Value, fontSize => GameService.Content.GetFont(ContentService.FontFace.Menomonia, fontSize, FontStyle.Regular));
    }

    public void AddTransactions(IEnumerable<PlayerTransaction> transactions)
    {
        if (transactions == null)
        {
            return;
        }

        foreach (PlayerTransaction transaction in transactions)
        {
            this.AddTransaction(transaction);
        }
    }

    public void ClearTransactions()
    {
        using (this._transactionLock.Lock())
        {
            this._transactions?.ForEach(t => t?.Dispose());
            this._transactions?.Clear();
        }

        Logger.Debug("Cleared all transactions.");
    }

    protected override CaptureType CapturesInput()
    {
        return CaptureType.Mouse | CaptureType.DoNotBlock;
    }

    protected override void InternalUpdate(GameTime gameTime)
    {
        this.Height = this._heightFromLastDraw <= 0 ? 1 : this._heightFromLastDraw;
    }

    protected override void DoPaint(SpriteBatch spriteBatch, Rectangle bounds)
    {
        Transaction prevHoveredTransaction = this._hoveredTransaction;
        this._hoveredTransaction = null;

        float y = 0;
        List<Transaction> transactions = new List<Transaction>();
        using (this._transactionLock.Lock())
        {
            transactions.AddRange(this._transactions.Take(this.Configuration.MaxTransactions.Value));
        }

        // Render transactions
        foreach (Transaction transaction in transactions)
        {
            RectangleF renderRect = new RectangleF(0, y, this.Width, this.Configuration.TransactionHeight.Value);
            RectangleF actualRenderRect = transaction.Render(spriteBatch, renderRect);
            if (actualRenderRect.ToBounds(this.AbsoluteBounds).Contains(GameService.Input.Mouse.Position))
            {
                this._hoveredTransaction = transaction;
            }

            y += actualRenderRect.Height;
        }

        // Render no data section
        if (transactions.Count == 0 && this.Configuration.ShowNoDataInfo.Value)
        {
            int height = this.Configuration.NoDataHeight.Value;
            if (this._noDataControl != null)
            {
                this._noDataControl.TextColor = this.Configuration.NoDataTextColor.Value.Id != 1
                    ? this.Configuration.NoDataTextColor.Value.Cloth.ToXnaColor()
                    : Microsoft.Xna.Framework.Color.Red;

                _ = this._noDataControl.Render(spriteBatch, new RectangleF(bounds.X, bounds.Y, bounds.Width, height));
            }

            y += height;
        }

        if (this._hoveredTransaction != prevHoveredTransaction)
        {
            this.Tooltip?.Dispose();
            this.Tooltip = null;

            if (this.Configuration.ShowTooltips.Value)
            {
                _ = this._hoveredTransaction?.BuildTooltip().ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        Logger.Warn(t.Exception, $"Could not build tooltip for transaction \"{transactions}\":");
                        return;
                    }

                    this.Tooltip = t.Result;
                }).ConfigureAwait(false);
            }
        }

        this._heightFromLastDraw = (int)Math.Ceiling(y);
    }

    protected override void InternalDispose()
    {
        this.ClearTransactions();

        this.Configuration.EnabledKeybinding.Value.Activated -= this.EnabledKeybinding_Activated;
        this.Configuration.BackgroundColor.SettingChanged -= this.BackgroundColor_SettingChanged;
        this.Configuration.Opacity.SettingChanged -= this.Opacity_SettingChanged;
        this.Configuration.Location.X.SettingChanged -= this.Location_X_SettingChanged;
        this.Configuration.Location.Y.SettingChanged -= this.Location_Y_SettingChanged;
        this.Configuration.Size.X.SettingChanged -= this.Size_X_SettingChanged;
        this.Configuration.ShowBuyTransactions.SettingChanged -= this.ShowBuyTransactions_SettingChanged;
        this.Configuration.ShowSellTransactions.SettingChanged -= this.ShowSellTransactions_SettingChanged;
        this.Configuration.ShowHighestTransactions.SettingChanged -= this.ShowHighestTransactions_SettingChanged;

        this._noDataControl?.Dispose();
        this._noDataControl = null;
    }
}