namespace Estreya.BlishHUD.TradingPostWatcher.UI.Views.Settings
{
    using Blish_HUD.Controls;
    using Blish_HUD.Modules.Managers;
    using Estreya.BlishHUD.Shared.State;
    using Estreya.BlishHUD.Shared.UI.Views;
    using MonoGame.Extended.BitmapFonts;
    using System;
    using System.Threading.Tasks;

    public class GraphicsSettingsView : BaseSettingsView
    {
        public GraphicsSettingsView(Gw2ApiManager apiManager, IconState iconState, BitmapFont font = null) : base(apiManager, iconState, font)
        {
        }

        protected override void BuildView(Panel parent)
        {
            _ = this.RenderIntSetting(parent, TradingPostWatcherModule.ModuleInstance.DrawerConfiguration.Location.X);
            _ = this.RenderIntSetting(parent, TradingPostWatcherModule.ModuleInstance.DrawerConfiguration.Location.Y);
            _ = this.RenderIntSetting(parent, TradingPostWatcherModule.ModuleInstance.DrawerConfiguration.Size.X);

            this.RenderEmptyLine(parent);

            _ = this.RenderEnumSetting(parent, TradingPostWatcherModule.ModuleInstance.DrawerConfiguration.FontSize);
            _ = this.RenderFloatSetting(parent, TradingPostWatcherModule.ModuleInstance.DrawerConfiguration.Opacity);
            this.RenderColorSetting(parent, TradingPostWatcherModule.ModuleInstance.DrawerConfiguration.BackgroundColor);
            //_ = this.RenderSetting(parent, TradingPostWatcherModule.ModuleInstance.ModuleSettings.BackgroundColorOpacity);
        }

        protected override Task<bool> InternalLoad(IProgress<string> progress)
        {
            return Task.FromResult(true);
        }
    }
}
