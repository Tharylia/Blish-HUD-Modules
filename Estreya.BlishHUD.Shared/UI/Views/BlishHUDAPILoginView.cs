namespace Estreya.BlishHUD.Shared.UI.Views;

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

public class BlishHUDAPILoginView : BaseView
{
    protected static readonly Point PADDING = new Point(25, 25);
    protected BlishHudApiService BlishHUDAPIService { get; }

    public BlishHUDAPILoginView(Gw2ApiManager apiManager, IconService iconService, TranslationService translationService, BlishHudApiService blishHudApiService) : base(apiManager, iconService, translationService)
    {
        this.BlishHUDAPIService = blishHudApiService;
    }

    protected sealed override void InternalBuild(Panel parent)
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

        this.OnBeforeBuildLoginSection(flowPanel);
        this.BuildLoginSection(flowPanel);
        this.OnAfterBuildLoginSection(flowPanel);
    }

    protected virtual void OnBeforeBuildLoginSection(FlowPanel parent) { }
    protected virtual void OnAfterBuildLoginSection(FlowPanel parent) { }

    private void BuildLoginSection(FlowPanel parent)
    {
        FlowPanel loginPanel = new FlowPanel
        {
            Parent = parent,
            WidthSizingMode = SizingMode.AutoSize,
            HeightSizingMode = SizingMode.AutoSize,
            ControlPadding = new Vector2(0, 5),
            FlowDirection = ControlFlowDirection.SingleTopToBottom
        };

        string password = AsyncHelper.RunSync(() => this.BlishHUDAPIService.GetAPIPassword());

        const string passwordUnchangedPhrase = "<<Unchanged>>";

        TextBox usernameTextBox = this.RenderTextbox(loginPanel, new Point(0, 0), 250, this.BlishHUDAPIService.GetAPIUsername(), this.TranslationService.GetTranslation("blishHUDAPIView-username", "Username"));

        TextBox passwordTextBox = this.RenderTextbox(loginPanel, new Point(0, 0), 250, !string.IsNullOrWhiteSpace(password) ? passwordUnchangedPhrase : null, this.TranslationService.GetTranslation("blishHUDAPIView-password", "Password"));

        FlowPanel buttonPanel = new FlowPanel
        {
            Parent = loginPanel,
            WidthSizingMode = SizingMode.AutoSize,
            HeightSizingMode = SizingMode.AutoSize,
            FlowDirection = ControlFlowDirection.SingleLeftToRight
        };

        this.RenderButtonAsync(buttonPanel, this.TranslationService.GetTranslation("blishHUDAPIView-btn-save", "Save"), async () =>
        {
            this.BlishHUDAPIService.SetAPIUsername(usernameTextBox.Text);
            await this.BlishHUDAPIService.SetAPIPassword(passwordTextBox.Text == passwordUnchangedPhrase ? password : passwordTextBox.Text);

            await this.BlishHUDAPIService.Login();
            this.ShowInfo("Login successful!");
        });

        this.RenderButtonAsync(buttonPanel, this.TranslationService.GetTranslation("blishHUDAPIView-btn-testLogin", "Test Login"), async () =>
        {
            await this.BlishHUDAPIService.TestLogin(usernameTextBox.Text, passwordTextBox.Text == passwordUnchangedPhrase ? password : passwordTextBox.Text);
            this.ShowInfo("Login successful!");
        });

        this.RenderButtonAsync(buttonPanel, this.TranslationService.GetTranslation("blishHUDAPIView-btn-clearCredentials", "Clear Credentials"), async () =>
        {
            this.BlishHUDAPIService.SetAPIUsername(null);
            await this.BlishHUDAPIService.SetAPIPassword(null);
            this.BlishHUDAPIService.Logout();
            this.ShowInfo("Logout successful!");
        });
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