namespace Estreya.BlishHUD.LiveMap.UI.Views;

using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.Shared.Service;
using Estreya.BlishHUD.Shared.UI.Views;
using Flurl.Util;
using Humanizer;
using MonoGame.Extended.BitmapFonts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class SettingsView : BaseSettingsView
{
    private readonly ModuleSettings _moduleSettings;
    private readonly Func<string> _getGlobalUrl;
    private readonly Func<string> _getGuildUrl;

    public SettingsView(Gw2ApiManager apiManager, IconService iconService, TranslationService translationService, SettingEventService settingEventService, ModuleSettings moduleSettings,  Func<string> getGlobalUrl, Func<string> getGuildUrl, BitmapFont font = null) : base(apiManager, iconService, translationService, settingEventService, font)
    {
        this._moduleSettings = moduleSettings;
        this._getGlobalUrl = getGlobalUrl;
        this._getGuildUrl = getGuildUrl;
    }

    protected override void BuildView(FlowPanel parent)
    {
        this.RenderBoolSetting(parent, _moduleSettings.HideCommander);
        this.RenderBoolSetting(parent, _moduleSettings.StreamerModeEnabled);
        this.RenderBoolSetting(parent, _moduleSettings.FollowOnMap);
        this.RenderBoolSetting(parent, _moduleSettings.SendGroupInformation);

        this.RenderEmptyLine(parent);

        this.RenderButton(parent, "Open Global Map", () =>
        {
            Process.Start(_getGlobalUrl());
        });

        this.RenderButton(parent, "Open Guild Map", () =>
        {
            Process.Start(_getGuildUrl());
        }, () => string.IsNullOrWhiteSpace(_getGuildUrl()));
    }

    protected override Task<bool> InternalLoad(IProgress<string> progress)
    {
        return Task.FromResult(true);
    }
}
