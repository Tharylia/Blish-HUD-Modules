namespace Estreya.BlishHUD.EventTable.UI.Views;

using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Controls;
using Estreya.BlishHUD.EventTable.Services;
using Estreya.BlishHUD.Shared.Services.GameIntegration;
using Estreya.BlishHUD.Shared.Controls;
using Microsoft.Xna.Framework;
using Models;
using MonoGame.Extended.BitmapFonts;
using Shared.Services;
using Shared.UI.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Estreya.BlishHUD.EventTable.Models.SelfHosting;
using Flurl.Http;
using Estreya.BlishHUD.Shared.Models.BlishHudAPI;

public class SelfHostingEventsView : BaseView
{
    private static readonly Point MAIN_PADDING = new Point(20, 20);

    private readonly ModuleSettings _moduleSettings;
    private readonly SelfHostingEventService _selfHostingEventService;
    private readonly AccountService _accountService;
    private readonly ChatService _chatService;

    private FlowPanel _activeEventsGroup;

    private TimeSpan _maxHostingDuration;
    private List<SelfHostingCategoryDefinition> _definitions;

    public SelfHostingEventsView(ModuleSettings moduleSettings, SelfHostingEventService selfHostingEventService, Gw2ApiManager apiManager, IconService iconService, TranslationService translationService, AccountService accountService, ChatService chatService) : base(apiManager, iconService, translationService)
    {
        this._moduleSettings = moduleSettings;
        this._selfHostingEventService = selfHostingEventService;
        this._accountService = accountService;
        this._chatService = chatService;
    }

    public Panel Panel { get; private set; }

    protected override void InternalBuild(Panel parent)
    {
        this.Panel = new Panel
        {
            Parent = parent,
            Location = new Point(MAIN_PADDING.X, MAIN_PADDING.Y),
            Width = parent.ContentRegion.Width - (MAIN_PADDING.X * 2),
            Height = parent.ContentRegion.Height - (MAIN_PADDING.Y * 2),
            CanScroll = true,
        };

        this.BuildEntriesPanel();
    }

