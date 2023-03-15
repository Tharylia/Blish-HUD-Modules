namespace Estreya.BlishHUD.StatsGraph.UI.Views;

using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.Shared.Helpers;
using Estreya.BlishHUD.Shared.State;
using Estreya.BlishHUD.Shared.UI.Views;
using Estreya.BlishHUD.Shared.Utils;
using MonoGame.Extended.BitmapFonts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
        this.RenderBoolSetting(parent, _moduleSettings.ShowCategoryNames);
        this.RenderBoolSetting(parent, _moduleSettings.ShowAxisValues);

        this.RenderEmptyLine(parent);

        this.RenderFloatSetting(parent, _moduleSettings.Zoom);
        this.RenderFloatSetting(parent, _moduleSettings.Scale);

        this.RenderEmptyLine(parent);

        this.RenderIntSetting(parent, _moduleSettings.Size);
        this.RenderIntSetting(parent, _moduleSettings.LocationX);
        this.RenderIntSetting(parent, _moduleSettings.LocationY);
    }

    protected override Task<bool> InternalLoad(IProgress<string> progress) => Task.FromResult(true);
}
