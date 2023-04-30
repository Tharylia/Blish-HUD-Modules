namespace Estreya.BlishHUD.EventTable.UI.Views;

using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Controls.Intern;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.EventTable.Models;
using Estreya.BlishHUD.EventTable.Services;
using Estreya.BlishHUD.Shared.Controls;
using Estreya.BlishHUD.Shared.Models.ArcDPS;
using Estreya.BlishHUD.Shared.Services;
using Estreya.BlishHUD.Shared.UI.Views;
using Estreya.BlishHUD.Shared.Utils;
using Humanizer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.BitmapFonts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Blish_HUD.ContentService;
using Menu = Shared.Controls.Menu;

public class AreaSettingsView : BaseSettingsView
{
    private const int PADDING_X = 20;
    private const int PADDING_Y = 20;

    private readonly Func<IEnumerable<EventAreaConfiguration>> _areaConfigurationFunc;
    private readonly Func<List<EventCategory>> _allEvents;
    private readonly ModuleSettings _moduleSettings;
    private readonly EventStateService _eventStateService;
    private IEnumerable<EventAreaConfiguration> _areaConfigurations;
    private Dictionary<string, MenuItem> _menuItems = new Dictionary<string, MenuItem>();
    private Panel _areaPanel;

    private StandardWindow _manageEventsWindow;
    private StandardWindow _reorderEventsWindow;

    public class AddAreaEventArgs
    {
        public string Name { get; set; }
        public EventAreaConfiguration AreaConfiguration { get; set; }
    }

    public event EventHandler<AddAreaEventArgs> AddArea;
    public event EventHandler<EventAreaConfiguration> RemoveArea;

