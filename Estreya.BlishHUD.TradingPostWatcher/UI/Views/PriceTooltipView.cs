namespace Estreya.BlishHUD.TradingPostWatcher.UI.Views;

using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.Shared.Service;
using Estreya.BlishHUD.Shared.UI.Views;
using Estreya.BlishHUD.Shared.Utils;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

internal class PriceTooltipView : TooltipView
{
    private readonly int _coins;
    private readonly string _priceComment;

    public PriceTooltipView(string title, string description, int coins, Gw2ApiManager apiManager, IconService iconService, TranslationService translationService) : base(title, description, translationService, apiManager, iconService)
    {
        this._coins = coins;
    }

    public PriceTooltipView(string title, string description, int coins, AsyncTexture2D icon, Gw2ApiManager apiManager, IconService iconService, TranslationService translationService) : base(title, description, icon, translationService, apiManager, iconService)
    {
        this._coins = coins;
    }

    public PriceTooltipView(string title, string description, int coins, string priceComment, AsyncTexture2D icon, Gw2ApiManager apiManager, IconService iconService, TranslationService translationService) : this(title, description,coins, icon, apiManager, iconService, translationService)
    {
        this._priceComment = priceComment;
    }

    protected override void InternalBuild(Panel parent)
    {
        base.InternalBuild(parent); // Ensure base TooltipView is build.

        Control lastAddedControl = parent.Children.Last();

        var splitCoins = GW2Utils.SplitCoins(this._coins);

        var coinImageTop = lastAddedControl.Bottom + 5;
        var coinLabelTop = coinImageTop + 5;

        Label goldLabel = new Label()
        {
            Parent = parent,
            Text = splitCoins.Gold.ToString(),
            Location = new Microsoft.Xna.Framework.Point(lastAddedControl.Left, coinLabelTop)
        };

        goldLabel.Width = (int) goldLabel.Font.MeasureString(goldLabel.Text).Width;

        Image goldImage = new Image()
        {
            Parent = parent,
            Texture = this.IconService?.GetIcon("156904.png"),
            Location = new Microsoft.Xna.Framework.Point(goldLabel.Right, coinImageTop),
            Size = new Microsoft.Xna.Framework.Point(32,32)
        };

        Label silverLabel = new Label()
        {
            Parent = parent,
            Text = splitCoins.Silver.ToString(),
            Location = new Microsoft.Xna.Framework.Point(goldImage.Right, coinLabelTop)
        };

        silverLabel.Width = (int)silverLabel.Font.MeasureString(silverLabel.Text).Width;

        Image silverImage = new Image()
        {
            Parent = parent,
            Texture = this.IconService?.GetIcon("156907.png"),
            Location = new Microsoft.Xna.Framework.Point(silverLabel.Right, coinImageTop),
            Size = new Microsoft.Xna.Framework.Point(32, 32)
        };

        Label copperLabel = new Label()
        {
            Parent = parent,
            Text = splitCoins.Copper.ToString(),
            Location = new Microsoft.Xna.Framework.Point(silverImage.Right, coinLabelTop)
        };

        copperLabel.Width = (int) copperLabel.Font.MeasureString(copperLabel.Text).Width;

        Image copperImage = new Image()
        {
            Parent = parent,
            Texture = this.IconService?.GetIcon("156902.png"),
            Location = new Microsoft.Xna.Framework.Point(copperLabel.Right, coinImageTop),
            Size = new Microsoft.Xna.Framework.Point(32, 32)
        };

        if (!string.IsNullOrWhiteSpace(this._priceComment))
        {
            Label priceComment = new Label()
            {
                Parent = parent,
                Text = this._priceComment,
                WrapText = true,
                Location = new Microsoft.Xna.Framework.Point(copperImage.Right + 5, coinLabelTop),
                TextColor = Control.StandardColors.DisabledText,
            };

            var priceCommentWidth = (int)priceComment.Font.MeasureString(priceComment.Text).Width + 20;
            priceComment.Width = priceCommentWidth;
        }
    }
}
