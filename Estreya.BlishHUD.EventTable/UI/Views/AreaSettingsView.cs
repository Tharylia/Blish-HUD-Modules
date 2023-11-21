namespace Estreya.BlishHUD.EventTable.UI.Views;

using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.Shared.Controls.Input;
using Estreya.BlishHUD.Shared.Extensions;
using Estreya.BlishHUD.Shared.Threading.Events;
using Microsoft.Xna.Framework;
using Models;
using MonoGame.Extended.BitmapFonts;
using Services;
using Shared.Controls;
using Shared.Services;
using Shared.UI.Views;
using Shared.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;
using Button = Shared.Controls.Button;
using Control = Blish_HUD.Controls.Control;
using HorizontalAlignment = Blish_HUD.Controls.HorizontalAlignment;
using Label = Blish_HUD.Controls.Label;
using Menu = Shared.Controls.Menu;
using MenuItem = Blish_HUD.Controls.MenuItem;
using Panel = Blish_HUD.Controls.Panel;
using StandardWindow = Shared.Controls.StandardWindow;
using TextBox = Blish_HUD.Controls.TextBox;

public class AreaSettingsView : BaseSettingsView
{
    private const int PADDING_X = 20;
    private const int PADDING_Y = 20;
    private readonly Func<List<EventCategory>> _allEvents;

    private readonly Func<IEnumerable<EventAreaConfiguration>> _areaConfigurationFunc;
    private readonly EventStateService _eventStateService;
    private readonly ModuleSettings _moduleSettings;
    private IEnumerable<EventAreaConfiguration> _areaConfigurations;
    private Panel _areaPanel;

    private StandardWindow _manageEventsWindow;
    private Dictionary<string, MenuItem> _menuItems;
    private StandardWindow _reorderEventsWindow;

    public AreaSettingsView(Func<IEnumerable<EventAreaConfiguration>> areaConfiguration, Func<List<EventCategory>> allEvents, ModuleSettings moduleSettings, Gw2ApiManager apiManager, IconService iconService, TranslationService translationService, SettingEventService settingEventService, EventStateService eventStateService) : base(apiManager, iconService, translationService, settingEventService)
    {
        this._areaConfigurationFunc = areaConfiguration;
        this._allEvents = allEvents;
        this._moduleSettings = moduleSettings;
        this._eventStateService = eventStateService;
    }

    public event EventHandler<AddAreaEventArgs> AddArea;
    public event EventHandler<EventAreaConfiguration> RemoveArea;
    public event AsyncEventHandler<EventAreaConfiguration> SyncEnabledEventsToReminders;
    public event AsyncEventHandler<EventAreaConfiguration> SyncEnabledEventsFromReminders;
    public event AsyncEventHandler<EventAreaConfiguration> SyncEnabledEventsToOtherAreas;

    private void LoadConfigurations()
    {
        this._areaConfigurations = this._areaConfigurationFunc.Invoke().ToList();
    }