    public AreaSettingsView(Func<IEnumerable<EventAreaConfiguration>> areaConfiguration, Func<List<EventCategory>> allEvents, ModuleSettings moduleSettings, Gw2ApiManager apiManager, IconService iconService, TranslationService translationService, SettingEventService settingEventService, EventStateService eventStateService, BitmapFont font = null) : base(apiManager, iconService , translationService, settingEventService, font)
    {
        this._areaConfigurationFunc = areaConfiguration;
        this._allEvents = allEvents;
        this._moduleSettings = moduleSettings;
        this._eventStateService = eventStateService;
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

        Rectangle bounds = new Rectangle(PADDING_X, PADDING_Y, newParent.ContentRegion.Width - PADDING_X, newParent.ContentRegion.Height - PADDING_Y * 2);

        Panel areaOverviewPanel = this.GetPanel(newParent);
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
            var menuItem = this._menuItems.First();
            EventAreaConfiguration areaConfiguration = this._areaConfigurations.Where(areaConfiguration => areaConfiguration.Name == menuItem.Key).First();
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

        TextBox areaName = new TextBox()
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

                AddAreaEventArgs addAreaEventArgs = new AddAreaEventArgs()
                {
                    Name = name
                };

                this.AddArea?.Invoke(this, addAreaEventArgs);

                EventAreaConfiguration configuration = addAreaEventArgs.AreaConfiguration ?? throw new ArgumentNullException("Area configuration could not be created.");

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

        Button cancelButton = this.RenderButton(this._areaPanel, this.TranslationService.GetTranslation("areaSettingsView-cancel-btn", "Cancel"), () =>
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

        settingsPanel.DoUpdate(GameService.Overlay.CurrentGameTime); // Dirty trick to get actual height and width

        this.RenderEnabledSettings(settingsPanel, areaConfiguration);

        this.RenderEmptyLine(settingsPanel);

        this.RenderLocationAndSizeSettings(settingsPanel, areaConfiguration);

        this.RenderEmptyLine(settingsPanel);

        this.RenderLayoutSettings(settingsPanel, areaConfiguration);

        this.RenderEmptyLine(settingsPanel);

        this.RenderVisibilitySettings(settingsPanel, areaConfiguration);

        this.RenderEmptyLine(settingsPanel);

        this.RenderTextAndColorSettings(settingsPanel, areaConfiguration);

        this.RenderEmptyLine(settingsPanel);

        this.RenderBehaviourSettings(settingsPanel, areaConfiguration);

        this.RenderEmptyLine(settingsPanel);

        this.RenderFillerSettings(settingsPanel, areaConfiguration);

        this.RenderEmptyLine(settingsPanel);

        var lastAdded = settingsPanel.Children.Last();

        var manageEventsButton = this.RenderButton(this._areaPanel, this.TranslationService.GetTranslation("areaSettingsView-manageEvents-btn", "Manage Events"), () =>
        {
            this.ManageEvents(areaConfiguration);
        });

        manageEventsButton.Top = areaName.Top;
        manageEventsButton.Left = settingsPanel.Left;

        var reorderEventsButton = this.RenderButton(this._areaPanel, this.TranslationService.GetTranslation("areaSettingsView-reorderEvents-btn", "Reorder Events"), () =>
        {
            this.ReorderEvents(areaConfiguration);
        });

        reorderEventsButton.Top = manageEventsButton.Bottom + 2;
        reorderEventsButton.Left = manageEventsButton.Left;

        Button removeButton = this.RenderButtonAsync(this._areaPanel, this.TranslationService.GetTranslation("areaSettingsView-remove-btn", "Remove"), async () =>
        {
            var dialog = new ConfirmDialog(
                    $"Delete Event Area \"{areaConfiguration.Name}\"", $"Your are in the process of deleting the event area \"{areaConfiguration.Name}\".\nThis action will delete all settings.\n\nContinue?",
                    this.IconService,
                    new[]
                    {
                        new ButtonDefinition("Yes", System.Windows.Forms.DialogResult.Yes),
                        new ButtonDefinition("No", System.Windows.Forms.DialogResult.No)
                    })
            {
                SelectedButtonIndex = 1
            };

            var result = await dialog.ShowDialog();
            dialog.Dispose();

            if (result != System.Windows.Forms.DialogResult.Yes) return;

            this.RemoveArea?.Invoke(this, areaConfiguration);
            Menu menu = menuItem.Parent as Menu;
            menu.RemoveChild(menuItem);
            this._menuItems.Remove(areaConfiguration.Name);
            this.ClearAreaPanel();
            this.LoadConfigurations();
        });

        removeButton.Top = areaName.Top;
        removeButton.Right = panelBounds.Right;

        areaName.Left = manageEventsButton.Right;
        areaName.Width = removeButton.Left - areaName.Left;
    }

    private void RenderEnabledSettings(FlowPanel settingsPanel, EventAreaConfiguration areaConfiguration)
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
        this.RenderKeybindingSetting(groupPanel, areaConfiguration.EnabledKeybinding);
        this.RenderEnumSetting(groupPanel, areaConfiguration.DrawInterval);
        this.RenderEmptyLine(groupPanel, 20); // Fake bottom padding
    }

