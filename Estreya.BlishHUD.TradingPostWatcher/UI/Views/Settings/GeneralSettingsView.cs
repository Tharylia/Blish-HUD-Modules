namespace Estreya.BlishHUD.TradingPostWatcher.UI.Views.Settings
{
    using Blish_HUD.Controls;
    using Estreya.BlishHUD.Shared.UI.Views;
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;

    public class GeneralSettingsView : BaseSettingsView
    {
        public GeneralSettingsView() : base()
        {
        }

        protected override void BuildView(Panel parent)
        {
            this.RenderSetting(parent, TradingPostWatcherModule.ModuleInstance.ModuleSettings.GlobalEnabled);
            this.RenderSetting(parent, TradingPostWatcherModule.ModuleInstance.ModuleSettings.GlobalEnabledHotkey);
            this.RenderSetting(parent, TradingPostWatcherModule.ModuleInstance.ModuleSettings.RegisterCornerIcon);

            this.RenderEmptyLine(parent);

            this.RenderSetting(parent, TradingPostWatcherModule.ModuleInstance.ModuleSettings.HideOnMissingMumbleTicks);
            this.RenderSetting(parent, TradingPostWatcherModule.ModuleInstance.ModuleSettings.HideOnOpenMap);
            this.RenderSetting(parent, TradingPostWatcherModule.ModuleInstance.ModuleSettings.HideInCombat);
            this.RenderSetting(parent, TradingPostWatcherModule.ModuleInstance.ModuleSettings.HideInWvW);
            this.RenderSetting(parent, TradingPostWatcherModule.ModuleInstance.ModuleSettings.HideInPvP);

            this.RenderEmptyLine(parent);

            this.RenderSetting(parent, TradingPostWatcherModule.ModuleInstance.ModuleSettings.BuildDirection);

        }

        protected override Task<bool> InternalLoad(IProgress<string> progress)
        {
            return Task.FromResult(true);
        }
    }
}
