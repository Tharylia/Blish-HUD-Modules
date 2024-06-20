namespace Estreya.BlishHUD.Shared.UI.Views;

using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Graphics;
using Blish_HUD.Modules.Managers;
using Flurl.Http;
using Microsoft.Xna.Framework.Graphics;
using Models;
using MonoGame.Extended.BitmapFonts;
using Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Utils;
using Image = Blish_HUD.Controls.Image;
using Point = Microsoft.Xna.Framework.Point;

public class NewsView : BaseView
{
    private const string ESTREYA_DISCORD_INVITE = "https://discord.gg/8Yb3jdca3r";
    private const string BLISH_HUD_DISCORD_INVITE = "https://discord.gg/nGbd3kU";
    private static readonly Point _importantIconSize = new Point(32, 32);
    private readonly IFlurlClient _flurlClient;
    private Texture2D _discordLogo;
    private NewsService _newsService;

    public NewsView(IFlurlClient flurlClient, Gw2ApiManager apiManager, IconService iconService, TranslationService translationService, NewsService newsService) : base(apiManager, iconService, translationService)
    {
        this._flurlClient = flurlClient;
        this._newsService = newsService;
    }

    protected override void InternalBuild(Panel parent)
    {
        FlowPanel newsList = new FlowPanel
        {
            Parent = parent,
            FlowDirection = ControlFlowDirection.SingleTopToBottom,
            Location = new Point(25, 25),
            Height = (int)(parent.ContentRegion.Height * 0.75),
            Width = parent.ContentRegion.Width - (25 * 2),
            CanScroll = true
        };

        List<News> sortedNews = this._newsService?.News?.OrderByDescending(n => n.Timestamp).ToList() ?? new List<News>();
        if (sortedNews.Count > 0)
        {
            foreach (News news in sortedNews)
            {
                this.RenderNews(newsList, news);
                this.RenderEmptyLine(newsList);
            }
        }
        else
        {
            this.RenderNoNewsInfo(newsList);
        }

        Panel discordSection = new Panel
        {
            Parent = parent,
            Location = new Point(newsList.Left, newsList.Bottom),
            Width = newsList.Width,
            ShowBorder = true
        };

        discordSection.Height = parent.ContentRegion.Height - discordSection.Top;

        Image image = new Image(this._discordLogo ?? ContentService.Textures.TransparentPixel) { Parent = discordSection };
        image.Location = new Point(30, (discordSection.Height / 2) - (image.Height / 2) - 5);

        FormattedLabelBuilder labelBuilder = new FormattedLabelBuilder().SetWidth(discordSection.ContentRegion.Width).AutoSizeHeight().SetVerticalAlignment(VerticalAlignment.Top).SetHorizontalAlignment(HorizontalAlignment.Center)
                                                                        .CreatePart("Join my Discord to stay up to date!", builder => { builder.SetFontSize(ContentService.FontSize.Size20).MakeUnderlined(); })
                                                                        .CreatePart("\n \n", builder => { })
                                                                        .CreatePart(ESTREYA_DISCORD_INVITE, builder => { builder.SetHyperLink(ESTREYA_DISCORD_INVITE); })
                                                                        .CreatePart("\n \n", builder => { })
                                                                        .CreatePart("BlishHUD:", builder => { })
                                                                        .CreatePart("\n ", builder => { })
                                                                        .CreatePart(BLISH_HUD_DISCORD_INVITE, builder => { builder.SetHyperLink(BLISH_HUD_DISCORD_INVITE); });

        //https://discordapp.com/invite/nGbd3kU

        FormattedLabel label = labelBuilder.Build();
        label.Parent = discordSection;
    }

    private void RenderNoNewsInfo(FlowPanel newsList)
    {
        Panel panel = new Panel
        {
            Parent = newsList,
            Size = newsList.ContentRegion.Size,
            ShowBorder = false
        };

        FormattedLabelBuilder builder = new FormattedLabelBuilder()
                                        .SetWidth(panel.ContentRegion.Width).AutoSizeHeight().SetHorizontalAlignment(HorizontalAlignment.Center)
                                        .CreatePart("There are no news.", builder => { });

        FormattedLabel lbl = builder.Build();
        lbl.Parent = panel;

        lbl.Top = (panel.Height / 2) - (lbl.Height / 2);
        lbl.Left = (panel.Width / 2) - (lbl.Width / 2);
    }

