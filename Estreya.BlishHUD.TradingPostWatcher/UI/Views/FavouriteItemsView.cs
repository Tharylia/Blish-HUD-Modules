namespace Estreya.BlishHUD.TradingPostWatcher.UI.Views;

using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.Shared.Controls;
using Estreya.BlishHUD.Shared.Services.TradingPost;
using Estreya.BlishHUD.Shared.Services;
using Estreya.BlishHUD.Shared.Utils;
using Estreya.BlishHUD.Shared.UI.Views;
using Estreya.BlishHUD.TradingPostWatcher.Services;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Estreya.BlishHUD.Shared.Models.GW2API.Commerce;
using System.Xml;
using Estreya.BlishHUD.Shared.Models.GW2API.Items;
using Estreya.BlishHUD.Shared.Extensions;

public class FavouriteItemsView : BaseView
{
    private const int PADDING_X = 20;
    private const int PADDING_Y = 20;

    private readonly FavouriteItemService _favouriteItemService;
    private readonly ItemService _itemService;
    private readonly TransactionsService _transactionsService;
    private Panel _contentPanel;

    public FavouriteItemsView(FavouriteItemService favouriteItemService, ItemService itemService, TransactionsService transactionsService, Gw2ApiManager apiManager, IconService iconService, TranslationService translationService) : base(apiManager, iconService, translationService)
    {
        this._favouriteItemService = favouriteItemService;
        this._itemService = itemService;
        this._transactionsService = transactionsService;
    }

    protected override void InternalBuild(Panel parent)
    {
        Rectangle bounds = new Rectangle(PADDING_X, PADDING_Y, parent.ContentRegion.Width - (PADDING_X * 2), parent.ContentRegion.Height - (PADDING_Y * 2));

        Rectangle contentBounds = new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height - (int)(Button.STANDARD_CONTROL_HEIGHT * 1.2));

        this.RenderItemOverview(parent, contentBounds);

        Button addButton = this.RenderButton(parent, "Add", () =>
        {
            this.RenderAddItem(parent, contentBounds);
        });

