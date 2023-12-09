namespace Estreya.BlishHUD.EventTable.UI.Views;

using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Microsoft.Xna.Framework;
using MonoGame.Extended.BitmapFonts;
using Shared.Helpers;
using Shared.Services;
using Shared.UI.Views;
using System;
using System.Threading.Tasks;

public class CustomEventView : BlishHUDAPILoginView
{
    public CustomEventView(Gw2ApiManager apiManager, IconService iconService, TranslationService translationService, BlishHudApiService blishHudApiService) : base(apiManager, iconService, translationService, blishHudApiService)
    {

    }

    protected override void OnBeforeBuildLoginSection(FlowPanel parent)
    {
        this.BuildInstructionSection(parent);
    }

    private void BuildInstructionSection(FlowPanel parent)
    {
        FlowPanel instructionPanel = new FlowPanel
        {
            Parent = parent,
            OuterControlPadding = new Vector2(20, 20),
            WidthSizingMode = SizingMode.AutoSize,
            HeightSizingMode = SizingMode.AutoSize,
            FlowDirection = ControlFlowDirection.SingleTopToBottom,
            ShowBorder = true
        };

        FormattedLabelBuilder labelBuilder = this.GetLabelBuilder(parent)
                                                 .CreatePart(this.TranslationService.GetTranslation("customEventView-manual1", "1. Make an account at") + " ", builder => { builder.SetFontSize(ContentService.FontSize.Size20); })
                                                 .CreatePart(this.TranslationService.GetTranslation("customEventView-manual2", "Estreya BlishHUD."), builder => { 
                                                     builder.SetFontSize(ContentService.FontSize.Size20).SetTextColor(Color.CornflowerBlue).SetHyperLink("https://blish-hud.estreya.de/register"); 
                                                 })
                                                 .CreatePart("\n \n", builder => { })
                                                 .CreatePart(this.TranslationService.GetTranslation("customEventView-manual3", "2. Follow steps send by mail."), builder => { builder.SetFontSize(ContentService.FontSize.Size20); })
                                                 .CreatePart("\n \n", builder => { })
                                                 .CreatePart(this.TranslationService.GetTranslation("customEventView-manual4", "3. Add your own custom events."), builder => { builder.SetFontSize(ContentService.FontSize.Size20); })
                                                 .CreatePart("\n \n", builder => { })
                                                 .CreatePart(this.TranslationService.GetTranslation("customEventView-manual5", "4. Enter login details below."), builder => { builder.SetFontSize(ContentService.FontSize.Size20); });

        FormattedLabel label = labelBuilder.Build();
        label.Parent = instructionPanel;

        this.RenderEmptyLine(instructionPanel, 20);
    }

    private FormattedLabelBuilder GetLabelBuilder(Panel parent)
    {
        return new FormattedLabelBuilder().SetWidth(parent.ContentRegion.Width - (PADDING.X * 2)).AutoSizeHeight().SetVerticalAlignment(VerticalAlignment.Top);
    }

    protected override Task<bool> InternalLoad(IProgress<string> progress)
    {
        return Task.FromResult(true);
    }
}