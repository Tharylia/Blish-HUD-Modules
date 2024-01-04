namespace Estreya.BlishHUD.ArcDPSLogManager.UI.Views;

using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Microsoft.Xna.Framework;
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
        this.RenderBoolSetting(parent, this._moduleSettings.RegisterCornerIcon);
        this.RenderEnumSetting(parent, this._moduleSettings.CornerIconLeftClickAction);
        this.RenderEnumSetting(parent, this._moduleSettings.CornerIconRightClickAction);

        this.RenderEmptyLine(parent);

        this.RenderBoolSetting(parent, this._moduleSettings.AnonymousPlayers);
        this.RenderBoolSetting(parent, this._moduleSettings.SkipFailedTries);
        this.RenderBoolSetting(parent, this._moduleSettings.ParsePhases);
        this.RenderBoolSetting(parent, this._moduleSettings.ParseCombatReplay);
        this.RenderBoolSetting(parent, this._moduleSettings.ComputeDamageModifiers);
        this.RenderBoolSetting(parent, this._moduleSettings.DetailedWvW);
        this.RenderIntSetting(parent, this._moduleSettings.TooShortLimit);

        this.RenderEmptyLine(parent);

        this.RenderBoolSetting(parent, this._moduleSettings.GenerateHTMLAfterParsing);
    }

    protected override Task<bool> InternalLoad(IProgress<string> progress)
    {
        return Task.FromResult(true);
    }
}