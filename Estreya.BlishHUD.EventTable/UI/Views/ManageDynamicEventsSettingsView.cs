namespace Estreya.BlishHUD.EventTable.UI.Views;

using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.EventTable.Controls;
using Estreya.BlishHUD.EventTable.Models;
using Estreya.BlishHUD.EventTable.State;
using Estreya.BlishHUD.Shared.State;
using Estreya.BlishHUD.Shared.UI.Views;
using Microsoft.Xna.Framework;
using MonoGame.Extended.BitmapFonts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class ManageDynamicEventsSettingsView : BaseView
{
    private static readonly Logger Logger = Logger.GetLogger<ManageDynamicEventsSettingsView>();
    private static Point MAIN_PADDING = new Point(20, 20);
    private readonly DynamicEventState _dynamicEventState;
    private readonly Func<List<string>> _getDisabledEventGuids;
    private List<Gw2Sharp.WebApi.V2.Models.Map> _maps = new List<Gw2Sharp.WebApi.V2.Models.Map>();

    public event EventHandler<ManageEventsView.EventChangedArgs> EventChanged;


    public Panel Panel { get; private set; }

    public ManageDynamicEventsSettingsView(DynamicEventState dynamicEventState, Func<List<string>> getDisabledEventGuids, Gw2ApiManager apiManager, IconState iconState, TranslationState translationState, BitmapFont font = null) : base(apiManager, iconState, translationState, font)
    {
        this._dynamicEventState = dynamicEventState;
        this._getDisabledEventGuids = getDisabledEventGuids;
    }
    private void UpdateToggleButton(GlowButton button)
    {
        GameService.Graphics.QueueMainThreadRender((graphicDevice) =>
        {
            button.Icon = button.Checked
                ? this.IconState.GetIcon("784259.png")
                : this.IconState.GetIcon("784261.png");
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

        var maps = this._maps.Where(m => this._dynamicEventState.Events?.Any(de => de.MapId == m.Id) ?? false);

        TextBox searchBox = new TextBox()
        {
            Parent = Panel,
            Width = Panel.MenuStandard.Size.X,
            Location = new Point(0, contentRegion.Y),
            PlaceholderText = "Search..."
        };

        Panel eventCategoriesPanel = new Panel
        {
            Title = "Maps",
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

        eventPanel.Size = new Point(contentRegion.Width - eventPanel.Location.X - MAIN_PADDING.X, contentRegion.Height - (int)(StandardButton.STANDARD_CONTROL_HEIGHT * 1.25));

        searchBox.TextChanged += (s, e) =>
        {
            eventPanel.FilterChildren<DataDetailsButton<DynamicEventState.DynamicEvent>>(detailsButton =>
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
                Gw2Sharp.WebApi.V2.Models.Map map = maps.Where(map => map.Name == menuItem.Text).FirstOrDefault();

                eventPanel.FilterChildren<DataDetailsButton<DynamicEventState.DynamicEvent>>(detailsButton =>
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

                if (control is DataDetailsButton<DynamicEventState.DynamicEvent> detailsButton && detailsButton.Visible)
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

                if (control is DataDetailsButton<DynamicEventState.DynamicEvent> detailsButton && detailsButton.Visible)
                {
                    if (detailsButton.Children.Last() is GlowButton glowButton)
                    {
                        glowButton.Checked = false;
                    }
                }
            });
        };

        var eventList = this._dynamicEventState.Events/*.Where(e => !string.IsNullOrWhiteSpace(e.Name))*/.ToList();
        foreach (var map in maps.Where(m => m.Id == GameService.Gw2Mumble.CurrentMap.Id)) // Limit to current map at the moment. Due to performance limits.
        {
            IEnumerable<DynamicEventState.DynamicEvent> events = eventList.Where(e => e.MapId == map.Id);
            foreach (DynamicEventState.DynamicEvent e in events)
            {
                bool enabled = !this._getDisabledEventGuids().Contains(e.ID);

                DataDetailsButton<DynamicEventState.DynamicEvent> button = new DataDetailsButton<DynamicEventState.DynamicEvent>()
                {
                    Data = e,
                    Parent = eventPanel,
                    Text = e.Name,
                    ShowToggleButton = true,
                    FillColor = Color.LightBlue,
                    //Size = new Point((events.ContentRegion.Size.X - Panel.ControlStandard.Size.X) / 2, events.ContentRegion.Size.X - Panel.ControlStandard.Size.X)
                };

                if (e.Icon?.FileID != null)
                {
                    GameService.Graphics.QueueMainThreadRender((graphicDevice) =>
                    {
                        button.Icon = this.IconState.GetIcon($"{e.Icon.FileID}.png");
                    });
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
                    this.EventChanged?.Invoke(this, new ManageEventsView.EventChangedArgs()
                    {
                        OldState = !eventArgs.Checked,
                        NewState = eventArgs.Checked,
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
            var maps = await this.APIManager.Gw2ApiClient.V2.Maps.AllAsync();
            this._maps.AddRange(maps);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, $"Failed to add maps:");
            return false;
        }

    }
}
