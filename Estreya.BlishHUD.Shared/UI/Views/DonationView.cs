namespace Estreya.BlishHUD.Shared.UI.Views;

using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.Shared.Helpers;
using Estreya.BlishHUD.Shared.State;
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

public class DonationView : BaseView
{
    private IFlurlClient _flurlClient;
    private Texture2D _kofiLogo;

    public DonationView(IFlurlClient flurlClient, Gw2ApiManager apiManager, IconState iconState, TranslationState translationState, BitmapFont font = null) : base(apiManager, iconState, translationState, font)
    {
        this._flurlClient = flurlClient;
    }

    protected override void InternalBuild(Panel parent)
    {
        FormattedLabelBuilder builder = new FormattedLabelBuilder().SetWidth(parent.ContentRegion.Width - 50).AutoSizeHeight().Wrap()
            .CreatePart("You enjoy my work on these modules and want to support it? Feels free to choose a donation method you like.", builder => { builder.SetFontSize(ContentService.FontSize.Size20); })
            .CreatePart("Donations are always optional and never expected to use my modules!", builder => { 
                builder.SetFontSize(ContentService.FontSize.Size16); 
                builder.MakeItalic();
            });

        var label = builder.Build();
        label.Parent = parent;
        label.Location = new Microsoft.Xna.Framework.Point(30, 30);

        StandardButton kofiSupport = new StandardButton
        {
            Left = label.Left,
            Top = label.Bottom + 20,
            Parent = parent,
            Icon = this._kofiLogo,
            Text = "Ko-fi",
            Height = 48,
            Width = 150,
           
        };

        var fontProperty = kofiSupport.GetType().GetField("_font", System.Reflection.BindingFlags.NonPublic |System.Reflection.BindingFlags.Instance);
        fontProperty?.SetValue(kofiSupport, GameService.Content.DefaultFont18);
        kofiSupport.Click += (s, e) =>
        {
            try
            {
                Process.Start("https://ko-fi.com/estreya");
            }
            catch (Exception)
            {
            }
        };
    }

    protected override async Task<bool> InternalLoad(IProgress<string> progress)
    {
        try
        {
            var stream = await this._flurlClient.Request("https://storage.ko-fi.com/cdn/nav-logo-stroke.png").GetStreamAsync();
            var bitmap = ResizeImage(System.Drawing.Image.FromStream(stream), 48,32);
            using MemoryStream memoryStream = new MemoryStream();
            bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
            await Task.Run(() =>
            {
                using var ctx = GameService.Graphics.LendGraphicsDeviceContext() ;
                this._kofiLogo = Texture2D.FromStream(ctx.GraphicsDevice, memoryStream);
            });
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Resize the image to the specified width and height.
    /// </summary>
    /// <param name="image">The image to resize.</param>
    /// <param name="width">The width to resize to.</param>
    /// <param name="height">The height to resize to.</param>
    /// <returns>The resized image.</returns>
    private static Bitmap ResizeImage(System.Drawing.Image image, int width, int height)
    {
        var destRect = new Rectangle(0, 0, width, height);
        var destImage = new Bitmap(width, height);

        destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

        using (var graphics = Graphics.FromImage(destImage))
        {
            graphics.CompositingMode = CompositingMode.SourceCopy;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            using (var wrapMode = new ImageAttributes())
            {
                wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
            }
        }

        return destImage;
    }
}