    protected override void BuildView(FlowPanel parent)
    {
        this._menuItems = new Dictionary<string, MenuItem>();

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
            KeyValuePair<string, MenuItem> menuItem = this._menuItems.First();
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

        TextBox areaName = new TextBox
        {
            Parent = this._areaPanel,
            Location = new Point(20, 20),
            PlaceholderText = "Area Name"
        };

        var copyFromTemplateLabel = this.RenderLabel(this._areaPanel, "Template").TitleLabel;
        copyFromTemplateLabel.Location = new Point(areaName.Left, areaName.Bottom + 20);
        copyFromTemplateLabel.Width = this.LABEL_WIDTH;
        Dropdown<string> copyFromTemplate = this.RenderDropdown<string>(this._areaPanel,
            new Point(copyFromTemplateLabel.Right + 20, copyFromTemplateLabel.Top),
            350,
            this._areaConfigurations.Select(x => x.Name).ToArray(),
            null);

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

                var copyFromTemplateName = copyFromTemplate.SelectedItem;
                if (copyFromTemplateName != null && !this._areaConfigurations.Any(x => x.Name == copyFromTemplateName))
                {
                    this.ShowError("Selected template does not exist.");
                    return;
                }

                AddAreaEventArgs addAreaEventArgs = new AddAreaEventArgs { Name = name };

                this.AddArea?.Invoke(this, addAreaEventArgs);

                EventAreaConfiguration configuration = addAreaEventArgs.AreaConfiguration ?? throw new ArgumentNullException("Area configuration could not be created.");

                if (copyFromTemplateName != null)
                {
                    var template = this._areaConfigurations.First(x => x.Name == copyFromTemplateName);
                    template.CopyTo(configuration);
                }

                this.LoadConfigurations();

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

    private void BuildEditPanel(Panel parent, Rectangle bounds, MenuItem menuItem, EventAreaConfiguration areaConfiguration)
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

        this.RenderGeneralSettings(settingsPanel, areaConfiguration);

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

        this.RenderSynchronizationSettings(settingsPanel, areaConfiguration);

        this.RenderEmptyLine(settingsPanel);

        Control lastAdded = settingsPanel.Children.Last();

        Button manageEventsButton = this.RenderButton(this._areaPanel, this.TranslationService.GetTranslation("areaSettingsView-manageEvents-btn", "Manage Events"), () =>
        {
            this.ManageEvents(areaConfiguration);
        });

        manageEventsButton.Top = areaName.Top;
        manageEventsButton.Left = settingsPanel.Left;

        Button reorderEventsButton = this.RenderButton(this._areaPanel, this.TranslationService.GetTranslation("areaSettingsView-reorderEvents-btn", "Reorder Events"), () =>
        {
            this.ReorderEvents(areaConfiguration);
        });

        reorderEventsButton.Top = manageEventsButton.Bottom + 2;
        reorderEventsButton.Left = manageEventsButton.Left;

        Button removeButton = this.RenderButtonAsync(this._areaPanel, this.TranslationService.GetTranslation("areaSettingsView-remove-btn", "Remove"), async () =>
        {
            ConfirmDialog dialog = new ConfirmDialog(
                $"Delete Event Area \"{areaConfiguration.Name}\"", $"Your are in the process of deleting the event area \"{areaConfiguration.Name}\".\nThis action will delete all settings.\n\nContinue?",
                this.IconService,
                new[]
                {
                    new ButtonDefinition("Yes", DialogResult.Yes),
                    new ButtonDefinition("No", DialogResult.No)
                })
            { SelectedButtonIndex = 1 };

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

        areaName.Left = manageEventsButton.Right;
        areaName.Width = removeButton.Left - areaName.Left;
    }

    private void RenderGeneralSettings(FlowPanel settingsPanel, EventAreaConfiguration areaConfiguration)
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
            Title = this.TranslationService.GetTranslation("areaSettingsView-group-general", "General")
        };

        this.RenderBoolSetting(groupPanel, areaConfiguration.Enabled);
        this.RenderKeybindingSetting(groupPanel, areaConfiguration.EnabledKeybinding);
        this.RenderEnumSetting(groupPanel, areaConfiguration.DrawInterval);
        this.RenderEmptyLine(groupPanel, 20); // Fake bottom padding
    }

