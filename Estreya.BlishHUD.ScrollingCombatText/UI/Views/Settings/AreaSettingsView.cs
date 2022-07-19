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
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static Blish_HUD.ContentService;

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

        var bounds = new Rectangle(PADDING_X, PADDING_Y, parent.ContentRegion.Width - PADDING_X, parent.ContentRegion.Height - PADDING_Y * 2);

        var areaOverviewPanel = this.GetPanel(parent);
        areaOverviewPanel.ShowBorder = true;
        areaOverviewPanel.CanScroll = true;
        areaOverviewPanel.HeightSizingMode = SizingMode.Standard;
        areaOverviewPanel.WidthSizingMode = SizingMode.Standard;
        areaOverviewPanel.Location = new Point(bounds.X, bounds.Y);
        areaOverviewPanel.Size = new Point(Panel.MenuStandard.Size.X - 75, bounds.Height - StandardButton.STANDARD_CONTROL_HEIGHT);

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
        var areaPanelBounds = new Rectangle(x, bounds.Y, bounds.Width - x, bounds.Height);

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

        areaName.Width = (panelBounds.Right - 20) - areaName.Left;
    }

    private void BuildEditPanel(Panel parent, Rectangle bounds, MenuItem menuItem, ScrollingTextAreaConfiguration areaConfiguration)
    {
        if (areaConfiguration == null)
        {
            throw new ArgumentNullException(nameof(areaConfiguration));
        }

        this.CreateAreaPanel(parent, bounds);

        var panelBounds = new Rectangle(this._areaPanel.ContentRegion.Location, new Point(this._areaPanel.ContentRegion.Size.X - 50, this._areaPanel.ContentRegion.Size.Y));

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
            HeightSizingMode = SizingMode.Fill,
            WidthSizingMode = SizingMode.Fill,
            FlowDirection = ControlFlowDirection.SingleTopToBottom,
            CanScroll = true
        };

        this.RenderSetting(settingsPanel, areaConfiguration.Enabled);

        this.RenderEmptyLine(settingsPanel);

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

        this.RenderColorSetting(settingsPanel, areaConfiguration.BackgroundColor);
        this.RenderSetting(settingsPanel, areaConfiguration.Opacity);

        this.RenderEmptyLine(settingsPanel);

        #region Categories
        var categoryPanel = this.GetPanel(settingsPanel);

        var categoriesLabel = this.RenderLabel(categoryPanel, "Categories").TitleLabel;
        categoriesLabel.AutoSizeWidth = false;
        categoriesLabel.Width = panelBounds.Width;
        categoriesLabel.HorizontalAlignment = HorizontalAlignment.Center;

        var categorySelectPanel = new FlowPanel()
        {
            Parent = categoryPanel,
            Top = categoriesLabel.Bottom + 20,
            ControlPadding = new Vector2(20, 0),
            FlowDirection = ControlFlowDirection.LeftToRight,
            Width = panelBounds.Width,
            HeightSizingMode = SizingMode.AutoSize
        };
        #endregion

        this.RenderEmptyLine(settingsPanel);

        #region Types
        var typePanel = this.GetPanel(settingsPanel);

        var typesLabel = this.RenderLabel(typePanel, "Types").TitleLabel;
        typesLabel.AutoSizeWidth = false;
        typesLabel.Width = panelBounds.Width;
        typesLabel.HorizontalAlignment = HorizontalAlignment.Center;

        var typeSelectPanel = new FlowPanel()
        {
            Parent = typePanel,
            Top = typesLabel.Bottom + 20,
            ControlPadding = new Vector2(20, 0),
            FlowDirection = ControlFlowDirection.LeftToRight,
            Width = panelBounds.Width,
            HeightSizingMode = SizingMode.AutoSize
        };
        #endregion

        this.RenderEmptyLine(settingsPanel);

        #region Format Rules
        var formatRulesLabel = this.RenderLabel(settingsPanel, "Format Rules").TitleLabel;
        formatRulesLabel.AutoSizeWidth = false;
        formatRulesLabel.Width = panelBounds.Width;
        formatRulesLabel.HorizontalAlignment = HorizontalAlignment.Center;

        this.RenderEmptyLine(settingsPanel);

        var formatRulesPanel = this.GetPanel(settingsPanel);

        #region Add Category Checkboxes
        foreach (var category in (CombatEventCategory[])Enum.GetValues(typeof(CombatEventCategory)))
        {
            var categoryCheckbox = new Checkbox()
            {
                Parent = categorySelectPanel,
                Text = category.Humanize(),
                Checked = areaConfiguration.Categories.Value.Contains(category)
            };

            categoryCheckbox.CheckedChanged += (s, e) =>
            {
                var value = ((CombatEventCategory[])Enum.GetValues(typeof(CombatEventCategory))).ToList().Find(category => category.Humanize() == categoryCheckbox.Text);
                if (categoryCheckbox.Checked)
                {
                    areaConfiguration.Categories.Value = new List<CombatEventCategory>(areaConfiguration.Categories.Value) { value };
                }
                else
                {
                    areaConfiguration.Categories.Value = new List<CombatEventCategory>(areaConfiguration.Categories.Value.Where(category => category != value));
                }

                this.BuildFormatRulesArea(formatRulesPanel, panelBounds, areaConfiguration);
            };
        }

        categorySelectPanel.RecalculateLayout();
        #endregion

        #region Add Types Checkboxes
        foreach (var type in (CombatEventType[])Enum.GetValues(typeof(CombatEventType)))
        {
            if (new CombatEventType[] { CombatEventType.NONE, CombatEventType.BUFF, CombatEventType.MIGHT, CombatEventType.FURY, CombatEventType.REGENERATION, CombatEventType.PROTECTION, CombatEventType.QUICKNESS, CombatEventType.ALACRITY, CombatEventType.VIGOR, CombatEventType.STABILITY, CombatEventType.AEGIS, CombatEventType.SWIFTNESS, CombatEventType.RESISTENCE, CombatEventType.STEALTH, CombatEventType.SUPERSPEED, CombatEventType.RESOLUTION }.Contains(type))
            {
                continue;
            }

            var typeCheckbox = new Checkbox()
            {
                Parent = typeSelectPanel,
                Text = type.Humanize(),
                Checked = areaConfiguration.Types.Value.Contains(type)
            };

            typeCheckbox.CheckedChanged += (s, e) =>
            {
                var value = ((CombatEventType[])Enum.GetValues(typeof(CombatEventType))).ToList().Find(category => category.Humanize() == typeCheckbox.Text);
                if (typeCheckbox.Checked)
                {
                    areaConfiguration.Types.Value = new List<CombatEventType>(areaConfiguration.Types.Value) { value };
                }
                else
                {
                    areaConfiguration.Types.Value = new List<CombatEventType>(areaConfiguration.Types.Value.Where(type => type != value));
                }

                this.BuildFormatRulesArea(formatRulesPanel, panelBounds, areaConfiguration);
            };
        }

        typeSelectPanel.RecalculateLayout();
        #endregion

        this.BuildFormatRulesArea(formatRulesPanel, panelBounds, areaConfiguration);
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
        removeButton.Right = panelBounds.Right;

        areaName.Width = removeButton.Left - areaName.Left;
    }

    private void BuildFormatRulesArea(Panel parent, Rectangle bounds, ScrollingTextAreaConfiguration areaConfiguration)
    {
        parent.ClearChildren();

        var currentFormatRules = areaConfiguration.FormatRules.Value.Where(rule => areaConfiguration.Categories.Value.Contains(rule.Category) && areaConfiguration.Types.Value.Contains(rule.Type)).ToList();

        if (currentFormatRules.Count == 0)
        {
            return;
        }

        Action changedAction = () => areaConfiguration.FormatRules.Value = new List<CombatEventFormatRule>(areaConfiguration.FormatRules.Value);

        var formatRulesMenuPanel = this.GetPanel(parent);
        formatRulesMenuPanel.ShowBorder = true;
        var formatRulesMenu = new Shared.Controls.Menu()
        {
            Parent = formatRulesMenuPanel,
            Width = Panel.MenuStandard.Size.X
        };

        Rectangle formatRuleAreaBounds = new Rectangle(formatRulesMenu.Right + Panel.MenuStandard.PanelOffset.X, formatRulesMenu.Top, bounds.Width - formatRulesMenu.Right - Panel.MenuStandard.PanelOffset.X, 600);

        Panel formatRuleArea = new Panel()
        {
            Parent = parent,
            Location = formatRuleAreaBounds.Location,
            Size = formatRuleAreaBounds.Size
        };

        foreach (var formatRule in currentFormatRules)
        {
            var formatRuleMenuItem = formatRulesMenu.AddMenuItem(formatRule.Name);

            formatRuleMenuItem.Click += (s, e) =>
            {
                formatRuleArea.ClearChildren();
                this.BuildFormatRuleArea(formatRuleArea, formatRuleAreaBounds, formatRule, changedAction);
            };
        }
    }

    private void BuildFormatRuleArea(Panel parent, Rectangle bounds, CombatEventFormatRule formatRule, Action changedAction)
    {
        Label formatName = new Label()
        {
            Parent = parent,
            Font = GameService.Content.DefaultFont18,
            AutoSizeHeight = true,
            Text = formatRule.Name,
            HorizontalAlignment = HorizontalAlignment.Center,
        };

        formatName.Width = bounds.Width;

        var formatRuleTextLabel = this.RenderLabel(parent, "Format").TitleLabel;
        formatRuleTextLabel.Location = new Point(0, formatName.Bottom + 20);

        var formatRuleText = new TextBox
        {
            Parent = parent,
            Location = new Point(formatRuleTextLabel.Right + 20, formatRuleTextLabel.Top),
            Font = GameService.Content.DefaultFont18,
            PlaceholderText = "Format",
            Text = formatRule.Format,
            BasicTooltipText = formatRule.GetType().GetProperty(nameof(formatRule.Format)).GetCustomAttribute<DescriptionAttribute>()?.Description,
        };

        formatRuleText.Width = bounds.Width  - formatRuleText.Left;

        formatRuleText.TextChanged += (s, e) =>
        {
            var valueChangeArgs = e as ValueChangedEventArgs<string>;

            formatRule.Format = valueChangeArgs.NewValue;

            changedAction?.Invoke();
        };

        var formatRuleTextSizeSelectLabel = this.RenderLabel(parent, "Text Size").TitleLabel;
        formatRuleTextSizeSelectLabel.Location = new Point(0, formatRuleTextLabel.Bottom + 20);

        if (formatRule.FontSize == 0)
        {
            formatRule.FontSize = ContentService.FontSize.Size16; // Default to something
            changedAction?.Invoke();
        }

        var formatRuleTextSizeSelect = new Dropdown()
        {
            Parent = parent,
            Location = new Point(formatRuleTextSizeSelectLabel.Right + 20, formatRuleTextSizeSelectLabel.Top),
            SelectedItem = formatRule.FontSize.Humanize()
        };

        formatRuleTextSizeSelect.Width = bounds.Width - formatRuleTextSizeSelect.Left;

        var formatRuleFontSizeOptions = (FontSize[])Enum.GetValues(typeof(FontSize));

        foreach (var formatRuleFontSizeOption in formatRuleFontSizeOptions)
        {
            formatRuleTextSizeSelect.Items.Add(formatRuleFontSizeOption.Humanize());
        }

        formatRuleTextSizeSelect.ValueChanged += (s, e) =>
        {
            formatRule.FontSize = ((FontSize[])Enum.GetValues(typeof(FontSize))).ToList().Find(fontSize => fontSize.Humanize() == e.CurrentValue);
            changedAction?.Invoke();
        };

        var colorLabel = this.RenderLabel(parent, "Text Color").TitleLabel;
        colorLabel.Location = new Point(0, formatRuleTextSizeSelectLabel.Bottom + 20);

        if (formatRule.TextColor == null)
        {
            formatRule.TextColor = this.DefaultColor;
        }

        var colorBox = this.RenderColor(parent, formatRule.TextColor, formatRule.Name, changedColor =>
        {
            formatRule.TextColor = changedColor;
            changedAction?.Invoke();
        });
        colorBox.Location = new Point(colorLabel.Right + 20, colorLabel.Top);
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
