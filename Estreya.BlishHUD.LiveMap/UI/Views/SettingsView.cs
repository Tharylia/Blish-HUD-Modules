namespace Estreya.BlishHUD.LiveMap.UI.Views;

using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using MonoGame.Extended.BitmapFonts;
using Shared.Services;
using Shared.UI.Views;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

public class SettingsView : BaseSettingsView
{
    private readonly Func<string> _getGlobalUrl;
    private readonly Func<string> _getGuildUrl;
    private readonly ModuleSettings _moduleSettings;

    public SettingsView(Gw2ApiManager apiManager, IconService iconService, TranslationService translationService, SettingEventService settingEventService, ModuleSettings moduleSettings, Func<string> getGlobalUrl, Func<string> getGuildUrl) : base(apiManager, iconService, translationService, settingEventService)
    {
        this._moduleSettings = moduleSettings;
        this._getGlobalUrl = getGlobalUrl;
        this._getGuildUrl = getGuildUrl;
    }

    protected override void BuildView(FlowPanel parent)
    {
        this.RenderBoolSetting(parent, this._moduleSettings.HideCommander);
        this.RenderBoolSetting(parent, this._moduleSettings.StreamerModeEnabled);
        this.RenderBoolSetting(parent, this._moduleSettings.FollowOnMap);
        this.RenderBoolSetting(parent, this._moduleSettings.SendGroupInformation);

        this.RenderEmptyLine(parent);

        this.RenderButton(parent, "Open Global Map", () =>
        {
            Process.Start(this._getGlobalUrl());
        });

        this.RenderButton(parent, "Open Guild Map", () =>
        {
            Process.Start(this._getGuildUrl());
        }, () => string.IsNullOrWhiteSpace(this._getGuildUrl()));
    }

    protected override Task<bool> InternalLoad(IProgress<string> progress)
    {
        return Task.FromResult(true);
    }
}