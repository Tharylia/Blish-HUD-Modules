namespace Estreya.BlishHUD.UniversalSearch.UI.Views.Settings
{
    using Blish_HUD.Controls;
    using Blish_HUD.Modules.Managers;
    using Estreya.BlishHUD.Shared.Services;
    using Estreya.BlishHUD.Shared.Threading.Events;
    using Estreya.BlishHUD.Shared.UI.Views;
    using MonoGame.Extended.BitmapFonts;
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;

    public class GeneralSettingsView : BaseSettingsView
    {
        private readonly ModuleSettings _moduleSettings;

        public event AsyncEventHandler ReloadServicesRequested;

        public GeneralSettingsView(ModuleSettings moduleSettings, Gw2ApiManager apiManager, IconService iconState, TranslationService translationState, SettingEventService settingEventState, BitmapFont font = null) : base(apiManager, iconState, translationState, settingEventState, font)
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

            this.RenderBoolSetting(parent, _moduleSettings.HideOnMissingMumbleTicks);
            this.RenderBoolSetting(parent, _moduleSettings.HideOnOpenMap);
            this.RenderBoolSetting(parent, _moduleSettings.HideInPvE_OpenWorld);
            this.RenderBoolSetting(parent, _moduleSettings.HideInPvE_Competetive);
            this.RenderBoolSetting(parent, _moduleSettings.HideInWvW);
            this.RenderBoolSetting(parent, _moduleSettings.HideInPvP);

            this.RenderEmptyLine(parent);

            //this.RenderButtonAsync(parent, "Reload Services", async () => await (this.ReloadServicesRequested?.Invoke(this) ?? Task.CompletedTask));

            //this.RenderEmptyLine(parent);

            //this.RenderSetting(parent, TradingPostWatcherModule.ModuleInstance.ModuleSettings.BuildDirection);

        }

        protected override Task<bool> InternalLoad(IProgress<string> progress)
        {
            return Task.FromResult(true);
        }
    }
}