    private void BuildEntriesPanel()
    {
        this.ClearPanel();

        Rectangle contentRegion = this.Panel.ContentRegion;

        var categories = this._definitions ?? new List<SelfHostingCategoryDefinition>();

        TextBox searchBox = new TextBox
        {
            Parent = this.Panel,
            Width = Panel.MenuStandard.Size.X,
            Location = new Point(0, contentRegion.Y),
            PlaceholderText = "Search..."
        };

        Panel categoriesPanel = new Panel
        {
            Title = "Categories",
            Parent = this.Panel,
            CanScroll = true,
            ShowBorder = true,
            Location = new Point(0, searchBox.Bottom + Panel.MenuStandard.ControlOffset.Y)
        };

        categoriesPanel.Size = new Point(Panel.MenuStandard.Size.X, contentRegion.Height - categoriesPanel.Location.Y - (int)(StandardButton.STANDARD_CONTROL_HEIGHT * 3));

        Shared.Controls.Menu categoryMenu = new Shared.Controls.Menu
        {
            Parent = categoriesPanel,
            Size = categoriesPanel.ContentRegion.Size,
            MenuItemHeight = 40
        };

        this._activeEventsGroup = new FlowPanel
        {
            FlowDirection = ControlFlowDirection.LeftToRight,
            CanScroll = true,
            ShowBorder = true,
            Location = new Point(categoriesPanel.Right + Control.ControlStandard.ControlOffset.X, contentRegion.Y),
            Parent = this.Panel,
            ControlPadding = new Vector2(5, 5),
            OuterControlPadding = new Vector2(5, 5),
        };

        this._activeEventsGroup.Size = new Point(contentRegion.Width - this._activeEventsGroup.Location.X - MAIN_PADDING.X, contentRegion.Height /*- (int)(StandardButton.STANDARD_CONTROL_HEIGHT * 1.25)*/);

        searchBox.TextChanged += (s, e) =>
        {
            this._activeEventsGroup.FilterChildren<DataDetailsButton<SelfHostingEventEntry>>(detailsButton =>
            {
                return detailsButton.Text.ToLowerInvariant().Contains(searchBox.Text.ToLowerInvariant());
            });
        };

        #region Register Categories
        Dictionary<string, MenuItem> menus = new Dictionary<string, MenuItem>();

        MenuItem allEvents = categoryMenu.AddMenuItem("All Events");
        allEvents.Select();
        menus.Add(nameof(allEvents), allEvents);
        //MenuItem enabledEvents = categoryMenu.AddMenuItem("Enabled Events");
        //menus.Add(nameof(enabledEvents), enabledEvents);
        MenuItem divider1MenuItem = categoryMenu.AddMenuItem("-------------------------------------"); // Arbitrary length. Seems to fit.
        divider1MenuItem.Enabled = false;
        menus.Add(nameof(divider1MenuItem), divider1MenuItem);

        switch (this._moduleSettings.MenuEventSortMode.Value)
        {
            case MenuEventSortMode.Alphabetical:
                categories = categories.OrderBy(c => c.Key).ToList();
                break;
            case MenuEventSortMode.AlphabeticalDesc:
                categories = categories.OrderByDescending(c => c.Key).ToList();
                break;
        }

        foreach (var category in categories)
        {
            var categoryItem = categoryMenu.AddMenuItem(category.Name);

            foreach (var zone in category.Zones)
            {
                var zoneItem = new MenuItem(zone.Name)
                {
                    Parent = categoryItem,
                };

                menus.Add($"{category.Key}-{zone.Key}", zoneItem);
            }

            menus.Add(category.Key, categoryItem);
        }

        menus.ToList().ForEach(menuItemPair => menuItemPair.Value.Click += (s, e) =>
        {
            try
            {
                if (s is MenuItem menuItem)
                {
                    var parts = menuItemPair.Key.Split('-');
                    var categoryKey = parts.Length >= 1 ? parts[0] : null;
                    var zoneKey = parts.Length >= 2 ? parts[1] : null;

                    var category = categories.Find(ec => ec.Key == categoryKey);
                    var zone = category?.Zones.Find(z => z.Key == zoneKey);

                    this._activeEventsGroup.FilterChildren<DataDetailsButton<SelfHostingEventEntry>>(detailsButton =>
                    {
                        if (menuItem == menus[nameof(allEvents)]) return true;
                        if (category == null) return true;

                        var entry = detailsButton.Data;

                        if (entry.CategoryKey != category.Key) return false;

                        if (zone != null && zone.Key != entry.ZoneKey) return false;

                        var catZone = category.Zones.Find(z => z.Key == entry.ZoneKey);

                        if (catZone == null) return false;

                        return catZone.Events.Any(e => e.Key == entry.EventKey);
                    });
                }
            }
            catch (Exception ex)
            {
                this.ShowError($"Failed to filter events:\n{ex.Message}");
            }
        });
        #endregion

        StandardButton addButton = new StandardButton
        {
            Text = "Add",
            Parent = this.Panel,
            Location = new Point(categoriesPanel.Left, categoriesPanel.Bottom),
            Size = new Point(categoriesPanel.Width, StandardButton.STANDARD_CONTROL_HEIGHT)
        };

        addButton.Click += (s, e) =>
        {
            this.BuildAddPanel();
        };

        StandardButton removeButton = new StandardButton
        {
            Text = "Remove",
            Parent = this.Panel,
            Location = new Point(addButton.Left, addButton.Bottom),
            Size = new Point(categoriesPanel.Width, StandardButton.STANDARD_CONTROL_HEIGHT)
        };

        removeButton.Click += async (s, e) =>
        {
            try
            {
                await this._selfHostingEventService.DeleteEntry();
                this.ShowInfo("Deleted your currently hosted entry.");
                this.UpdateActiveEventsGroup();
            }
            catch (Exception ex)
            {
                this.ShowError($"Could not delete currently hosted entry: {ex.Message}");
            }
        };

        StandardButton reloadButton = new StandardButton
        {
            Text = "Reload",
            Parent = this.Panel,
            Location = new Point(removeButton.Left, removeButton.Bottom),
            Size = new Point(categoriesPanel.Width, StandardButton.STANDARD_CONTROL_HEIGHT)
        };

        reloadButton.Click += (s, e) =>
        {
            try
            {
                this.RefreshEvents();
                this.ShowInfo("Reloaded");
            }
            catch (Exception ex)
            {
                this.ShowError($"Could not reload: {ex.Message}");
            }
        };

        this.UpdateActiveEventsGroup();
    }

