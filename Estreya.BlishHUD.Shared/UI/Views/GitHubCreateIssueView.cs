namespace Estreya.BlishHUD.Shared.UI.Views;

using Blish_HUD;
using Blish_HUD.Controls;
using Controls;
using Microsoft.Xna.Framework;
using MonoGame.Extended.BitmapFonts;
using Services;
using System;
using System.Threading.Tasks;
using Threading.Events;

public class GitHubCreateIssueView : BaseView
{
    private readonly string _message;
    private readonly string _moduleName;
    private readonly string _title;

    public GitHubCreateIssueView(string moduleName, IconService iconService, TranslationService translationService, BitmapFont font = null) : base(null, iconService, translationService, font)
    {
        this._moduleName = moduleName;
    }

    public GitHubCreateIssueView(string moduleName, IconService iconService, TranslationService translationService, BitmapFont font = null, string title = null, string message = null) : this(moduleName, iconService, translationService, font)
    {
        this._title = title;
        this._message = message;
    }

    public event AsyncEventHandler<(string Title, string Message, string DiscordName, bool IncludeSystemInformation)> CreateClicked;
    public event EventHandler CancelClicked;

    protected override void InternalBuild(Panel parent)
    {
        Rectangle contentRegion = parent.ContentRegion;
        parent.CanScroll = true;

        Label titleLabel = new Label
        {
            Parent = parent,
            Text = $"New Issue for Module {this._moduleName}",
            Width = contentRegion.Width,
            HorizontalAlignment = HorizontalAlignment.Center,
            Height = GameService.Content.DefaultFont32.LineHeight,
            Font = GameService.Content.DefaultFont32
        };

        Label issueTitleLabel = this.RenderLabel(parent, "Title").TitleLabel;
        issueTitleLabel.Top = titleLabel.Bottom + 50;

        TextBox issueTitleTextBox = this.RenderTextbox(parent,
            new Point(this.LABEL_WIDTH, issueTitleLabel.Top),
            parent.Width - this.LABEL_WIDTH,
            !string.IsNullOrWhiteSpace(this._title) ? this._title : $"[BUG/FEATURE] {this._moduleName} ....",
            "Issue Title");

        issueTitleTextBox.BasicTooltipText = "Should contain a clear title describing the feature or bug.";

        Label issueMessageLabel = this.RenderLabel(parent, "Issue").TitleLabel;
        issueMessageLabel.Top = issueTitleLabel.Bottom + 20;

        // TODO: Replace with TextArea
        TextBox issueMessageTextBox = this.RenderTextbox(parent,
            new Point(this.LABEL_WIDTH, issueMessageLabel.Top),
            parent.Width - this.LABEL_WIDTH,
            !string.IsNullOrWhiteSpace(this._message) ? this._message : string.Empty,
            "Issue Message");

        issueMessageTextBox.BasicTooltipText = "Describe your feature or bug here.";

        //issueMessageTextBox.Height = 400;

        Label discordNameLabel = this.RenderLabel(parent, "Discord Username (optional)").TitleLabel;
        discordNameLabel.Top = issueMessageLabel.Bottom + 20;

        TextBox discordNameTextBox = this.RenderTextbox(parent,
            new Point(this.LABEL_WIDTH, discordNameLabel.Top),
            parent.Width - this.LABEL_WIDTH,
            "",
            "Discord#0001");

        discordNameTextBox.BasicTooltipText = "If provided, its used to ask additional questions or notify you about the status.";

        Label includeSystemInformationLabel = this.RenderLabel(parent, "Include System Info").TitleLabel;
        includeSystemInformationLabel.Top = discordNameLabel.Bottom + 20;

        Checkbox includeSystemInformationCheckbox = this.RenderCheckbox(parent,
            new Point(this.LABEL_WIDTH, includeSystemInformationLabel.Top),
            false);

        includeSystemInformationCheckbox.BasicTooltipText = "If checked, additional system information will be included to assist looking into your issue.";

        Button cancelButton = this.RenderButton(parent, "Cancel", () => this.CancelClicked?.Invoke(this, EventArgs.Empty));
        cancelButton.Bottom = contentRegion.Bottom;
        cancelButton.Right = contentRegion.Right;

        Button createButton = this.RenderButtonAsync(parent, "Create", async () => await this.CreateClicked?.Invoke(this, (issueTitleTextBox.Text, issueMessageTextBox.Text, discordNameTextBox.Text, includeSystemInformationCheckbox.Checked)));
        createButton.Top = cancelButton.Top;
        createButton.Right = cancelButton.Left + 10;
    }

    protected override Task<bool> InternalLoad(IProgress<string> progress)
    {
        return Task.FromResult(true);
    }
}