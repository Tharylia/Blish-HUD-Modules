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
        private static readonly Logger Logger = Logger.GetLogger<ModuleSettings>();

        public ModuleSettings(SettingCollection settings) : base(settings, new KeyBinding(Microsoft.Xna.Framework.Input.ModifierKeys.Alt, Microsoft.Xna.Framework.Input.Keys.T)) { }

        public TransactionDrawerConfiguration AddDrawer(string name)
        {
            var drawer = base.AddDrawer(name);

            var maxTransactions = this.DrawerSettings.DefineSetting($"{name}-maxTransactions", 10, () => "Max Transactions", () => "Defines the max number of transactions shown.");
            maxTransactions.SetRange(1, 50);

            var showBuyTransactions = this.DrawerSettings.DefineSetting($"{name}-showBuyTransactions", true, () => "Show Buy Transactions", () => "Whether buy transactions should be shown.");

            var showSellTransactions = this.DrawerSettings.DefineSetting($"{name}-showSellTransactions", true, () => "Show Sell Transactions", () => "Whether sell transactions should be shown.");

            var showHighestTransactions = this.DrawerSettings.DefineSetting($"{name}-showHighestTransactions", true, () => "Show Highest Buy/Sell Transactions", () => "Whether the highest buy/sell transactions should be shown or only outbid ones.");

            var showPrice = this.DrawerSettings.DefineSetting($"{name}-showPrice", true, () => "Show Price", () => "Whether the price of the transaction should be shown.");

            var showPriceAsTotal = this.DrawerSettings.DefineSetting($"{name}-showPriceAsTotal", true, () => "Show Price as Total", () => "Whether the price of the transaction should be shown as the total price.");

            var showRemaining = this.DrawerSettings.DefineSetting($"{name}-ShowRemaining", true, () => "Show Remaining Quantity", () => "Whether the remaining quantity of the transaction should be shown.");

            var showCreated = this.DrawerSettings.DefineSetting($"{name}-showCreated", false, () => "Show Created Date", () => "Whether the created date of the transaction should be shown.");

            return new TransactionDrawerConfiguration()
            {
                Name = drawer.Name,
                Enabled = drawer.Enabled,
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
            this.DrawerSettings.UndefineSetting($"{name}-ShowRemaining");
            this.DrawerSettings.UndefineSetting($"{name}-showCreated");
        }

        public override void Unload()
        {
            base.Unload();
        }
    }
}
