namespace Estreya.BlishHUD.TradingPostWatcher.UI.Views.Settings
{
    using Blish_HUD.Controls;
    using Estreya.BlishHUD.Shared.UI.Views;
    using System;
    using System.Threading.Tasks;

    public class GraphicsSettingsView : BaseSettingsView
    {
        public GraphicsSettingsView() : base()
        {
        }

        protected override void BuildView(Panel parent)
        {
            _ = this.RenderSetting(parent, TradingPostWatcherModule.ModuleInstance.DrawerConfiguration.Location.X);
            _ = this.RenderSetting(parent, TradingPostWatcherModule.ModuleInstance.DrawerConfiguration.Location.Y);
            _ = this.RenderSetting(parent, TradingPostWatcherModule.ModuleInstance.DrawerConfiguration.Size.X);

            this.RenderEmptyLine(parent);

            _ = this.RenderSetting(parent, TradingPostWatcherModule.ModuleInstance.DrawerConfiguration.FontSize);
            _ = this.RenderSetting(parent, TradingPostWatcherModule.ModuleInstance.DrawerConfiguration.Opacity);
            this.RenderColorSetting(parent, TradingPostWatcherModule.ModuleInstance.DrawerConfiguration.BackgroundColor);
            //_ = this.RenderSetting(parent, TradingPostWatcherModule.ModuleInstance.ModuleSettings.BackgroundColorOpacity);
        }

        protected override Task<bool> InternalLoad(IProgress<string> progress)
        {
            return Task.FromResult(true);
        }
    }
}
