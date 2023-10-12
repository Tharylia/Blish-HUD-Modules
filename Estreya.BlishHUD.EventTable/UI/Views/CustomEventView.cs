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

public class CustomEventView : BaseView
{
    private static readonly Point PADDING = new Point(25, 25);
    private readonly BlishHudApiService _blishHudApiService;

    public CustomEventView(Gw2ApiManager apiManager, IconService iconService, TranslationService translationService, BlishHudApiService blishHudApiService) : base(apiManager, iconService, translationService)
    {
        this._blishHudApiService = blishHudApiService;
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

        this.BuildInstructionSection(flowPanel);
        this.BuildLoginSection(flowPanel);
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
                                                 .CreatePart(this.TranslationService.GetTranslation("customEventView-manual2", "Estreya BlishHUD API."), builder => { 
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

        string password = AsyncHelper.RunSync(() => this._blishHudApiService.GetAPIPassword());

        const string passwordUnchangedPhrase = "<<Unchanged>>";

        TextBox usernameTextBox = this.RenderTextbox(loginPanel, new Point(0, 0), 250, this._blishHudApiService.GetAPIUsername(), this.TranslationService.GetTranslation("customEventView-apiUsername", "API Username"));

        TextBox passwordTextBox = this.RenderTextbox(loginPanel, new Point(0, 0), 250, !string.IsNullOrWhiteSpace(password) ? passwordUnchangedPhrase : null, this.TranslationService.GetTranslation("customEventView-apiPassword", "API Password"));

        FlowPanel buttonPanel = new FlowPanel
        {
            Parent = loginPanel,
            WidthSizingMode = SizingMode.AutoSize,
            HeightSizingMode = SizingMode.AutoSize,
            FlowDirection = ControlFlowDirection.SingleLeftToRight
        };

        this.RenderButtonAsync(buttonPanel, this.TranslationService.GetTranslation("customEventView-btn-save", "Save"), async () =>
        {
            this._blishHudApiService.SetAPIUsername(usernameTextBox.Text);
            await this._blishHudApiService.SetAPIPassword(passwordTextBox.Text == passwordUnchangedPhrase ? password : passwordTextBox.Text);

            await this._blishHudApiService.Login();
        });

        this.RenderButtonAsync(buttonPanel, this.TranslationService.GetTranslation("customEventView-btn-testLogin", "Test Login"), async () =>
        {
            await this._blishHudApiService.TestLogin(usernameTextBox.Text, passwordTextBox.Text == passwordUnchangedPhrase ? password : passwordTextBox.Text);
            this.ShowInfo("Login successful!");
        });

        this.RenderButtonAsync(buttonPanel, this.TranslationService.GetTranslation("customEventView-btn-clearCredentials", "Clear Credentials"), async () =>
        {
            this._blishHudApiService.SetAPIUsername(null);
            await this._blishHudApiService.SetAPIPassword(null);
            this._blishHudApiService.Logout();
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