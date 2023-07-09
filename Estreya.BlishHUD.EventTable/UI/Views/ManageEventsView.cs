namespace Estreya.BlishHUD.EventTable.UI.Views;

using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Controls;
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
using Event = Models.Event;

public class ManageEventsView : BaseView
{
    private static readonly Point MAIN_PADDING = new Point(20, 20);

    private static readonly Logger Logger = Logger.GetLogger<ManageEventsView>();
    private readonly Dictionary<string, object> _additionalData;
    private readonly Func<List<string>> _getDisabledEventKeys;
    private readonly ModuleSettings _moduleSettings;
    private readonly List<EventCategory> allEvents;

    public ManageEventsView(List<EventCategory> allEvents, Dictionary<string, object> additionalData, Func<List<string>> getDisabledEventKeys, ModuleSettings moduleSettings, Gw2ApiManager apiManager, IconService iconService, TranslationService translationService, BitmapFont font = null) : base(apiManager, iconService, translationService, font)
    {
        this.allEvents = allEvents;
        this._additionalData = additionalData ?? new Dictionary<string, object>();
        this._getDisabledEventKeys = getDisabledEventKeys;
        this._moduleSettings = moduleSettings;
    }

    public Panel Panel { get; private set; }

    public event EventHandler<EventChangedArgs> EventChanged;

    private void UpdateToggleButton(GlowButton button)
    {
        GameService.Graphics.QueueMainThreadRender(graphicDevice =>
        {
            button.Icon = button.Checked
                ? this.IconService.GetIcon("784259.png")
                : this.IconService.GetIcon("784261.png");
        });
    }

