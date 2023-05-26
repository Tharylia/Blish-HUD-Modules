namespace Estreya.BlishHUD.TradingPostWatcher.UI.Views;

using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Models;
using MonoGame.Extended.BitmapFonts;
using Shared.Controls;
using Shared.Services;
using Shared.UI.Views;
using Shared.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dropdown = Shared.Controls.Dropdown;
using Item = Shared.Models.GW2API.Items.Item;
using Menu = Shared.Controls.Menu;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

public class TrackedTransactionView : BaseView
{
    private const int PADDING_X = 20;
    private const int PADDING_Y = 20;

    private readonly Func<List<TrackedTransaction>> _getTrackedTransactions;
    private readonly ItemService _itemService;
    private List<TrackedTransaction> _trackedTransactions;
    private Panel _transactionPanel;

    public TrackedTransactionView(Func<List<TrackedTransaction>> getTrackedTransactions, Gw2ApiManager apiManager, IconService iconService, ItemService itemService, TranslationService translationService, BitmapFont font = null) : base(apiManager, iconService, translationService, font)
    {
        this._getTrackedTransactions = getTrackedTransactions;
        this._itemService = itemService;
    }

    public event EventHandler<TrackedTransaction> AddTracking;
    public event EventHandler<TrackedTransaction> RemoveTracking;

    protected override void InternalBuild(Panel parent)
    {
        Rectangle bounds = new Rectangle(PADDING_X, PADDING_Y, parent.ContentRegion.Width - (PADDING_X * 2), parent.ContentRegion.Height - (PADDING_Y * 2));

        Panel trackedOverviewPanel = this.GetPanel(parent);
        trackedOverviewPanel.ShowBorder = true;
        trackedOverviewPanel.CanScroll = true;
        trackedOverviewPanel.HeightSizingMode = SizingMode.Standard;
        trackedOverviewPanel.WidthSizingMode = SizingMode.Standard;
        trackedOverviewPanel.Location = new Point(bounds.X, bounds.Y);
        trackedOverviewPanel.Size = new Point(Panel.MenuStandard.Size.X, bounds.Height - StandardButton.STANDARD_CONTROL_HEIGHT);

        Menu trackedOverviewMenu = new Menu
        {
            Parent = trackedOverviewPanel,
            WidthSizingMode = SizingMode.Fill
        };

        this._trackedTransactions = this._getTrackedTransactions.Invoke();

        int x = trackedOverviewPanel.Right + Panel.MenuStandard.PanelOffset.X;
        Rectangle transactionPanelBounds = new Rectangle(x, bounds.Y, bounds.Width - x, bounds.Height);

        foreach (TrackedTransaction trackedTransaction in this._trackedTransactions)
        {
            MenuItem menuItem = new MenuItem(this.GetMenuItemText(trackedTransaction), this.IconService.GetIcon(trackedTransaction.Item?.Icon))
            {
                Parent = trackedOverviewMenu,
                WidthSizingMode = SizingMode.Fill,
                HeightSizingMode = SizingMode.AutoSize
            };

            menuItem.Click += (s, e) =>
            {
                this.BuildEditTransactionPanel(parent, transactionPanelBounds, menuItem, trackedTransaction);
            };
        }

        Button addButton = this.RenderButton(parent, "Add", () =>
        {
            this.BuildAddTransactionPanel(parent, transactionPanelBounds, trackedOverviewMenu);
        });

        addButton.Location = new Point(trackedOverviewPanel.Left, trackedOverviewPanel.Bottom + 10);
        addButton.Width = trackedOverviewPanel.Width;
    }

    private void CreateTransactionPanel(Panel parent, Rectangle bounds)
    {
        this.ClearTransactionPanel();

        this._transactionPanel = this.GetPanel(parent);
        this._transactionPanel.ShowBorder = true;
        this._transactionPanel.CanScroll = false; // Should not be needed
        this._transactionPanel.HeightSizingMode = SizingMode.Standard;
        this._transactionPanel.WidthSizingMode = SizingMode.Standard;
        this._transactionPanel.Location = new Point(bounds.X, bounds.Y);
        this._transactionPanel.Size = new Point(bounds.Width, bounds.Height);
    }

