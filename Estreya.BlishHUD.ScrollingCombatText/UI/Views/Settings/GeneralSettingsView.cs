namespace Estreya.BlishHUD.ScrollingCombatText.UI.Views.Settings
{
    using Blish_HUD.Controls;
    using Blish_HUD.Modules.Managers;
    using Estreya.BlishHUD.ScrollingCombatText;
    using Estreya.BlishHUD.Shared.Services;
    using Estreya.BlishHUD.Shared.UI.Views;
    using MonoGame.Extended.BitmapFonts;
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;

    public class GeneralSettingsView : BaseSettingsView
    {
        private readonly ModuleSettings _moduleSettings;

        public GeneralSettingsView(ModuleSettings moduleSettings, Gw2ApiManager apiManager, IconService iconService, TranslationService translationService, SettingEventService settingEventService, BitmapFont font = null) : base(apiManager, iconService, translationService, settingEventService, font)
        {
            this._moduleSettings = moduleSettings;
        }

        protected override void BuildView(FlowPanel parent)
        {
            this.RenderBoolSetting(parent, this._moduleSettings.GlobalDrawerVisible);
            this.RenderKeybindingSetting(parent, this._moduleSettings.GlobalDrawerVisibleHotkey);
            this.RenderBoolSetting(parent, this._moduleSettings.RegisterCornerIcon);
            this.RenderEnumSetting(parent, this._moduleSettings.CornerIconLeftClickAction);
            this.RenderEnumSetting(parent, this._moduleSettings.CornerIconRightClickAction);

            this.RenderEmptyLine(parent);

            this.RenderBoolSetting(parent, this._moduleSettings.HideOnMissingMumbleTicks);
            this.RenderBoolSetting(parent, this._moduleSettings.HideOnOpenMap);
            this.RenderBoolSetting(parent, this._moduleSettings.HideInPvE_OpenWorld);
            this.RenderBoolSetting(parent, this._moduleSettings.HideInPvE_Competetive);
            this.RenderBoolSetting(parent, this._moduleSettings.HideInWvW);
            this.RenderBoolSetting(parent, this._moduleSettings.HideInPvP);
        }

        protected override Task<bool> InternalLoad(IProgress<string> progress) => Task.FromResult(true);
    }
}
