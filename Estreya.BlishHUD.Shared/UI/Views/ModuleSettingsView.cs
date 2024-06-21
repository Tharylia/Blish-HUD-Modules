namespace Estreya.BlishHUD.Shared.UI.Views;

using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using Services;
using System;
using System.Threading.Tasks;

public class ModuleSettingsView : BaseView
{
    public ModuleSettingsView(IconService iconService, TranslationService translationService) : base(null, iconService, translationService)
    {
    }

    public event EventHandler OpenClicked;
    public event EventHandler CreateGithubIssueClicked;
    public event EventHandler OpenMessageLogClicked;

    protected override Task<bool> InternalLoad(IProgress<string> progress)
    {
        return Task.FromResult(true);
    }

    protected override void InternalBuild(Panel parent)
    {
        Rectangle bounds = parent.ContentRegion;

        FlowPanel parentPanel = new FlowPanel
        {
            Size = bounds.Size,
            FlowDirection = ControlFlowDirection.SingleTopToBottom,
            ControlPadding = new Vector2(5, 2),
            OuterControlPadding = new Vector2(10, 15),
            WidthSizingMode = SizingMode.Fill,
            HeightSizingMode = SizingMode.AutoSize,
            AutoSizePadding = new Point(0, 15),
            Parent = parent
        };

        ViewContainer settingContainer = new ViewContainer
        {
            WidthSizingMode = SizingMode.Fill,
            HeightSizingMode = SizingMode.AutoSize,
            Parent = parentPanel
        };

        string buttonText = this.TranslationService.GetTranslation("moduleSettingsView-openSettingsBtn", "Open Settings");

        StandardButton openSettingsButton = new StandardButton
        {
            Parent = settingContainer,
            Text = buttonText
        };

        var font = this.ControlFonts[Models.ControlType.Button];

        if (font != null)
        {
            openSettingsButton.Width = (int)font.MeasureString(buttonText).Width;
        }

        openSettingsButton.Location = new Point(Math.Max((parentPanel.Width / 2) - (openSettingsButton.Width / 2), 20), Math.Max((parentPanel.Height / 3) - openSettingsButton.Height, 20));

        openSettingsButton.Click += (s, e) => this.OpenClicked?.Invoke(this, EventArgs.Empty);

        string githubIssueText = this.TranslationService.GetTranslation("moduleSettingsView-createGitHubIssueBtn", "Create Bug/Feature Issue");

        StandardButton createGithubIssue = new StandardButton
        {
            Parent = settingContainer,
            Text = githubIssueText
        };

        if (font != null)
        {
            createGithubIssue.Width = (int)font.MeasureString(githubIssueText).Width;
        }

        createGithubIssue.Location = new Point(Math.Max((parentPanel.Width / 2) - (createGithubIssue.Width / 2), 20), openSettingsButton.Bottom + 10);

        createGithubIssue.Click += (s, e) => this.CreateGithubIssueClicked?.Invoke(this, EventArgs.Empty);

        string openMessageLogText = this.TranslationService.GetTranslation("moduleSettingsView-openMessageLogBtn", "Open Message Log");

        StandardButton openMessageLog = new StandardButton
        {
            Parent = settingContainer,
            Text = openMessageLogText,
        };

        if (font != null)
        {
            openMessageLog.Width = (int)font.MeasureString(openMessageLogText).Width;
        }

        openMessageLog.Location = new Point(Math.Max((parentPanel.Width / 2) - (openMessageLog.Width / 2), 20), createGithubIssue.Bottom + 10);

        openMessageLog.Click += (s, e) => this.OpenMessageLogClicked?.Invoke(this, EventArgs.Empty);
    }
}