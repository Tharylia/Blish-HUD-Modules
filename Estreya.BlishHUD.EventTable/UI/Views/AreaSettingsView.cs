namespace Estreya.BlishHUD.EventTable.UI.Views;

using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.EventTable.Models;
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

    private readonly Func<IEnumerable<EventAreaConfiguration>> _areaConfigurationFunc;
    private IEnumerable<EventAreaConfiguration> _areaConfigurations;
    private Dictionary<string, MenuItem> _menuItems = new Dictionary<string, MenuItem>();
    private Panel _areaPanel;

    public class AddAreaEventArgs
    {
        public string Name { get; set; }
        public EventAreaConfiguration AreaConfiguration { get; set; }
    }

    public event EventHandler<AddAreaEventArgs> AddArea;
    public event EventHandler<EventAreaConfiguration> RemoveArea;

    public AreaSettingsView(Func<IEnumerable<EventAreaConfiguration>> areaConfiguration, Gw2ApiManager apiManager, IconState iconState, BitmapFont font = null) : base(apiManager, iconState, font)
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

        foreach (EventAreaConfiguration areaConfiguration in this._areaConfigurations)
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
                EventAreaConfiguration areaConfiguration = this._areaConfigurations.Where(areaConfiguration => areaConfiguration.Name == menuItem.Key).First();
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

                EventAreaConfiguration configuration = addAreaEventArgs.AreaConfiguration;

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

    private void BuildEditPanel(Panel parent, Rectangle bounds, MenuItem menuItem, EventAreaConfiguration areaConfiguration)
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
        //this.RenderIntSetting(settingsPanel, areaConfiguration.Size.Y);

        //this.RenderEmptyLine(settingsPanel);

        //this.RenderEnumSetting(settingsPanel, areaConfiguration.Curve);
        //this.RenderIntSetting(settingsPanel, areaConfiguration.EventHeight);
        //this.RenderFloatSetting(settingsPanel, areaConfiguration.ScrollSpeed);

        this.RenderEmptyLine(settingsPanel);

        this.RenderColorSetting(settingsPanel, areaConfiguration.BackgroundColor);
        this.RenderFloatSetting(settingsPanel, areaConfiguration.Opacity);

        this.RenderEmptyLine(settingsPanel);


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
