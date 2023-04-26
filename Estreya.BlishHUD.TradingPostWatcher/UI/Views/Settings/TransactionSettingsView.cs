namespace Estreya.BlishHUD.TradingPostWatcher.UI.Views.Settings
{
    using Blish_HUD.Controls;
    using Blish_HUD.Modules.Managers;
    using Estreya.BlishHUD.Shared.Service;
    using Estreya.BlishHUD.Shared.UI.Views;
    using Estreya.BlishHUD.TradingPostWatcher;
    using Microsoft.Xna.Framework;
    using MonoGame.Extended.BitmapFonts;
    using System;
    using System.Threading.Tasks;

    public class TransactionSettingsView : BaseSettingsView
    {
        public TransactionSettingsView(Gw2ApiManager apiManager, IconService iconService, TranslationService translationService,SettingEventService settingEventService,  BitmapFont font = null) : base(apiManager, iconService, translationService, settingEventService, font)
        {
        }

        protected override void BuildView(FlowPanel parent)
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
            this.RenderEmptyLine(parent);
            this.RenderColorSetting(parent, TradingPostWatcherModule.ModuleInstance.DrawerConfiguration.HighestTransactionColor);
            this.RenderColorSetting(parent, TradingPostWatcherModule.ModuleInstance.DrawerConfiguration.OutbidTransactionColor);
        }

        protected override Task<bool> InternalLoad(IProgress<string> progress)
        {
            return Task.FromResult(true);
        }
    }
}
