namespace Estreya.BlishHUD.Shared.UI.Views
{
    using Blish_HUD;
    using Blish_HUD.Controls;
    using Blish_HUD.Modules.Managers;
    using Estreya.BlishHUD.Shared.Models;
    using Estreya.BlishHUD.Shared.State;
    using Microsoft.Xna.Framework;
    using MonoGame.Extended.BitmapFonts;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading.Tasks;

    public class NewsView : BaseView
    {
        private static Point _importantIconSize = new Point(32, 32);
        private NewsState _newsState;

        public NewsView(Gw2ApiManager apiManager, IconState iconState, TranslationState translationState, NewsState newsState, BitmapFont font = null) : base(apiManager, iconState, translationState, font)
        {
            this._newsState = newsState;
        }

        protected override void InternalBuild(Panel parent)
        {
            FlowPanel newsList = new FlowPanel()
            {
                Parent = parent,
                FlowDirection = ControlFlowDirection.SingleTopToBottom,
                Location= new Microsoft.Xna.Framework.Point(25, 25),
                Height = parent.ContentRegion.Height - 25 * 2,
                Width = parent.ContentRegion.Width - 25 * 2,
                CanScroll = true,
            };

            var sortedNews = this._newsState.News.OrderByDescending(n => n.Timestamp).ToList();
            if (sortedNews.Count > 0)
            {
                foreach (var news in sortedNews)
                {
                    this.RenderNews(newsList, news);
                    this.RenderEmptyLine(newsList);
                }
            }
            else
            {
                this.RenderNoNewsInfo(newsList);
            }
        }

        private void RenderNoNewsInfo(FlowPanel newsList)
        {
            Panel panel = new Panel()
            {
                Parent = newsList,
                Size = newsList.ContentRegion.Size,
                ShowBorder = false
            };

            FormattedLabelBuilder builder = new FormattedLabelBuilder()
                .SetWidth(panel.ContentRegion.Width).AutoSizeHeight().SetHorizontalAlignment(HorizontalAlignment.Center)
                .CreatePart("There are no news.", builder => { });

            var lbl = builder.Build();
            lbl.Parent = panel;

            lbl.Top = panel.Height / 2 - lbl.Height / 2;
            lbl.Left = panel.Width / 2 - lbl.Width / 2;
        }

        private void RenderNews(FlowPanel newsList, News news)
        {
            Panel newsPanel = new Panel()
            {
                Parent = newsList,
                Width = newsList.ContentRegion.Width - 25,
                HeightSizingMode = SizingMode.AutoSize,
                ShowBorder = true
            };

            var titleFontSize = ContentService.FontSize.Size20;
            var titleFont = GameService.Content.GetFont(ContentService.FontFace.Menomonia, titleFontSize, ContentService.FontStyle.Regular);
            var title = news.Title;
            var titleWidth = titleFont.MeasureString(title).Width;

            var timeFontSize = ContentService.FontSize.Size16;
            var timeFont = GameService.Content.GetFont(ContentService.FontFace.Menomonia, timeFontSize, ContentService.FontStyle.Regular);

            FormattedLabelBuilder builder = new FormattedLabelBuilder();
            builder.SetWidth(newsList.ContentRegion.Width).AutoSizeHeight()
                .CreatePart("", builder =>
                {
                    if (news.Important)
                    {
                        builder.SetPrefixImage(this.IconState.GetIcon("222246.png")).SetPrefixImageSize(_importantIconSize);
                    }
                })
                .CreatePart(title, builder => { builder.SetFontSize(titleFontSize).MakeUnderlined(); })
                .CreatePart(this.GenerateTimestampAlignment(news, timeFont, newsPanel.ContentRegion.Width - (int)titleWidth - 5 - (news.Important ? _importantIconSize.X : 0)), builder => { builder.SetFontSize(timeFontSize); })
                .CreatePart("\n \n", builder => { });

            if (news.AsPoints)
            {
                foreach (var point in news.Content)
                {
                    builder.CreatePart($"- {point}", builder => { })
                        .CreatePart("\n \n", builder => { });
                }
            }
            else
            {
                var content = string.Join("\n", news.Content);
                    builder.CreatePart(content, builder => { })
                    .CreatePart("\n \n", builder => { });
            }

            var newsPart = builder.Build();
            newsPart.Parent = newsPanel;
        }

        private string GenerateTimestampAlignment(News news, BitmapFont font, int maxWidth)
        {
            string text = string.Empty;
            int spaceCounter = 0;
            var width = 0;
            while (width < maxWidth)
            {
                text = $"\t{new string(' ', spaceCounter)}{news.Timestamp.ToLocalTime().ToString()}";
                spaceCounter++;
                width = (int)font.MeasureString(text).Width;
            }

            return text;
        } 

        protected override Task<bool> InternalLoad(IProgress<string> progress)
        {
            return Task.FromResult(true);
        }

        protected override void Unload()
        {
            base.Unload();

            this._newsState = null;
        }
    }
}