    private void BuildAddTransactionPanel(Panel parent, Rectangle bounds, Menu menu)
    {
        this.CreateTransactionPanel(parent, bounds);

        Rectangle panelBounds = this._transactionPanel.ContentRegion;

        Image itemImage = new Image
        {
            Parent = this._transactionPanel,
            Location = new Point(20, 20),
            Size = new Point(32, 32)
        };

        TextBox itemName = new TextBox
        {
            Parent = this._transactionPanel,
            Top = itemImage.Top,
            Left = itemImage.Right + 10,
            PlaceholderText = "Item Name"
        };

        TextBoxSuggestions textBoxSuggestions = new TextBoxSuggestions(itemName, this._transactionPanel)
        {
            ZIndex = 10,
            Mode = TextBoxSuggestions.SuggestionMode.Contains,
            StringComparison = StringComparison.InvariantCultureIgnoreCase
        };

        textBoxSuggestions.Suggestions = this._itemService.Items.Where(item => !item.Flags?.Any(flag => flag is ItemFlag.AccountBound or ItemFlag.SoulbindOnAcquire) ?? false).Select(item => item.Name).ToArray();

        (Shared.Models.GW2API.Items.Item Item, Texture2D Icon) loadedItem = (null, null);

        Panel coinInputPanel = this.GetPanel(this._transactionPanel);

        TextBox goldInput = new TextBox
        {
            Parent = coinInputPanel,
            PlaceholderText = "Gold",
            Location = new Point(0, 0),
            Width = 150
        };

        Image goldImage = new Image(this.IconService?.GetIcon("090A980A96D39FD36FBB004903644C6DBEFB1FFB/156904") ?? ContentService.Textures.Error)
        {
            Parent = coinInputPanel,
            Location = new Point(goldInput.Right + 10, 0)
        };

        TextBox silverInput = new TextBox
        {
            Parent = coinInputPanel,
            PlaceholderText = "Copper",
            Location = new Point(goldImage.Right + 30, 0),
            Width = 150
        };

        Image silverImage = new Image(this.IconService?.GetIcon("E5A2197D78ECE4AE0349C8B3710D033D22DB0DA6/156907") ?? ContentService.Textures.Error)
        {
            Parent = coinInputPanel,
            Location = new Point(silverInput.Right + 10, 0)
        };

        TextBox copperInput = new TextBox
        {
            Parent = coinInputPanel,
            PlaceholderText = "Copper",
            Location = new Point(silverImage.Right + 30, 0),
            Width = 150
        };

        Image copperImage = new Image(this.IconService?.GetIcon("6CF8F96A3299CFC75D5CC90617C3C70331A1EF0E/156902") ?? ContentService.Textures.Error)
        {
            Parent = coinInputPanel,
            Location = new Point(copperInput.Right + 10, 0)
        };

        TrackedTransactionType trackedTransactionType = TrackedTransactionType.BuyGT;
        Dropdown transactionTypeDropDown = this.RenderDropdown<TrackedTransactionType>(this._transactionPanel, Point.Zero, 1, trackedTransactionType, onChangeAction: val =>
        {
            trackedTransactionType = val;
        });

        Label typeDescription = new Label
        {
            Parent = this._transactionPanel,
            AutoSizeHeight = true,
            Text = "Buy >=: Tracks the price of the highest buy orders. Notifies when prices are greater or equal to the wish price.\n" +
                   "Buy <=: Tracks the price of the highest buy orders. Notifies when prices are less or equal to the wish price.\n" +
                   "Sell >=: Tracks the price of the lowest sell offers. Notifies when prices are higher or equal to the wish price.\n" +
                   "Sell <=: Tracks the price of the lowest sell offers. Notifies when prices are less or equal to the wish price."
        };

        Button saveButton = this.RenderButton(this._transactionPanel, "Save", () =>
        {
            if (this._trackedTransactions.Any(trackedTransaction => trackedTransaction.Type == trackedTransactionType && trackedTransaction.ItemId == loadedItem.Item.Id))
            {
                this.ShowError("Item already tracked");
                return;
            }

            TrackedTransaction newTrackedTransaction = new TrackedTransaction
            {
                ItemId = loadedItem.Item.Id,
                Item = loadedItem.Item,
                Created = DateTime.UtcNow,
                Type = trackedTransactionType,
                WishPrice = GW2Utils.ToCoins(int.Parse(goldInput.Text), int.Parse(silverInput.Text), int.Parse(copperInput.Text))
            };

            MenuItem menuItem = menu.AddMenuItem(this.GetMenuItemText(newTrackedTransaction), loadedItem.Icon);
            menuItem.Click += (s, e) =>
            {
                this.BuildEditTransactionPanel(parent, bounds, menuItem, newTrackedTransaction);
            };

            this.AddTracking?.Invoke(this, newTrackedTransaction);

            this.ClearTransactionPanel();
        });
        saveButton.Enabled = false;
        saveButton.Right = panelBounds.Right - 20;
        saveButton.Bottom = panelBounds.Bottom - 20;

        Button cancelButton = this.RenderButton(this._transactionPanel, "Cancel", () =>
        {
            this.ClearTransactionPanel();
        });
        cancelButton.Right = saveButton.Left - 10;
        cancelButton.Bottom = panelBounds.Bottom - 20;

        Button loadItemButton = this.RenderButtonAsync(this._transactionPanel, "Load Item", async () =>
        {
            if (this._itemService?.Loading ?? true)
            {
                throw new Exception("Items are still being loaded.");
            }

            Item item = this._itemService?.GetItemByName(itemName.Text.Trim()) ?? throw new Exception($"The name \"{itemName.Text}\" is not a valid item.");

            AsyncTexture2D itemIcon = this.IconService?.GetIcon(item.Icon) ?? ContentService.Textures.Error;
            loadedItem = (item, itemIcon);

            CommercePrices itemPrice = null;

            try
            {
                itemPrice = await this.APIManager.Gw2ApiClient.V2.Commerce.Prices.GetAsync(item.Id);
            }
            catch (Exception)
            {
                throw new Exception("Could not load item price.");
            }

            (int Gold, int Silver, int Copper) splitPrice = GW2Utils.SplitCoins(itemPrice.Sells.UnitPrice);

            goldInput.Text = splitPrice.Gold.ToString();
            silverInput.Text = splitPrice.Silver.ToString();
            copperInput.Text = splitPrice.Copper.ToString();

            itemImage.Texture = itemIcon;
            saveButton.Enabled = true;
        });

        loadItemButton.Top = itemName.Top;
        loadItemButton.Right = panelBounds.Right - 20;

        itemName.Width = loadItemButton.Left - 20 - itemImage.Right;

        coinInputPanel.Top = itemImage.Bottom + 50;
        coinInputPanel.Left = itemImage.Left;
        coinInputPanel.Size = new Point(panelBounds.Width - (copperInput.Left * 2), panelBounds.Height - copperInput.Top);

        transactionTypeDropDown.Top = coinInputPanel.Bottom + 80;
        transactionTypeDropDown.Left = coinInputPanel.Left;
        transactionTypeDropDown.Width = loadItemButton.Right - transactionTypeDropDown.Left;

        typeDescription.Top = transactionTypeDropDown.Bottom + 20;
        typeDescription.Left = transactionTypeDropDown.Left;
        typeDescription.Width = loadItemButton.Right - transactionTypeDropDown.Left;
    }

