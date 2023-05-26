namespace Estreya.BlishHUD.FoodReminder.UI.Views;

using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Microsoft.Xna.Framework;
using Models;
using MonoGame.Extended.BitmapFonts;
using Shared.Controls;
using Shared.UI.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Button = Shared.Controls.Button;
using Control = Blish_HUD.Controls.Control;
using HorizontalAlignment = Blish_HUD.Controls.HorizontalAlignment;
using Label = Blish_HUD.Controls.Label;
using Menu = Shared.Controls.Menu;
using MenuItem = Blish_HUD.Controls.MenuItem;
using Panel = Blish_HUD.Controls.Panel;
using TextBox = Blish_HUD.Controls.TextBox;

public class AreaSettingsView : BaseSettingsView
{
    private const int PADDING_X = 20;
    private const int PADDING_Y = 20;

    private readonly Func<IEnumerable<OverviewDrawerConfiguration>> _areaConfigurationFunc;
    private readonly ModuleSettings _moduleSettings;
    private IEnumerable<OverviewDrawerConfiguration> _areaConfigurations;
    private Panel _areaPanel;
    private readonly Dictionary<string, MenuItem> _menuItems = new Dictionary<string, MenuItem>();

    public AreaSettingsView(Func<IEnumerable<OverviewDrawerConfiguration>> areaConfiguration, ModuleSettings moduleSettings, Gw2ApiManager apiManager, IconService iconService, TranslationService translationService, SettingEventService settingEventService, BitmapFont font = null) : base(apiManager, iconService, translationService, settingEventService, font)
    {
        this._areaConfigurationFunc = areaConfiguration;
        this._moduleSettings = moduleSettings;
    }

    public event EventHandler<AddAreaEventArgs> AddArea;
    public event EventHandler<OverviewDrawerConfiguration> RemoveArea;

    private void LoadConfigurations()
    {
        this._areaConfigurations = this._areaConfigurationFunc.Invoke().ToList();
    }

