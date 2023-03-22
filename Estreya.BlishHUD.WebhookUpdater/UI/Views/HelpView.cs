namespace Estreya.BlishHUD.WebhookUpdater.UI.Views;

using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.Shared.Extensions;
using Estreya.BlishHUD.Shared.Helpers;
using Estreya.BlishHUD.Shared.State;
using Estreya.BlishHUD.Shared.UI.Views;
using Microsoft.Xna.Framework;
using MonoGame.Extended.BitmapFonts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

public class HelpView : BaseView
{
    private const string DISCORD_USERNAME = "Estreya#0001";
    private static Point PADDING = new Point(25, 25);

    public HelpView(Gw2ApiManager apiManager, IconState iconState, TranslationState translationState, BitmapFont font = null) : base(apiManager, iconState, translationState, font)
    {
    }

    protected override void InternalBuild(Panel parent)
    {
        var flowPanel = new FlowPanel()
        {
            Parent = parent,
            Width = parent.ContentRegion.Width - PADDING.X * 2,
            Height = parent.ContentRegion.Height - PADDING.Y * 2,
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
        var panel = new Panel()
        {
            Parent = parent,
            WidthSizingMode = SizingMode.AutoSize,
            HeightSizingMode = SizingMode.AutoSize,
            ShowBorder = true
        };

        var labelBuilder = this.GetLabelBuilder(parent)
            .CreatePart("Why can't my VS Code Installation be found?", builder => { builder.SetFontSize(Blish_HUD.ContentService.FontSize.Size20).MakeUnderlined(); })
            .CreatePart("\n \n", builder => { })
            .CreatePart($"- You have VS Code installed only for your user but run BlishHUD as admin.", builder => { })
            .CreatePart("\n", builder => { })
            .CreatePart("- You actually don't have VS Code installed.", builder => { });

        var label = labelBuilder.Build();
        label.Parent = panel;

        var showInstallPath = this.RenderButton(panel, "Show VS Code Path", () =>
        {
            var path = VSCodeHelper.GetExePath() ?? "No install accessible for current user found!";
            ScreenNotification.ShowNotification(path);
        });
        showInstallPath.Top = label.Bottom + 20;
    }

    private void BuildQuestionNotFoundSection(FlowPanel parent)
    {
        var panel = new Panel()
        {
            Parent = parent,
            WidthSizingMode = SizingMode.AutoSize,
            HeightSizingMode = SizingMode.AutoSize,
            ShowBorder = true
        };

        var labelBuilder = this.GetLabelBuilder(parent)
            .CreatePart("My question was not listed?", builder => { builder.SetFontSize(Blish_HUD.ContentService.FontSize.Size20).MakeUnderlined(); })
            .CreatePart("\n \n", builder => { })
            .CreatePart("Ping ", builder => { })
            .CreatePart(DISCORD_USERNAME, builder => { builder.MakeBold(); })
            .CreatePart(" on BlishHUD Discord.", builder => { });

        var label = labelBuilder.Build();
        label.Parent = panel;
    }

    private FormattedLabelBuilder GetLabelBuilder(Panel parent)
    {
        return new FormattedLabelBuilder().SetWidth(parent.ContentRegion.Width - PADDING.X * 2).AutoSizeHeight().SetVerticalAlignment(VerticalAlignment.Top);
    }

    protected override Task<bool> InternalLoad(IProgress<string> progress) => Task.FromResult(true);
}
