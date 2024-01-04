namespace Estreya.BlishHUD.Shared.UI.Views;

using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Graphics;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.Shared.Controls;
using Estreya.BlishHUD.Shared.Models.BlishHudAPI;
using Estreya.BlishHUD.Shared.Utils;
using Flurl.Http;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared.Helpers;
using Shared.Services;
using Shared.UI.Views;
using System;
using System.CodeDom;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;

public class BlishHUDAPIView : BaseView
{
    private const string REGISTER_URL = "https://blish-hud.estreya.de/register";

    protected static readonly Point PADDING = new Point(25, 25);
    private readonly IFlurlClient _flurlClient;

    protected BlishHudApiService BlishHUDAPIService { get; }
    private KofiStatus _kofiStatus;

    private FlowPanel _mainParent;
    private FlowPanel _kofiStatusGroup;
    private Texture2D _kofiLogo;

    public BlishHUDAPIView(Gw2ApiManager apiManager, IconService iconService, TranslationService translationService, BlishHudApiService blishHudApiService, IFlurlClient flurlClient) : base(apiManager, iconService, translationService)
    {
        this.BlishHUDAPIService = blishHudApiService;
        this._flurlClient = flurlClient;
    }

    protected sealed override void InternalBuild(Panel parent)
    {
        this._mainParent = new FlowPanel
        {
            Parent = parent,
            Width = parent.ContentRegion.Width - (PADDING.X * 2),
            Height = parent.ContentRegion.Height - (PADDING.Y * 2),
            Location = new Point(PADDING.X, PADDING.Y),
            CanScroll = true,
            FlowDirection = ControlFlowDirection.SingleTopToBottom
        };

        this.BuildLoginSection(this._mainParent);

        //this.RenderEmptyLine(this._mainParent);
        //this.RenderKofiStatus(this._mainParent);

        //if (this.BlishHUDAPIService != null)
        //{
        //    this.BlishHUDAPIService.NewLogin += this.RedrawKofiStatusGroup;
        //    this.BlishHUDAPIService.RefreshedLogin += this.RedrawKofiStatusGroup;
        //    this.BlishHUDAPIService.LoggedOut += this.RedrawKofiStatusGroup;
        //}
    }

    private void RenderKofiStatus(FlowPanel flowPanel)
    {
        this._kofiStatusGroup ??= new FlowPanel()
        {
            Parent = flowPanel,
            Width = flowPanel.ContentRegion.Width - (int)flowPanel.OuterControlPadding.X * 2,
            HeightSizingMode = SizingMode.AutoSize,
            FlowDirection = ControlFlowDirection.SingleTopToBottom
        };

        var subscriptionActive = this._kofiStatus?.Active ?? false;
        var lastPayment = this._kofiStatus?.LastPayment?.ToLocalTime().ToString();
        this.RenderLabel(this._kofiStatusGroup, "Subscription active?", subscriptionActive.ToString(), textColorValue: subscriptionActive ? Color.Green : Color.Red, valueXLocation: 200);
        this.RenderLabel(this._kofiStatusGroup, "Last Payment", lastPayment ?? "Never", valueXLocation: 200);

        this.RenderEmptyLine(this._kofiStatusGroup);
        Button kofiSupport = this.RenderButton(this._kofiStatusGroup, "Ko-fi", () =>
        {
            Process.Start("https://ko-fi.com/estreya");
        });

        kofiSupport.Icon = this._kofiLogo;
        kofiSupport.Height = 48;
        kofiSupport.Width = 150;
        kofiSupport.Font = GameService.Content.DefaultFont18;

        this.RenderEmptyLine(this._kofiStatusGroup);

        var paymentNotDetectedLabelBuilder = this.GetLabelBuilder(this._kofiStatusGroup);

        paymentNotDetectedLabelBuilder
            .CreatePart("Your payment hasn't been detected?", b => { b.MakeBold().MakeUnderlined().SetFontSize(ContentService.FontSize.Size20); })
            .CreatePart("\n \n", b => { })
            .CreatePart("Make sure you have used the same email on ko-fi as you have used on Estreya BlishHUD.", b => { })
            .CreatePart("\n \n", b => { })
            .CreatePart("If it is still not picked up, dm ", builder => { })
            .CreatePart(DiscordUtil.ESTREYA_DISCORD_NAME, builder => { builder.SetHyperLink(DiscordUtil.ESTREYA_DISCORD_LINK).MakeBold(); })
            .CreatePart(" on Discord.", builder => { });

        var paymentNotDetectedLabel = paymentNotDetectedLabelBuilder.Build();
        paymentNotDetectedLabel.Parent = this._kofiStatusGroup;

        this.RenderEmptyLine(this._kofiStatusGroup);

        var whatCountsAsSubscribtionLabelBuilder = this.GetLabelBuilder(this._kofiStatusGroup);

        whatCountsAsSubscribtionLabelBuilder
            .CreatePart("What counts as a subscription?", b => { b.MakeBold().MakeUnderlined().SetFontSize(ContentService.FontSize.Size20); })
            .CreatePart("\n \n", b => { })
            .CreatePart("Currently only the membership subscription counts.", b => { })
            .CreatePart("\n", b => { })
            .CreatePart("The price you choose does not matter. It can be as low as 1€.", b => { });

        var whatCountsAsSubscribtionLabel = whatCountsAsSubscribtionLabelBuilder.Build();
        whatCountsAsSubscribtionLabel.Parent = this._kofiStatusGroup;
    }

