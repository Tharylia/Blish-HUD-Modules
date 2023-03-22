namespace Estreya.BlishHUD.EventTable.UI.Views;

using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.EventTable.Controls;
using Estreya.BlishHUD.EventTable.Models;
using Estreya.BlishHUD.Shared.Extensions;
using Estreya.BlishHUD.Shared.Helpers;
using Estreya.BlishHUD.Shared.Security;
using Estreya.BlishHUD.Shared.State;
using Estreya.BlishHUD.Shared.UI.Views;
using Microsoft.Xna.Framework;
using MonoGame.Extended.BitmapFonts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

public class CustomEventView : BaseView
{
    private static Point PADDING = new Point(25, 25);
    private BlishHudApiState _blishHudApiState;

    public CustomEventView(Gw2ApiManager apiManager, IconState iconState, TranslationState translationState, BlishHudApiState blishHudApiState, BitmapFont font = null) : base(apiManager, iconState, translationState, font)
    {
        this._blishHudApiState = blishHudApiState;
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

        this.BuildInstructionSection(flowPanel);
        this.BuildLoginSection(flowPanel);
    }

    private void BuildInstructionSection(FlowPanel parent)
    {
        var instructionPanel = new FlowPanel()
        {
            Parent = parent,
            OuterControlPadding = new Vector2(20, 20),
            WidthSizingMode = SizingMode.AutoSize,
            HeightSizingMode = SizingMode.AutoSize,
            FlowDirection = ControlFlowDirection.SingleTopToBottom,
            ShowBorder = true
        };

        var labelBuilder = this.GetLabelBuilder(parent)
            .CreatePart("1. Make an account at ", builder => { builder.SetFontSize(Blish_HUD.ContentService.FontSize.Size20); })
            .CreatePart("Estreya BlishHUD API.", builder => { builder.SetFontSize(Blish_HUD.ContentService.FontSize.Size20).SetHyperLink("https://blish-hud.estreya.de/register"); })
            .CreatePart("\n \n", builder => { })
            .CreatePart("2. Follow steps send by mail.", builder => { builder.SetFontSize(Blish_HUD.ContentService.FontSize.Size20); })
            .CreatePart("\n \n", builder => { })
            .CreatePart("3. Add your own custom events.", builder => { builder.SetFontSize(Blish_HUD.ContentService.FontSize.Size20); })
            .CreatePart("\n \n", builder => { })
            .CreatePart("4. Enter login details below.", builder => { builder.SetFontSize(Blish_HUD.ContentService.FontSize.Size20); });

        var label = labelBuilder.Build();
        label.Parent = instructionPanel;

        this.RenderEmptyLine(instructionPanel, 20);
    }

    private void BuildLoginSection(FlowPanel parent)
    {
        var loginPanel = new FlowPanel()
        {
            Parent = parent,
            WidthSizingMode = SizingMode.AutoSize,
            HeightSizingMode = SizingMode.AutoSize,
            ControlPadding = new Vector2(0, 5),
            FlowDirection = ControlFlowDirection.SingleTopToBottom,
        };

        var password = AsyncHelper.RunSync(() => this._blishHudApiState.GetAPIPassword());

        const string passwordUnchangedPhrase = "<<Unchanged>>";

        var usernameTextBox = this.RenderTextbox(loginPanel, new Point(0, 0), 250, this._blishHudApiState.GetAPIUsername(), "API Username");

        var passwordTextBox = this.RenderTextbox(loginPanel, new Point(0, 0), 250, !string.IsNullOrWhiteSpace(password) ? passwordUnchangedPhrase : null, "API Password");

        var buttonPanel = new FlowPanel()
        {
            Parent = loginPanel,
            WidthSizingMode = SizingMode.AutoSize,
            HeightSizingMode = SizingMode.AutoSize,
            FlowDirection = ControlFlowDirection.SingleLeftToRight,
        };

        this.RenderButtonAsync(buttonPanel, "Save", async () =>
        {
            this._blishHudApiState.SetAPIUsername(usernameTextBox.Text);
            await this._blishHudApiState.SetAPIPassword(passwordTextBox.Text == passwordUnchangedPhrase ? password : passwordTextBox.Text);

            await this._blishHudApiState.Login();
        });

        this.RenderButtonAsync(buttonPanel, "Test Login", async () =>
        {
            await this._blishHudApiState.TestLogin(usernameTextBox.Text, passwordTextBox.Text == passwordUnchangedPhrase ? password : passwordTextBox.Text);
            this.ShowInfo("Login successful!");
        });

        this.RenderButtonAsync(buttonPanel, "Clear Credentials", async () =>
        {
            this._blishHudApiState.SetAPIUsername(null);
            await this._blishHudApiState.SetAPIPassword(null);
            this._blishHudApiState.Logout();
            this.ShowInfo("Logout successful!");
        });
    }

    private FormattedLabelBuilder GetLabelBuilder(Panel parent)
    {
        return new FormattedLabelBuilder().SetWidth(parent.ContentRegion.Width - PADDING.X * 2).AutoSizeHeight().SetVerticalAlignment(VerticalAlignment.Top);
    }

    protected override Task<bool> InternalLoad(IProgress<string> progress) => Task.FromResult(true);
}
