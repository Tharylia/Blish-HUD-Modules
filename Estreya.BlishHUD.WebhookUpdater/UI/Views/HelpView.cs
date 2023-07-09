namespace Estreya.BlishHUD.WebhookUpdater.UI.Views;

using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Microsoft.Xna.Framework;
using MonoGame.Extended.BitmapFonts;
using Shared.Controls;
using Shared.Helpers;
using Shared.Services;
using Shared.UI.Views;
using System;
using System.Threading.Tasks;
using ScreenNotification = Blish_HUD.Controls.ScreenNotification;

public class HelpView : BaseView
{
    private const string DISCORD_USERNAME = "Estreya#0001";
    private static readonly Point PADDING = new Point(25, 25);

    public HelpView(Gw2ApiManager apiManager, IconService iconService, TranslationService translationService, BitmapFont font = null) : base(apiManager, iconService, translationService, font)
    {
    }

    protected override void InternalBuild(Panel parent)
    {
        FlowPanel flowPanel = new FlowPanel
        {
            Parent = parent,
            Width = parent.ContentRegion.Width - (PADDING.X * 2),
            Height = parent.ContentRegion.Height - (PADDING.Y * 2),
            Location = new Point(PADDING.X, PADDING.Y),
            CanScroll = true,
            FlowDirection = ControlFlowDirection.SingleTopToBottom
        };

        this.BuildVSCodeNotFoundSection(flowPanel);

        this.RenderEmptyLine(flowPanel);
        this.BuildQuestionNotFoundSection(flowPanel);
    }

    private void BuildVSCodeNotFoundSection(FlowPanel parent)
    {
        Panel panel = new Panel
        {
            Parent = parent,
            WidthSizingMode = SizingMode.AutoSize,
            HeightSizingMode = SizingMode.AutoSize,
            ShowBorder = true
        };

        FormattedLabelBuilder labelBuilder = this.GetLabelBuilder(parent)
                                                 .CreatePart("Why can't my VS Code Installation be found?", builder => { builder.SetFontSize(ContentService.FontSize.Size20).MakeUnderlined(); })
                                                 .CreatePart("\n \n", builder => { })
                                                 .CreatePart("- You have VS Code installed only for your user but run BlishHUD as admin.", builder => { })
                                                 .CreatePart("\n", builder => { })
                                                 .CreatePart("- You actually don't have VS Code installed.", builder => { });

        FormattedLabel label = labelBuilder.Build();
        label.Parent = panel;

        Button showInstallPath = this.RenderButton(panel, "Show VS Code Path", () =>
        {
            string path = VSCodeHelper.GetExePath() ?? "No install accessible for current user found!";
            ScreenNotification.ShowNotification(path);
        });
        showInstallPath.Top = label.Bottom + 20;
    }

    private void BuildQuestionNotFoundSection(FlowPanel parent)
    {
        Panel panel = new Panel
        {
            Parent = parent,
            WidthSizingMode = SizingMode.AutoSize,
            HeightSizingMode = SizingMode.AutoSize,
            ShowBorder = true
        };

        FormattedLabelBuilder labelBuilder = this.GetLabelBuilder(parent)
                                                 .CreatePart("My question was not listed?", builder => { builder.SetFontSize(ContentService.FontSize.Size20).MakeUnderlined(); })
                                                 .CreatePart("\n \n", builder => { })
                                                 .CreatePart("Ping ", builder => { })
                                                 .CreatePart(DISCORD_USERNAME, builder => { builder.MakeBold(); })
                                                 .CreatePart(" on BlishHUD Discord.", builder => { });

        FormattedLabel label = labelBuilder.Build();
        label.Parent = panel;
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