    protected override void BuildView(FlowPanel parent)
    {
        this.LoadConfigurations();

        Panel newParent = this.GetPanel(parent.Parent);
        newParent.Location = parent.Location;
        newParent.Size = parent.Size;
        newParent.HeightSizingMode = parent.HeightSizingMode;
        newParent.WidthSizingMode = parent.WidthSizingMode;

        Rectangle bounds = new Rectangle(PADDING_X, PADDING_Y, newParent.ContentRegion.Width - PADDING_X, newParent.ContentRegion.Height - (PADDING_Y * 2));

        Panel areaOverviewPanel = this.GetPanel(newParent);
        areaOverviewPanel.ShowBorder = true;
        areaOverviewPanel.CanScroll = true;
        areaOverviewPanel.HeightSizingMode = SizingMode.Standard;
        areaOverviewPanel.WidthSizingMode = SizingMode.Standard;
        areaOverviewPanel.Location = new Point(bounds.X, bounds.Y);
        areaOverviewPanel.Size = new Point(Panel.MenuStandard.Size.X - 75, bounds.Height - StandardButton.STANDARD_CONTROL_HEIGHT);

        Menu areaOverviewMenu = new Menu
        {
            Parent = areaOverviewPanel,
            WidthSizingMode = SizingMode.Fill
        };

        foreach (OverviewDrawerConfiguration areaConfiguration in this._areaConfigurations)
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
                OverviewDrawerConfiguration areaConfiguration = this._areaConfigurations.Where(areaConfiguration => areaConfiguration.Name == menuItem.Key).First();
                this.BuildEditPanel(newParent, areaPanelBounds, menuItem.Value, areaConfiguration);
            };
        });

        Button addButton = this.RenderButton(newParent, this.TranslationService.GetTranslation("areaSettingsView-add-btn", "Add"), () =>
        {
            this.BuildAddPanel(newParent, areaPanelBounds, areaOverviewMenu);
        });

        addButton.Location = new Point(areaOverviewPanel.Left, areaOverviewPanel.Bottom + 10);
        addButton.Width = areaOverviewPanel.Width;

        if (this._menuItems.Count > 0)
        {
            KeyValuePair<string, MenuItem> menuItem = this._menuItems.First();
            OverviewDrawerConfiguration areaConfiguration = this._areaConfigurations.Where(areaConfiguration => areaConfiguration.Name == menuItem.Key).First();
            this.BuildEditPanel(newParent, areaPanelBounds, menuItem.Value, areaConfiguration);
        }
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

        TextBox areaName = new TextBox
        {
            Parent = this._areaPanel,
            Location = new Point(20, 20),
            PlaceholderText = "Area Name"
        };

        Button saveButton = this.RenderButton(this._areaPanel, this.TranslationService.GetTranslation("areaSettingsView-save-btn", "Save"), () =>
        {
            try
            {
                string name = areaName.Text;

                if (this._areaConfigurations.Any(configuration => configuration.Name == name))
                {
                    this.ShowError("Name already used");
                    return;
                }

                AddAreaEventArgs addAreaEventArgs = new AddAreaEventArgs { Name = name };

                this.AddArea?.Invoke(this, addAreaEventArgs);

                OverviewDrawerConfiguration configuration = addAreaEventArgs.AreaConfiguration ?? throw new ArgumentNullException("Area configuration could not be created.");
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
            TextBox textBox = s as TextBox;
            saveButton.Enabled = !string.IsNullOrWhiteSpace(textBox.Text);
        };

        Button cancelButton = this.RenderButton(this._areaPanel, this.TranslationService.GetTranslation("areaSettingsView-cancel-btn", "Cancel"), () =>
        {
            this.ClearAreaPanel();
        });
        cancelButton.Right = saveButton.Left - 10;
        cancelButton.Bottom = panelBounds.Bottom - 20;

        areaName.Width = panelBounds.Right - 20 - areaName.Left;
    }

    private void BuildEditPanel(Panel parent, Rectangle bounds, MenuItem menuItem, OverviewDrawerConfiguration areaConfiguration)
    {
        if (areaConfiguration == null)
        {
            throw new ArgumentNullException(nameof(areaConfiguration));
        }

        this.CreateAreaPanel(parent, bounds);

        Rectangle panelBounds = new Rectangle(this._areaPanel.ContentRegion.Location, new Point(this._areaPanel.ContentRegion.Size.X - 50, this._areaPanel.ContentRegion.Size.Y));

        Label areaName = new Label
        {
            Location = new Point(20, 20),
            Parent = this._areaPanel,
            Font = GameService.Content.DefaultFont18,
            AutoSizeHeight = true,
            Text = areaConfiguration.Name,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        FlowPanel settingsPanel = new FlowPanel
        {
            Left = areaName.Left,
            Top = areaName.Bottom + 50,
            Parent = this._areaPanel,
            HeightSizingMode = SizingMode.Fill,
            WidthSizingMode = SizingMode.Fill,
            FlowDirection = ControlFlowDirection.SingleTopToBottom,
            CanScroll = true
        };

        settingsPanel.DoUpdate(GameService.Overlay.CurrentGameTime); // Dirty trick to get actual height and width

        this.RenderEnabledSettings(settingsPanel, areaConfiguration);

        this.RenderEmptyLine(settingsPanel);

        this.RenderLocationAndSizeSettings(settingsPanel, areaConfiguration);

        this.RenderEmptyLine(settingsPanel);

        this.RenderTextAndColorSettings(settingsPanel, areaConfiguration);

        this.RenderEmptyLine(settingsPanel);

        this.RenderLayoutSettings(settingsPanel, areaConfiguration);

        this.RenderEmptyLine(settingsPanel);

        Control lastAdded = settingsPanel.Children.Last();

        Button removeButton = this.RenderButtonAsync(this._areaPanel, this.TranslationService.GetTranslation("areaSettingsView-remove-btn", "Remove"), async () =>
        {
            ConfirmDialog dialog = new ConfirmDialog(
                $"Delete Event Area \"{areaConfiguration.Name}\"", $"Your are in the process of deleting the event area \"{areaConfiguration.Name}\".\nThis action will delete all settings.\n\nContinue?",
                this.IconService,
                new[]
                {
                    new ButtonDefinition("Yes", DialogResult.Yes),
                    new ButtonDefinition("No", DialogResult.No)
                }) { SelectedButtonIndex = 1 };

            DialogResult result = await dialog.ShowDialog();
            dialog.Dispose();

            if (result != DialogResult.Yes)
            {
                return;
            }

            this.RemoveArea?.Invoke(this, areaConfiguration);
            Menu menu = menuItem.Parent as Menu;
            menu.RemoveChild(menuItem);
            this._menuItems.Remove(areaConfiguration.Name);
            this.ClearAreaPanel();
            this.LoadConfigurations();
        });

        removeButton.Top = areaName.Top;
        removeButton.Right = panelBounds.Right;

        //areaName.Left = 0;
        areaName.Width = removeButton.Left - areaName.Left;
    }

    private void RenderEnabledSettings(FlowPanel settingsPanel, OverviewDrawerConfiguration areaConfiguration)
    {
        FlowPanel groupPanel = new FlowPanel
        {
            Parent = settingsPanel,
            HeightSizingMode = SizingMode.AutoSize,
            Width = settingsPanel.Width - 30,
            FlowDirection = ControlFlowDirection.SingleTopToBottom,
            OuterControlPadding = new Vector2(20, 20),
            ShowBorder = true,
            CanCollapse = true,
            Collapsed = false,
            Title = "Enabled"
        };

        this.RenderBoolSetting(groupPanel, areaConfiguration.Enabled);
        this.RenderKeybindingSetting(groupPanel, areaConfiguration.EnabledKeybinding);
        //this.RenderEnumSetting(groupPanel, areaConfiguration.DrawInterval);
        this.RenderEmptyLine(groupPanel, 20); // Fake bottom padding
    }

    private void RenderLocationAndSizeSettings(FlowPanel settingsPanel, OverviewDrawerConfiguration areaConfiguration)
    {
        FlowPanel groupPanel = new FlowPanel
        {
            Parent = settingsPanel,
            HeightSizingMode = SizingMode.AutoSize,
            Width = settingsPanel.Width - 30,
            FlowDirection = ControlFlowDirection.SingleTopToBottom,
            OuterControlPadding = new Vector2(20, 20),
            ShowBorder = true,
            CanCollapse = true,
            Collapsed = true,
            Title = "Location & Size"
        };

        this.RenderIntSetting(groupPanel, areaConfiguration.Location.X);
        this.RenderIntSetting(groupPanel, areaConfiguration.Location.Y);

        this.RenderEmptyLine(groupPanel);

        this.RenderIntSetting(groupPanel, areaConfiguration.Size.Y);
        //this.RenderIntSetting(groupPanel, areaConfiguration.EventHeight);
        this.RenderEmptyLine(groupPanel, 20); // Fake bottom padding
    }

    private void RenderTextAndColorSettings(FlowPanel settingsPanel, OverviewDrawerConfiguration areaConfiguration)
    {
        FlowPanel groupPanel = new FlowPanel
        {
            Parent = settingsPanel,
            HeightSizingMode = SizingMode.AutoSize,
            Width = settingsPanel.Width - 30,
            FlowDirection = ControlFlowDirection.SingleTopToBottom,
            OuterControlPadding = new Vector2(20, 20),
            ShowBorder = true,
            CanCollapse = true,
            Collapsed = true,
            Title = "Text & Color"
        };

        this.RenderEnumSetting(groupPanel, areaConfiguration.FontSize);
        this.RenderColorSetting(groupPanel, areaConfiguration.TextColor);

        this.RenderEmptyLine(groupPanel);

        this.RenderColorSetting(groupPanel, areaConfiguration.BackgroundColor);
        this.RenderFloatSetting(groupPanel, areaConfiguration.Opacity);

        this.RenderEmptyLine(groupPanel, 20); // Fake bottom padding
    }

    private void RenderLayoutSettings(FlowPanel settingsPanel, OverviewDrawerConfiguration areaConfiguration)
    {
        FlowPanel groupPanel = new FlowPanel
        {
            Parent = settingsPanel,
            HeightSizingMode = SizingMode.AutoSize,
            Width = settingsPanel.Width - 30,
            FlowDirection = ControlFlowDirection.SingleTopToBottom,
            OuterControlPadding = new Vector2(20, 20),
            ShowBorder = true,
            CanCollapse = true,
            Collapsed = true,
            Title = "Layout"
        };

        this.RenderIntSetting(groupPanel, areaConfiguration.HeaderHeight);
        this.RenderIntSetting(groupPanel, areaConfiguration.PlayerHeight);

        this.RenderEmptyLine(groupPanel);

        this.RenderFloatSetting(groupPanel, areaConfiguration.ColumnSizes.Name);
        this.RenderFloatSetting(groupPanel, areaConfiguration.ColumnSizes.Food);
        this.RenderFloatSetting(groupPanel, areaConfiguration.ColumnSizes.Utility);
        this.RenderFloatSetting(groupPanel, areaConfiguration.ColumnSizes.Reinforced);

        this.RenderEmptyLine(groupPanel, 20); // Fake bottom padding
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

    public class AddAreaEventArgs
    {
        public string Name { get; set; }
        public OverviewDrawerConfiguration AreaConfiguration { get; set; }
    }
}