    private void BuildAddPanel()
    {
        this.ClearPanel();

        var addPanel = new Panel()
        {
            Parent = this.Panel,
            Width = this.Panel.ContentRegion.Width,
            Height = this.Panel.ContentRegion.Height,
        };

        var parsedCategories = this._definitions?.Select(c => new AddCategoryDropdown
        {
            Key = c.Key,
            Name = c.Name
        }).ToList() ?? new List<AddCategoryDropdown>();

        Dropdown<AddZoneDropdown> zoneDropdown = null;
        Dropdown<AddEventDropdown> eventDropdown = null;

        var categoryGroup = new FlowPanel
        {
            Parent = addPanel,
            FlowDirection = ControlFlowDirection.SingleLeftToRight,
            HeightSizingMode = SizingMode.AutoSize,
            Width = (int)(addPanel.ContentRegion.Width * 0.75f),
            Top = 100
        };

        var categoryLabel = this.RenderLabel(categoryGroup, "Category:").TitleLabel;
        categoryLabel.AutoSizeWidth = false;
        categoryLabel.Width = 125;

        var categoryDropdownLocation = new Point(categoryLabel.Right + 20, 0);
        var categoryDropdown = this.RenderDropdown<AddCategoryDropdown>(categoryGroup, categoryDropdownLocation, categoryGroup.ContentRegion.Width - categoryDropdownLocation.X, parsedCategories.ToArray(), null, onBeforeChangeAction: async (oldVal, newVal) =>
        {
            if (zoneDropdown is not null && newVal is not null)
            {
                var zones = await this._selfHostingEventService.GetCategoryZones(newVal.Key);
                zoneDropdown.Items.Clear();
                //zoneDropdown.SelectedItem = null;
                foreach (var z in zones)
                {
                    zoneDropdown.Items.Add(new AddZoneDropdown
                    {
                        CategoryKey = newVal.Key,
                        Key = z.Key,
                        Name = z.Name
                    });
                }

                zoneDropdown.SelectedItem = null;
            }

            return true;
        });
        categoryDropdown.PanelHeight = 500;
        categoryDropdown.PreselectOnItemsChange = false;

        categoryGroup.RecalculateLayout();
        categoryGroup.Update(GameService.Overlay.CurrentGameTime);

        categoryGroup.Left = this.Panel.ContentRegion.Width / 2 - categoryGroup.Width / 2;

        var zoneGroup = new FlowPanel
        {
            Parent = addPanel,
            FlowDirection = ControlFlowDirection.SingleLeftToRight,
            HeightSizingMode = SizingMode.AutoSize,
            Width = categoryGroup.Width,
            Top = categoryGroup.Bottom + 10
        };
        var zoneLabel = this.RenderLabel(zoneGroup, "Zone:").TitleLabel;
        zoneLabel.AutoSizeWidth = false;
        zoneLabel.Width = categoryLabel.Width;

        var zoneDropdownLocation = new Point(zoneLabel.Right + 20, 0);
        zoneDropdown = this.RenderDropdown<AddZoneDropdown>(zoneGroup, zoneDropdownLocation, zoneGroup.ContentRegion.Width - zoneDropdownLocation.X, new AddZoneDropdown[0], null, onBeforeChangeAction: async (oldVal, newVal) =>
        {
            if (eventDropdown is not null && newVal is not null)
            {
                var events = await this._selfHostingEventService.GetCategoryZoneEvents(newVal.CategoryKey, newVal.Key);
                eventDropdown.Items.Clear();
                //eventDropdown.SelectedItem = null;
                foreach (var e in events.OrderBy(e => e.Name))
                {
                    eventDropdown.Items.Add(new AddEventDropdown
                    {
                        CategoryKey = newVal.CategoryKey,
                        ZoneKey = newVal.Key,
                        Key = e.Key,
                        Name = e.Name
                    });
                }

                eventDropdown.SelectedItem = null;
            }

            return true;
        });
        zoneDropdown.PanelHeight = 500;
        zoneDropdown.PreselectOnItemsChange = false;

        zoneGroup.RecalculateLayout();
        zoneGroup.Update(GameService.Overlay.CurrentGameTime);

        zoneGroup.Left = this.Panel.ContentRegion.Width / 2 - zoneGroup.Width / 2;

        var eventGroup = new FlowPanel
        {
            Parent = addPanel,
            FlowDirection = ControlFlowDirection.SingleLeftToRight,
            HeightSizingMode = SizingMode.AutoSize,
            Width = zoneGroup.Width,
            Top = zoneGroup.Bottom + 10
        };
        var eventLabel = this.RenderLabel(eventGroup, "Event:").TitleLabel;
        eventLabel.AutoSizeWidth = false;
        eventLabel.Width = categoryLabel.Width;

        var eventDropdownLocation = new Point(eventLabel.Right + 20, 0);

        eventDropdown = this.RenderDropdown<AddEventDropdown>(eventGroup, eventDropdownLocation, eventGroup.ContentRegion.Width - eventDropdownLocation.X, new AddEventDropdown[0], null);
        eventDropdown.PanelHeight = 500;
        eventDropdown.PreselectOnItemsChange = false;

        eventGroup.RecalculateLayout();
        eventGroup.Update(GameService.Overlay.CurrentGameTime);

        eventGroup.Left = this.Panel.ContentRegion.Width / 2 - eventGroup.Width / 2;

        var durationGroup = new FlowPanel
        {
            Parent = addPanel,
            FlowDirection = ControlFlowDirection.SingleLeftToRight,
            HeightSizingMode = SizingMode.AutoSize,
            Width = eventGroup.Width,
            Top = eventGroup.Bottom + 10
        };
        var durationLabel = this.RenderLabel(durationGroup, "Duration (min):").TitleLabel;
        durationLabel.AutoSizeWidth = false;
        durationLabel.Width = categoryLabel.Width;

        var durationDropdownLocation = new Point(durationLabel.Right + 20, 0);

        var maxHostingMinutes = (int)this._maxHostingDuration.TotalMinutes;
        var durationDropdown = this.RenderDropdown<int>(durationGroup, durationDropdownLocation, durationGroup.ContentRegion.Width - durationDropdownLocation.X, Enumerable.Range(1, maxHostingMinutes).ToArray(), Math.Min(5, maxHostingMinutes));
        durationDropdown.PanelHeight = 500;

        durationGroup.RecalculateLayout();
        durationGroup.Update(GameService.Overlay.CurrentGameTime);

        durationGroup.Left = this.Panel.ContentRegion.Width / 2 - durationGroup.Width / 2;

        var startTimeInfoLbl = new FormattedLabelBuilder().SetWidth(durationGroup.Width).AutoSizeHeight().Wrap()
            .SetHorizontalAlignment(HorizontalAlignment.Center)
            .CreatePart("The start time will be the current time you click on \"Add\".", b => { })
            .Build();

        startTimeInfoLbl.Parent = addPanel;
        startTimeInfoLbl.Left = durationGroup.Left;
        startTimeInfoLbl.Top = durationGroup.Bottom + 20;

        var buttonGroup = new FlowPanel
        {
            Parent = addPanel,
            Top = addPanel.ContentRegion.Bottom - (StandardButton.STANDARD_CONTROL_HEIGHT + 10),
            FlowDirection = ControlFlowDirection.SingleLeftToRight,
            WidthSizingMode = SizingMode.AutoSize,
            HeightSizingMode = SizingMode.AutoSize
        };

        this.RenderButtonAsync(buttonGroup, "Add", async () =>
        {
            try
            {
                await this._selfHostingEventService.AddEntry(categoryDropdown.SelectedItem?.Key, zoneDropdown.SelectedItem?.Key, eventDropdown.SelectedItem?.Key, DateTimeOffset.UtcNow, durationDropdown.SelectedItem);
                this.ShowInfo("Added successfully.");
                this.BuildEntriesPanel();
            }
            catch (FlurlHttpException fhex)
            {
                var error = await fhex.GetResponseJsonAsync<APIError>();
                this.ShowError($"Could not add entry: {error.Message}");
            }
            catch (Exception ex)
            {
                this.ShowError($"Could not add entry: {ex.Message}");
            }
        });

        this.RenderButton(buttonGroup, "Cancel", () =>
        {
            this.BuildEntriesPanel();
        });

        var removeCurrentButton = this.RenderButtonAsync(buttonGroup, "Remove current", async () =>
        {
            try
            {
                await this._selfHostingEventService.DeleteEntry();
                this.ShowInfo("Deleted your currently hosted entry.");
            }
            catch (Exception ex)
            {
                this.ShowError("Could not delete currently hosted entry.");
            }
        });

        removeCurrentButton.Enabled = this._selfHostingEventService.HasSelfHostingEntry();

        buttonGroup.RecalculateLayout();
        buttonGroup.Update(GameService.Overlay.CurrentGameTime);

        buttonGroup.Left = addPanel.ContentRegion.Width / 2 - buttonGroup.Width / 2;
    }

