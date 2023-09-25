namespace Estreya.BlishHUD.ValuableItems.UI.Views;

using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.Shared.Extensions;
using Estreya.BlishHUD.Shared.Services;
using Estreya.BlishHUD.Shared.UI.Views;
using Estreya.BlishHUD.ValuableItems.Controls;
using Estreya.BlishHUD.ValuableItems.Extensions;
using Estreya.BlishHUD.ValuableItems.Utils;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tesseract;

public class OCRDebugView : BaseView
{
    private readonly TesseractEngine _tesseractEngine;
    private readonly ModuleSettings _moduleSettings;

    public OCRDebugView(TesseractEngine tesseractEngine, ModuleSettings moduleSettings, Gw2ApiManager apiManager, IconService iconService, TranslationService translationService) : base(apiManager, iconService, translationService)
    {
        this._tesseractEngine = tesseractEngine;
        this._moduleSettings = moduleSettings;
    }

    protected override void InternalBuild(Panel parent)
    {
        FlowPanel flowPanel = new FlowPanel()
        {
            Parent = parent,
            Size = parent.ContentRegion.Size,
            FlowDirection = ControlFlowDirection.SingleTopToBottom
        };

        this.RenderButton(flowPanel, "Set Region", () =>
        {
            OCRSelection selection = new OCRSelection();
            selection.Size = this._moduleSettings.OCRRegion.Value == Rectangle.Empty ? new Point(100,100): this._moduleSettings.OCRRegion.Value.Size.UiToScale();
            selection.Location = this._moduleSettings.OCRRegion.Value.Location.UiToScale();
            selection.SelectionConfirmed += this.Selection_SelectionConfirmed;
            selection.SelectionCanceled += this.Selection_SelectionCanceled;
            selection.Show();
        });

        this.RenderButton(flowPanel, "OCR Screen", () =>
        {
            var fullScreenRect = OCRUtils.GetBlishWindowRectangle();
            var offset =new Point( Math.Abs(fullScreenRect.Location.X) - GameService.Graphics.Resolution.X, Math.Abs(fullScreenRect.Location.Y) - GameService.Graphics.Resolution.Y);
            
            var rect = this._moduleSettings.OCRRegion.Value .OffsetBy(fullScreenRect.Location).OffsetBy(offset);
            using var page = this._tesseractEngine.ProcessScreenRegion(rect);

            this._logger.Debug($"OCR Result:\n{page.GetText()}");
        });
    }

    private void Selection_SelectionCanceled(object sender, EventArgs e)
    {
        var selection = sender as OCRSelection;
        selection?.Dispose();
    }

    private void Selection_SelectionConfirmed(object sender, EventArgs e)
    {
        var selection = sender as OCRSelection;
        this._moduleSettings.OCRRegion.Value = new Rectangle(selection.Location.ScaleToUi(), selection.Size.ScaleToUi());
        selection?.Dispose();
    }

    protected override Task<bool> InternalLoad(IProgress<string> progress)
    {
        return Task.FromResult(true);
    }
}
