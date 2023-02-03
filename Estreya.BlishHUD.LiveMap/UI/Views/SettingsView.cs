namespace Estreya.BlishHUD.LiveMap.UI.Views;

using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.Shared.State;
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
    private readonly Func<string> _getGuildId;
    private readonly Func<double> _getPosX;
    private readonly Func<double> _getPosY;
    private readonly Func<string> _getGlobalUrl;
    private readonly Func<string> _getGuildUrl;

    public SettingsView(Gw2ApiManager apiManager, IconState iconState, TranslationState translationState, ModuleSettings moduleSettings, Func<string> getGuildId, Func<double> getPosX, Func<double> getPosY, Func<string> getGlobalUrl, Func<string> getGuildUrl, BitmapFont font = null) : base(apiManager, iconState, translationState, font)
    {
        this._moduleSettings = moduleSettings;
        this._getGuildId = getGuildId;
        this._getPosX = getPosX;
        this._getPosY = getPosY;
        this._getGlobalUrl = getGlobalUrl;
        this._getGuildUrl = getGuildUrl;
    }

    protected override void BuildView(Panel parent)
    {
        this.RenderEnumSetting(parent, _moduleSettings.PublishType);
        this.RenderEnumSetting(parent, _moduleSettings.PlayerFacingType);
        this.RenderBoolSetting(parent, _moduleSettings.HideCommander);
        this.RenderBoolSetting(parent, _moduleSettings.StreamerModeEnabled);

        this.RenderEmptyLine(parent);

        this.RenderButton(parent, "Open Global Map", () =>
        {
            Process.Start(FormatUrlWithPositions(_getGlobalUrl()));
        });

        this.RenderButton(parent, "Open Guild Map", () =>
        {
            var url = _getGuildUrl().FormatWith(_getGuildId());
            Process.Start(FormatUrlWithPositions(url));
        }, () => string.IsNullOrWhiteSpace(_getGuildId()));
    }

    private string FormatUrlWithPositions(string url)
    {
        return $"{url}?posX={_getPosX().ToInvariantString()}&posY={_getPosY().ToInvariantString()}&zoom=6";
    }

    protected override Task<bool> InternalLoad(IProgress<string> progress)
    {
        return Task.FromResult(true);
    }
}
