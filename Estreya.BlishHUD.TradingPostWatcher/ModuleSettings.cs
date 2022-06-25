namespace Estreya.BlishHUD.TradingPostWatcher
{
    using Blish_HUD;
    using Blish_HUD.Input;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.Shared.Settings;
    using Estreya.BlishHUD.TradingPostWatcher.Resources;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using static Blish_HUD.ContentService;

    public class ModuleSettings : BaseModuleSettings
    {
        private static readonly Logger Logger = Logger.GetLogger<ModuleSettings>();

        #region Transactions
        private const string TRANSACTION_SETTINGS = "transaction-settings";
        public SettingCollection TransactionSettings { get; private set; }
        public SettingEntry<int> MaxTransactions { get; private set; }
        public SettingEntry<bool> ShowBuyTransactions { get; private set; }
        public SettingEntry<bool> ShowSellTransactions { get; private set; }
        public SettingEntry<bool> ShowHighestTransactions { get; private set; }
        public SettingEntry<bool> ShowPrice { get; private set; }
        public SettingEntry<bool> ShowPriceAsTotal { get; private set; }
        public SettingEntry<bool> ShowRemaining { get; private set; }
        public SettingEntry<bool> ShowCreated { get; private set; }
        #endregion

        public ModuleSettings(SettingCollection settings) : base(settings, new KeyBinding(Microsoft.Xna.Framework.Input.ModifierKeys.Alt, Microsoft.Xna.Framework.Input.Keys.T)) { }

        protected override void InitializeAdditionalSettings(SettingCollection settings)
        {
            this.InitializeTransactionSettings(settings);
        }

        private void InitializeTransactionSettings(SettingCollection settings)
        {
            this.TransactionSettings = settings.AddSubCollection(TRANSACTION_SETTINGS);

            this.MaxTransactions = this.TransactionSettings.DefineSetting(nameof(this.MaxTransactions), 10, () => "Max Transactions", () => "Defines the max number of transactions shown.");
            this.MaxTransactions.SetRange(1, 50);
            this.MaxTransactions.SettingChanged += this.SettingChanged;

            this.ShowBuyTransactions = this.TransactionSettings.DefineSetting(nameof(this.ShowBuyTransactions), true, () => "Show Buy Transactions", () => "Whether buy transactions should be shown.");
            this.ShowBuyTransactions.SettingChanged += this.SettingChanged;

            this.ShowSellTransactions = this.TransactionSettings.DefineSetting(nameof(this.ShowSellTransactions), true, () => "Show Sell Transactions", () => "Whether sell transactions should be shown.");
            this.ShowSellTransactions.SettingChanged += this.SettingChanged;

            this.ShowHighestTransactions = this.TransactionSettings.DefineSetting(nameof(this.ShowHighestTransactions), true, () => "Show Highest Buy/Sell Transactions", () => "Whether the highest buy/sell transactions should be shown or only outbid ones.");
            this.ShowHighestTransactions.SettingChanged += this.SettingChanged;

            this.ShowPrice = this.TransactionSettings.DefineSetting(nameof(this.ShowPrice), true, () => "Show Price", () => "Whether the price of the transaction should be shown.");
            this.ShowPrice.SettingChanged += this.SettingChanged;

            this.ShowPriceAsTotal = this.TransactionSettings.DefineSetting(nameof(this.ShowPriceAsTotal), true, () => "Show Price as Total", () => "Whether the price of the transaction should be shown as the total price.");
            this.ShowPriceAsTotal.SettingChanged += this.SettingChanged;

            this.ShowRemaining = this.TransactionSettings.DefineSetting(nameof(this.ShowRemaining), true, () => "Show Remaining Quantity", () => "Whether the remaining quantity of the transaction should be shown.");
            this.ShowRemaining.SettingChanged += this.SettingChanged;

            this.ShowCreated = this.TransactionSettings.DefineSetting(nameof(this.ShowCreated), false, () => "Show Created Date", () => "Whether the created date of the transaction should be shown.");
            this.ShowCreated.SettingChanged += this.SettingChanged;

        }

        public override void Unload()
        {
            base.Unload();

            this.MaxTransactions.SettingChanged -= this.SettingChanged;
            this.ShowBuyTransactions.SettingChanged -= this.SettingChanged;
            this.ShowSellTransactions.SettingChanged -= this.SettingChanged;
            this.ShowHighestTransactions.SettingChanged -= this.SettingChanged;
            this.ShowPrice.SettingChanged -= this.SettingChanged;
            this.ShowPriceAsTotal.SettingChanged -= this.SettingChanged;
            this.ShowRemaining.SettingChanged -= this.SettingChanged;
            this.ShowCreated.SettingChanged -= this.SettingChanged;

            for (int i = this.TransactionSettings.Entries.Count - 1; i >= 0; i--)
            {
                this.TransactionSettings.UndefineSetting(this.TransactionSettings.Entries[i].EntryKey);
            }
        }
    }
}