    private async void RedrawKofiStatusGroup(object sender, EventArgs e)
    {
        if (this._kofiStatusGroup != null)
        {
            await this.LoadKofiStatus();
            this._kofiStatusGroup.ClearChildren();
            this.RenderKofiStatus(this._mainParent);
        }
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

        string password = AsyncHelper.RunSync(() => this.BlishHUDAPIService.GetAPIPassword());

        const string passwordUnchangedPhrase = "<<Unchanged>>";

        TextBox usernameTextBox = this.RenderTextbox(loginPanel, new Point(0, 0), 500, this.BlishHUDAPIService.GetAPIUsername(), this.TranslationService.GetTranslation("blishHUDAPIView-username", "Username"));

        TextBox passwordTextBox = this.RenderTextbox(loginPanel, new Point(0, 0), 500, !string.IsNullOrWhiteSpace(password) ? passwordUnchangedPhrase : null, this.TranslationService.GetTranslation("blishHUDAPIView-password", "Password"));

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

        this.RenderButton(buttonPanel, this.TranslationService.GetTranslation("blishHUDAPIView-btn-register", "Register"), () =>
        {
            try
            {
                _ = Process.Start(REGISTER_URL);
            }
            catch (Exception ex)
            {
                this.ShowError(ex.Message);
            }
        });
    }

    private FormattedLabelBuilder GetLabelBuilder(Panel parent)
    {
        return new FormattedLabelBuilder().SetWidth(parent.ContentRegion.Width - (PADDING.X * 2)).AutoSizeHeight().SetVerticalAlignment(VerticalAlignment.Top);
    }

    protected override async Task<bool> InternalLoad(IProgress<string> progress)
    {
        await this.LoadKofiStatus();
        await this.LoadKofiLogo();

        return true;
    }

    protected override void Unload()
    {
        if (this.BlishHUDAPIService != null)
        {
            this.BlishHUDAPIService.NewLogin -= this.RedrawKofiStatusGroup;
            this.BlishHUDAPIService.RefreshedLogin -= this.RedrawKofiStatusGroup;
            this.BlishHUDAPIService.LoggedOut -= this.RedrawKofiStatusGroup;
        }

        this._kofiStatus = null;
        this._kofiLogo = null;
        this._kofiStatusGroup = null;
        this._mainParent = null;

        base.Unload();
    }

    private async Task LoadKofiStatus()
    {
        try
        {
            this._kofiStatus = await this.BlishHUDAPIService.GetKofiStatus();
        }
        catch (Exception ex)
        {
            this._kofiStatus = null;
            this._logger.Warn(ex, "Could not load ko-fi status.");
        }
    }

    private async Task LoadKofiLogo()
    {
        try
        {
            Stream stream = await this._flurlClient.Request("https://storage.ko-fi.com/cdn/nav-logo-stroke.png").GetStreamAsync();
            System.Drawing.Bitmap bitmap = Utils.ImageUtil.ResizeImage(System.Drawing.Image.FromStream(stream), 48, 32);
            using MemoryStream memoryStream = new MemoryStream();
            bitmap.Save(memoryStream, ImageFormat.Png);
            await Task.Run(() =>
            {
                using GraphicsDeviceContext ctx = GameService.Graphics.LendGraphicsDeviceContext();
                this._kofiLogo = Texture2D.FromStream(ctx.GraphicsDevice, memoryStream);
            });
        }
        catch (Exception)
        {
        }
    }
}