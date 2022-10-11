namespace Estreya.BlishHUD.TradingPostWatcher.UI.Views.Settings
{
    using Blish_HUD.Controls;
    using Blish_HUD.Modules.Managers;
    using Estreya.BlishHUD.Shared.State;
    using Estreya.BlishHUD.Shared.UI.Views;
    using Estreya.BlishHUD.TradingPostWatcher;
    using Microsoft.Xna.Framework;
    using MonoGame.Extended.BitmapFonts;
    using System;
    using System.Threading.Tasks;

    public class TransactionSettingsView : BaseSettingsView
    {
        public TransactionSettingsView(Gw2ApiManager apiManager, IconState iconState, BitmapFont font = null) : base(apiManager, iconState, font)
        {
        }

        protected override void BuildView(Panel parent)
        {
            this.RenderIntSetting(parent, TradingPostWatcherModule.ModuleInstance.DrawerConfiguration.MaxTransactions);
            this.RenderBoolSetting(parent, TradingPostWatcherModule.ModuleInstance.DrawerConfiguration.ShowBuyTransactions);
            this.RenderBoolSetting(parent, TradingPostWatcherModule.ModuleInstance.DrawerConfiguration.ShowSellTransactions);
            this.RenderBoolSetting(parent, TradingPostWatcherModule.ModuleInstance.DrawerConfiguration.ShowHighestTransactions);
            this.RenderBoolSetting(parent, TradingPostWatcherModule.ModuleInstance.DrawerConfiguration.ShowPrice);
            this.RenderBoolSetting(parent, TradingPostWatcherModule.ModuleInstance.DrawerConfiguration.ShowPriceAsTotal);
            this.RenderBoolSetting(parent, TradingPostWatcherModule.ModuleInstance.DrawerConfiguration.ShowRemaining);
            this.RenderBoolSetting(parent, TradingPostWatcherModule.ModuleInstance.DrawerConfiguration.ShowCreated);
            this.RenderBoolSetting(parent, TradingPostWatcherModule.ModuleInstance.DrawerConfiguration.ShowTooltips);
        }

        protected override Task<bool> InternalLoad(IProgress<string> progress)
        {
            return Task.FromResult(true);
        }
    }
}
