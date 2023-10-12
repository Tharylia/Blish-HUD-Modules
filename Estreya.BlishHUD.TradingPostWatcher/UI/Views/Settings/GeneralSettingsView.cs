namespace Estreya.BlishHUD.TradingPostWatcher.UI.Views.Settings;

using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using MonoGame.Extended.BitmapFonts;
using Shared.Services;
using Shared.UI.Views;
using System;
using System.Threading.Tasks;

public class GeneralSettingsView : BaseSettingsView
{
    private readonly ModuleSettings _moduleSettings;

    public GeneralSettingsView(ModuleSettings moduleSettings, Gw2ApiManager apiManager, IconService iconService, TranslationService translationService, SettingEventService settingEventService) : base(apiManager, iconService, translationService, settingEventService)
    {
        this._moduleSettings = moduleSettings;
    }

    protected override void BuildView(FlowPanel parent)
    {
        this.RenderBoolSetting(parent, this._moduleSettings.GlobalDrawerVisible);
        this.RenderKeybindingSetting(parent, this._moduleSettings.GlobalDrawerVisibleHotkey);
        this.RenderBoolSetting(parent, this._moduleSettings.RegisterCornerIcon);

        this.RenderEmptyLine(parent);

        this.RenderBoolSetting(parent, this._moduleSettings.HideOnMissingMumbleTicks);
        this.RenderBoolSetting(parent, this._moduleSettings.HideOnOpenMap);
        this.RenderBoolSetting(parent, this._moduleSettings.HideInCombat);
        this.RenderBoolSetting(parent, this._moduleSettings.HideInWvW);
        this.RenderBoolSetting(parent, this._moduleSettings.HideInPvP);
        this.RenderBoolSetting(parent, this._moduleSettings.HideInPvE_OpenWorld);
        this.RenderBoolSetting(parent, this._moduleSettings.HideInPvE_Competetive);

        //this.RenderEmptyLine(parent);

        //this.RenderSetting(parent, this._moduleSettings.BuildDirection);
    }

    protected override Task<bool> InternalLoad(IProgress<string> progress)
    {
        return Task.FromResult(true);
    }
}