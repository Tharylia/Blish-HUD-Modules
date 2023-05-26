namespace Estreya.BlishHUD.EventTable.UI.Views;

using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Controls;
using Gw2Sharp.WebApi.V2;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.Xna.Framework;
using MonoGame.Extended.BitmapFonts;
using Services;
using Shared.Services;
using Shared.UI.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

public class ManageDynamicEventsSettingsView : BaseView
{
    private static readonly Logger Logger = Logger.GetLogger<ManageDynamicEventsSettingsView>();
    private static readonly Point MAIN_PADDING = new Point(20, 20);
    private readonly DynamicEventService _dynamicEventService;
    private readonly Func<List<string>> _getDisabledEventGuids;
    private readonly List<Map> _maps = new List<Map>();

    public ManageDynamicEventsSettingsView(DynamicEventService dynamicEventService, Func<List<string>> getDisabledEventGuids, Gw2ApiManager apiManager, IconService iconService, TranslationService translationService, BitmapFont font = null) : base(apiManager, iconService, translationService, font)
    {
        this._dynamicEventService = dynamicEventService;
        this._getDisabledEventGuids = getDisabledEventGuids;
    }

    public Panel Panel { get; private set; }

    public event EventHandler<ManageEventsView.EventChangedArgs> EventChanged;

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

        IEnumerable<Map> maps = this._maps.Where(m => this._dynamicEventService.Events?.Any(de => de.MapId == m.Id) ?? false);

        TextBox searchBox = new TextBox
        {
            Parent = this.Panel,
            Width = Panel.MenuStandard.Size.X,
            Location = new Point(0, contentRegion.Y),
            PlaceholderText = "Search..."
        };

        Panel eventCategoriesPanel = new Panel
        {
            Title = "Maps",
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
            eventPanel.FilterChildren<DataDetailsButton<DynamicEventService.DynamicEvent>>(detailsButton =>
            {
                return detailsButton.Text.ToLowerInvariant().Contains(searchBox.Text.ToLowerInvariant());
            });
        };

        #region Register Categories

        Dictionary<string, MenuItem> menus = new Dictionary<string, MenuItem>();

        MenuItem allEvents = eventCategoryMenu.AddMenuItem("Current Map");
        allEvents.Select();
        menus.Add(nameof(allEvents), allEvents);

        /*foreach (var map in maps)
        {
            menus.Add(map.Id.ToString(), eventCategoryMenu.AddMenuItem(map.Name));
        }*/

        menus.ToList().ForEach(menuItemPair => menuItemPair.Value.Click += (s, e) =>
        {
            if (s is MenuItem menuItem)
            {
                Map map = maps.Where(map => map.Name == menuItem.Text).FirstOrDefault();

                eventPanel.FilterChildren<DataDetailsButton<DynamicEventService.DynamicEvent>>(detailsButton =>
                {
                    if (menuItem == menus[nameof(allEvents)])
                    {
                        return true;
                    }

                    return detailsButton.Data.MapId == map.Id;
                });
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

                if (control is DataDetailsButton<DynamicEventService.DynamicEvent> detailsButton && detailsButton.Visible)
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

                if (control is DataDetailsButton<DynamicEventService.DynamicEvent> detailsButton && detailsButton.Visible)
                {
                    if (detailsButton.Children.Last() is GlowButton glowButton)
                    {
                        glowButton.Checked = false;
                    }
                }
            });
        };

        List<DynamicEventService.DynamicEvent> eventList = this._dynamicEventService.Events /*.Where(e => !string.IsNullOrWhiteSpace(e.Name))*/.ToList();
        foreach (Map map in maps.Where(m => m.Id == GameService.Gw2Mumble.CurrentMap.Id)) // Limit to current map at the moment. Due to performance limits.
        {
            IEnumerable<DynamicEventService.DynamicEvent> events = eventList.Where(e => e.MapId == map.Id);
            foreach (DynamicEventService.DynamicEvent e in events)
            {
                bool enabled = !this._getDisabledEventGuids().Contains(e.ID);

                DataDetailsButton<DynamicEventService.DynamicEvent> button = new DataDetailsButton<DynamicEventService.DynamicEvent>
                {
                    Data = e,
                    Parent = eventPanel,
                    Text = e.Name,
                    ShowToggleButton = true,
                    FillColor = Color.LightBlue
                    //Size = new Point((events.ContentRegion.Size.X - Panel.ControlStandard.Size.X) / 2, events.ContentRegion.Size.X - Panel.ControlStandard.Size.X)
                };

                if (e.Icon?.FileID != null)
                {
                    GameService.Graphics.QueueMainThreadRender(graphicDevice =>
                    {
                        button.Icon = this.IconService.GetIcon($"{e.Icon.FileID}.png");
                    });
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
                    this.EventChanged?.Invoke(this, new ManageEventsView.EventChangedArgs
                    {
                        OldService = !eventArgs.Checked,
                        NewService = eventArgs.Checked,
                        EventSettingKey = button.Data.ID
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

    protected override async Task<bool> InternalLoad(IProgress<string> progress)
    {
        try
        {
            IApiV2ObjectList<Map> maps = await this.APIManager.Gw2ApiClient.V2.Maps.AllAsync();
            this._maps.AddRange(maps);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, "Failed to add maps:");
            return false;
        }
    }
}