    private void BuildEditTransactionPanel(Panel parent, Rectangle bounds, MenuItem menuItem, TrackedTransaction trackedTransaction)
    {
        if (trackedTransaction == null)
        {
            throw new ArgumentNullException(nameof(trackedTransaction));
        }

        this.CreateTransactionPanel(parent, bounds);

        Rectangle panelBounds = this._transactionPanel.ContentRegion;

        Image itemImage = new Image
        {
            Parent = this._transactionPanel,
            Location = new Point(20, 20),
            Size = new Point(32, 32),
            Texture = this.IconService?.GetIcon(trackedTransaction.Item?.Icon) ?? ContentService.Textures.Error
        };

        Label itemName = new Label
        {
            Top = itemImage.Top,
            Parent = this._transactionPanel,
            Font = GameService.Content.DefaultFont18,
            AutoSizeHeight = true,
            Text = trackedTransaction.Item.Name,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        Button removeButton = this.RenderButton(this._transactionPanel, "Remove", () =>
        {
            this.RemoveTracking?.Invoke(this, trackedTransaction);
            Menu menu = menuItem.Parent as Menu;
            menu.RemoveChild(menuItem);
            this.ClearTransactionPanel();
        });

        removeButton.Top = itemName.Top;
        removeButton.Right = panelBounds.Right - 20;

        itemName.Width = removeButton.Left - 20 - itemImage.Right;

        Panel coinInputPanel = this.GetPanel(this._transactionPanel);

        (int Gold, int Silver, int Copper) splitCoins = GW2Utils.SplitCoins(trackedTransaction.WishPrice);

        TextBox goldInput = new TextBox
        {
            Parent = coinInputPanel,
            PlaceholderText = "Gold",
            Location = new Point(0, 0),
            Width = 150,
            Text = splitCoins.Gold.ToString()
        };

        Image goldImage = new Image(this.IconService?.GetIcon("090A980A96D39FD36FBB004903644C6DBEFB1FFB/156904") ?? ContentService.Textures.Error)
        {
            Parent = coinInputPanel,
            Location = new Point(goldInput.Right + 10, 0)
        };

        TextBox silverInput = new TextBox
        {
            Parent = coinInputPanel,
            PlaceholderText = "Copper",
            Location = new Point(goldImage.Right + 30, 0),
            Width = 150,
            Text = splitCoins.Silver.ToString()
        };

        Image silverImage = new Image(this.IconService?.GetIcon("E5A2197D78ECE4AE0349C8B3710D033D22DB0DA6/156907") ?? ContentService.Textures.Error)
        {
            Parent = coinInputPanel,
            Location = new Point(silverInput.Right + 10, 0)
        };

        TextBox copperInput = new TextBox
        {
            Parent = coinInputPanel,
            PlaceholderText = "Copper",
            Location = new Point(silverImage.Right + 30, 0),
            Width = 150,
            Text = splitCoins.Copper.ToString()
        };

        Image copperImage = new Image(this.IconService?.GetIcon("6CF8F96A3299CFC75D5CC90617C3C70331A1EF0E/156902") ?? ContentService.Textures.Error)
        {
            Parent = coinInputPanel,
            Location = new Point(copperInput.Right + 10, 0)
        };

        coinInputPanel.Top = itemImage.Bottom + 50;
        coinInputPanel.Left = itemImage.Left;
        coinInputPanel.Size = new Point(panelBounds.Width - (copperInput.Left * 2), panelBounds.Height - copperInput.Top);

        TrackedTransactionType trackedTransactionType = trackedTransaction.Type;
        Dropdown transactionTypeDropDown = this.RenderDropdown<TrackedTransactionType>(this._transactionPanel, new Point(coinInputPanel.Left, coinInputPanel.Bottom + 80), removeButton.Right - coinInputPanel.Left, trackedTransactionType, onChangeAction: val =>
        {
            trackedTransactionType = val;
        });

        Label typeDescription = new Label
        {
            Parent = this._transactionPanel,
            Top = transactionTypeDropDown.Bottom + 20,
            Left = transactionTypeDropDown.Left,
            Width = removeButton.Right - transactionTypeDropDown.Left,
            AutoSizeHeight = true,
            WrapText = true,
            Font = GameService.Content.DefaultFont18,
            Text = "Buy >=: Tracks the price of the highest buy orders. Notifies when prices are greater or equal to the wish price.\n" +
                   "Buy <=: Tracks the price of the highest buy orders. Notifies when prices are less or equal to the wish price.\n" +
                   "Sell >=: Tracks the price of the lowest sell offers. Notifies when prices are higher or equal to the wish price.\n" +
                   "Sell <=: Tracks the price of the lowest sell offers. Notifies when prices are less or equal to the wish price."
        };

        Button saveButton = this.RenderButton(this._transactionPanel, "Save", () =>
        {
            try
            {
                if (this._trackedTransactions.Any(checkTransaction => checkTransaction.Type == trackedTransactionType && checkTransaction.ItemId == trackedTransaction.ItemId && !checkTransaction.Equals(trackedTransaction)))
                {
                    this.ShowError("Item already tracked");
                    return;
                }

                trackedTransaction.Type = trackedTransactionType;
                trackedTransaction.WishPrice = GW2Utils.ToCoins(int.Parse(goldInput.Text), int.Parse(silverInput.Text), int.Parse(copperInput.Text));
                menuItem.Text = this.GetMenuItemText(trackedTransaction);
            }
            catch (Exception ex)
            {
                this.ShowError(ex.Message);
            }
        });
        saveButton.Right = panelBounds.Right - 20;
        saveButton.Bottom = panelBounds.Bottom - 20;

        Button cancelButton = this.RenderButton(this._transactionPanel, "Cancel", () =>
        {
            this.ClearTransactionPanel();
        });
        cancelButton.Right = saveButton.Left - 10;
        cancelButton.Bottom = panelBounds.Bottom - 20;
    }

    private string GetMenuItemText(TrackedTransaction trackedTransaction)
    {
        string typeName = null;

        switch (trackedTransaction.Type)
        {
            case TrackedTransactionType.BuyGT:
                typeName = "B >=";
                break;
            case TrackedTransactionType.BuyLT:
                typeName = "B <=";
                break;
            case TrackedTransactionType.SellGT:
                typeName = "S >=";
                break;
            case TrackedTransactionType.SellLT:
                typeName = "S <=";
                break;
        }

        return $"{typeName} : {trackedTransaction.Item?.Name ?? "Unknown"}";
    }

    private void ClearTransactionPanel()
    {
        if (this._transactionPanel != null)
        {
            this._transactionPanel.Hide();
            this._transactionPanel.ClearChildren();
            this._transactionPanel.Dispose();
            this._transactionPanel = null;
        }
    }

    protected override Task<bool> InternalLoad(IProgress<string> progress)
    {
        return Task.FromResult(true);
    }

    protected override void Unload()
    {
        base.Unload();

        this._trackedTransactions = null;

        this.ClearTransactionPanel();
    }
}