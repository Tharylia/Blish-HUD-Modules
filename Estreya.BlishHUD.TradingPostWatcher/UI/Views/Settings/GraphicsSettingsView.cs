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
            _ = this.RenderSetting(parent, TradingPostWatcherModule.ModuleInstance.ModuleSettings.LocationX);
            _ = this.RenderSetting(parent, TradingPostWatcherModule.ModuleInstance.ModuleSettings.LocationY);
            _ = this.RenderSetting(parent, TradingPostWatcherModule.ModuleInstance.ModuleSettings.Width);

            this.RenderEmptyLine(parent);

            _ = this.RenderSetting(parent, TradingPostWatcherModule.ModuleInstance.ModuleSettings.FontSize);
            _ = this.RenderSetting(parent, TradingPostWatcherModule.ModuleInstance.ModuleSettings.Opacity);
            this.RenderColorSetting(parent, TradingPostWatcherModule.ModuleInstance.ModuleSettings.BackgroundColor);
            _ = this.RenderSetting(parent, TradingPostWatcherModule.ModuleInstance.ModuleSettings.BackgroundColorOpacity);
        }

        protected override Task<bool> InternalLoad(IProgress<string> progress)
        {
            return Task.FromResult(true);
        }
    }
}