    private void RenderLocationAndSizeSettings(FlowPanel settingsPanel, EventAreaConfiguration areaConfiguration)
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
            Title = this.TranslationService.GetTranslation("areaSettingsView-group-locationAndSize", "Location & Size")
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
            Title = this.TranslationService.GetTranslation("areaSettingsView-group-layout", "Layout")
        };

        this.RenderBoolSetting(groupPanel, areaConfiguration.DrawBorders);
        this.RenderEnumSetting(groupPanel, areaConfiguration.BuildDirection);
        this.RenderIntSetting(groupPanel, areaConfiguration.TimeSpan);
        this.RenderIntSetting(groupPanel, areaConfiguration.HistorySplit);

        this.RenderEmptyLine(groupPanel);

        this.RenderBoolSetting(groupPanel, areaConfiguration.ShowCategoryNames);

        this.RenderEmptyLine(groupPanel);

        this.RenderBoolSetting(groupPanel, areaConfiguration.ShowTopTimeline);
        this.RenderBoolSetting(groupPanel, areaConfiguration.TopTimelineLinesOverWholeHeight);
        this.RenderBoolSetting(groupPanel, areaConfiguration.TopTimelineLinesInBackground);

        this.RenderEmptyLine(groupPanel, 20); // Fake bottom padding
    }

    private void RenderVisibilitySettings(FlowPanel settingsPanel, EventAreaConfiguration areaConfiguration)
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
            Title = this.TranslationService.GetTranslation("areaSettingsView-group-visibility", "Visibility")
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
            Title = this.TranslationService.GetTranslation("areaSettingsView-group-textAndColor", "Text & Color")
        };

        var fontFaceDropDown = this.RenderEnumSetting(groupPanel, areaConfiguration.FontFace).dropdown;
        fontFaceDropDown.Width = 500;
        this.RenderEnumSetting(groupPanel, areaConfiguration.FontSize);

        var customFontPathTextBox = this.RenderTextSetting(groupPanel, areaConfiguration.CustomFontPath).textBox;
        customFontPathTextBox.Width = groupPanel.ContentRegion.Width - customFontPathTextBox.Left;
        //customFontPathTextBox.Enabled = false;
        //this.RenderButton(groupPanel, "Select Font File",  () =>
        //{
        //        OpenFileDialog dialog = new OpenFileDialog();
        //        dialog.Filter = "TrueFont (*.ttf)|*.ttf|BM Font (*.fnt)|*.fnt";
        //        dialog.CheckFileExists = true;

        //        var result = dialog.ShowDialog();

        //        if (result != DialogResult.OK) return;

        //        areaConfiguration.CustomFontPath.Value = dialog.FileName;
        //        customFontPathTextBox.Text = dialog.FileName;
        //});

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

        this.RenderEmptyLine(groupPanel);

        this.RenderBoolSetting(groupPanel, areaConfiguration.EnableColorGradients);

        this.RenderEmptyLine(groupPanel);

        this.RenderColorSetting(groupPanel, areaConfiguration.TopTimelineBackgroundColor);
        this.RenderColorSetting(groupPanel, areaConfiguration.TopTimelineLineColor);
        this.RenderColorSetting(groupPanel, areaConfiguration.TopTimelineTimeColor);
        this.RenderFloatSetting(groupPanel, areaConfiguration.TopTimelineBackgroundOpacity);
        this.RenderFloatSetting(groupPanel, areaConfiguration.TopTimelineLineOpacity);
        this.RenderFloatSetting(groupPanel, areaConfiguration.TopTimelineTimeOpacity);

        this.RenderEmptyLine(groupPanel, 20); // Fake bottom padding
    }

    private void RenderBehaviourSettings(FlowPanel settingsPanel, EventAreaConfiguration areaConfiguration)
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
            Title = this.TranslationService.GetTranslation("areaSettingsView-group-behaviours", "Behaviours")
        };

        this.RenderEnumSetting(groupPanel, areaConfiguration.LeftClickAction);
        this.RenderBoolSetting(groupPanel, areaConfiguration.AcceptWaypointPrompt);

        this.RenderEmptyLine(groupPanel);

        #region Tooltip Option Group

        FlowPanel tooltipOptionGroup = new FlowPanel
        {
            Parent = groupPanel,
            Width = groupPanel.ContentRegion.Width,
            HeightSizingMode = SizingMode.AutoSize,
            OuterControlPadding = new Vector2(10, 20),
            ShowBorder = true,
            FlowDirection = ControlFlowDirection.SingleTopToBottom
        };

        this.RenderBoolSetting(tooltipOptionGroup, areaConfiguration.ShowTooltips);

        this.RenderEmptyLine(tooltipOptionGroup);

        this.RenderTextSetting(tooltipOptionGroup, areaConfiguration.EventAbsoluteTimeFormatString);
        FormattedLabel lbl = new FormattedLabelBuilder().SetWidth(tooltipOptionGroup.ContentRegion.Width - 20).AutoSizeHeight().Wrap()
                                                        .CreatePart(this.TranslationService.GetTranslation("areaSettingsView-tooltipOptionGroup-absoluteTimeFormatExamples", "Examples:\n24-Hour: HH\\:mm\n12-Hour: hh\\:mm tt"), builder =>
                                                        {
                                                            builder.MakeBold().SetFontSize(ContentService.FontSize.Size16);
                                                        })
                                                        .CreatePart("\n\n", builder => { })
                                                        .CreatePart(this.TranslationService.GetTranslation("areaSettingsView-tooltipOptionGroup-absoluteTimeFormatTestLink", "Click here for testing."), builder =>
                                                        {
                                                            builder.SetHyperLink("https://www.homedev.com.au/Online/DateFormat");
                                                        }).Build();
        lbl.Parent = tooltipOptionGroup;

        this.RenderEmptyLine(tooltipOptionGroup);

        this.RenderTextSetting(tooltipOptionGroup, areaConfiguration.EventTimespanDaysFormatString);
        this.RenderTextSetting(tooltipOptionGroup, areaConfiguration.EventTimespanHoursFormatString);
        this.RenderTextSetting(tooltipOptionGroup, areaConfiguration.EventTimespanMinutesFormatString);

        this.RenderEmptyLine(tooltipOptionGroup, (int)tooltipOptionGroup.OuterControlPadding.Y);

        #endregion

        this.RenderEmptyLine(groupPanel);

        this.RenderTextSetting(groupPanel, areaConfiguration.TopTimelineTimeFormatString);

        this.RenderEmptyLine(groupPanel);

        this.RenderBoolSetting(groupPanel, areaConfiguration.EnableHistorySplitScrolling);
        this.RenderIntSetting(groupPanel, areaConfiguration.HistorySplitScrollingSpeed);

        this.RenderEmptyLine(groupPanel);

        this.RenderEnumSetting(groupPanel, areaConfiguration.CompletionAction);
        this.RenderFloatSetting(groupPanel, areaConfiguration.CompletedEventsBackgroundOpacity);
        this.RenderFloatSetting(groupPanel, areaConfiguration.CompletedEventsTextOpacity);
        this.RenderBoolSetting(groupPanel, areaConfiguration.CompletedEventsInvertTextColor);
        this.RenderButton(groupPanel, this.TranslationService.GetTranslation("areaSettingsView-group-behaviours-btn-resetHiddenEvents", "Reset hidden Events"), () =>
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
            Title = this.TranslationService.GetTranslation("areaSettingsView-group-fillers", "Fillers")
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
    private void RenderSynchronizationSettings(FlowPanel settingsPanel, EventAreaConfiguration areaConfiguration)
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
            Title = this.TranslationService.GetTranslation("areaSettingsView-group-synchronization", "Synchronization")
        };

        this.RenderButtonAsync(groupPanel, "Sync enabled Events to Reminders",
            async () =>
            {
                var confirmDialog = new ConfirmDialog(
                    "Synchronizing",
                    "You are in the process of synchronizing the enabled events of this area to the reminders.\n\nThis will override all previously configured enabled/disabled reminder settings.",
                    this.IconService)
                {
                    SelectedButtonIndex = 1 // Preselect cancel
                };

                var confirmResult = await confirmDialog.ShowDialog();
                if (confirmResult != DialogResult.OK) return;

                await (this.SyncEnabledEventsToReminders?.Invoke(this, areaConfiguration) ?? Task.FromException(new NotImplementedException()));

                Blish_HUD.Controls.ScreenNotification.ShowNotification("Synchronization complete!");
            });

        this.RenderButtonAsync(groupPanel, "Sync enabled Events from Reminders",
            async () =>
            {
                var confirmDialog = new ConfirmDialog(
                    "Synchronizing",
                    "You are in the process of synchronizing the enabled events for reminders to this area.\n\nThis will override all previously configured enabled/disabled event settings of this area.",
                    this.IconService)
                {
                    SelectedButtonIndex = 1 // Preselect cancel
                };

                var confirmResult = await confirmDialog.ShowDialog();
                if (confirmResult != DialogResult.OK) return;

                await (this.SyncEnabledEventsFromReminders?.Invoke(this, areaConfiguration) ?? Task.FromException(new NotImplementedException()));

                Blish_HUD.Controls.ScreenNotification.ShowNotification("Synchronization complete!");
            });

        this.RenderButtonAsync(groupPanel, "Sync enabled Events to other Areas",
            async () =>
            {
                var confirmDialog = new ConfirmDialog(
                    "Synchronizing",
                    "You are in the process of synchronizing the enabled events of this area to all other areas.\n\nThis will override all previously configured enabled/disabled event settings of other areas.",
                    this.IconService)
                {
                    SelectedButtonIndex = 1 // Preselect cancel
                };

                var confirmResult = await confirmDialog.ShowDialog();
                if (confirmResult != DialogResult.OK) return;

                await (this.SyncEnabledEventsToOtherAreas?.Invoke(this, areaConfiguration) ?? Task.FromException(new NotImplementedException()));

                Blish_HUD.Controls.ScreenNotification.ShowNotification("Synchronization complete!");
            });

        this.RenderEmptyLine(groupPanel, (int)groupPanel.OuterControlPadding.Y); // Fake bottom padding
    }

    private void ReorderEvents(EventAreaConfiguration configuration)
    {
        this._reorderEventsWindow ??= WindowUtil.CreateStandardWindow(this._moduleSettings, "Reorder Events", this.GetType(), Guid.Parse("b5cbbd99-f02d-4229-8dda-869b42ac242e"), this.IconService);

        if (this._reorderEventsWindow.CurrentView != null)
        {
            ReorderEventsView reorderEventView = this._reorderEventsWindow.CurrentView as ReorderEventsView;
            reorderEventView.SaveClicked -= this.ReorderView_SaveClicked;
        }

        ReorderEventsView view = new ReorderEventsView(this._allEvents(), configuration.EventOrder.Value, configuration, this.APIManager, this.IconService, this.TranslationService);
        view.SaveClicked += this.ReorderView_SaveClicked;

        this._reorderEventsWindow.Show(view);
    }

    private void ReorderView_SaveClicked(object sender, (EventAreaConfiguration AreaConfiguration, string[] CategoryKeys) e)
    {
        e.AreaConfiguration.EventOrder.Value = new List<string>(e.CategoryKeys);
    }

    private void ManageEvents(EventAreaConfiguration configuration)
    {
        this._manageEventsWindow ??= WindowUtil.CreateStandardWindow(this._moduleSettings, "Manage Events", this.GetType(), Guid.Parse("7dc52c82-67ae-4cfb-9fe3-a16a8b30892c"), this.IconService);

        if (this._manageEventsWindow.CurrentView != null)
        {
            ManageEventsView manageEventView = this._manageEventsWindow.CurrentView as ManageEventsView;
            manageEventView.EventChanged -= this.ManageView_EventChanged;
        }

        ManageEventsView view = new ManageEventsView(this._allEvents(), new Dictionary<string, object>
        {
            { "configuration", configuration },
            { "hiddenEventKeys", this._eventStateService.Instances.Where(x => x.AreaName == configuration.Name && x.State == EventStateService.EventStates.Hidden).Select(x => x.EventKey).ToList() }
        }, () => configuration.DisabledEventKeys.Value, this._moduleSettings, this.APIManager, this.IconService, this.TranslationService);
        view.EventChanged += this.ManageView_EventChanged;

        this._manageEventsWindow.Show(view);
    }

    private void ManageView_EventChanged(object sender, ManageEventsView.EventChangedArgs e)
    {
        EventAreaConfiguration configuration = e.AdditionalData["configuration"] as EventAreaConfiguration;
        configuration.DisabledEventKeys.Value = e.NewState
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

    public class AddAreaEventArgs
    {
        public string Name { get; set; }
        public EventAreaConfiguration AreaConfiguration { get; set; }
    }
}