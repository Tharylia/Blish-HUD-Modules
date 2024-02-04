using Humanizer;

namespace Estreya.BlishHUD.ArcDPSLogManager.UI.Views;

using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.ArcDPSLogManager.UI.Views;
using Estreya.BlishHUD.ArcDPSLogManager.Models;
using Microsoft.Xna.Framework;
using MonoGame.Extended.BitmapFonts;
using Shared.Services;
using Shared.UI.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blish_HUD;

public class LogManagerView : BaseView
{
    private const int PADDING_X = 20;
    private const int PADDING_Y = 20;
    private readonly Func<List<LogData>> _getLogData;
    private readonly ModuleSettings _moduleSettings;
    private FlowPanel _logList;
    private Dictionary<string, MenuItem> _menuItems;

    private List<LogData> _logs; 

    public LogManagerView(ModuleSettings moduleSettings, Func<List<LogData>> getLogData, Gw2ApiManager apiManager, IconService iconService, TranslationService translationService) : base(apiManager, iconService, translationService)
    {
        this._moduleSettings = moduleSettings;
        this._getLogData = getLogData;
    }

    private void LoadLogs()
    {
        var logData = this._getLogData()
            .OrderByDescending(l => l.EncounterStartTime.HasValue ? l.EncounterStartTime.Value.Date : DateTime.MaxValue)
            .ThenByDescending(l => l.EncounterResult == Models.Enums.EncounterResult.Success)
            .Take(100)
            .ToList();

        this._logs = logData;
    }

    protected override void InternalBuild(Panel parent)
    {
        this.LoadLogs();
        //this._menuItems = new Dictionary<string, MenuItem>();
        //Rectangle bounds = new Rectangle(PADDING_X, PADDING_Y, parent.ContentRegion.Width - PADDING_X, parent.ContentRegion.Height - (PADDING_Y * 2));

        //Panel typeOverviewPanel = this.GetPanel(parent);
        //typeOverviewPanel.ShowBorder = true;
        //typeOverviewPanel.CanScroll = true;
        //typeOverviewPanel.HeightSizingMode = SizingMode.Standard;
        //typeOverviewPanel.WidthSizingMode = SizingMode.Standard;
        //typeOverviewPanel.Location = new Point(bounds.X, bounds.Y);
        //typeOverviewPanel.Size = new Point(Panel.MenuStandard.Size.X - 75, bounds.Height - StandardButton.STANDARD_CONTROL_HEIGHT);

        //Menu typeOverviewMenu = new Menu
        //{
        //    Parent = typeOverviewPanel,
        //    WidthSizingMode = SizingMode.Fill
        //};

        //foreach (EventAreaConfiguration areaConfiguration in this._logs)
        //{
        //    string itemName = areaConfiguration.Name;

        //    if (string.IsNullOrWhiteSpace(itemName))
        //    {
        //        continue;
        //    }

        //    MenuItem menuItem = new MenuItem(itemName)
        //    {
        //        Parent = typeOverviewMenu,
        //        Text = itemName,
        //        WidthSizingMode = SizingMode.Fill,
        //        HeightSizingMode = SizingMode.AutoSize
        //    };

        //    this._menuItems.Add(itemName, menuItem);
        //}

        //int x = typeOverviewPanel.Right + Panel.MenuStandard.PanelOffset.X;
        //Rectangle areaPanelBounds = new Rectangle(x, bounds.Y, bounds.Width - x, bounds.Height);

        //this._menuItems.ToList().ForEach(menuItem =>
        //{
        //    menuItem.Value.Click += (s, e) =>
        //    {
        //        EventAreaConfiguration areaConfiguration = this._areaConfigurations.Where(areaConfiguration => areaConfiguration.Name == menuItem.Key).First();
        //        this.BuildEditPanel(newParent, areaPanelBounds, menuItem.Value, areaConfiguration);
        //    };
        //});

        this._logList = new FlowPanel
        {
            Parent = parent,
            FlowDirection = ControlFlowDirection.SingleTopToBottom,
            Size = parent.ContentRegion.Size,
            CanScroll = true,
            OuterControlPadding = new Microsoft.Xna.Framework.Vector2(20,20),
            ControlPadding = new Microsoft.Xna.Framework.Vector2(0,5)
        };

        _logList.MouseWheelScrolled += this.LogList_MouseWheelScrolled;

        foreach (var log in this._logs)
        {
            //var panel = new Panel()
            //{
            //    Parent = this._logList,
            //    Width = this._logList.ContentRegion.Width - (int)this._logList.OuterControlPadding.X * 2,
            //    HeightSizingMode = SizingMode.AutoSize,
            //    Title = this.GetLogTitle(log),
            //    CanCollapse = true,
            //    Collapsed = true
            //};

            var viewContainer = new ViewContainer()
            {
                Parent = this._logList,
                Width = this._logList.ContentRegion.Width - (int)this._logList.OuterControlPadding.X * 2,
                HeightSizingMode = SizingMode.AutoSize,
                //CanCollapse = true,
                //Collapsed = true,
                //ShowBorder= true,
                ////AutoSizePadding = new Point(0, 5),
                Title = log.GetLogTitle()

            };

            viewContainer.Show(new LogOverviewView(log,this.APIManager, this.IconService, this.TranslationService));
        }
    }

    private void LogList_MouseWheelScrolled(object sender, Blish_HUD.Input.MouseEventArgs e)
    {
        var heightSum = this._logList.Children.Sum(c => c.Height);
        var scrollingOffset = this._logList.VerticalScrollOffset;
        var percent = scrollingOffset * 100 / heightSum;
        //this._logger.Debug($"{scrollingOffset} - {heightSum} - {percent}");
    }

    public void Rebuild()
    {
        this.InternalBuild(this.MainPanel);
    }

    protected override Task<bool> InternalLoad(IProgress<string> progress)
    {
        return Task.FromResult(true);
    }

    protected override void Unload()
    {
        _logList.MouseWheelScrolled -= this.LogList_MouseWheelScrolled;

        base.Unload();
    }
}