        addButton.Top = bounds.Bottom - Button.STANDARD_CONTROL_HEIGHT;
        addButton.Left = bounds.Left;
    }

    private void CreateContentPanel(Panel parent, Rectangle bounds)
    {
        this.ClearContentPanel();

        this._contentPanel = this.GetPanel(parent);
        this._contentPanel.CanScroll = false; // Should not be needed
        this._contentPanel.HeightSizingMode = SizingMode.Standard;
        this._contentPanel.WidthSizingMode = SizingMode.Standard;
        this._contentPanel.Location = new Point(bounds.X, bounds.Y);
        this._contentPanel.Size = new Point(bounds.Width, bounds.Height);
    }

    private void ClearContentPanel()
    {
        if (this._contentPanel != null)
        {
            this._contentPanel.Hide();
            this._contentPanel.ClearChildren();
            this._contentPanel.Dispose();
            this._contentPanel = null;
        }
    }

    private void RenderAddItem(Panel parent, Rectangle bounds)
    {
        this.CreateContentPanel(parent, bounds);

        TextBox itemName = new TextBox
        {
            Parent = this._contentPanel,
            Top = 25,
            Width = this._contentPanel.ContentRegion.Width,
            PlaceholderText = "Item Name"
        };

        TextBoxSuggestions textBoxSuggestions = new TextBoxSuggestions(itemName, this._contentPanel)
        {
            ZIndex = 10,
            Mode = TextBoxSuggestions.SuggestionMode.Contains,
            StringComparison = StringComparison.InvariantCultureIgnoreCase,
            Suggestions = this._itemService.Items.Where(item => !item.Flags?.Any(flag => flag is Gw2Sharp.WebApi.V2.Models.ItemFlag.AccountBound or Gw2Sharp.WebApi.V2.Models.ItemFlag.SoulbindOnAcquire) ?? false).Select(item => item.Name).ToArray()
        };

        var cancelButton = this.RenderButton(this._contentPanel, "Cancel", () =>
        {
            this.RenderItemOverview(parent, bounds);
        });

        var saveButton = this.RenderButtonAsync(this._contentPanel, "Save", async () =>
        {
            var item = this._itemService.GetItemByName(itemName.Text);
            if (item is null) throw new ArgumentNullException(nameof(item));

            await this._favouriteItemService.AddItem(item.Id);
            this.RenderItemOverview(parent, bounds);
        });

        cancelButton.Bottom = bounds.Bottom - 20;
        cancelButton.Right = bounds.Right - 20;

        saveButton.Bottom = cancelButton.Bottom;
        saveButton.Right = cancelButton.Left - 5;

    }

    private void RenderItemOverview(Panel parent, Rectangle bounds)
    {
        if (this._transactionsService?.Loading ?? false)
        {
            this.ShowInfo("Transaction Service is still loading. No data available yet.");
        }

        this.CreateContentPanel(parent, bounds);

        var itemOverview = new FlowPanel
        {
            Parent = this._contentPanel,
            FlowDirection = ControlFlowDirection.SingleTopToBottom,
            CanScroll = true,
            Height = this._contentPanel.ContentRegion.Height,
            Width = this._contentPanel.ContentRegion.Width,
        };

        var favouritedItems = this._favouriteItemService.GetAll();

        foreach (var item in favouritedItems)
        {
            this.RenderFavouritedItem(item, itemOverview, bounds);
        }
    }

    private void RenderFavouritedItem(int id, FlowPanel parent, Rectangle contentBounds)
    {
        var item = this._itemService.GetItemById(id);
        if (item is null) return;

        var itemPanel = new FlowPanel()
        {
            Parent = parent,
            ShowBorder = true,
            HeightSizingMode = SizingMode.AutoSize,
            FlowDirection = ControlFlowDirection.SingleTopToBottom,
            OuterControlPadding = new Vector2(20,20),

        };
        itemPanel.Width = parent.ContentRegion.Width - (int)parent.OuterControlPadding.X * 2;

        var firstRowPanel = new FlowPanel()
        {
            Parent = itemPanel,
            Width = itemPanel.ContentRegion.Width - (int)itemPanel.OuterControlPadding.X * 2,
            HeightSizingMode = SizingMode.AutoSize,
            FlowDirection = ControlFlowDirection.SingleRightToLeft
        };

        var removeButton = this.RenderButton(firstRowPanel, "Remove", () =>
        {
            this._favouriteItemService.RemoveItem(id);
            this.RenderItemOverview(this._contentPanel.Parent as Panel, contentBounds);
        });

        var nameLabel = this.RenderLabel(firstRowPanel, item.Name).TitleLabel;
        nameLabel.AutoSizeWidth = false;
        nameLabel.Width = (int)(firstRowPanel.ContentRegion.Width - removeButton.Width);
        nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
        nameLabel.Font = GameService.Content.DefaultFont18;

        this.RenderEmptyLine(itemPanel);

        var buyInfos = this.RenderItemInformations(itemPanel, item, TransactionType.Buy);

        this.RenderEmptyLine(itemPanel);

        var sellInfos = this.RenderItemInformations(itemPanel, item, TransactionType.Sell);

        this.RenderEmptyLine(itemPanel, (int)itemPanel.OuterControlPadding.X * 2);
    }

    private Panel RenderItemInformations(Panel parent, Item item, TransactionType type)
    {
        var labelValueXLocation = 250;

        var infoGroup = new FlowPanel
        {
            Parent = parent,
            FlowDirection = ControlFlowDirection.SingleTopToBottom,
            Width = parent.ContentRegion.Width,
            HeightSizingMode= SizingMode.AutoSize,
        };

        PriceRange priceRange = new PriceRange()
        {
            Lowest = 0,
            Highest = 0
        };
        int lowestPriceQuantity = 0;
        int highestPriceQuantity = 0;
        int quantity = 0;

        switch (type)
        {
            case TransactionType.Buy:
                priceRange = this._transactionsService.GetBuyPricesForItem(item.Id);
                quantity = this._transactionsService.GetBuyQuantity(item.Id);
                lowestPriceQuantity = this._transactionsService.GetLowestBuyQuantity(item.Id);
                highestPriceQuantity = this._transactionsService.GetHighestBuyQuantity(item.Id);
                break;
            case TransactionType.Sell:
                priceRange = this._transactionsService.GetSellPricesForItem(item.Id);
                quantity = this._transactionsService.GetSellQuantity(item.Id);
                lowestPriceQuantity = this._transactionsService.GetLowestSellQuantity(item.Id);
                highestPriceQuantity = this._transactionsService.GetHighestSellQuantity(item.Id);
                break;
        }

        var typeName = type.GetTranslatedValue(this.TranslationService);

        #region Price Range
        var priceRangeGroup = new FlowPanel
        {
            Parent = infoGroup,
            FlowDirection = ControlFlowDirection.SingleLeftToRight,
            WidthSizingMode = SizingMode.AutoSize,
            HeightSizingMode = SizingMode.AutoSize,
        };

        var priceRangeLabel = this.RenderLabel(priceRangeGroup, $"{typeName} Price Range:").TitleLabel;
        priceRangeLabel.AutoSizeWidth = false;
        priceRangeLabel.Width = labelValueXLocation;

        var lowestPriceGroup = this.RenderCoins(priceRangeGroup, priceRange.Lowest);

        var toLabel = this.RenderLabel(priceRangeGroup, $"---").TitleLabel;
        toLabel.AutoSizeWidth = false;
        toLabel.Width = 50;
        toLabel.HorizontalAlignment = HorizontalAlignment.Center;

        var highestPriceGroup = this.RenderCoins(priceRangeGroup, priceRange.Highest);
        #endregion


        var quantityGroup = new FlowPanel
        {
            Parent = infoGroup,
            FlowDirection = ControlFlowDirection.SingleTopToBottom,
            WidthSizingMode = SizingMode.AutoSize,
            HeightSizingMode = SizingMode.AutoSize,
        };
        this.RenderLabel(quantityGroup, $"Total {typeName} Quantity:", quantity.ToString(), valueXLocation: labelValueXLocation);
        this.RenderLabel(quantityGroup, $"Quantity of lowest {typeName} Price:", lowestPriceQuantity.ToString(), valueXLocation: labelValueXLocation);
        this.RenderLabel(quantityGroup, $"Quantity of highest {typeName} Price:", highestPriceQuantity.ToString(), valueXLocation: labelValueXLocation);

        return infoGroup;
    }

    private Panel RenderCoins(Panel parent, int coins)
    {
        Panel coinPanel = this.GetPanel(parent);

        (int Gold, int Silver, int Copper) splitCoins = GW2Utils.SplitCoins(coins);

        Label goldInput = new Label
        {
            Parent = coinPanel,
            Location = new Point(0, 0),
            Text = splitCoins.Gold.ToString(),
            AutoSizeWidth = true,
        };
        goldInput.RecalculateLayout();

        Image goldImage = new Image(this.IconService?.GetIcon("090A980A96D39FD36FBB004903644C6DBEFB1FFB/156904") ?? ContentService.Textures.Error)
        {
            Parent = coinPanel,
            Location = new Point(goldInput.Right + 10, 0)
        };

        Label silverInput = new Label
        {
            Parent = coinPanel,
            Location = new Point(goldImage.Right + 10, 0),
            Text = splitCoins.Silver.ToString(),
            AutoSizeWidth = true,
        };
        silverInput.RecalculateLayout();

        Image silverImage = new Image(this.IconService?.GetIcon("E5A2197D78ECE4AE0349C8B3710D033D22DB0DA6/156907") ?? ContentService.Textures.Error)
        {
            Parent = coinPanel,
            Location = new Point(silverInput.Right + 10, 0)
        };

        Label copperInput = new Label
        {
            Parent = coinPanel,
            Location = new Point(silverImage.Right + 10, 0),
            Text = splitCoins.Copper.ToString(),
            AutoSizeWidth = true,
        };
        copperInput.RecalculateLayout();

        Image copperImage = new Image(this.IconService?.GetIcon("6CF8F96A3299CFC75D5CC90617C3C70331A1EF0E/156902") ?? ContentService.Textures.Error)
        {
            Parent = coinPanel,
            Location = new Point(copperInput.Right + 10, 0)
        };

        return coinPanel;
    }

    protected override Task<bool> InternalLoad(IProgress<string> progress) => Task.FromResult(true);
}
