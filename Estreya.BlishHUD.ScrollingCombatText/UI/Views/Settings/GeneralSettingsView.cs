namespace Estreya.BlishHUD.ScrollingCombatText.UI.Views.Settings
{
    using Blish_HUD.Controls;
    using Estreya.BlishHUD.ScrollingCombatText;
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
            this.RenderSetting(parent, ScrollingCombatTextModule.ModuleInstance.ModuleSettings.GlobalDrawerVisible);
            this.RenderSetting(parent, ScrollingCombatTextModule.ModuleInstance.ModuleSettings.GlobalDrawerVisibleHotkey);
            this.RenderSetting(parent, ScrollingCombatTextModule.ModuleInstance.ModuleSettings.RegisterCornerIcon);

            this.RenderEmptyLine(parent);

            this.RenderSetting(parent, ScrollingCombatTextModule.ModuleInstance.ModuleSettings.HideOnMissingMumbleTicks);
            this.RenderSetting(parent, ScrollingCombatTextModule.ModuleInstance.ModuleSettings.HideOnOpenMap);
            this.RenderSetting(parent, ScrollingCombatTextModule.ModuleInstance.ModuleSettings.HideInWvW);
            this.RenderSetting(parent, ScrollingCombatTextModule.ModuleInstance.ModuleSettings.HideInPvP);

            //this.RenderEmptyLine(parent);

            //this.RenderSetting(parent, TradingPostWatcherModule.ModuleInstance.ModuleSettings.BuildDirection);

        }

        protected override Task<bool> InternalLoad(IProgress<string> progress)
        {
            return Task.FromResult(true);
        }
    }
}
