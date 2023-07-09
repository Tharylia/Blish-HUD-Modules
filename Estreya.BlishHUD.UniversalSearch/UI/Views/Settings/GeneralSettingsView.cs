namespace Estreya.BlishHUD.UniversalSearch.UI.Views.Settings;

using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using MonoGame.Extended.BitmapFonts;
using Shared.Services;
using Shared.Threading.Events;
using Shared.UI.Views;
using System;
using System.Threading.Tasks;

public class GeneralSettingsView : BaseSettingsView
{
    private readonly ModuleSettings _moduleSettings;

    public GeneralSettingsView(ModuleSettings moduleSettings, Gw2ApiManager apiManager, IconService iconState, TranslationService translationState, SettingEventService settingEventState, BitmapFont font = null) : base(apiManager, iconState, translationState, settingEventState, font)
    {
        this._moduleSettings = moduleSettings;
    }

    public event AsyncEventHandler ReloadServicesRequested;

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