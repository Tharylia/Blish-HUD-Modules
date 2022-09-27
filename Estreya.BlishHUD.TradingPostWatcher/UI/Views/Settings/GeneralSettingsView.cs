namespace Estreya.BlishHUD.TradingPostWatcher.UI.Views.Settings
{
    using Blish_HUD.Controls;
    using Blish_HUD.Modules.Managers;
    using Estreya.BlishHUD.Shared.State;
    using Estreya.BlishHUD.Shared.UI.Views;
    using MonoGame.Extended.BitmapFonts;
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;

    public class GeneralSettingsView : BaseSettingsView
    {
        public GeneralSettingsView(Gw2ApiManager apiManager, IconState iconState, BitmapFont font = null) : base(apiManager, iconState, font)
        {
        }

        protected override void BuildView(Panel parent)
        {
            this.RenderBoolSetting(parent, TradingPostWatcherModule.ModuleInstance.ModuleSettings.GlobalDrawerVisible);
            this.RenderKeybindingSetting(parent, TradingPostWatcherModule.ModuleInstance.ModuleSettings.GlobalDrawerVisibleHotkey);
            this.RenderBoolSetting(parent, TradingPostWatcherModule.ModuleInstance.ModuleSettings.RegisterCornerIcon);

            this.RenderEmptyLine(parent);

            this.RenderBoolSetting(parent, TradingPostWatcherModule.ModuleInstance.ModuleSettings.HideOnMissingMumbleTicks);
            this.RenderBoolSetting(parent, TradingPostWatcherModule.ModuleInstance.ModuleSettings.HideOnOpenMap);
            this.RenderBoolSetting(parent, TradingPostWatcherModule.ModuleInstance.ModuleSettings.HideInCombat);
            this.RenderBoolSetting(parent, TradingPostWatcherModule.ModuleInstance.ModuleSettings.HideInWvW);
            this.RenderBoolSetting(parent, TradingPostWatcherModule.ModuleInstance.ModuleSettings.HideInPvP);
            this.RenderBoolSetting(parent, TradingPostWatcherModule.ModuleInstance.ModuleSettings.HideInPvE_OpenWorld);
            this.RenderBoolSetting(parent, TradingPostWatcherModule.ModuleInstance.ModuleSettings.HideInPvE_Competetive);

            //this.RenderEmptyLine(parent);

            //this.RenderSetting(parent, TradingPostWatcherModule.ModuleInstance.ModuleSettings.BuildDirection);

        }

        protected override Task<bool> InternalLoad(IProgress<string> progress)
        {
            return Task.FromResult(true);
        }
    }
}
