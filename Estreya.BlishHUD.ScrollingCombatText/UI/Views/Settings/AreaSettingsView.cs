namespace Estreya.BlishHUD.ScrollingCombatText.UI.Views.Settings;

using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.ScrollingCombatText.Models;
using Estreya.BlishHUD.Shared.Models.ArcDPS;
using Estreya.BlishHUD.Shared.State;
using Estreya.BlishHUD.Shared.UI.Views;
using Humanizer;
using Microsoft.Xna.Framework;
using MonoGame.Extended.BitmapFonts;
using System;
using System.Collections.Generic;
using System.Linq;
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

    public class AddAreaEventArgs
    {
        public string Name { get; set; }
        public ScrollingTextAreaConfiguration AreaConfiguration { get; set; }
    }

    public event EventHandler<AddAreaEventArgs> AddArea;
    public event EventHandler<ScrollingTextAreaConfiguration> RemoveArea;

    public AreaSettingsView(Func<IEnumerable<ScrollingTextAreaConfiguration>> areaConfiguration, Gw2ApiManager apiManager, IconState iconState, BitmapFont font = null) : base(apiManager, iconState, font)
    {
        this._areaConfigurationFunc = areaConfiguration;
    }

    private void LoadConfigurations()
    {
        this._areaConfigurations = this._areaConfigurationFunc.Invoke().ToList();
    }

    protected override void BuildView(Panel parent)
    {
        this.LoadConfigurations();

        Panel newParent = this.GetPanel(parent.Parent);
        newParent.Location = parent.Location;
        newParent.Size = parent.Size;
        newParent.HeightSizingMode = parent.HeightSizingMode;
        newParent.WidthSizingMode = parent.WidthSizingMode;

        parent = newParent;

        Rectangle bounds = new Rectangle(PADDING_X, PADDING_Y, parent.ContentRegion.Width - PADDING_X, parent.ContentRegion.Height - PADDING_Y * 2);

        Panel areaOverviewPanel = this.GetPanel(parent);
        areaOverviewPanel.ShowBorder = true;
        areaOverviewPanel.CanScroll = true;
        areaOverviewPanel.HeightSizingMode = SizingMode.Standard;
        areaOverviewPanel.WidthSizingMode = SizingMode.Standard;
        areaOverviewPanel.Location = new Point(bounds.X, bounds.Y);
        areaOverviewPanel.Size = new Point(Panel.MenuStandard.Size.X - 75, bounds.Height - StandardButton.STANDARD_CONTROL_HEIGHT);

        Shared.Controls.Menu areaOverviewMenu = new Shared.Controls.Menu
        {
            Parent = areaOverviewPanel,
            WidthSizingMode = SizingMode.Fill
        };

        foreach (ScrollingTextAreaConfiguration areaConfiguration in this._areaConfigurations)
        {
            string itemName = areaConfiguration.Name;

            if (string.IsNullOrWhiteSpace(itemName))
            {
                continue;
            }

            MenuItem menuItem = new MenuItem(itemName)
            {
                Parent = areaOverviewMenu,
                Text = itemName,
                WidthSizingMode = SizingMode.Fill,
                HeightSizingMode = SizingMode.AutoSize
            };

            this._menuItems.Add(itemName, menuItem);
        }

        int x = areaOverviewPanel.Right + Panel.MenuStandard.PanelOffset.X;
        Rectangle areaPanelBounds = new Rectangle(x, bounds.Y, bounds.Width - x, bounds.Height);

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
        this._areaPanel.ShowBorder = true;
        this._areaPanel.CanScroll = false; // Should not be needed
        this._areaPanel.HeightSizingMode = SizingMode.Standard;
        this._areaPanel.WidthSizingMode = SizingMode.Standard;
        this._areaPanel.Location = new Point(bounds.X, bounds.Y);
        this._areaPanel.Size = new Point(bounds.Width, bounds.Height);
    }

    private void BuildAddPanel(Panel parent, Rectangle bounds, Menu menu)
    {
        this.CreateAreaPanel(parent, bounds);

        Rectangle panelBounds = this._areaPanel.ContentRegion;

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
                string name = areaName.Text;

                if (this._areaConfigurations.Any(configuration => configuration.Name == name))
                {
                    this.ShowError("Name already used");
                    return;
                }

                AddAreaEventArgs addAreaEventArgs = new AddAreaEventArgs()
                {
                    Name = name
                };

                this.AddArea?.Invoke(this, addAreaEventArgs);

                ScrollingTextAreaConfiguration configuration = addAreaEventArgs.AreaConfiguration;

                if (configuration == null)
                {
                    throw new ArgumentNullException("Area configuration could not be created.");
                }

                MenuItem menuItem = menu.AddMenuItem(name);
                menuItem.Click += (s, e) =>
                {
                    this.BuildEditPanel(parent, bounds, menuItem, configuration);
                };

                this.BuildEditPanel(parent, bounds, menuItem, configuration);
            }
            catch (Exception ex)
            {
                this.ShowError(ex.Message);
            }
        });
        saveButton.Enabled = false;
        saveButton.Right = panelBounds.Right - 20;
        saveButton.Bottom = panelBounds.Bottom - 20;

        areaName.TextChanged += (s, e) =>
        {
            var textBox = s as TextBox;
            saveButton.Enabled = !string.IsNullOrWhiteSpace(textBox.Text);
        };

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

        Rectangle panelBounds = new Rectangle(this._areaPanel.ContentRegion.Location, new Point(this._areaPanel.ContentRegion.Size.X - 50, this._areaPanel.ContentRegion.Size.Y));

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

        this.RenderBoolSetting(settingsPanel, areaConfiguration.Enabled);
        this.RenderEmptyLine(settingsPanel);

        this.RenderIntSetting(settingsPanel, areaConfiguration.Location.X);
        this.RenderIntSetting(settingsPanel, areaConfiguration.Location.Y);

        this.RenderEmptyLine(settingsPanel);

        this.RenderIntSetting(settingsPanel, areaConfiguration.Size.X);
        this.RenderIntSetting(settingsPanel, areaConfiguration.Size.Y);

        this.RenderEmptyLine(settingsPanel);

        this.RenderEnumSetting(settingsPanel, areaConfiguration.Curve);
        this.RenderIntSetting(settingsPanel, areaConfiguration.EventHeight);
        this.RenderFloatSetting(settingsPanel, areaConfiguration.ScrollSpeed);

        this.RenderEmptyLine(settingsPanel);

        this.RenderColorSetting(settingsPanel, areaConfiguration.BackgroundColor);
        this.RenderFloatSetting(settingsPanel, areaConfiguration.Opacity);

        this.RenderEmptyLine(settingsPanel);

        #region Categories
        Panel categoryPanel = this.GetPanel(settingsPanel);

        Label categoriesLabel = this.RenderLabel(categoryPanel, "Categories").TitleLabel;
        categoriesLabel.AutoSizeWidth = false;
        categoriesLabel.Width = panelBounds.Width;
        categoriesLabel.HorizontalAlignment = HorizontalAlignment.Center;

        FlowPanel categorySelectPanel = new FlowPanel()
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
        Panel typePanel = this.GetPanel(settingsPanel);

        Label typesLabel = this.RenderLabel(typePanel, "Types").TitleLabel;
        typesLabel.AutoSizeWidth = false;
        typesLabel.Width = panelBounds.Width;
        typesLabel.HorizontalAlignment = HorizontalAlignment.Center;

        FlowPanel typeSelectPanel = new FlowPanel()
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
        Label formatRulesLabel = this.RenderLabel(settingsPanel, "Format Rules").TitleLabel;
        formatRulesLabel.AutoSizeWidth = false;
        formatRulesLabel.Width = panelBounds.Width;
        formatRulesLabel.HorizontalAlignment = HorizontalAlignment.Center;

        this.RenderEmptyLine(settingsPanel);

        Panel formatRulesPanel = this.GetPanel(settingsPanel);

        #region Add Category Checkboxes
        foreach (CombatEventCategory category in (CombatEventCategory[])Enum.GetValues(typeof(CombatEventCategory)))
        {
            Checkbox categoryCheckbox = new Checkbox()
            {
                Parent = categorySelectPanel,
                Text = category.Humanize(),
                Checked = areaConfiguration.Categories.Value.Contains(category)
            };

            categoryCheckbox.CheckedChanged += (s, e) =>
            {
                CombatEventCategory value = ((CombatEventCategory[])Enum.GetValues(typeof(CombatEventCategory))).ToList().Find(category => category.Humanize() == categoryCheckbox.Text);
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
        foreach (CombatEventType type in (CombatEventType[])Enum.GetValues(typeof(CombatEventType)))
        {
            if (new CombatEventType[] { CombatEventType.NONE, CombatEventType.BUFF, CombatEventType.MIGHT, CombatEventType.FURY, CombatEventType.REGENERATION, CombatEventType.PROTECTION, CombatEventType.QUICKNESS, CombatEventType.ALACRITY, CombatEventType.VIGOR, CombatEventType.STABILITY, CombatEventType.AEGIS, CombatEventType.SWIFTNESS, CombatEventType.RESISTENCE, CombatEventType.STEALTH, CombatEventType.SUPERSPEED, CombatEventType.RESOLUTION }.Contains(type))
            {
                continue;
            }

            Checkbox typeCheckbox = new Checkbox()
            {
                Parent = typeSelectPanel,
                Text = type.Humanize(),
                Checked = areaConfiguration.Types.Value.Contains(type)
            };

            typeCheckbox.CheckedChanged += (s, e) =>
            {
                CombatEventType value = ((CombatEventType[])Enum.GetValues(typeof(CombatEventType))).ToList().Find(category => category.Humanize() == typeCheckbox.Text);
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

        StandardButton removeButton = this.RenderButton(this._areaPanel, "Remove", () =>
        {
            this.RemoveArea?.Invoke(this, areaConfiguration);
            Menu menu = menuItem.Parent as Menu;
            menu.RemoveChild(menuItem);
            this._menuItems.Remove(areaConfiguration.Name);
            this.ClearAreaPanel();
            this.LoadConfigurations();
        });

        removeButton.Top = areaName.Top;
        removeButton.Right = panelBounds.Right;

        areaName.Width = removeButton.Left - areaName.Left;
    }

    private void BuildFormatRulesArea(Panel parent, Rectangle bounds, ScrollingTextAreaConfiguration areaConfiguration)
    {
        parent.ClearChildren();

        List<CombatEventFormatRule> currentFormatRules = areaConfiguration.FormatRules.Value.Where(rule => areaConfiguration.Categories.Value.Contains(rule.Category) && areaConfiguration.Types.Value.Contains(rule.Type)).ToList();

        if (currentFormatRules.Count == 0)
        {
            return;
        }

        Action changedAction = () => areaConfiguration.FormatRules.Value = new List<CombatEventFormatRule>(areaConfiguration.FormatRules.Value);

        Panel formatRulesMenuPanel = this.GetPanel(parent);
        formatRulesMenuPanel.ShowBorder = true;
        Shared.Controls.Menu formatRulesMenu = new Shared.Controls.Menu()
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

        foreach (CombatEventFormatRule formatRule in currentFormatRules)
        {
            MenuItem formatRuleMenuItem = formatRulesMenu.AddMenuItem(formatRule.Name);

            formatRuleMenuItem.Click += (s, e) =>
            {
                formatRuleArea.Children?.ToList().ForEach(child => child.Dispose());
                formatRuleArea.ClearChildren();
                this.BuildFormatRuleArea(formatRuleArea, formatRuleAreaBounds, formatRule, changedAction);
            };
        }
    }

    private void BuildFormatRuleArea(Panel parent, Rectangle bounds, CombatEventFormatRule formatRule, Action changedAction)
    {
        Label formatName = new Label
        {
            Parent = parent,
            Font = GameService.Content.DefaultFont18,
            AutoSizeHeight = true,
            Text = formatRule.Name,
            HorizontalAlignment = HorizontalAlignment.Center,
            Width = bounds.Width
        };

        Label formatRuleTextLabel = this.RenderLabel(parent, "Format").TitleLabel;

        int formatRuleTextX = formatRuleTextLabel.Right + 40;

        formatRuleTextLabel.Location = new Point(0, formatName.Bottom + 20);

        TextBox formatRuleText = this.RenderTextbox(
            parent,
    new Point(formatRuleTextX, formatRuleTextLabel.Top),
       bounds.Width - formatRuleTextX,
       formatRule.Format,
       "Format",
       newValue =>
       {
           formatRule.Format = newValue;

           changedAction?.Invoke();
       });

        Label formatRuleTextSizeSelectLabel = this.RenderLabel(parent, "Text Size").TitleLabel;
        formatRuleTextSizeSelectLabel.Location = new Point(0, formatRuleTextLabel.Bottom + 20);

        if (formatRule.FontSize == 0)
        {
            formatRule.FontSize = ContentService.FontSize.Size16; // Default to something
            changedAction?.Invoke();
        }

        Dropdown formatRuleTextSizeSelect = this.RenderDropdown(
             parent,
            new Point(formatRuleText.Left, formatRuleTextSizeSelectLabel.Top),
            bounds.Width - formatRuleText.Left,
            ((FontSize[])Enum.GetValues(typeof(FontSize))).Select(enumValue => enumValue.Humanize()).ToArray(),
            formatRule.FontSize.Humanize(),
           newValue =>
           {
               formatRule.FontSize = ((FontSize[])Enum.GetValues(typeof(FontSize))).ToList().Find(fontSize => fontSize.Humanize() == newValue);
               changedAction?.Invoke();
           });

        Label colorLabel = this.RenderLabel(parent, "Text Color").TitleLabel;
        colorLabel.Location = new Point(0, formatRuleTextSizeSelectLabel.Bottom + 20);

        formatRule.TextColor ??= this.DefaultColor;

        ColorBox colorBox = this.RenderColorBox(
            parent,
            new Point(formatRuleText.Left, colorLabel.Top),
            formatRule.TextColor,
            changedColor =>
            {
                formatRule.TextColor = changedColor;
                changedAction?.Invoke();
            },
            selectorPanel: this.MainPanel,
            innerSelectorPanelPadding: new Thickness(20, 20));

        StandardButton suggestBetterColorButton = this.RenderButton(parent, "Suggest better format colors", 
            () => ScrollingCombatTextModule.ModuleInstance.GitHubHelper.OpenIssueWindow("Color ... would look better for ..."));

        suggestBetterColorButton.Location = new Point(0, colorLabel.Bottom + 20);
    }

    private void ClearAreaPanel()
    {
        if (this._areaPanel != null)
        {
            this._areaPanel.Hide();
            this._areaPanel.Children?.ToList().ForEach(child => child.Dispose());
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
        this._areaConfigurations = null;
        this._menuItems?.Clear();

    }
}
