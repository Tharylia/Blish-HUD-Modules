namespace Estreya.BlishHUD.Shared.UI.Views
{
    using Blish_HUD.Controls;
    using Blish_HUD.Graphics.UI;
    using Estreya.BlishHUD.Shared.Helpers;
    using Estreya.BlishHUD.Shared.Services;
    using Microsoft.Xna.Framework;
    using MonoGame.Extended.BitmapFonts;
    using System;
    using System.Threading.Tasks;

    public class ModuleSettingsView : BaseView
    {
        public event EventHandler OpenClicked;
        public event EventHandler CreateGithubIssueClicked;

        public ModuleSettingsView(IconService iconService, TranslationService translationService): base(null, iconService, translationService)
        {
        }

        protected override Task<bool> InternalLoad(IProgress<string> progress)
        {
            return Task.FromResult(true);
        }

        protected override void InternalBuild(Panel parent)
        {
            Rectangle bounds = parent.ContentRegion;

            FlowPanel parentPanel = new FlowPanel()
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

            ViewContainer settingContainer = new ViewContainer()
            {
                WidthSizingMode = SizingMode.Fill,
                HeightSizingMode = SizingMode.AutoSize,
                Parent = parentPanel
            };

            string buttonText = this.TranslationService.GetTranslation("moduleSettingsView-openSettingsBtn", "Open Settings");

            StandardButton openSettingsButton = new StandardButton()
            {
                Parent = settingContainer,
                Text = buttonText,
            };

            if (this.Font != null)
            {
                openSettingsButton.Width = (int)this.Font.MeasureString(buttonText).Width;
            }

            openSettingsButton.Location = new Point(Math.Max(parentPanel.Width / 2 - openSettingsButton.Width / 2, 20), Math.Max(parentPanel.Height / 2 - openSettingsButton.Height, 20));

            openSettingsButton.Click += (s, e) => this.OpenClicked?.Invoke(this, EventArgs.Empty);

            var githubIssueText = this.TranslationService.GetTranslation("moduleSettingsView-createGitHubIssueBtn", "Create Bug/Feature Issue");

            StandardButton createGithubIssue = new StandardButton()
            {
                Parent = settingContainer,
                Text = githubIssueText
            };

            if (this.Font != null)
            {
                createGithubIssue.Width = (int)this.Font.MeasureString(githubIssueText).Width;
            }

            createGithubIssue.Location = new Point(Math.Max(parentPanel.Width / 2 - createGithubIssue.Width / 2, 20), openSettingsButton.Bottom + 10);

            createGithubIssue.Click += (s, e) => CreateGithubIssueClicked?.Invoke(this, EventArgs.Empty);
        }
    }
}
