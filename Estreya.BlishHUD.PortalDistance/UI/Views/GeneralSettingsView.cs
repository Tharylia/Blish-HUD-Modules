namespace Estreya.BlishHUD.PortalDistance.UI.Views;

using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using MonoGame.Extended.BitmapFonts;
using Shared.Services;
using Shared.UI.Views;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Media.Animation;

public class GeneralSettingsView : BaseSettingsView
{
    private readonly ModuleSettings _moduleSettings;

    public GeneralSettingsView(Gw2ApiManager apiManager, IconService iconService, TranslationService translationService, SettingEventService settingEventService, ModuleSettings moduleSettings) : base(apiManager, iconService, translationService, settingEventService)
    {
        this._moduleSettings = moduleSettings;
    }

    protected override void BuildView(FlowPanel parent)
    {
        this.RenderBoolSetting(parent, this._moduleSettings.RegisterCornerIcon);

        this.RenderEmptyLine(parent);

        this.RenderKeybindingSetting(parent, this._moduleSettings.ManualKeyBinding);

        this.RenderEmptyLine(parent, 10);

        var lbl = new FormattedLabelBuilder().AutoSizeHeight().SetWidth(parent.ContentRegion.Width)
            .CreatePart("This should not be the same key as your portal in-game keybind. It will prevent you from pressing it.", builder => { })
            .CreatePart(" \n ", b => { })
            .CreatePart("Allowing the same key would result in desyncs over time as tracking is not 100% accurate.", b => { })
            .Build();
        lbl.Parent = parent;

        this.RenderEmptyLine(parent);

        this.RenderBoolSetting(parent, this._moduleSettings.UseArcDPS);
    }

    protected override Task<bool> InternalLoad(IProgress<string> progress)
    {
        return Task.FromResult(true);
    }
}