namespace Estreya.BlishHUD.EventTable.UI.Views
{
    using Blish_HUD;
    using Blish_HUD.Content;
    using Blish_HUD.Controls;
    using Blish_HUD.Graphics.UI;
    using Blish_HUD.Modules.Managers;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.EventTable.Controls;
    using Estreya.BlishHUD.EventTable.Models;
    using Estreya.BlishHUD.Shared.State;
    using Estreya.BlishHUD.Shared.UI.Views;
    using Microsoft.Xna.Framework;
    using MonoGame.Extended.BitmapFonts;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class ManageEventsView : BaseView
    {
        private static Point MAIN_PADDING = new Point(20, 20);

        public event EventHandler<EventChangedArgs> EventChanged;

        private static readonly Logger Logger = Logger.GetLogger<ManageEventsView>();
        private readonly List<EventCategory> allEvents;
        private readonly EventAreaConfiguration _eventAreaConfiguration;

        public ManageEventsView(List<EventCategory> allEvents, EventAreaConfiguration eventAreaConfiguration,  Gw2ApiManager apiManager, IconState iconState, TranslationState translationState, BitmapFont font = null) : base(apiManager, iconState,translationState, font)
        {
            this.allEvents = allEvents;
            this._eventAreaConfiguration = eventAreaConfiguration;
        }

        public Panel Panel { get; private set; }

        private void UpdateToggleButton(GlowButton button)
        {
            GameService.Graphics.QueueMainThreadRender((graphicDevice) =>
            {
                button.Icon = button.Checked
                    ? EventTableModule.ModuleInstance.IconState.GetIcon("784259.png")
                    : EventTableModule.ModuleInstance.IconState.GetIcon("784261.png");
            });
        }


        protected override void InternalBuild(Panel parent)
        {
            this.Panel = new Panel
            {
                Parent = parent,
                Location = new Point(MAIN_PADDING.X, MAIN_PADDING.Y),
                Width = parent.ContentRegion.Width - MAIN_PADDING.X * 1,
                Height = parent.ContentRegion.Height - MAIN_PADDING.Y,
                CanScroll = true
            };

            Rectangle contentRegion = this.Panel.ContentRegion;

            var eventCategories = this.allEvents;

            TextBox searchBox = new TextBox()
            {
                Parent = Panel,
                Width = Panel.MenuStandard.Size.X,
                Location = new Point(0, contentRegion.Y),
                PlaceholderText = "Search..."
            };

            Panel eventCategoriesPanel = new Panel
            {
                Title = "Event Categories",
                Parent = Panel,
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
                Location = new Point(eventCategoriesPanel.Right + Panel.ControlStandard.ControlOffset.X, contentRegion.Y),
                Parent = Panel
            };

            eventPanel.Size = new Point(contentRegion.Width - eventPanel.Left - MAIN_PADDING.X, contentRegion.Height - (int)(StandardButton.STANDARD_CONTROL_HEIGHT * 1.25));

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

            foreach (EventCategory category in eventCategories.GroupBy(ec => ec.Key).Select(ec => ec.First()))
            {
                menus.Add(category.Key, eventCategoryMenu.AddMenuItem(category.Name));
            }

            menus.ToList().ForEach(menuItemPair => menuItemPair.Value.Click += (s, e) =>
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
            });

            #endregion

            Panel buttons = new Panel()
            {
                Parent = Panel,
                Location = new Point(eventPanel.Left, eventPanel.Bottom),
                Size = new Point(eventPanel.Width, StandardButton.STANDARD_CONTROL_HEIGHT),
            };

            StandardButton checkAllButton = new StandardButton()
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

            StandardButton uncheckAllButton = new StandardButton()
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
                IEnumerable<Models.Event> events = category.ShowCombined ? category.Events.GroupBy(e => e.Key).Select(eg => eg.First()) : category.Events;
                foreach (Models.Event e in events)
                {
                    if (e.Filler)
                    {
                        continue;
                    }

                    bool enabled = this._eventAreaConfiguration.ActiveEventKeys.Value.Contains(e.SettingKey);

                    EventDetailsButton button = new EventDetailsButton()
                    {
                        Event = e,
                        Parent = eventPanel,
                        Text = e.Name,
                        ShowToggleButton = true,
                        FillColor = Color.LightBlue,
                        //Size = new Point((events.ContentRegion.Size.X - Panel.ControlStandard.Size.X) / 2, events.ContentRegion.Size.X - Panel.ControlStandard.Size.X)
                    };

                    GameService.Graphics.QueueMainThreadRender((graphicDevice) =>
                    {
                        button.Icon = this.IconState.GetIcon(e.Icon);
                    });

                    if (!string.IsNullOrWhiteSpace(e.Waypoint))
                    {
                        GlowButton waypointButton = new GlowButton()
                        {
                            Parent = button,
                            ToggleGlow = false
                        };

                        GameService.Graphics.QueueMainThreadRender((graphicDevice) =>
                        {
                            var icon = this.IconState.GetIcon("102348.png");
                            waypointButton.Tooltip = new Tooltip(new TooltipView("Waypoint", "Click to copy waypoint!", icon, this.TranslationState));
                            waypointButton.Icon = icon;
                        });

                        waypointButton.Click += (s, eventArgs) =>
                        {
                            //e.CopyWaypoint();
                        };
                    }

                    if (!string.IsNullOrWhiteSpace(e.Wiki))
                    {
                        GlowButton wikiButton = new GlowButton()
                        {
                            Parent = button,
                            ToggleGlow = false
                        };

                        GameService.Graphics.QueueMainThreadRender((graphicDevice) =>
                        {
                            var icon = this.IconState.GetIcon("102353.png");
                            wikiButton.Tooltip = new Tooltip(new TooltipView("Wiki", "Click to open wiki!", icon, this.TranslationState));
                            wikiButton.Icon = icon;
                        });

                        wikiButton.Click += (s, eventArgs) =>
                        {
                            //e.OpenWiki();
                        };
                    }

                    GlowButton toggleButton = new GlowButton()
                    {
                        Parent = button,
                        Checked = enabled,
                        ToggleGlow = false
                    };

                    this.UpdateToggleButton(toggleButton);

                    toggleButton.CheckedChanged += (s, eventArgs) =>
                    {
                        this.EventChanged?.Invoke(this, new EventChangedArgs()
                        {
                            OldState = !eventArgs.Checked,
                            NewState = eventArgs.Checked,
                            EventSettingKey = button.Event.SettingKey,
                            Configuration = _eventAreaConfiguration
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
    }

    public class EventChangedArgs
    {
        public bool OldState { get; set; }
        public bool NewState { get; set; }

        public EventAreaConfiguration Configuration { get; set; }

        public string EventSettingKey { get; set; }
    }
}
