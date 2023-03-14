namespace Estreya.BlishHUD.WebhookUpdater.UI.Views;

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
        this.RenderEnumSetting(parent, _moduleSettings.UpdateMode);
        this.RenderIntSetting(parent, _moduleSettings.UpdateInterval);
        this.RenderBoolSetting(parent, _moduleSettings.UpdateOnlyOnUrlOrDataChange);

        this.RenderEmptyLine(parent);

        var textBox = this.RenderTextSetting(parent, _moduleSettings.WebhookUrl).textBox;
        textBox.Width = parent.ContentRegion.Width - textBox.Left- 100;

        this.RenderButtonAsync(parent, "Edit Content", async () =>
        {
            var tempFile = FileUtil.CreateTempFile("handlebars");
            await FileUtil.WriteStringAsync(tempFile, _moduleSettings.WebhookStringContent.Value);

            await VSCodeHelper.EditAsync(tempFile);

            _moduleSettings.WebhookStringContent.Value = await FileUtil.ReadStringAsync(tempFile);
            File.Delete(tempFile);
        });
        
    }

    protected override Task<bool> InternalLoad(IProgress<string> progress) => Task.FromResult(true);
}
