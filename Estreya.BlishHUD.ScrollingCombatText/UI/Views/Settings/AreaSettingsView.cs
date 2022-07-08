namespace Estreya.BlishHUD.ScrollingCombatText.UI.Views.Settings;

using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Estreya.BlishHUD.ScrollingCombatText.Models;
using Estreya.BlishHUD.Shared.Models.ArcDPS;
using Estreya.BlishHUD.Shared.Models.Drawers;
using Estreya.BlishHUD.Shared.Models.GW2API.Commerce;
using Estreya.BlishHUD.Shared.UI.Views;
using Estreya.BlishHUD.Shared.Utils;
using Humanizer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class AreaSettingsView : BaseSettingsView
{
    private const int PADDING_X = 20;
    private const int PADDING_Y = 20;

    private readonly Func<IEnumerable<ScrollingTextAreaConfiguration>> _areaConfigurationFunc;
    private IEnumerable<ScrollingTextAreaConfiguration> _areaConfigurations;
    private Dictionary<string, MenuItem> _menuItems = new Dictionary<string, MenuItem>();
    private Panel _areaPanel;

    public event EventHandler<ScrollingTextAreaConfiguration> AddArea;
    public event EventHandler<ScrollingTextAreaConfiguration> RemoveArea;

    public AreaSettingsView(Func<IEnumerable<ScrollingTextAreaConfiguration>> areaConfiguration)
    {
        this._areaConfigurationFunc = areaConfiguration;
    }

    protected override void BuildView(Panel parent)
    {
        this._areaConfigurations = _areaConfigurationFunc.Invoke().ToList();

        var newParent = this.GetPanel(parent.Parent);
        newParent.Location = parent.Location;
        newParent.Size = parent.Size;
        newParent.HeightSizingMode = parent.HeightSizingMode;
        newParent.WidthSizingMode = parent.WidthSizingMode;

        parent = newParent;

        var bounds = new Rectangle(PADDING_X, PADDING_Y, parent.ContentRegion.Width - PADDING_X * 2, parent.ContentRegion.Height - PADDING_Y * 2);

        var areaOverviewPanel = this.GetPanel(parent);
        areaOverviewPanel.ShowBorder = true;
        areaOverviewPanel.CanScroll = true;
        areaOverviewPanel.HeightSizingMode = SizingMode.Standard;
        areaOverviewPanel.WidthSizingMode = SizingMode.Standard;
        areaOverviewPanel.Location = new Point(bounds.X, bounds.Y);
        areaOverviewPanel.Size = new Point(Panel.MenuStandard.Size.X, bounds.Height - StandardButton.STANDARD_CONTROL_HEIGHT);

        var areaOverviewMenu = new Shared.Controls.Menu
        {
            Parent = areaOverviewPanel,
            WidthSizingMode = SizingMode.Fill
        };

        foreach (var areaConfiguration in this._areaConfigurations)
        {
            var itemName = areaConfiguration.Name;

            if (string.IsNullOrWhiteSpace(itemName)) continue;

            var menuItem = new MenuItem(itemName)
            {
                Parent = areaOverviewMenu,
                Text = itemName,
                WidthSizingMode = SizingMode.Fill,
                HeightSizingMode = SizingMode.AutoSize
            };

            this._menuItems.Add(itemName, menuItem);
        }

        var x = areaOverviewPanel.Right + Panel.MenuStandard.PanelOffset.X;
        var areaPanelBounds = new Rectangle(x, bounds.Y, bounds.Width -  x, bounds.Height);

        this._menuItems.ToList().ForEach(menuItem =>
        {
            menuItem.Value.Click += (s, e) =>
            {
                ScrollingTextAreaConfiguration areaConfiguration = this._areaConfigurations.Where(areaConfiguration => areaConfiguration.Name == menuItem.Key).First();
                this.BuildEditPanel(parent, areaPanelBounds, menuItem.Value, areaConfiguration);
            };
        });

        StandardButton addButton = this.RenderButton(parent, "Add", () =>
        {
            this.BuildAddPanel(parent, areaPanelBounds, areaOverviewMenu);
        });

        addButton.Location = new Point(areaOverviewPanel.Left, areaOverviewPanel.Bottom + 10);
        addButton.Width = areaOverviewPanel.Width;
    }
    private void CreateAreaPanel(Panel parent, Rectangle bounds)
    {
        this.ClearAreaPanel();

        this._areaPanel = this.GetPanel(parent);
        _areaPanel.ShowBorder = true;
        _areaPanel.CanScroll = false; // Should not be needed
        _areaPanel.HeightSizingMode = SizingMode.Standard;
        _areaPanel.WidthSizingMode = SizingMode.Standard;
        _areaPanel.Location = new Point(bounds.X, bounds.Y);
        _areaPanel.Size = new Point(bounds.Width, bounds.Height);
    }

    private void BuildAddPanel(Panel parent, Rectangle bounds, Menu menu)
    {
        this.CreateAreaPanel(parent, bounds);

        var panelBounds = this._areaPanel.ContentRegion;

        TextBox areaName = new TextBox()
        {
            Parent = this._areaPanel,
            Location = new Point(20, 20),
            PlaceholderText = "Area Name"
        };

        StandardButton saveButton = this.RenderButton(this._areaPanel, "Save", () =>
        {
            try
            {
                var name = areaName.Text;

                if (this._areaConfigurations.Any(configuration => configuration.Name == name))
                {
                    this.ShowError("Name already used");
                    return;
                }

                ScrollingTextAreaConfiguration configuration = ScrollingCombatTextModule.ModuleInstance.ModuleSettings.AddDrawer(name);

                configuration.Categories.Value = new List<Shared.Models.ArcDPS.CombatEventCategory>(configuration.Categories.Value) { Shared.Models.ArcDPS.CombatEventCategory.PLAYER_OUT, Shared.Models.ArcDPS.CombatEventCategory.PLAYER_IN, Shared.Models.ArcDPS.CombatEventCategory.PET_OUT, Shared.Models.ArcDPS.CombatEventCategory.PET_IN };
                configuration.Types.Value = new List<Shared.Models.ArcDPS.CombatEventType>((CombatEventType[])Enum.GetValues(typeof(CombatEventType)));

                var menuItem = menu.AddMenuItem(name);
                menuItem.Click += (s, e) =>
                {
                    this.BuildEditPanel(parent, bounds, menuItem, configuration);
                };

                this.AddArea?.Invoke(this, configuration);

                this.BuildEditPanel(parent, bounds, menuItem, configuration);
            }
            catch (Exception ex)
            {
                this.ShowError(ex.Message);
            }
        });
        saveButton.Right = panelBounds.Right - 20;
        saveButton.Bottom = panelBounds.Bottom - 20;

        StandardButton cancelButton = this.RenderButton(this._areaPanel, "Cancel", () =>
        {
            this.ClearAreaPanel();
        });
        cancelButton.Right = saveButton.Left - 10;
        cancelButton.Bottom = panelBounds.Bottom - 20;

        areaName.Width = (panelBounds.Right - 20) - areaName.Left ;
    }

    private void BuildEditPanel(Panel parent, Rectangle bounds, MenuItem menuItem, ScrollingTextAreaConfiguration areaConfiguration)
    {
        if (areaConfiguration == null)
        {
            throw new ArgumentNullException(nameof(areaConfiguration));
        }

        this.CreateAreaPanel(parent, bounds);

        var panelBounds = this._areaPanel.ContentRegion;

        Label areaName = new Label()
        {
            Location = new Point(20, 20),
            Parent = this._areaPanel,
            Font = GameService.Content.DefaultFont18,
            AutoSizeHeight = true,
            Text = areaConfiguration.Name,
            HorizontalAlignment = HorizontalAlignment.Center,
        };

        FlowPanel settingsPanel = new FlowPanel()
        {
            Left = areaName.Left,
            Top = areaName.Bottom + 50,
            Parent = this._areaPanel,
            HeightSizingMode = SizingMode.AutoSize,
            WidthSizingMode = SizingMode.AutoSize,
            FlowDirection = ControlFlowDirection.SingleTopToBottom
        };

        this.RenderSetting(settingsPanel, areaConfiguration.Location.X);
        this.RenderSetting(settingsPanel, areaConfiguration.Location.Y);

        this.RenderEmptyLine(settingsPanel);

        this.RenderSetting(settingsPanel, areaConfiguration.Size.X);
        this.RenderSetting(settingsPanel, areaConfiguration.Size.Y);

        this.RenderEmptyLine(settingsPanel);

        this.RenderSetting(settingsPanel, areaConfiguration.Curve);
        this.RenderSetting(settingsPanel, areaConfiguration.EventHeight);
        this.RenderSetting(settingsPanel, areaConfiguration.ScrollSpeed);

        this.RenderEmptyLine(settingsPanel);

        this.RenderSetting(settingsPanel, areaConfiguration.Opacity);

        this.RenderEmptyLine(settingsPanel);

        this.RenderColorSetting(settingsPanel, areaConfiguration.BackgroundColor);

        this.RenderEmptyLine(settingsPanel);

        this.RenderSetting(settingsPanel, areaConfiguration.FontSize);

        this.RenderEmptyLine(settingsPanel);

        Action<Label> resizeLabelToText = (label) =>
        {
            label.AutoSizeHeight = false;
            var height = (int)label.Font.MeasureString(label.Text).Height;
            if (height <= 0)
            {
                height = label.Font.LineHeight;
            }

            label.Height = height;
        };

        #region Categories
        var categoryPanel = this.GetPanel(settingsPanel);

        Func<ScrollingTextAreaConfiguration, string> getCategoriesAsString = (configuration) => configuration.Categories.Value.Select(category => category.Humanize()).DefaultIfEmpty("----").Aggregate((a, b) => $"{a}, {b}");
       

        var currentCategoriesLabel = this.RenderLabel(categoryPanel, "Categories", getCategoriesAsString.Invoke(areaConfiguration));
        currentCategoriesLabel.WrapText = true;

        Dropdown categorySelect = new Dropdown()
        {
            Parent = categoryPanel,
            Top = currentCategoriesLabel.Bottom + 5
        };

        foreach (var category in (CombatEventCategory[])Enum.GetValues(typeof(CombatEventCategory)))
        {
            categorySelect.Items.Add(category.Humanize());
        }

        categorySelect.SelectedItem = areaConfiguration.Categories.Value.FirstOrDefault().Humanize();

        var addCategoryButton = this.RenderButton(categoryPanel, "Add", () =>
        {
            var newCategory = ((CombatEventCategory[])Enum.GetValues(typeof(CombatEventCategory))).First(category => category.Humanize() == categorySelect.SelectedItem);
            if (!areaConfiguration.Categories.Value.Contains(newCategory))
            {
                areaConfiguration.Categories.Value = new List<CombatEventCategory>(areaConfiguration.Categories.Value) { newCategory };
                currentCategoriesLabel.Text = getCategoriesAsString.Invoke(areaConfiguration);
            }
            else
            {
                this.ShowError("Category already added.");
            }
        });
        addCategoryButton.Left = categorySelect.Right + 5;
        addCategoryButton.Top = categorySelect.Top;

        var removeCategoryButton = this.RenderButton(categoryPanel, "Remove", () =>
        {
            areaConfiguration.Categories.Value = new List<CombatEventCategory>(areaConfiguration.Categories.Value.Where(category => category.Humanize() != categorySelect.SelectedItem));
            currentCategoriesLabel.Text = getCategoriesAsString.Invoke(areaConfiguration);
        });
        removeCategoryButton.Left = addCategoryButton.Right + 5;
        removeCategoryButton.Top = addCategoryButton.Top;
        #endregion

        #region Types
        var typePanel = this.GetPanel(settingsPanel);

        Func<ScrollingTextAreaConfiguration, string> getTypesAsString = (configuration) => configuration.Types.Value.Select(type => type.Humanize()).DefaultIfEmpty("----").Aggregate((a, b) => $"{a}, {b}");


        var currentTypesLabel = this.RenderLabel(typePanel, "Types", getTypesAsString.Invoke(areaConfiguration));
        currentTypesLabel.WrapText = true;

        Dropdown typeSelect = new Dropdown()
        {
            Parent = typePanel,
            Top = currentTypesLabel.Bottom + 5
        };

        foreach (var type in (CombatEventType[])Enum.GetValues(typeof(CombatEventType)))
        {
            typeSelect.Items.Add(type.Humanize());
        }

        typeSelect.SelectedItem = areaConfiguration.Types.Value.FirstOrDefault().Humanize();

        var addTypeButton = this.RenderButton(typePanel, "Add", () =>
        {
            var newType = ((CombatEventType[])Enum.GetValues(typeof(CombatEventType))).First(type => type.Humanize() == typeSelect.SelectedItem);
            if (!areaConfiguration.Types.Value.Contains(newType))
            {
                areaConfiguration.Types.Value = new List<CombatEventType>(areaConfiguration.Types.Value) { newType };
                currentTypesLabel.Text = getTypesAsString.Invoke(areaConfiguration);
            }
            else
            {
                this.ShowError("Type already added.");
            }
        });
        addTypeButton.Left = typeSelect.Right + 5;
        addTypeButton.Top = typeSelect.Top;

        var removeTypeButton = this.RenderButton(typePanel, "Remove", () =>
        {
            areaConfiguration.Types.Value = new List<CombatEventType>(areaConfiguration.Types.Value.Where(type => type.Humanize() != typeSelect.SelectedItem));
            currentTypesLabel.Text = getTypesAsString.Invoke(areaConfiguration);
        });
        removeTypeButton.Left = addTypeButton.Right + 5;
        removeTypeButton.Top = addTypeButton.Top;
        #endregion


        var removeButton = this.RenderButton(this._areaPanel, "Remove", () =>
        {
            this.RemoveArea?.Invoke(this, areaConfiguration);
            var menu = menuItem.Parent as Menu;
            menu.RemoveChild(menuItem);
            this._menuItems.Remove(areaConfiguration.Name);
            this.ClearAreaPanel();
        });

        removeButton.Top = areaName.Top;
        removeButton.Right = panelBounds.Right - 20;

        areaName.Width = removeButton.Left - 20 - panelBounds.Left;


        //StandardButton saveButton = this.RenderButton(this._areaPanel, "Save", () =>
        //{
        //    try
        //    {
        //        var name = areaName.Text;

        //        if (this._areaConfiguration.Any(configuration => configuration.Name == name && configuration.GetHashCode() != areaConfiguration.GetHashCode()))
        //        {
        //            this.ShowError("Name already used");
        //            return;
        //        }

        //        areaConfiguration.Name = name;
        //        areaConfiguration.WishPrice = GW2Utils.ToCoins(int.Parse(goldInput.Text), int.Parse(silverInput.Text), int.Parse(copperInput.Text));
        //    }
        //    catch (Exception ex)
        //    {
        //        this.ShowError(ex.Message);
        //    }
        //});
        //saveButton.Right = panelBounds.Right - 20;
        //saveButton.Bottom = panelBounds.Bottom - 20;

        //StandardButton cancelButton = this.RenderButton(this._areaPanel, "Cancel", () =>
        //{
        //    this.ClearAreaPanel();
        //});
        //cancelButton.Right = saveButton.Left - 10;
        //cancelButton.Bottom = panelBounds.Bottom - 20;

        areaName.Width = removeButton.Left - 20 - areaName.Left;
    }

    private void ClearAreaPanel()
    {
        if (this._areaPanel != null)
        {
            this._areaPanel.Hide();
            this._areaPanel.ClearChildren();
            this._areaPanel.Dispose();
            this._areaPanel = null;
        }
    }

    protected override Task<bool> InternalLoad(IProgress<string> progress)
    {
        return Task.FromResult(true);
    }

    protected override void Unload()
    {
        base.Unload();

        this.ClearAreaPanel();
        _areaConfigurations = null;
        this._menuItems?.Clear();

    }
}
