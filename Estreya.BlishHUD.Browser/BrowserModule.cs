namespace Estreya.BlishHUD.Browser;

using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Modules;
using Blish_HUD.Settings;
using Estreya.BlishHUD.Browser.CEF;
using Estreya.BlishHUD.Browser.UI.Views;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Shared.Modules;
using Shared.Services;
using Shared.Settings;
using Shared.Threading;
using Shared.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TabbedWindow = Shared.Controls.TabbedWindow;

[Export(typeof(Module))]
public class BrowserModule : BaseModule<BrowserModule, ModuleSettings>
{
    private Shared.Controls.StandardWindow _window;

    [ImportingConstructor]
    public BrowserModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) { }

    public override string UrlModuleName => "browser";

    protected override string API_VERSION_NO => "1";

    protected override BaseModuleSettings DefineModuleSettings(SettingCollection settings)
    {
        return new ModuleSettings(settings);
    }

    protected override AsyncTexture2D GetCornerIcon()
    {
        return this.IconService.GetIcon("textures/webhook.png");
    }

    protected override string GetDirectoryName()
    {
        return "browser";
    }

    protected override AsyncTexture2D GetEmblem()
    {
        return this.IconService.GetIcon("textures/webhook.png");
    }

    protected override void Initialize()
    {
        base.Initialize();

        OffscreenBrowserRenderer.Init(this.GetCefBasePath(), Path.Combine(this.DirectoriesManager.GetFullDirectoryPath(this.GetDirectoryName()), "cache"));
    }

    protected override async Task LoadAsync()
    {
        await base.LoadAsync();

        this._window = WindowUtil.CreateStandardWindow(this.ModuleSettings, "Browser", this.GetType(), Guid.Parse("8d143453-67a8-467f-945b-6b06985b0150"), this.IconService, null);

        this._window.SavesSize = true;
        this._window.SavesPosition = true;
        this._window.CanResize = true;
        this._window.RebuildViewAfterResize = true;
        this._window.UnloadOnRebuild = false;

        this._window.Show(new BrowserView(() => "https://wiki.guildwars2.com/wiki/Event_timers", this.Gw2ApiManager, this.IconService, this.TranslationService));
    }

    private string GetCefBasePath()
    {
        var gw2Path = Path.GetDirectoryName(GameService.GameIntegration.Gw2Instance.Gw2ExecutablePath);

        if (gw2Path.Contains("WindowsApps"))
        {
            gw2Path = "C:\\Program Files\\Guild Wars 2";
        }

        return string.IsNullOrEmpty(gw2Path) ? string.Empty : Path.Combine(gw2Path, "bin64", "cef");
    }

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
    }

    protected override void OnSettingWindowBuild(TabbedWindow settingWindow)
    {
    }

    
    protected override void Unload()
    {
        base.Unload();
        OffscreenBrowserRenderer.Shutdown();
    }

    protected override int CornerIconPriority => 1_289_351_271;
}