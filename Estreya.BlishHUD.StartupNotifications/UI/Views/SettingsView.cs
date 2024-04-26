namespace Estreya.BlishHUD.StartupNotifications.UI.Views;

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
    private readonly ModuleSettings _moduleSettings;

    public SettingsView(Gw2ApiManager apiManager, IconService iconService, TranslationService translationService, SettingEventService settingEventService, ModuleSettings moduleSettings) : base(apiManager, iconService, translationService, settingEventService)
    {
        this._moduleSettings = moduleSettings;
    }

    protected override void BuildView(FlowPanel parent)
    {
        this.RenderIntSetting(parent, this._moduleSettings.Duration);
        this.RenderEnumSetting(parent, this._moduleSettings.Type);
        this.RenderBoolSetting(parent, this._moduleSettings.AwaitEach);

        this.RenderEmptyLine(parent);

        this.RenderFormattedLabel(parent, (b) =>
        {
            b.SetWidth(parent.ContentRegion.Width - (int)parent.OuterControlPadding.X * 3).SetHorizontalAlignment(HorizontalAlignment.Center).Wrap();

            b.CreatePart("To change the files, use the settings cog at the top right and open the assigned directory.", pb =>
            {
                pb.SetFontSize(Blish_HUD.ContentService.FontSize.Size20);
            });
        });
    }

    protected override Task<bool> InternalLoad(IProgress<string> progress)
    {
        return Task.FromResult(true);
    }
}