    private void ClearPanel()
    {
        this.Panel.ClearChildren();
    }

    private void RefreshEvents()
    {
        this.UpdateActiveEventsGroup();
    }

    private void UpdateActiveEventsGroup()
    {
        this._activeEventsGroup.ClearChildren();
        var currentAccountName = this._accountService.Account?.Name;

        var events = this._selfHostingEventService.Events.ToArray().ToList();

        foreach (var ev in events)
        {
            var isMyHosting = ev.AccountName == currentAccountName;

            var runningDuration = DateTimeOffset.UtcNow - ev.StartTime;

            var categoryDef = this._definitions?.FirstOrDefault(c => c.Key == ev.CategoryKey);
            var categoryName = categoryDef?.Name ?? ev.CategoryKey;
            var zoneDef = categoryDef?.Zones?.FirstOrDefault(z => z.Key == ev.ZoneKey);
            var zoneName = zoneDef?.Name ?? ev.ZoneKey;
            var eventDef = zoneDef?.Events?.FirstOrDefault(e => e.Key == ev.EventKey);
            var eventName = eventDef?.Name ?? ev.EventKey;
            var startTimeLocal = ev.StartTime.ToLocalTime();
            var startTimeString = startTimeLocal.Date == DateTimeOffset.Now.Date
                ? startTimeLocal.ToString("t")
                : startTimeLocal.ToString("g");

            var detailsButton = new DataDetailsButton<SelfHostingEventEntry>()
            {
                Parent = this._activeEventsGroup,
                Data = ev,
                Text = $"{categoryName} - {zoneName} - {eventName}\n\nHosted by: {ev.AccountName}\nStarting: {startTimeString}\nDuration: {ev.Duration} minutes",
                Icon = this.IconService.GetIcon("42681.png"),
                ShowToggleButton = true,
                Height = 175,
                Width = 500,
                IconSize = DetailsIconSize.Small,
                BackgroundColor = isMyHosting ? Color.LightGreen * 0.25f : Color.Transparent,
            };

            // Fill does not render correctly in current version
            if (Program.OverlayVersion >= new SemVer.Version(1, 2, 0))
            {
                detailsButton.MaxFill = 100;
                detailsButton.ShowVignette = true;
                detailsButton.CurrentFill = (int)Shared.Helpers.MathHelper.Scale(runningDuration.TotalMinutes, 0, ev.Duration, 0, detailsButton.MaxFill);
                detailsButton.ShowFillFraction = true;
            }

            var askForInviteButton = new GlowButton()
            {
                Parent = detailsButton,
                BasicTooltipText = !isMyHosting ? "Ask for invite" : "You can't ask yourself. Or can you?",
                Icon = this.IconService.GetIcon("156386.png"),
                Enabled = !isMyHosting,
            };

            if (isMyHosting) askForInviteButton.GlowColor = Color.Transparent;

            askForInviteButton.Click += async (s, e) =>
            {
                this._logger.Debug($"Asking for invite for event: {ev.CategoryKey} - {ev.EventKey} | Hosted by: {ev.AccountName}");
                try
                {
                    await this._chatService.ChangeChannel(Shared.Models.GameIntegration.Chat.ChatChannel.Squad);
                    await this._chatService.ChangeChannel(Shared.Models.GameIntegration.Chat.ChatChannel.Private, wispherRecipient: ev.AccountName);
                    await this._chatService.Send("Hey, I would like to join your event! Can you invite me to a party/squad?");
                }
                catch (Exception ex)
                {
                    this.ShowError("Could not send chat message.");
                    this._logger.Warn(ex, "Could not send chat message.");
                }
            };

            var sameInstance = ev.InstanceIP == GameService.Gw2Mumble.Info.ServerAddress;

            if (!isMyHosting)
            {
                if (sameInstance)
                {
                    var sameInstanceButton = new GlowButton()
                    {
                        Parent = detailsButton,
                        BasicTooltipText = "You are on the same instance as the host.",
                        Icon = this.IconService.GetIcon("155023.png")
                    };
                }
                else
                {
                    var differentInstanceButton = new GlowButton()
                    {
                        Parent = detailsButton,
                        BasicTooltipText = "You are NOT on the same instance as the host.",
                        Icon = this.IconService.GetIcon("155018.png")
                    };
                }
            }

            if (!string.IsNullOrWhiteSpace(eventDef?.WikiUrl))
            {
                var openWikiButton = new GlowButton()
                {
                    Parent = detailsButton,
                    BasicTooltipText = "Open wiki page for this event.",
                    Icon = this.IconService.GetIcon("102353.png"),
                };

                openWikiButton.Click += (s, e) =>
                {
                    try
                    {
                        var wikiUrl = eventDef.WikiUrl;
                        Process.Start(wikiUrl);
                    }
                    catch (Exception ex)
                    {
                        this.ShowError(ex.Message);
                    }
                };
            }

            // Must be last
            var reportButton = new GlowButton()
            {
                Parent = detailsButton,
                BasicTooltipText = !isMyHosting ? "Report this host." : "Why would you report yourself?",
                Icon = this.IconService.GetIcon("851256.png"),
                Enabled = !isMyHosting
            };

            if (isMyHosting) reportButton.GlowColor = Color.Transparent;

            reportButton.Click += async (s, e) =>
            {
                var detailsButton = (s as GlowButton).Parent as DataDetailsButton<SelfHostingEventEntry>;
                var detailsButtonEvent = detailsButton.Data;

                try
                {
                    var dropdownReportType = new Dropdown<SelfHostingReportType>()
                    {
                        PanelHeight = 200,
                    };

                    foreach (var item in (SelfHostingReportType[])Enum.GetValues(typeof(SelfHostingReportType)))
                    {
                        dropdownReportType.Items.Add(item);
                    }

                    dropdownReportType.SelectedItem = SelfHostingReportType.Spam;

                    var inputDialogReportType = new Shared.Controls.Input.InputDialog<Dropdown<SelfHostingReportType>>(dropdownReportType, $"Reporting {detailsButtonEvent.AccountName} - Select Type", "Please selected the matching offence for the report", this.IconService);
                    var resultReportType = await inputDialogReportType.ShowDialog();
                    if (resultReportType != System.Windows.Forms.DialogResult.OK) return;

                    var reportType = (SelfHostingReportType)inputDialogReportType.Input;

                    var inputDialogReportReason = new Shared.Controls.Input.InputDialog<TextBox>(new TextBox()
                    {
                        PlaceholderText = "Reason"
                    }, $"Reporting {detailsButtonEvent.AccountName} - Select Reason", "Please provide a reason for the report.", this.IconService);

                    var resultReportReason = await inputDialogReportReason.ShowDialog();
                    if (resultReportReason != System.Windows.Forms.DialogResult.OK) return;

                    var reportReason = (string)inputDialogReportReason.Input;

                    await this._selfHostingEventService.ReportHost(detailsButtonEvent.AccountName, reportType, reportReason);
                }
                catch (FlurlHttpException fhex)
                {
                    var content = await fhex.GetResponseJsonAsync<Shared.Models.BlishHudAPI.APIError>();
                    this._logger.Warn(fhex, $"Could not report host: {content.Message}");
                    this.ShowError($"Could not report host: {content.Message}");
                }
                catch (Exception ex)
                {
                    this.ShowError("Could not report host.");
                    this._logger.Warn(ex, "Could not report host.");
                }
            };
        }

        this._activeEventsGroup.Height -= 1;
        this._activeEventsGroup.Height += 1;

        this.SortActiveEvents();
    }

