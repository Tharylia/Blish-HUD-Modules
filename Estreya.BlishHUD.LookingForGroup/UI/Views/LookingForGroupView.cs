namespace Estreya.BlishHUD.LookingForGroup.UI.Views;

using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.LookingForGroup.Controls;
using Estreya.BlishHUD.Shared.Threading.Events;
using Microsoft.Xna.Framework;
using Models;
using MonoGame.Extended.BitmapFonts;
using Shared.Services;
using Shared.UI.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class LookingForGroupView : BaseView
{
    private readonly AccountService _accountService;
    private Func<IEnumerable<CategoryDefinition>> _getCategories;
    private readonly Func<IEnumerable<Models.LFGEntry>> _getEntries;
    private readonly Func<int> _getMapId;
    private List<CategoryDefinition> _categories;
    private MapDefinition _selectedMap;
    private FlowPanel _groupSelectionPanel;

    public event AsyncEventHandler<Models.LFGEntry> JoinClicked;

    public LookingForGroupView(AccountService accountService, Func<IEnumerable<CategoryDefinition>> getCategories, Func<IEnumerable<Models.LFGEntry>> getEntries, Func<int> getMapId, Gw2ApiManager apiManager, IconService iconService, TranslationService translationService, BitmapFont font = null) : base(apiManager, iconService, translationService, font)
    {
        this._accountService = accountService;
        this._getCategories = getCategories;
        this._getEntries = getEntries;
        this._getMapId = getMapId;
    }

    protected override Task<bool> InternalLoad(IProgress<string> progress) => Task.FromResult(true);

    protected override void InternalBuild(Panel parent)
    {
        this._categories = this._getCategories()?.ToList() ?? new List<CategoryDefinition>();
        var mapId = this._getMapId();
        
        var mapSelectionMenu = new FlowPanel()
        {
            Location = new Microsoft.Xna.Framework.Point(10,40),
            Parent = parent,
            Height = parent.ContentRegion.Height - 20,
            Width = (parent.ContentRegion.Width /3) - Control.ControlStandard.PanelOffset.X,
            FlowDirection = ControlFlowDirection.SingleTopToBottom,
            CanScroll=true
        };

        this._groupSelectionPanel = new FlowPanel()
        {
            Parent = parent,
            Top = mapSelectionMenu.Top,
            Height = mapSelectionMenu.Height,
            Left = mapSelectionMenu.Right + Control.ControlStandard.PanelOffset.X,
            FlowDirection = ControlFlowDirection.SingleTopToBottom
        };

        _groupSelectionPanel.Width = parent.ContentRegion.Width - _groupSelectionPanel.Left - 20;

        var mapItems = new List<MenuItem>();
        foreach (CategoryDefinition categoryDefinition in this._categories)
        {
            var categoryMenu = new FlowPanel()
            {
                Parent = mapSelectionMenu,
                CanCollapse = true,
                Collapsed = true,
                FlowDirection = ControlFlowDirection.SingleTopToBottom,
                WidthSizingMode = SizingMode.Fill,
                Title = categoryDefinition.Name,
                BasicTooltipText = categoryDefinition.Description,
                HeightSizingMode = SizingMode.AutoSize
            };

            foreach (MapDefinition mapDefinition in categoryDefinition.Maps)
            {
                var mapItem =new MenuItem()
                {
                    MenuItemHeight = 25,
                    Parent = categoryMenu,
                    WidthSizingMode = SizingMode.Fill,
                    HeightSizingMode = SizingMode.AutoSize,
                    Text = mapDefinition.Name,
                    CanCheck = false,
                };

                mapItem.ItemSelected += (s, e) =>
                {
                    mapItems.ForEach(mi =>
                    {
                        if (mi == mapItem) return;

                        mi.Deselect();
                    });

                    this._selectedMap = mapDefinition;
                    this.BuildMapGroupSection(_groupSelectionPanel, mapDefinition);
                };

                mapItems.Add(mapItem);

                if (this._selectedMap is null && mapDefinition.MapId == mapId)
                {
                    categoryMenu.Expand();
                    mapItem.Select();
                }

                if (this._selectedMap == mapDefinition)
                {
                    categoryMenu.Expand();
                    mapItem.Select();
                }
            }
        }
    }

    private void BuildMapGroupSection(FlowPanel groupSelectionPanel, MapDefinition mapDefinition)
    {
        groupSelectionPanel.Children?.Clear();

        if (mapDefinition is null) return;

        if (!mapDefinition.Category.TryGetTarget(out var category)) return;


        new Label()
        {
            Parent = groupSelectionPanel,
            Text = $"{category.Name} - {mapDefinition.Name}",
            HorizontalAlignment = HorizontalAlignment.Center,
            Width = groupSelectionPanel.ContentRegion.Width,
            Font = GameService.Content.DefaultFont18,
        };

        this.RenderEmptyLine(groupSelectionPanel, 10);

        var groupList = new FlowPanel()
        {
            // Dont assign parent here
            FlowDirection = ControlFlowDirection.SingleTopToBottom,
            CanScroll = true,
            ShowBorder = true,
            OuterControlPadding = new Vector2(10,10),
        };

        var searchTextBox = this.RenderTextbox(groupSelectionPanel, Point.Zero, groupSelectionPanel.ContentRegion.Width, null, "Search...", newVal =>
        {
            if (string.IsNullOrWhiteSpace(newVal)) groupList.FilterChildren<Controls.LFGEntry>(entry =>true);

            groupList.FilterChildren<Controls.LFGEntry>(entry => entry.Model.Description.Contains(newVal));
        });

        this.RenderEmptyLine(groupSelectionPanel, 10);

        groupList.Parent = groupSelectionPanel;
        groupList.Width = groupSelectionPanel.ContentRegion.Width;
        groupList.Height = groupSelectionPanel.ContentRegion.Height - groupSelectionPanel.Children.Last().Bottom;

        foreach ( var item in this._getEntries().Where(e => e.CategoryKey == category.Key && e.MapKey == mapDefinition.Key)) {
            var lfgEntry = new Controls.LFGEntry(item, item.Players.Any(p => p.AccountName == this._accountService.Account?.Name))
            {
                Parent = groupList,
                Width = groupList.ContentRegion.Width - 20,
                Height = 100,
            };

            lfgEntry.Build();

            lfgEntry.JoinClicked += this.LfgEntry_JoinClicked;
        }
    }

    public void UpdateEntries()
    {
        this.BuildMapGroupSection(this._groupSelectionPanel, this._selectedMap);
    }

    private async Task LfgEntry_JoinClicked(object sender)
    {
        try
        {
            var entry = sender as Controls.LFGEntry;

            await (this.JoinClicked?.Invoke(this, entry.Model) ?? Task.CompletedTask);
        }
        catch (Exception ex)
        {
            this.ShowError(ex.Message);
        }
    }

    protected override void Unload()
    {
        base.Unload();

        this._selectedMap = null;
        this._groupSelectionPanel = null;
        this._categories?.Clear();
        this._categories = null;
    }
}