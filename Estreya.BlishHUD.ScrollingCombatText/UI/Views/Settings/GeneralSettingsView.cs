namespace Estreya.BlishHUD.ScrollingCombatText.UI.Views.Settings
{
    using Blish_HUD.Controls;
    using Blish_HUD.Modules.Managers;
    using Estreya.BlishHUD.ScrollingCombatText;
    using Estreya.BlishHUD.Shared.Service;
    using Estreya.BlishHUD.Shared.UI.Views;
    using MonoGame.Extended.BitmapFonts;
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;

    public class GeneralSettingsView : BaseSettingsView
    {
        public GeneralSettingsView(Gw2ApiManager apiManager, IconService iconService, TranslationService translationService, SettingEventService settingEventService, BitmapFont font = null) : base(apiManager, iconService, translationService, settingEventService, font)
        {
        }

        protected override void BuildView(FlowPanel parent)
        {
            this.RenderBoolSetting(parent, ScrollingCombatTextModule.ModuleInstance.ModuleSettings.GlobalDrawerVisible);
            this.RenderKeybindingSetting(parent, ScrollingCombatTextModule.ModuleInstance.ModuleSettings.GlobalDrawerVisibleHotkey);
            this.RenderBoolSetting(parent, ScrollingCombatTextModule.ModuleInstance.ModuleSettings.RegisterCornerIcon);
            this.RenderEnumSetting(parent, ScrollingCombatTextModule.ModuleInstance.ModuleSettings.CornerIconLeftClickAction);
            this.RenderEnumSetting(parent, ScrollingCombatTextModule.ModuleInstance.ModuleSettings.CornerIconRightClickAction);

            this.RenderEmptyLine(parent);

            this.RenderBoolSetting(parent, ScrollingCombatTextModule.ModuleInstance.ModuleSettings.HideOnMissingMumbleTicks);
            this.RenderBoolSetting(parent, ScrollingCombatTextModule.ModuleInstance.ModuleSettings.HideOnOpenMap);
            this.RenderBoolSetting(parent, ScrollingCombatTextModule.ModuleInstance.ModuleSettings.HideInPvE_OpenWorld);
            this.RenderBoolSetting(parent, ScrollingCombatTextModule.ModuleInstance.ModuleSettings.HideInPvE_Competetive);
            this.RenderBoolSetting(parent, ScrollingCombatTextModule.ModuleInstance.ModuleSettings.HideInWvW);
            this.RenderBoolSetting(parent, ScrollingCombatTextModule.ModuleInstance.ModuleSettings.HideInPvP);

            //this.RenderEmptyLine(parent);

            //this.RenderSetting(parent, TradingPostWatcherModule.ModuleInstance.ModuleSettings.BuildDirection);

        }

        protected override Task<bool> InternalLoad(IProgress<string> progress)
        {
            return Task.FromResult(true);
        }
    }
}
