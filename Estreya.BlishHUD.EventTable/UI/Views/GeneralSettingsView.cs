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
        public GeneralSettingsView(Gw2ApiManager apiManager, IconState iconState, TranslationState translationState, BitmapFont font = null) : base(apiManager, iconState, translationState, font)
        {
        }

        protected override void BuildView(Panel parent)
        {
            this.RenderBoolSetting(parent, EventTableModule.ModuleInstance.ModuleSettings.GlobalDrawerVisible);
            this.RenderKeybindingSetting(parent, EventTableModule.ModuleInstance.ModuleSettings.GlobalDrawerVisibleHotkey);
            this.RenderBoolSetting(parent, EventTableModule.ModuleInstance.ModuleSettings.RegisterCornerIcon);

            this.RenderEmptyLine(parent);

            this.RenderBoolSetting(parent, EventTableModule.ModuleInstance.ModuleSettings.AutomaticallyUpdateEventFile);
            this.RenderKeybindingSetting(parent, EventTableModule.ModuleInstance.ModuleSettings.MapKeybinding);
            this.RenderBoolSetting(parent, EventTableModule.ModuleInstance.ModuleSettings.AutomaticallyUpdateEventFile);

            this.RenderBoolSetting(parent, EventTableModule.ModuleInstance.ModuleSettings.HideOnMissingMumbleTicks);
            this.RenderBoolSetting(parent, EventTableModule.ModuleInstance.ModuleSettings.HideOnOpenMap);
            this.RenderBoolSetting(parent, EventTableModule.ModuleInstance.ModuleSettings.HideInPvE_OpenWorld);
            this.RenderBoolSetting(parent, EventTableModule.ModuleInstance.ModuleSettings.HideInPvE_Competetive);
            this.RenderBoolSetting(parent, EventTableModule.ModuleInstance.ModuleSettings.HideInWvW);
            this.RenderBoolSetting(parent, EventTableModule.ModuleInstance.ModuleSettings.HideInPvP);

            //this.RenderEmptyLine(parent);

            //this.RenderSetting(parent, TradingPostWatcherModule.ModuleInstance.ModuleSettings.BuildDirection);

        }

        protected override Task<bool> InternalLoad(IProgress<string> progress)
        {
            return Task.FromResult(true);
        }
    }
}