    protected override void InternalBuild(Panel parent)
    {
        this.Panel = new Panel
        {
            Parent = parent,
            Location = new Point(MAIN_PADDING.X, MAIN_PADDING.Y),
            Width = parent.ContentRegion.Width - (MAIN_PADDING.X * 1),
            Height = parent.ContentRegion.Height - MAIN_PADDING.Y,
            CanScroll = true
        };

        Rectangle contentRegion = this.Panel.ContentRegion;

        List<EventCategory> eventCategories = this.allEvents;

        TextBox searchBox = new TextBox
        {
            Parent = this.Panel,
            Width = Panel.MenuStandard.Size.X,
            Location = new Point(0, contentRegion.Y),
            PlaceholderText = "Search..."
        };

        Panel eventCategoriesPanel = new Panel
        {
            Title = "Event Categories",
            Parent = this.Panel,
            CanScroll = true,
            ShowBorder = true,
            Location = new Point(0, searchBox.Bottom + Panel.MenuStandard.ControlOffset.Y)
        };

        eventCategoriesPanel.Size = new Point(Panel.MenuStandard.Size.X, contentRegion.Height - eventCategoriesPanel.Location.Y);

        Menu eventCategoryMenu = new Menu
        {
            Parent = eventCategoriesPanel,
            Size = eventCategoriesPanel.ContentRegion.Size,
            MenuItemHeight = 40
        };

        FlowPanel eventPanel = new FlowPanel
        {
            FlowDirection = ControlFlowDirection.LeftToRight,
            CanScroll = true,
            ShowBorder = true,
            Location = new Point(eventCategoriesPanel.Right + Control.ControlStandard.ControlOffset.X, contentRegion.Y),
            Parent = this.Panel
        };

        eventPanel.Size = new Point(contentRegion.Width - eventPanel.Location.X - MAIN_PADDING.X, contentRegion.Height - (int)(StandardButton.STANDARD_CONTROL_HEIGHT * 1.25));

        searchBox.TextChanged += (s, e) =>
        {
            eventPanel.FilterChildren<EventDetailsButton>(detailsButton =>
            {
                return detailsButton.Text.ToLowerInvariant().Contains(searchBox.Text.ToLowerInvariant());
            });
        };

        #region Register Categories

        Dictionary<string, MenuItem> menus = new Dictionary<string, MenuItem>();

        MenuItem allEvents = eventCategoryMenu.AddMenuItem("All Events");
        allEvents.Select();
        menus.Add(nameof(allEvents), allEvents);

        IEnumerable<EventCategory> categoryList = eventCategories.GroupBy(ec => ec.Key).Select(ec => ec.First());

        switch (this._moduleSettings.MenuEventSortMode.Value)
        {
            case MenuEventSortMode.Alphabetical:
                categoryList = categoryList.OrderBy(c => c.Name);
                break;
            case MenuEventSortMode.AlphabeticalDesc:
                categoryList = categoryList.OrderByDescending(c => c.Name);
                break;
        }

        foreach (EventCategory category in categoryList)
        {
            menus.Add(category.Key, eventCategoryMenu.AddMenuItem(category.Name));
        }

        menus.ToList().ForEach(menuItemPair => menuItemPair.Value.Click += (s, e) =>
        {
            try
            {
                if (s is MenuItem menuItem)
                {
                    EventCategory category = eventCategories.Where(ec => ec.Name == menuItem.Text).FirstOrDefault();

                    eventPanel.FilterChildren<EventDetailsButton>(detailsButton =>
                    {
                        if (menuItem == menus[nameof(allEvents)])
                        {
                            return true;
                        }

                        //IEnumerable<EventCategory> categories = EventCategories.Where(ec => ec.Events.Any(ev => ev.Name == detailsButton.Text));
                        return category.Events.Any(ev => ev.SettingKey.Split('_')[0] == detailsButton.Event.SettingKey.Split('_')[0] && ev.Key == detailsButton.Event.Key);
                    });
                }
            }
            catch (Exception ex)
            {
                this.ShowError($"Failed to filter events:\n{ex.Message}");
            }
        });

        #endregion

        Panel buttons = new Panel
        {
            Parent = this.Panel,
            Location = new Point(eventPanel.Left, eventPanel.Bottom),
            Size = new Point(eventPanel.Width, StandardButton.STANDARD_CONTROL_HEIGHT)
        };

        StandardButton checkAllButton = new StandardButton
        {
            Text = "Check all",
            Parent = buttons,
            Right = buttons.Width,
            Bottom = buttons.Height
        };
        checkAllButton.Click += (s, e) =>
        {
            eventPanel.Children.ToList().ForEach(control =>
            {
                if (menus[nameof(allEvents)].Selected)
                {
                    // Check Yes - No
                }

                if (control is EventDetailsButton detailsButton && detailsButton.Visible)
                {
                    if (detailsButton.Children.Last() is GlowButton glowButton)
                    {
                        glowButton.Checked = true;
                    }
                }
            });
        };

        StandardButton uncheckAllButton = new StandardButton
        {
            Text = "Uncheck all",
            Parent = buttons,
            Right = checkAllButton.Left,
            Bottom = buttons.Height
        };
        uncheckAllButton.Click += (s, e) =>
        {
            eventPanel.Children.ToList().ForEach(control =>
            {
                if (menus[nameof(allEvents)].Selected)
                {
                    // Check Yes - No
                }

                if (control is EventDetailsButton detailsButton && detailsButton.Visible)
                {
                    if (detailsButton.Children.Last() is GlowButton glowButton)
                    {
                        glowButton.Checked = false;
                    }
                }
            });
        };

        foreach (EventCategory category in eventCategories)
        {
            IEnumerable<Event> events = category.ShowCombined ? category.Events.GroupBy(e => e.Key).Select(eg => eg.First()) : category.Events;
            foreach (Event e in events)
            {
                if (e.Filler)
                {
                    continue;
                }

                bool enabled = !this._getDisabledEventKeys().Contains(e.SettingKey);

                EventDetailsButton button = new EventDetailsButton
                {
                    Event = e,
                    Parent = eventPanel,
                    Text = e.Name,
                    Icon = this.IconService.GetIcon(e.Icon),
                    ShowToggleButton = true,
                    FillColor = Color.LightBlue
                    //Size = new Point((events.ContentRegion.Size.X - Panel.ControlStandard.Size.X) / 2, events.ContentRegion.Size.X - Panel.ControlStandard.Size.X)
                };

                if (!string.IsNullOrWhiteSpace(e.Waypoint))
                {
                    AsyncTexture2D icon = this.IconService.GetIcon("102348.png");
                    GlowButton waypointButton = new GlowButton
                    {
                        Parent = button,
                        ToggleGlow = false,
                        Tooltip = new Tooltip(new TooltipView("Waypoint", "Click to copy waypoint!", icon, this.TranslationService)),
                        Icon = icon
                    };

                    waypointButton.Click += (s, eventArgs) =>
                    {
                        ClipboardUtil.WindowsClipboardService.SetTextAsync(e.Waypoint).ContinueWith(clipboardTask =>
                        {
                            string message = "Copied!";
                            ScreenNotification.NotificationType type = ScreenNotification.NotificationType.Info;
                            if (clipboardTask.IsFaulted)
                            {
                                message = clipboardTask.Exception.Message;
                                type = ScreenNotification.NotificationType.Error;
                            }

                            GameService.Graphics.QueueMainThreadRender(graphicDevice =>
                            {
                                ScreenNotification.ShowNotification(message, type);
                            });
                        });
                    };
                }

                if (!string.IsNullOrWhiteSpace(e.Wiki))
                {
                    AsyncTexture2D icon = this.IconService.GetIcon("102353.png");
                    GlowButton wikiButton = new GlowButton
                    {
                        Parent = button,
                        ToggleGlow = false,
                        Tooltip = new Tooltip(new TooltipView("Wiki", "Click to open wiki!", icon, this.TranslationService)),
                        Icon = icon
                    };

                    wikiButton.Click += (s, eventArgs) =>
                    {
                        Process.Start(e.Wiki);
                    };
                }

                if (this._additionalData.ContainsKey("hiddenEventKeys") && this._additionalData["hiddenEventKeys"] is List<string> hiddenEventKeys && hiddenEventKeys.Contains(e.SettingKey))
                {
                    //155018.png
                    GlowButton wikiButton = new GlowButton
                    {
                        Parent = button,
                        ToggleGlow = false,
                        Icon = this.IconService.GetIcon("155018.png"),
                        BasicTooltipText = "This event is currently hidden due to dynamic states.",
                        Enabled = false
                    };
                }

                if (this._additionalData.ContainsKey("customActions") && this._additionalData["customActions"] is List<CustomActionDefinition> customActions)
                {
                    foreach (CustomActionDefinition customAction in customActions)
                    {
                        if (string.IsNullOrWhiteSpace(customAction.Name) || customAction.Action == null)
                        {
                            continue;
                        }

                        //155018.png
                        GlowButton customButton = new GlowButton
                        {
                            Parent = button,
                            ToggleGlow = false,
                            Icon = customAction.Icon != null ? this.IconService.GetIcon(customAction.Icon) : null,
                            BasicTooltipText = customAction.Tooltip
                        };

                        customButton.Click += (s, ea) =>
                        {
                            customAction.Action?.Invoke(e);
                        };
                    }
                }

                GlowButton toggleButton = new GlowButton
                {
                    Parent = button,
                    Checked = enabled,
                    ToggleGlow = false
                };

                this.UpdateToggleButton(toggleButton);

                toggleButton.CheckedChanged += (s, eventArgs) =>
                {
                    this.EventChanged?.Invoke(this, new EventChangedArgs
                    {
                        OldService = !eventArgs.Checked,
                        NewService = eventArgs.Checked,
                        EventSettingKey = button.Event.SettingKey,
                        AdditionalData = this._additionalData
                    });

                    toggleButton.Checked = eventArgs.Checked;
                    //settings.Where(x => x.EntryKey != setting.EntryKey).ToList().ForEach(x => x.Value = setting.Value);
                    this.UpdateToggleButton(toggleButton);
                };

                toggleButton.Click += (s, eventArgs) =>
                {
                    toggleButton.Checked = !toggleButton.Checked;
                };
            }
        }
    }

    protected override Task<bool> InternalLoad(IProgress<string> progress)
    {
        return Task.FromResult(true);
    }

    public class EventChangedArgs
    {
        public bool OldService { get; set; }
        public bool NewService { get; set; }

        public Dictionary<string, object> AdditionalData { get; set; }

        public string EventSettingKey { get; set; }
    }

    public struct CustomActionDefinition
    {
        public string Name { get; set; }
        public string Tooltip { get; set; }
        public string Icon { get; set; }

        public Action<Event> Action { get; set; }
    }
}