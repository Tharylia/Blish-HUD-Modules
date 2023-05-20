namespace Estreya.BlishHUD.TradingPostWatcher
{
    using Blish_HUD;
    using Blish_HUD.Input;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.Shared.Settings;
    using Estreya.BlishHUD.TradingPostWatcher.Models;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using static Blish_HUD.ContentService;

    public class ModuleSettings : BaseModuleSettings
    {
        public SettingEntry<List<string>> AreaNames { get; set; }

        public ModuleSettings(SettingCollection settings) : base(settings, new KeyBinding(Microsoft.Xna.Framework.Input.ModifierKeys.Alt, Microsoft.Xna.Framework.Input.Keys.T)) { }

        protected override void DoInitializeGlobalSettings(SettingCollection globalSettingCollection)
        {
            this.AreaNames = globalSettingCollection.DefineSetting(nameof(this.AreaNames), new List<string>());
        }

        public TransactionAreaConfiguration AddDrawer(string name)
        {
            var drawer = base.AddDrawer(name);

            var maxTransactions = this.DrawerSettings.DefineSetting($"{name}-maxTransactions", 10, () => "Max Transactions", () => "Defines the max number of transactions shown.");
            maxTransactions.SetRange(1, 50);

            var showBuyTransactions = this.DrawerSettings.DefineSetting($"{name}-showBuyTransactions", true, () => "Show Buy Transactions", () => "Whether buy transactions should be shown.");

            var showSellTransactions = this.DrawerSettings.DefineSetting($"{name}-showSellTransactions", true, () => "Show Sell Transactions", () => "Whether sell transactions should be shown.");

            var showHighestTransactions = this.DrawerSettings.DefineSetting($"{name}-showHighestTransactions", true, () => "Show Highest Buy/Sell Transactions", () => "Whether the highest buy/sell transactions should be shown or only outbid ones.");

            var showPrice = this.DrawerSettings.DefineSetting($"{name}-showPrice", true, () => "Show Price", () => "Whether the price of the transaction should be shown.");

            var showPriceAsTotal = this.DrawerSettings.DefineSetting($"{name}-showPriceAsTotal", true, () => "Show Price as Total", () => "Whether the price of the transaction should be shown as the total price.");

            var showRemaining = this.DrawerSettings.DefineSetting($"{name}-showRemaining", true, () => "Show Remaining Quantity", () => "Whether the remaining quantity of the transaction should be shown.");

            var showCreated = this.DrawerSettings.DefineSetting($"{name}-showCreated", false, () => "Show Created Date", () => "Whether the created date of the transaction should be shown.");

            var showTooltips = this.DrawerSettings.DefineSetting($"{name}-showTooltips", true, () => "Show Tooltips", () => "Whether the transactions displays a tooltip on mouse hover.");

            var highestTransactionColor = this.DrawerSettings.DefineSetting($"{name}-highestTransactionColor", this.DefaultGW2Color, () => "Highest Transaction Color", () => "Defines the color which the highest transations should be displayed in.");

            var outbidTransactionColor = this.DrawerSettings.DefineSetting($"{name}-outbidTransactionColor", this.DefaultGW2Color, () => "Outbid Transaction Color", () => "Defines the color which the outbid transations should be displayed in.");

            var transactionHeight = this.DrawerSettings.DefineSetting($"{name}-transactionHeight", 30, () => "Transaction Height", () => "Defines the height of the individual rendered transactions.");
            transactionHeight.SetRange(0, 50);

            var showNoDataInfo = this.DrawerSettings.DefineSetting($"{name}-showNoDataInfo", true, () => "Show \"No Data\" Info", () => "Defines whether a no data info text should be displayed in case no transactions are available.");

            var noDataTextColor = this.DrawerSettings.DefineSetting($"{name}-noDataTextColor", this.DefaultGW2Color, () => "No Data Text Color", () => "Defines the color of the no data info text.");

            var noDataHeight = this.DrawerSettings.DefineSetting($"{name}-noDataHeight", 30, () => "No Data Height", () => "Defines the height of the no data info text.");
            noDataHeight.SetRange(20, 200);

            return new TransactionAreaConfiguration()
            {
                Name = drawer.Name,
                Enabled = drawer.Enabled,
                EnabledKeybinding = drawer.EnabledKeybinding,
                BuildDirection = drawer.BuildDirection,
                BackgroundColor = drawer.BackgroundColor,
                FontSize = drawer.FontSize,
                TextColor = drawer.TextColor,
                Location = drawer.Location,
                Opacity = drawer.Opacity,
                Size = drawer.Size,
                MaxTransactions = maxTransactions,
                ShowBuyTransactions = showBuyTransactions,
                ShowSellTransactions = showSellTransactions,
                ShowHighestTransactions = showHighestTransactions,
                ShowPrice = showPrice,
                ShowPriceAsTotal = showPriceAsTotal,
                ShowRemaining = showRemaining,
                ShowCreated = showCreated,
                ShowTooltips = showTooltips,
                HighestTransactionColor = highestTransactionColor,
                OutbidTransactionColor = outbidTransactionColor,
                TransactionHeight = transactionHeight,
                ShowNoDataInfo = showNoDataInfo,
                NoDataTextColor = noDataTextColor,
                NoDataHeight = noDataHeight,
            };
        }

        public new void RemoveDrawer(string name)
        {
            base.RemoveDrawer(name);

            this.DrawerSettings.UndefineSetting($"{name}-maxTransactions");
            this.DrawerSettings.UndefineSetting($"{name}-showBuyTransactions");
            this.DrawerSettings.UndefineSetting($"{name}-showSellTransactions");
            this.DrawerSettings.UndefineSetting($"{name}-showHighestTransactions");
            this.DrawerSettings.UndefineSetting($"{name}-showPrice");
            this.DrawerSettings.UndefineSetting($"{name}-showPriceAsTotal");
            this.DrawerSettings.UndefineSetting($"{name}-showRemaining");
            this.DrawerSettings.UndefineSetting($"{name}-showCreated");
            this.DrawerSettings.UndefineSetting($"{name}-showTooltips");
            this.DrawerSettings.UndefineSetting($"{name}-highestTransactionColor");
            this.DrawerSettings.UndefineSetting($"{name}-outbidTransactionColor");
            this.DrawerSettings.UndefineSetting($"{name}-transactionHeight");
            this.DrawerSettings.UndefineSetting($"{name}-showNoDataInfo");
            this.DrawerSettings.UndefineSetting($"{name}-noDataTextColor");
            this.DrawerSettings.UndefineSetting($"{name}-noDataHeight");
        }
    }
}
