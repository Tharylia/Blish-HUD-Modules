namespace Estreya.BlishHUD.TradingPostWatcher.UI.Views.Settings
{
    using Blish_HUD.Controls;
    using Estreya.BlishHUD.Shared.State;
    using Estreya.BlishHUD.Shared.UI.Views;
    using Estreya.BlishHUD.TradingPostWatcher;
    using Microsoft.Xna.Framework;
    using System;
    using System.Threading.Tasks;

    public class TransactionSettingsView : BaseSettingsView
    {
        public TransactionSettingsView() : base()
        {
        }

        protected override void BuildView(Panel parent)
        {
            this.RenderSetting(parent, TradingPostWatcherModule.ModuleInstance.ModuleSettings.MaxTransactions);
            this.RenderSetting(parent, TradingPostWatcherModule.ModuleInstance.ModuleSettings.ShowBuyTransactions);
            this.RenderSetting(parent, TradingPostWatcherModule.ModuleInstance.ModuleSettings.ShowSellTransactions);
            this.RenderSetting(parent, TradingPostWatcherModule.ModuleInstance.ModuleSettings.ShowHighestTransactions);
            this.RenderSetting(parent, TradingPostWatcherModule.ModuleInstance.ModuleSettings.ShowPrice);
            this.RenderSetting(parent, TradingPostWatcherModule.ModuleInstance.ModuleSettings.ShowPriceAsTotal);
            this.RenderSetting(parent, TradingPostWatcherModule.ModuleInstance.ModuleSettings.ShowRemaining);
            this.RenderSetting(parent, TradingPostWatcherModule.ModuleInstance.ModuleSettings.ShowCreated);
        }

        protected override Task<bool> InternalLoad(IProgress<string> progress)
        {
            return Task.FromResult(true);
        }
    }
}