    private void RenderNews(FlowPanel newsList, News news)
    {
        Panel newsPanel = new Panel
        {
            Parent = newsList,
            Width = newsList.ContentRegion.Width - 25,
            HeightSizingMode = SizingMode.AutoSize,
            ShowBorder = true
        };

        ContentService.FontSize titleFontSize = ContentService.FontSize.Size20;
        BitmapFont titleFont = GameService.Content.GetFont(ContentService.FontFace.Menomonia, titleFontSize, ContentService.FontStyle.Regular);
        string title = news.Title;
        float titleWidth = titleFont.MeasureString(title).Width;

        ContentService.FontSize timeFontSize = ContentService.FontSize.Size16;
        BitmapFont timeFont = GameService.Content.GetFont(ContentService.FontFace.Menomonia, timeFontSize, ContentService.FontStyle.Regular);

        FormattedLabelBuilder builder = new FormattedLabelBuilder();
        builder.SetWidth(newsList.ContentRegion.Width).AutoSizeHeight()
               .CreatePart("", builder =>
               {
                   if (news.Important)
                   {
                       builder.SetPrefixImage(this.IconService.GetIcon("222246.png")).SetPrefixImageSize(_importantIconSize);
                   }
               })
               .CreatePart(title, builder => { builder.SetFontSize(titleFontSize).MakeUnderlined(); })
               .CreatePart(this.GenerateTimestampAlignment(news, timeFont, newsPanel.ContentRegion.Width - (int)titleWidth - 5 - (news.Important ? _importantIconSize.X : 0)), builder => { builder.SetFontSize(timeFontSize); })
               .CreatePart("\n \n", builder => { });

        if (news.AsPoints)
        {
            foreach (string point in news.Content)
            {
                builder.CreatePart($"- {point}", builder => { })
                       .CreatePart("\n \n", builder => { });
            }
        }
        else
        {
            string content = string.Join("\n", news.Content);
            builder.CreatePart(content, builder => { })
                   .CreatePart("\n \n", builder => { });
        }

        FormattedLabel newsPart = builder.Build();
        newsPart.Parent = newsPanel;
    }

    private string GenerateTimestampAlignment(News news, BitmapFont font, int maxWidth)
    {
        string text = string.Empty;
        int spaceCounter = 0;
        int width = 0;
        while (width < maxWidth)
        {
            text = $"\t{new string(' ', spaceCounter)}{news.Timestamp.ToLocalTime().ToString()}";
            spaceCounter++;
            width = (int)font.MeasureString(text).Width;
        }

        return text;
    }

    protected override async Task<bool> InternalLoad(IProgress<string> progress)
    {
        await this.TryLoadDiscordLogo();
        return true;
    }

    private async Task TryLoadDiscordLogo()
    {
        try
        {
            Stream stream = await this._flurlClient.Request("https://assets-global.website-files.com/6257adef93867e50d84d30e2/636e0b544a3e3c7c05753bcd_full_logo_white_RGB.png").GetStreamAsync();
            Bitmap bitmap = ImageUtil.ResizeImage(System.Drawing.Image.FromStream(stream), 200, 38);
            using MemoryStream memoryStream = new MemoryStream();
            bitmap.Save(memoryStream, ImageFormat.Png);
            await Task.Run(() =>
            {
                using GraphicsDeviceContext ctx = GameService.Graphics.LendGraphicsDeviceContext();
                this._discordLogo = Texture2D.FromStream(ctx.GraphicsDevice, memoryStream);
            });
        }
        catch (Exception)
        {
        }
    }

    protected override void Unload()
    {
        base.Unload();

        this._newsService = null;
        this._discordLogo?.Dispose();
        this._discordLogo = null;
    }
}