namespace Estreya.BlishHUD.Shared.UI.Views;

using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.Shared.Helpers;
using Estreya.BlishHUD.Shared.Services;
using Flurl.Http;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.BitmapFonts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Humanizer;
using Estreya.BlishHUD.Shared.Utils;

public class DonationView : BaseView
{
    private IFlurlClient _flurlClient;
    private Texture2D _kofiLogo;

    public DonationView(IFlurlClient flurlClient, Gw2ApiManager apiManager, IconService iconService, TranslationService translationService, BitmapFont font = null) : base(apiManager, iconService, translationService, font)
    {
        this._flurlClient = flurlClient;
    }

    protected override void InternalBuild(Panel parent)
    {
        var sectionsPanel = new FlowPanel()
        {
            Parent = parent,
            FlowDirection = ControlFlowDirection.SingleTopToBottom,
            Size = parent.ContentRegion.Size,
            CanScroll = true
        };

        this.BuildDonationSection(sectionsPanel);
    }

    private void BuildDonationSection(FlowPanel parent)
    {
        var sectionPanel = new Panel()
        {
            Parent = parent,
            Width = parent.ContentRegion.Width,
            HeightSizingMode = SizingMode.AutoSize
        };

        FormattedLabelBuilder builder = new FormattedLabelBuilder().SetWidth(sectionPanel.ContentRegion.Width - 50).AutoSizeHeight().Wrap()
            .CreatePart("You enjoy my work on these modules and want to support it? Feels free to choose a donation method you like.", builder => { builder.SetFontSize(ContentService.FontSize.Size20); })
            .CreatePart("Donations are always optional and never expected to use my modules!", builder =>
            {
                builder.SetFontSize(ContentService.FontSize.Size16);
                builder.MakeItalic();
            });

        var label = builder.Build();
        label.Parent = sectionPanel;
        label.Location = new Microsoft.Xna.Framework.Point(30, 30);

        var kofiSupport = this.RenderButton(sectionPanel, "Ko-fi", () =>
        {
            Process.Start("https://ko-fi.com/estreya");
        });

        kofiSupport.Left = label.Left;
        kofiSupport.Top = label.Bottom + 20;
        kofiSupport.Icon = this._kofiLogo;
        kofiSupport.Height = 48;
        kofiSupport.Width = 150;
        kofiSupport.Font = GameService.Content.DefaultFont18;
    }

    protected override async Task<bool> InternalLoad(IProgress<string> progress)
    {
        try
        {
            var stream = await this._flurlClient.Request("https://storage.ko-fi.com/cdn/nav-logo-stroke.png").GetStreamAsync();
            var bitmap = ImageUtil.ResizeImage(System.Drawing.Image.FromStream(stream), 48, 32);
            using MemoryStream memoryStream = new MemoryStream();
            bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
            await Task.Run(() =>
            {
                using var ctx = GameService.Graphics.LendGraphicsDeviceContext();
                this._kofiLogo = Texture2D.FromStream(ctx.GraphicsDevice, memoryStream);
            });
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    
}
