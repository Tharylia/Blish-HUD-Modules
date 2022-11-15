namespace Estreya.BlishHUD.LiveMap.UI.Views;

using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.Shared.State;
using Estreya.BlishHUD.Shared.UI.Views;
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
    public SettingsView(Gw2ApiManager apiManager, IconState iconState, BitmapFont font = null) : base(apiManager, iconState, font)
    {
    }

    protected override void BuildView(Panel parent)
    {
        this.RenderEnumSetting(parent, LiveMapModule.Instance.ModuleSettings.PublishType);
        this.RenderEnumSetting(parent, LiveMapModule.Instance.ModuleSettings.PlayerFacingType);

        this.RenderEmptyLine(parent);

        this.RenderButton(parent, "Open Global Map", () =>
        {
            Process.Start(LiveMapModule.LIVE_MAP_GLOBAL_URL);
        });

        this.RenderButton(parent, "Open Guild Map", () =>
        {
            Process.Start(LiveMapModule.LIVE_MAP_GUILD_URL.FormatWith(LiveMapModule.Instance.GuildId));
        }, () => string.IsNullOrWhiteSpace(LiveMapModule.Instance.GuildId));
    }

    protected override Task<bool> InternalLoad(IProgress<string> progress)
    {
        return Task.FromResult(true);
    }
}
