namespace Estreya.BlishHUD.UniversalSearch.UI.Views.Settings;

using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.Shared.Controls;
using Estreya.BlishHUD.Shared.Services;
using Estreya.BlishHUD.Shared.UI.Views;
using Estreya.BlishHUD.UniversalSearch.Models;
using Estreya.BlishHUD.UniversalSearch.Services.SearchHandlers;
using Microsoft.Xna.Framework;
using MonoGame.Extended.BitmapFonts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Menu = Shared.Controls.Menu;

public class SearchHandlerSettingsView : BaseSettingsView
{
    private const int PADDING_X = 20;
    private const int PADDING_Y = 20;

    private readonly Func<IEnumerable<SearchHandlerConfiguration>> _areaConfigurationFunc;
    private readonly ModuleSettings _moduleSettings;
    private IEnumerable<SearchHandlerConfiguration> _areaConfigurations;
    private Dictionary<string, MenuItem> _menuItems = new Dictionary<string, MenuItem>();
    private Panel _areaPanel;

    public SearchHandlerSettingsView(Func<IEnumerable<SearchHandlerConfiguration>> areaConfiguration,  ModuleSettings moduleSettings, Gw2ApiManager apiManager, IconService iconService, TranslationService translationService, SettingEventService settingEventService, BitmapFont font = null) : base(apiManager, iconService, translationService, settingEventService, font)
    {
        this._areaConfigurationFunc = areaConfiguration;
        this._moduleSettings = moduleSettings;
    }

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

        Rectangle bounds = new Rectangle(PADDING_X, PADDING_Y, newParent.ContentRegion.Width - PADDING_X, newParent.ContentRegion.Height - PADDING_Y );

        Panel areaOverviewPanel = this.GetPanel(newParent);
        areaOverviewPanel.ShowBorder = true;
        areaOverviewPanel.CanScroll = true;
        areaOverviewPanel.HeightSizingMode = SizingMode.Standard;
        areaOverviewPanel.WidthSizingMode = SizingMode.Standard;
        areaOverviewPanel.Location = new Point(bounds.X, bounds.Y);
        areaOverviewPanel.Size = new Point(Panel.MenuStandard.Size.X - 75, bounds.Height);

        Shared.Controls.Menu areaOverviewMenu = new Shared.Controls.Menu
        {
            Parent = areaOverviewPanel,
            WidthSizingMode = SizingMode.Fill
        };

        foreach (SearchHandlerConfiguration areaConfiguration in this._areaConfigurations)
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
                SearchHandlerConfiguration areaConfiguration = this._areaConfigurations.Where(areaConfiguration => areaConfiguration.Name == menuItem.Key).First();
                this.BuildEditPanel(newParent, areaPanelBounds, menuItem.Value, areaConfiguration);
            };
        });

        if (this._menuItems.Count > 0)
        {
            var menuItem = this._menuItems.First();
            SearchHandlerConfiguration areaConfiguration = this._areaConfigurations.Where(areaConfiguration => areaConfiguration.Name == menuItem.Key).First();
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

    private void BuildEditPanel(Panel parent, Rectangle bounds, MenuItem menuItem, SearchHandlerConfiguration areaConfiguration)
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

        settingsPanel.DoUpdate(GameService.Overlay.CurrentGameTime); // Dirty trick to get actual height and width

        this.RenderEnabledSettings(settingsPanel, areaConfiguration);

        this.RenderEmptyLine(settingsPanel);

        this.RenderBehaviourSettings(settingsPanel, areaConfiguration);

        this.RenderEmptyLine(settingsPanel);

        var lastAdded = settingsPanel.Children.Last();

        areaName.Left = 0;
        areaName.Width = this._areaPanel.ContentRegion.Width;
    }

    private void RenderEnabledSettings(FlowPanel settingsPanel, SearchHandlerConfiguration areaConfiguration)
    {
        FlowPanel groupPanel = new FlowPanel()
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
        this.RenderEmptyLine(groupPanel, 20); // Fake bottom padding
    }

    private void RenderBehaviourSettings(FlowPanel settingsPanel, SearchHandlerConfiguration areaConfiguration)
    {
        FlowPanel groupPanel = new FlowPanel()
        {
            Parent = settingsPanel,
            HeightSizingMode = SizingMode.AutoSize,
            Width = settingsPanel.Width - 30,
            FlowDirection = ControlFlowDirection.SingleTopToBottom,
            OuterControlPadding = new Vector2(20, 20),
            ShowBorder = true,
            CanCollapse = true,
            //Collapsed = true, // False until more settings added
            Title = "Behaviours"
        };

        this.RenderEnumSetting(groupPanel, areaConfiguration.SearchMode);

        this.RenderEmptyLine(groupPanel);

        this.RenderIntSetting(groupPanel, areaConfiguration.MaxSearchResults);

        this.RenderEmptyLine(groupPanel);

        this.RenderBoolSetting(groupPanel, areaConfiguration.IncludeBrokenItem, async (oldVal,newVal) =>
        {
            if (!newVal) return true;

            var confirmDialog = new ConfirmDialog($"Activate \"{areaConfiguration.IncludeBrokenItem.DisplayName}\"?",
                $"You are in the process of activating \"{areaConfiguration.IncludeBrokenItem.DisplayName}\".\n" +
                $"This will in some way mess with your shown results.\n\n" +
                $"Do you really want to enable it?",
                this.IconService);

            var result = await confirmDialog.ShowDialog();
            return result == System.Windows.Forms.DialogResult.OK;
        });

        this.RenderEmptyLine(groupPanel);

        this.RenderBoolSetting(groupPanel, areaConfiguration.NotifyOnCopy);
        this.RenderBoolSetting(groupPanel, areaConfiguration.CloseWindowAfterCopy);
        this.RenderBoolSetting(groupPanel, areaConfiguration.PasteInChatAfterCopy);

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
}