    private void RenderLocationAndSizeSettings(FlowPanel settingsPanel, EventAreaConfiguration areaConfiguration)
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
            Collapsed = true,
            Title = "Location & Size"
        };

        this.RenderIntSetting(groupPanel, areaConfiguration.Location.X);
        this.RenderIntSetting(groupPanel, areaConfiguration.Location.Y);

        this.RenderEmptyLine(groupPanel);

        this.RenderIntSetting(groupPanel, areaConfiguration.Size.X);
        this.RenderIntSetting(groupPanel, areaConfiguration.EventHeight);
        this.RenderEmptyLine(groupPanel, 20); // Fake bottom padding
    }

    private void RenderLayoutSettings(FlowPanel settingsPanel, EventAreaConfiguration areaConfiguration)
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
            Collapsed = true,
            Title = "Layout"
        };

        this.RenderBoolSetting(groupPanel, areaConfiguration.DrawBorders);
        this.RenderEnumSetting(groupPanel, areaConfiguration.BuildDirection);
        this.RenderIntSetting(groupPanel, areaConfiguration.TimeSpan);
        this.RenderIntSetting(groupPanel, areaConfiguration.HistorySplit);

        this.RenderEmptyLine(groupPanel);

        this.RenderBoolSetting(groupPanel, areaConfiguration.ShowCategoryNames);
        this.RenderEmptyLine(groupPanel, 20); // Fake bottom padding
    }

    private void RenderVisibilitySettings(FlowPanel settingsPanel, EventAreaConfiguration areaConfiguration)
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
            Collapsed = true,
            Title = "Visibility"
        };

        this.RenderBoolSetting(groupPanel, areaConfiguration.HideOnMissingMumbleTicks);
        this.RenderBoolSetting(groupPanel, areaConfiguration.HideOnOpenMap);
        this.RenderBoolSetting(groupPanel, areaConfiguration.HideInCombat);
        this.RenderBoolSetting(groupPanel, areaConfiguration.HideInPvE_OpenWorld);
        this.RenderBoolSetting(groupPanel, areaConfiguration.HideInPvE_Competetive);
        this.RenderBoolSetting(groupPanel, areaConfiguration.HideInWvW);
        this.RenderBoolSetting(groupPanel, areaConfiguration.HideInPvP);
        this.RenderEmptyLine(groupPanel, 20); // Fake bottom padding
    }

    private void RenderTextAndColorSettings(FlowPanel settingsPanel, EventAreaConfiguration areaConfiguration)
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
            Collapsed = true,
            Title = "Text & Color"
        };

        this.RenderEnumSetting(groupPanel, areaConfiguration.FontSize);
        this.RenderColorSetting(groupPanel, areaConfiguration.TextColor);
        this.RenderFloatSetting(groupPanel, areaConfiguration.EventTextOpacity);

        this.RenderEmptyLine(groupPanel);

        this.RenderBoolSetting(groupPanel, areaConfiguration.DrawShadows);
        this.RenderColorSetting(groupPanel, areaConfiguration.ShadowColor);
        this.RenderFloatSetting(groupPanel, areaConfiguration.ShadowOpacity);

        this.RenderEmptyLine(groupPanel);

        this.RenderFloatSetting(groupPanel, areaConfiguration.TimeLineOpacity);

        this.RenderEmptyLine(groupPanel);

        this.RenderColorSetting(groupPanel, areaConfiguration.BackgroundColor);
        this.RenderFloatSetting(groupPanel, areaConfiguration.Opacity);
        this.RenderFloatSetting(groupPanel, areaConfiguration.EventBackgroundOpacity);

        this.RenderEmptyLine(groupPanel);

        this.RenderColorSetting(groupPanel, areaConfiguration.CategoryNameColor);

        this.RenderEmptyLine(groupPanel, 20); // Fake bottom padding
    }
    private void RenderBehaviourSettings(FlowPanel settingsPanel, EventAreaConfiguration areaConfiguration)
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
            Collapsed = true,
            Title = "Behaviours"
        };

        this.RenderEnumSetting(groupPanel, areaConfiguration.LeftClickAction);
        this.RenderBoolSetting(groupPanel, areaConfiguration.AcceptWaypointPrompt);
        this.RenderBoolSetting(groupPanel, areaConfiguration.ShowTooltips);

        this.RenderEmptyLine(groupPanel);

        this.RenderEnumSetting(groupPanel, areaConfiguration.CompletionAction);
        this.RenderFloatSetting(groupPanel, areaConfiguration.CompletedEventsBackgroundOpacity);
        this.RenderFloatSetting(groupPanel, areaConfiguration.CompletedEventsTextOpacity);
        this.RenderBoolSetting(groupPanel, areaConfiguration.CompletedEventsInvertTextColor);
        this.RenderButton(groupPanel, "Reset hidden Events", () =>
        {
            this._eventStateService.Remove(areaConfiguration.Name, EventStateService.EventStates.Hidden);
        });

        this.RenderEmptyLine(groupPanel);

        this.RenderBoolSetting(groupPanel, areaConfiguration.LimitToCurrentMap);
        this.RenderBoolSetting(groupPanel, areaConfiguration.AllowUnspecifiedMap);

        this.RenderEmptyLine(groupPanel, 20); // Fake bottom padding
    }
    private void RenderFillerSettings(FlowPanel settingsPanel, EventAreaConfiguration areaConfiguration)
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
            Collapsed = true,
            Title = "Fillers"
        };

        this.RenderBoolSetting(groupPanel, areaConfiguration.UseFiller);
        this.RenderColorSetting(groupPanel, areaConfiguration.FillerTextColor);
        this.RenderFloatSetting(groupPanel, areaConfiguration.FillerTextOpacity);

        this.RenderEmptyLine(groupPanel);

        this.RenderBoolSetting(groupPanel, areaConfiguration.DrawShadowsForFiller);
        this.RenderColorSetting(groupPanel, areaConfiguration.FillerShadowColor);
        this.RenderFloatSetting(groupPanel, areaConfiguration.FillerShadowOpacity);
        this.RenderEmptyLine(groupPanel, 20); // Fake bottom padding
    }

    private void ReorderEvents(EventAreaConfiguration configuration)
    {
        this._reorderEventsWindow ??= WindowUtil.CreateStandardWindow("Reorder Events", this.GetType(), Guid.Parse("b5cbbd99-f02d-4229-8dda-869b42ac242e"), this.IconService);

        if (_reorderEventsWindow.CurrentView != null)
        {
            var reorderEventView = _reorderEventsWindow.CurrentView as ReorderEventsView;
            reorderEventView.SaveClicked -= this.ReorderView_SaveClicked;
        }

        var view = new ReorderEventsView(this._allEvents(), configuration.EventOrder.Value, configuration, this.APIManager, this.IconService, this.TranslationService);
        view.SaveClicked += this.ReorderView_SaveClicked;

        _reorderEventsWindow.Show(view);
    }

    private void ReorderView_SaveClicked(object sender, (EventAreaConfiguration AreaConfiguration, string[] CategoryKeys) e)
    {
        e.AreaConfiguration.EventOrder.Value = new List<string>(e.CategoryKeys);
    }

    private void ManageEvents(EventAreaConfiguration configuration)
    {
        this._manageEventsWindow ??= WindowUtil.CreateStandardWindow("Manage Events", this.GetType(), Guid.Parse("7dc52c82-67ae-4cfb-9fe3-a16a8b30892c"), this.IconService);

        if (_manageEventsWindow.CurrentView != null)
        {
            var manageEventView = _manageEventsWindow.CurrentView as ManageEventsView;
            manageEventView.EventChanged -= this.ManageView_EventChanged;
        }

        var view = new ManageEventsView(this._allEvents(), new Dictionary<string, object>() {
            { "configuration", configuration },
            { "hiddenEventKeys",  this._eventStateService.Instances.Where(x => x.AreaName == configuration.Name && x.State == EventStateService.EventStates.Hidden).Select(x => x.EventKey).ToList() }
        }, () => configuration.DisabledEventKeys.Value, this._moduleSettings, this.APIManager, this.IconService, this.TranslationService);
        view.EventChanged += this.ManageView_EventChanged;

        _manageEventsWindow.Show(view);
    }

    private void ManageView_EventChanged(object sender, ManageEventsView.EventChangedArgs e)
    {
        var configuration = e.AdditionalData["configuration"] as EventAreaConfiguration;
        configuration.DisabledEventKeys.Value = e.NewService
            ? new List<string>(configuration.DisabledEventKeys.Value.Where(aek => aek != e.EventSettingKey))
            : new List<string>(configuration.DisabledEventKeys.Value) { e.EventSettingKey };
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
        this._manageEventsWindow?.Dispose();
        this._manageEventsWindow = null;

        this._reorderEventsWindow?.Dispose();
        this._reorderEventsWindow = null;

    }
}
