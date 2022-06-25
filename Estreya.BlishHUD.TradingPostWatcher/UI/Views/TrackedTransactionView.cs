namespace Estreya.BlishHUD.TradingPostWatcher.UI.Views;

using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Estreya.BlishHUD.Shared.Models.GW2API.Commerce;
using Estreya.BlishHUD.Shared.UI.Views;
using Estreya.BlishHUD.Shared.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class TrackedTransactionView : BaseView
{
    private const int PADDING_X = 20;
    private const int PADDING_Y = 20;

    private readonly List<TrackedTransaction> _trackedTransactions;
    private Dictionary<(TransactionType Type, int Id), MenuItem> _menuItems = new Dictionary<(TransactionType Type, int Id), MenuItem>();
    private Panel _transactionPanel;

    public event EventHandler<TrackedTransaction> AddTracking;
    public event EventHandler<TrackedTransaction> RemoveTracking;

    public TrackedTransactionView(List<TrackedTransaction> trackedTransactions)
    {
        this._trackedTransactions = trackedTransactions;
    }

    protected override void DoBuild(Panel parent)
    {
        var bounds = new Rectangle(PADDING_X, PADDING_Y, parent.ContentRegion.Width - (PADDING_X * 2), parent.ContentRegion.Height - (PADDING_Y * 2));

        var trackedOverviewPanel = this.GetPanel(parent);
        trackedOverviewPanel.ShowBorder = true;
        trackedOverviewPanel.CanScroll = true;
        trackedOverviewPanel.HeightSizingMode = SizingMode.Standard;
        trackedOverviewPanel.WidthSizingMode = SizingMode.Standard;
        trackedOverviewPanel.Location = new Point(bounds.X, bounds.Y);
        trackedOverviewPanel.Size = new Point(Panel.MenuStandard.Size.X, bounds.Height - StandardButton.STANDARD_CONTROL_HEIGHT);

        var trackedOverviewMenu = new Controls.Menu
        {
            Parent = trackedOverviewPanel,
            WidthSizingMode = SizingMode.Fill
        };

        foreach (var trackedTransaction in this._trackedTransactions)
        {
            var itemName = trackedTransaction.Item?.Name ?? "Unknown";
            var menuItem = new MenuItem(itemName, TradingPostWatcherModule.ModuleInstance.IconState.GetIcon(trackedTransaction.Item?.Icon))
            {
                Parent = trackedOverviewMenu,
                Text = itemName,
                WidthSizingMode = SizingMode.Fill,
                HeightSizingMode = SizingMode.AutoSize
            };

            this._menuItems.Add((trackedTransaction.Type, trackedTransaction.ItemId), menuItem);
        }

        var x = trackedOverviewPanel.Right + Panel.MenuStandard.PanelOffset.X;
        var transactionPanelBounds = new Rectangle(x, bounds.Y, bounds.Width - x, bounds.Height);

        this._menuItems.ToList().ForEach(menuItem =>
        {
            menuItem.Value.Click += (s, e) =>
            {
                TrackedTransaction trackedTransaction = this._trackedTransactions.Where(transaction => transaction.ItemId == menuItem.Key.Id && transaction.Type == menuItem.Key.Type).First();
                this.BuildEditTransactionPanel(parent, transactionPanelBounds, menuItem.Value, trackedTransaction);
            };
        });

        StandardButton addButton = this.RenderButton(parent, "Add", () =>
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
        _transactionPanel.ShowBorder = true;
        _transactionPanel.CanScroll = false; // Should not be needed
        _transactionPanel.HeightSizingMode = SizingMode.Standard;
        _transactionPanel.WidthSizingMode = SizingMode.Standard;
        _transactionPanel.Location = new Point(bounds.X, bounds.Y);
        _transactionPanel.Size = new Point(bounds.Width, bounds.Height);
    }

    private void BuildAddTransactionPanel(Panel parent, Rectangle bounds, Menu menu)
    {
        this.CreateTransactionPanel(parent, bounds);

        var panelBounds = this._transactionPanel.ContentRegion;

        Image itemImage = new Image()
        {
            Parent = this._transactionPanel,
            Location = new Point(20, 20),
            Size = new Point(32, 32)
        };

        TextBox itemName = new TextBox()
        {
            Parent = this._transactionPanel,
            Top = itemImage.Top,
            Left = itemImage.Right + 10,
            PlaceholderText = "WIP: For now enter the item id"
        };

        (Gw2Sharp.WebApi.V2.Models.Item Item, Texture2D Icon) loadedItem = (null, null);

        Panel coinInputPanel = this.GetPanel(this._transactionPanel);

        TextBox goldInput = new TextBox()
        {
            Parent = coinInputPanel,
            PlaceholderText = "Gold",
            Location = new Point(0, 0),
            Width = 150
        };

        Image goldImage = new Image(this.IconState?.GetIcon("090A980A96D39FD36FBB004903644C6DBEFB1FFB/156904") ?? ContentService.Textures.Error)
        {
            Parent = coinInputPanel,
            Location = new Point(goldInput.Right + 10, 0)
        };

        TextBox silverInput = new TextBox()
        {
            Parent = coinInputPanel,
            PlaceholderText = "Copper",
            Location = new Point(goldImage.Right + 30, 0),
            Width = 150
        };

        Image silverImage = new Image(this.IconState?.GetIcon("E5A2197D78ECE4AE0349C8B3710D033D22DB0DA6/156907") ?? ContentService.Textures.Error)
        {
            Parent = coinInputPanel,
            Location = new Point(silverInput.Right + 10, 0)
        };

        TextBox copperInput = new TextBox()
        {
            Parent = coinInputPanel,
            PlaceholderText = "Copper",
            Location = new Point(silverImage.Right + 30, 0),
            Width = 150
        };

        Image copperImage = new Image(this.IconState?.GetIcon("6CF8F96A3299CFC75D5CC90617C3C70331A1EF0E/156902") ?? ContentService.Textures.Error)
        {
            Parent = coinInputPanel,
            Location = new Point(copperInput.Right + 10, 0)
        };

        Dropdown transactionTypeDropDown = new Dropdown()
        {
            Parent = this._transactionPanel
        };

        foreach (string transactionType in Enum.GetNames(typeof(TransactionType)))
        {
            transactionTypeDropDown.Items.Add(transactionType);
        }

        StandardButton saveButton = this.RenderButton(this._transactionPanel, "Save", () =>
        {
            try
            {
                var type = (TransactionType)Enum.Parse(typeof(TransactionType), transactionTypeDropDown.SelectedItem);

                if (this._trackedTransactions.Any(trackedTransaction => trackedTransaction.Type == type && trackedTransaction.ItemId == loadedItem.Item.Id))
                {
                    this.ShowError("Item already tracked");
                    return;
                }

                TrackedTransaction trackedTransaction = new TrackedTransaction()
                {
                    ItemId = loadedItem.Item.Id,
                    Item = loadedItem.Item,
                    Created = DateTime.UtcNow,
                    Type = type,
                    WishPrice = GW2Utils.ToCoins(int.Parse(goldInput.Text), int.Parse(silverInput.Text), int.Parse(copperInput.Text))
                };

                var menuItem = menu.AddMenuItem(loadedItem.Item.Name, loadedItem.Icon);
                menuItem.Click += (s, e) =>
                {
                    this.BuildEditTransactionPanel(parent, bounds, menuItem, trackedTransaction);
                };

                this.AddTracking?.Invoke(this, trackedTransaction);

                this.ClearTransactionPanel();
            }
            catch (Exception ex)
            {
                this.ShowError(ex.Message);
            }
        });
        saveButton.Enabled = false;
        saveButton.Right = panelBounds.Right - 20;
        saveButton.Bottom = panelBounds.Bottom - 20;

        StandardButton cancelButton = this.RenderButton(this._transactionPanel, "Cancel", () =>
        {
            this.ClearTransactionPanel();
        });
        cancelButton.Right = saveButton.Left - 10;
        cancelButton.Bottom = panelBounds.Bottom - 20;

        StandardButton loadItemButton = this.RenderButtonAsyncWait(this._transactionPanel, "Load Item", async () =>
        {
            // TODO: Get Id from entered item name
            var id = int.Parse(itemName.Text);
            var item = await this.APIManager.Gw2ApiClient.V2.Items.GetAsync(id);
            var itemIcon = await this.IconState?.GetIconAsync(item.Icon) ?? ContentService.Textures.Error;
            loadedItem = (item, itemIcon);

            var itemPrice = await this.APIManager.Gw2ApiClient.V2.Commerce.Prices.GetAsync(id);

            var splitPrice = GW2Utils.SplitCoins(itemPrice.Buys.UnitPrice);

            goldInput.Text = splitPrice.Gold.ToString();
            silverInput.Text = splitPrice.Silver.ToString();
            copperInput.Text = splitPrice.Copper.ToString();

            itemImage.Texture = itemIcon;
            saveButton.Enabled = true;
        });

        loadItemButton.Top = itemName.Top;
        loadItemButton.Right = panelBounds.Right - 20;

        itemName.Width = (loadItemButton.Left - 20) - itemImage.Right;

        coinInputPanel.Top = itemImage.Bottom + 50;
        coinInputPanel.Left = itemImage.Left;
        coinInputPanel.Size = new Point(panelBounds.Width - (copperInput.Left * 2), panelBounds.Height - copperInput.Top);

        transactionTypeDropDown.Top = coinInputPanel.Bottom + 80;
        transactionTypeDropDown.Left = coinInputPanel.Left;
        transactionTypeDropDown.Width = loadItemButton.Right - transactionTypeDropDown.Left;
    }

    private void BuildEditTransactionPanel(Panel parent, Rectangle bounds, MenuItem menuItem, TrackedTransaction trackedTransaction)
    {
        if (trackedTransaction == null)
        {
            throw new ArgumentNullException(nameof(trackedTransaction));
        }

        this.CreateTransactionPanel(parent, bounds);

        var panelBounds = this._transactionPanel.ContentRegion;

        Image itemImage = new Image()
        {
            Parent = this._transactionPanel,
            Location = new Point(20, 20),
            Size = new Point(32, 32)
        };

        itemImage.Texture = this.IconState?.GetIcon(trackedTransaction.Item?.Icon) ?? ContentService.Textures.Error;

        Label itemName = new Label()
        {
            Top = itemImage.Top,
            Parent = this._transactionPanel,
            Font = GameService.Content.DefaultFont18,
            AutoSizeHeight = true,
            Text = trackedTransaction.Item.Name,
            HorizontalAlignment = HorizontalAlignment.Center,
        };

        var removeButton = this.RenderButton(this._transactionPanel, "Remove", () =>
        {
            this.RemoveTracking?.Invoke(this, trackedTransaction);
            var menu = menuItem.Parent as Menu;
            menu.RemoveChild(menuItem);
            this.ClearTransactionPanel();
        });

        removeButton.Top = itemName.Top;
        removeButton.Right = panelBounds.Right - 20;

        itemName.Width = (removeButton.Left - 20) - panelBounds.Left;

        Panel coinInputPanel = this.GetPanel(this._transactionPanel);

        var splitCoins = GW2Utils.SplitCoins(trackedTransaction.WishPrice);

        TextBox goldInput = new TextBox()
        {
            Parent = coinInputPanel,
            PlaceholderText = "Gold",
            Location = new Point(0, 0),
            Width = 150,
            Text = splitCoins.Gold.ToString()
        };

        Image goldImage = new Image(this.IconState?.GetIcon("090A980A96D39FD36FBB004903644C6DBEFB1FFB/156904") ?? ContentService.Textures.Error)
        {
            Parent = coinInputPanel,
            Location = new Point(goldInput.Right + 10, 0)
        };

        TextBox silverInput = new TextBox()
        {
            Parent = coinInputPanel,
            PlaceholderText = "Copper",
            Location = new Point(goldImage.Right + 30, 0),
            Width = 150,
            Text = splitCoins.Silver.ToString()
        };

        Image silverImage = new Image(this.IconState?.GetIcon("E5A2197D78ECE4AE0349C8B3710D033D22DB0DA6/156907") ?? ContentService.Textures.Error)
        {
            Parent = coinInputPanel,
            Location = new Point(silverInput.Right + 10, 0)
        };

        TextBox copperInput = new TextBox()
        {
            Parent = coinInputPanel,
            PlaceholderText = "Copper",
            Location = new Point(silverImage.Right + 30, 0),
            Width = 150,
            Text = splitCoins.Copper.ToString()
        };

        Image copperImage = new Image(this.IconState?.GetIcon("6CF8F96A3299CFC75D5CC90617C3C70331A1EF0E/156902") ?? ContentService.Textures.Error)
        {
            Parent = coinInputPanel,
            Location = new Point(copperInput.Right + 10, 0)
        };

        Dropdown transactionTypeDropDown = new Dropdown()
        {
            Parent = this._transactionPanel
        };

        foreach (string transactionType in Enum.GetNames(typeof(TransactionType)))
        {
            transactionTypeDropDown.Items.Add(transactionType);
        }

        StandardButton saveButton = this.RenderButton(this._transactionPanel, "Save", () =>
        {
            try
            {
                var type = (TransactionType)Enum.Parse(typeof(TransactionType), transactionTypeDropDown.SelectedItem);

                if (this._trackedTransactions.Any(checkTrackedTransaction => checkTrackedTransaction.Type == type && checkTrackedTransaction.ItemId == trackedTransaction.ItemId && checkTrackedTransaction.GetHashCode() != trackedTransaction.GetHashCode()))
                {
                    this.ShowError("Item already tracked");
                    return;
                }

                trackedTransaction.Type = type;
                trackedTransaction.WishPrice = GW2Utils.ToCoins(int.Parse(goldInput.Text), int.Parse(silverInput.Text), int.Parse(copperInput.Text));
            }
            catch (Exception ex)
            {
                this.ShowError(ex.Message);
            }
        });
        saveButton.Right = panelBounds.Right - 20;
        saveButton.Bottom = panelBounds.Bottom - 20;

        StandardButton cancelButton = this.RenderButton(this._transactionPanel, "Cancel", () =>
        {
            this.ClearTransactionPanel();
        });
        cancelButton.Right = saveButton.Left - 10;
        cancelButton.Bottom = panelBounds.Bottom - 20;

        transactionTypeDropDown.SelectedItem = Enum.GetName(typeof(TransactionType), trackedTransaction.Type);

        itemName.Width = (removeButton.Left - 20) - itemImage.Right;

        coinInputPanel.Top = itemImage.Bottom + 50;
        coinInputPanel.Left = itemImage.Left;
        coinInputPanel.Size = new Point(panelBounds.Width - (copperInput.Left * 2), panelBounds.Height - copperInput.Top);

        transactionTypeDropDown.Top = coinInputPanel.Bottom + 80;
        transactionTypeDropDown.Left = coinInputPanel.Left;
        transactionTypeDropDown.Width = removeButton.Right - transactionTypeDropDown.Left;
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
}
