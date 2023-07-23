namespace Estreya.BlishHUD.EventTable.UI.Views;

using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Controls;
using Estreya.BlishHUD.EventTable.Models;
using Estreya.BlishHUD.Shared.Extensions;
using Estreya.BlishHUD.Shared.Utils;
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
    private readonly ModuleSettings _moduleSettings;
    private readonly List<Map> _maps = new List<Map>();
    private Shared.Controls.StandardWindow _editEventWindow;

    public ManageDynamicEventsSettingsView(DynamicEventService dynamicEventService, Func<List<string>> getDisabledEventGuids, ModuleSettings moduleSettings, Gw2ApiManager apiManager, IconService iconService, TranslationService translationService, BitmapFont font = null) : base(apiManager, iconService, translationService, font)
    {
        this._dynamicEventService = dynamicEventService;
        this._getDisabledEventGuids = getDisabledEventGuids;
        this._moduleSettings = moduleSettings;

        this._dynamicEventService.CustomEventsUpdated += this.DynamicEventService_CustomEventsUpdated;
    }

    private Task DynamicEventService_CustomEventsUpdated(object sender)
    {
        this.MainPanel.ClearChildren();
        this.InternalBuild(this.MainPanel);
        return Task.CompletedTask;
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
            eventPanel.FilterChildren<DataDetailsButton<DynamicEvent>>(detailsButton =>
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

                eventPanel.FilterChildren<DataDetailsButton<DynamicEvent>>(detailsButton =>
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

                if (control is DataDetailsButton<DynamicEvent> detailsButton && detailsButton.Visible)
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

                if (control is DataDetailsButton<DynamicEvent> detailsButton && detailsButton.Visible)
                {
                    if (detailsButton.Children.Last() is GlowButton glowButton)
                    {
                        glowButton.Checked = false;
                    }
                }
            });
        };

        List<DynamicEvent> eventList = this._dynamicEventService.Events /*.Where(e => !string.IsNullOrWhiteSpace(e.Name))*/.ToList();
        foreach (Map map in maps.Where(m => m.Id == GameService.Gw2Mumble.CurrentMap.Id)) // Limit to current map at the moment. Due to performance limits.
        {
            IEnumerable<DynamicEvent> events = eventList.Where(e => e.MapId == map.Id).OrderByDescending(e => e.IsCustom);
            foreach (DynamicEvent e in events)
            {
                bool enabled = !this._getDisabledEventGuids().Contains(e.ID);

                DataDetailsButton<DynamicEvent> button = new DataDetailsButton<DynamicEvent>
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

                GlowButton editButton = new GlowButton()
                {
                    Parent = button,
                    ToggleGlow = false,
                    Icon = this.IconService.GetIcon("156706.png")
                };

                editButton.Click += (s, eArgs) =>
                {
                    this._editEventWindow ??= WindowUtil.CreateStandardWindow(this._moduleSettings, "Edit Dynamic Event", this.GetType(), Guid.Parse("5e20b5b0-d0a8-4e36-b65b-3bfc9c971d3d"), this.IconService);

                    if (this._editEventWindow.CurrentView != null)
                    {
                        EditDynamicEventView editEventView = this._editEventWindow.CurrentView as EditDynamicEventView;
                        editEventView.SaveClicked -= this.EditEventView_SaveClicked;
                        editEventView.RemoveClicked -= this.EditEventView_RemoveClicked;
                        editEventView.CloseRequested -= this.EditEventView_CloseRequested;
                    }

                    EditDynamicEventView view = new EditDynamicEventView(e.CopyWithJson(new Newtonsoft.Json.JsonSerializerSettings()), this.APIManager, this.IconService, this.TranslationService);
                    view.SaveClicked += this.EditEventView_SaveClicked;
                    view.RemoveClicked += this.EditEventView_RemoveClicked;
                    view.CloseRequested += this.EditEventView_CloseRequested;

                    this._editEventWindow.Show(view);
                };

                if (e.IsCustom)
                {
                    GlowButton isCustomInfo = new GlowButton()
                    {
                        Parent = button,
                        ToggleGlow = false,
                        Icon = this.IconService.GetIcon("440023.png"),
                        BasicTooltipText = "This event is customized!"
                    };
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

    private void EditEventView_CloseRequested(object sender, EventArgs e)
    {
        this._editEventWindow.Hide();
    }

    private async Task EditEventView_RemoveClicked(object sender, DynamicEvent e)
    {
        await this._dynamicEventService.RemoveCustomEvent(e.ID);

        await this._dynamicEventService.NotifyCustomEventsUpdated();
    }

    private async Task EditEventView_SaveClicked(object sender, DynamicEvent e)
    {
        await this._dynamicEventService.AddCustomEvent(e);

        await this._dynamicEventService.NotifyCustomEventsUpdated();
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
            Logger.Warn(ex, "Failed to add maps:");
            return false;
        }
    }

    protected override void Unload()
    {
        base.Unload();

        this._dynamicEventService.CustomEventsUpdated -= this.DynamicEventService_CustomEventsUpdated;

        this._editEventWindow?.Dispose();
        this._editEventWindow = null;
    }
}