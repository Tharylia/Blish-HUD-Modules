﻿namespace Estreya.BlishHUD.Shared.UI.Views;

using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Graphics.UI;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.Shared.State;
using Estreya.BlishHUD.Shared.Threading.Events;
using MonoGame.Extended.BitmapFonts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Animation;

public class GitHubCreateIssueView : BaseView
{
    private readonly string _moduleName;

    public event AsyncEventHandler<(string Title, string Message, string DiscordName, bool IncludeSystemInformation)> CreateClicked;
    public event EventHandler CancelClicked;

    public GitHubCreateIssueView(string moduleName, IconState iconState, BitmapFont font = null) : base(null,iconState, font)
    {
        this._moduleName = moduleName;
    }

    protected override void InternalBuild(Panel parent)
    {
        var contentRegion = parent.ContentRegion;
        parent.CanScroll = true;

        var titleLabel = new Label()
        {
            Parent = parent,
            Text = $"New Issue for Module {this._moduleName}",
            Width = contentRegion.Width,
            HorizontalAlignment = HorizontalAlignment.Center,
            Height = GameService.Content.DefaultFont32.LineHeight,
            Font = GameService.Content.DefaultFont32
        };

        var issueTitleLabel = this.RenderLabel(parent, "Title").TitleLabel;
        issueTitleLabel.Top = titleLabel.Bottom + 50;

        var issueTitleTextBox = this.RenderTextbox(parent, 
            new Microsoft.Xna.Framework.Point(this.LABEL_WIDTH, issueTitleLabel.Top), 
            parent.Width - this.LABEL_WIDTH, 
            $"[BUG/FEATURE] {this._moduleName} ....", 
            "Issue Title");

        issueTitleTextBox.BasicTooltipText = "Should contain a clear title describing the feature or bug.";

        var issueMessageLabel = this.RenderLabel(parent, "Issue").TitleLabel;
        issueMessageLabel.Top = issueTitleLabel.Bottom + 20;

        // TODO: Replace with TextArea
        var issueMessageTextBox = this.RenderTextbox(parent,
            new Microsoft.Xna.Framework.Point(this.LABEL_WIDTH, issueMessageLabel.Top),
            parent.Width - this.LABEL_WIDTH,
            $"",
            "Issue Message");

        issueMessageTextBox.BasicTooltipText = "Describe your feature or bug here.";

        //issueMessageTextBox.Height = 400;

        var discordNameLabel = this.RenderLabel(parent, "Discord Username (optional)").TitleLabel;
        discordNameLabel.Top = issueMessageLabel.Bottom + 20;

        var discordNameTextBox = this.RenderTextbox(parent,
            new Microsoft.Xna.Framework.Point(this.LABEL_WIDTH, discordNameLabel.Top),
            parent.Width - this.LABEL_WIDTH,
            "",
            "Discord#0001");

        discordNameTextBox.BasicTooltipText = "If provided, its used to ask additional questions or notify you about the status.";

        var includeSystemInformationLabel = this.RenderLabel(parent, "Include System Info").TitleLabel;
        includeSystemInformationLabel.Top = discordNameLabel.Bottom + 20;

        var includeSystemInformationCheckbox = this.RenderCheckbox(parent,
            new Microsoft.Xna.Framework.Point(this.LABEL_WIDTH, includeSystemInformationLabel.Top),
            false);

        includeSystemInformationCheckbox.BasicTooltipText = "If checked, additional system information will be included to assist looking into your issue.";

        var cancelButton = this.RenderButton(parent, "Cancel", () => this.CancelClicked?.Invoke(this, EventArgs.Empty));
        cancelButton.Bottom = contentRegion.Bottom;
        cancelButton.Right = contentRegion.Right;

        var createButton = this.RenderButtonAsync(parent, "Create", async () => await this.CreateClicked?.Invoke(this, (issueTitleTextBox.Text, issueMessageTextBox.Text, discordNameTextBox.Text, includeSystemInformationCheckbox.Checked)));
        createButton.Top = cancelButton.Top;
        createButton.Right = cancelButton.Left + 10;
    }

    protected override Task<bool> InternalLoad(IProgress<string> progress)
    {
        return Task.FromResult(true);
    }
}