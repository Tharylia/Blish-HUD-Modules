namespace Estreya.BlishHUD.EventTable.UI.Views
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
        private readonly ModuleSettings _moduleSettings;

        public GeneralSettingsView(ModuleSettings moduleSettings, Gw2ApiManager apiManager, IconState iconState, TranslationState translationState, SettingEventState settingEventState, BitmapFont font = null) : base(apiManager, iconState, translationState, settingEventState, font)
        {
            this._moduleSettings = moduleSettings;
        }

        protected override void BuildView(FlowPanel parent)
        {
            this.RenderBoolSetting(parent, _moduleSettings.GlobalDrawerVisible);
            this.RenderKeybindingSetting(parent, _moduleSettings.GlobalDrawerVisibleHotkey);
            this.RenderBoolSetting(parent, _moduleSettings.RegisterCornerIcon);
            this.RenderEnumSetting(parent, _moduleSettings.CornerIconLeftClickAction);
            this.RenderEnumSetting(parent, _moduleSettings.CornerIconRightClickAction);

            this.RenderEmptyLine(parent);

            this.RenderKeybindingSetting(parent, _moduleSettings.MapKeybinding);

            this.RenderBoolSetting(parent, _moduleSettings.HideOnMissingMumbleTicks);
            this.RenderBoolSetting(parent, _moduleSettings.HideOnOpenMap);
            this.RenderBoolSetting(parent, _moduleSettings.HideInCombat);
            this.RenderBoolSetting(parent, _moduleSettings.HideInPvE_OpenWorld);
            this.RenderBoolSetting(parent, _moduleSettings.HideInPvE_Competetive);
            this.RenderBoolSetting(parent, _moduleSettings.HideInWvW);
            this.RenderBoolSetting(parent, _moduleSettings.HideInPvP);

            this.RenderEmptyLine(parent);

            this.RenderEnumSetting(parent, _moduleSettings.MenuEventSortMenu);

            //this.RenderEmptyLine(parent);

            //this.RenderSetting(parent, TradingPostWatcherModule.ModuleInstance.BuildDirection);

        }

        protected override Task<bool> InternalLoad(IProgress<string> progress)
        {
            return Task.FromResult(true);
        }
    }
}
