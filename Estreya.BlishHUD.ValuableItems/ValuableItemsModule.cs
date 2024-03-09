namespace Estreya.BlishHUD.ValuableItems;

using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Graphics.UI;
using Blish_HUD.Modules;
using Blish_HUD.Settings;
using Flurl.Util;
using Gw2Sharp.WebApi.V2;
using Microsoft.Xna.Framework;
using Shared.Extensions;
using Shared.Helpers;
using Shared.Modules;
using Shared.Settings;
using Shared.Threading;
using Shared.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Tesseract;
using Estreya.BlishHUD.Shared.Controls;
using Estreya.BlishHUD.ValuableItems.UI.Views;

[Export(typeof(Module))]
public class ValuableItemsModule : BaseModule<ValuableItemsModule, ModuleSettings>
{
    [ImportingConstructor]
    public ValuableItemsModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) { }

    protected override string UrlModuleName => "valuable-items";

    protected override string API_VERSION_NO => "1";

    protected override bool NeedsBackend => false;

    private TesseractEngine _tesseractEngine;

    protected override void Initialize()
    {
        base.Initialize();
    }

    protected override async Task LoadAsync()
    {
        await base.LoadAsync();
        var tesseractFolderPath = await this.ExtractTesseract();

        TesseractEnviornment.CustomSearchPath = tesseractFolderPath;
        this._tesseractEngine = new TesseractEngine(Path.Combine(tesseractFolderPath, "tessdata"), "eng", EngineMode.TesseractAndLstm);
    }

    private async Task<string> ExtractTesseract()
    {
        var directoryPath = this.DirectoriesManager.GetFullDirectoryPath(this.GetDirectoryName());
        var runtimePath = Path.Combine(directoryPath, "runtime");
        if (Directory.Exists(runtimePath))
        {
            Directory.Delete(runtimePath, true);
        }
        Directory.CreateDirectory(runtimePath);

        var runtimeZipPath = Path.Combine(runtimePath, "runtime.zip");
        using var runtimeZipStream = this.ContentsManager.GetFileStream("runtime.zip");
        await FileUtil.WriteBytesAsync(runtimeZipPath, runtimeZipStream.ToByteArray());

        System.IO.Compression.ZipFile.ExtractToDirectory(runtimeZipPath, runtimePath);
        File.Delete(runtimeZipPath);

        return runtimePath;
    }

    private System.Drawing.Bitmap TakeScreenshot(Rectangle rect)
    {
        var bmp = new System.Drawing.Bitmap(rect.Width, rect.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bmp);
        g.CopyFromScreen(rect.Left, rect.Top, 0, 0, new System.Drawing.Size(rect.Width, rect.Height), System.Drawing.CopyPixelOperation.SourceCopy);
        return bmp;
    }


    protected override void Update(GameTime gameTime)
    {
    }

    /// <inheritdoc />
    protected override void Unload()
    {
        base.Unload();
        this._tesseractEngine?.Dispose();
        this._tesseractEngine = null;
    }

    protected override void OnSettingWindowBuild(TabbedWindow settingWindow)
    {
        settingWindow.Tabs.Add(
            new Blish_HUD.Controls.Tab(
            this.IconService.GetIcon("155052.png"),
            () => new OCRDebugView(this._tesseractEngine,this.ModuleSettings, this.Gw2ApiManager, this.IconService, this.TranslationService),
            "OCR Debug")
        );
    }



    public override IView GetSettingsView()
    {
        return new Shared.UI.Views.ModuleSettingsView(this.IconService, this.TranslationService);
    }

    protected override BaseModuleSettings DefineModuleSettings(SettingCollection settings)
    {
        return new ModuleSettings(settings);
    }

    protected override string GetDirectoryName()
    {
        return "valuable_items";
    }

    protected override AsyncTexture2D GetEmblem()
    {
        return null;
    }

    protected override AsyncTexture2D GetCornerIcon()
    {
        return null;
    }

    protected override int CornerIconPriority => 1_289_351_270;
}