    private void SortActiveEvents()
    {
        this._activeEventsGroup.SortChildren(new Comparison<DataDetailsButton<SelfHostingEventEntry>>((a, b) =>
        {
            var currentAccountName = this._accountService.Account?.Name;

            if (a.Data.AccountName == b.Data.AccountName) return 0;
            else if (a.Data.AccountName == currentAccountName && b.Data.AccountName != currentAccountName) return -1;
            else if (a.Data.AccountName != currentAccountName && b.Data.AccountName == currentAccountName) return 1;
            else return 0;
        }));
    }

    protected override async Task<bool> InternalLoad(IProgress<string> progress)
    {
        await this.TryLoadingCategories();
        await this.TryLoadingMaxHostingDuration();
        return true;
    }

    private async Task TryLoadingCategories()
    {
        try
        {
            var definitions = await this._selfHostingEventService.GetDefinitions();

            this._definitions = definitions;
        }
        catch (Exception ex)
        {
            this._logger.Warn(ex, "Failed to load definitions.");
        }
    }

    private async Task TryLoadingMaxHostingDuration()
    {
        try
        {
            var durationSec = await this._selfHostingEventService.GetMaxHostingDuration();

            this._maxHostingDuration = TimeSpan.FromSeconds(durationSec);
        }
        catch (Exception ex)
        {
            this._logger.Warn(ex, "Failed to load max hosting duration.");
        }
    }

    public class EventChangedArgs
    {
        public bool OldState { get; set; }
        public bool NewState { get; set; }

        public Dictionary<string, object> AdditionalData { get; set; }

        public string EventSettingKey { get; set; }
    }

    private class AddCategoryDropdown
    {
        public string Key { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            return this.Name;
        }
    }
    private class AddZoneDropdown
    {
        public string CategoryKey { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            return this.Name;
        }
    }
    private class AddEventDropdown
    {
        public string CategoryKey { get; set; }

        public string ZoneKey { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            return this.Name;
        }
    }
}