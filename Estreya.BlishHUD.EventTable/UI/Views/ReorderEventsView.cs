namespace Estreya.BlishHUD.EventTable.UI.Views;

using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Microsoft.Xna.Framework;
using Models;
using MonoGame.Extended.BitmapFonts;
using Shared.Controls;
using Shared.Services;
using Shared.UI.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class ReorderEventsView : BaseView
{
    private static readonly Point MAIN_PADDING = new Point(20, 20);

    private static readonly Logger Logger = Logger.GetLogger<ReorderEventsView>();
    private readonly List<EventCategory> _allEvents;
    private readonly EventAreaConfiguration _areaConfiguration;
    private readonly List<string> _order;

    public ReorderEventsView(List<EventCategory> allEvents, List<string> order, EventAreaConfiguration areaConfiguration, Gw2ApiManager apiManager, IconService iconService, TranslationService translationService) : base(apiManager, iconService, translationService)
    {
        this._allEvents = allEvents;
        this._order = order;
        this._areaConfiguration = areaConfiguration;
    }

    private Panel Panel { get; set; }

    public event EventHandler<(EventAreaConfiguration AreaConfiguration, string[] CategoryKeys)> SaveClicked;

    private void DrawEntries(ListView<EventCategory> listView)
    {
        listView.ClearChildren();

        //Random random = new Random();

        foreach (EventCategory eventCategory in this._allEvents.GroupBy(ec => ec.Key).Select(g => g.First()).OrderBy(x => this._order.IndexOf(x.Key)))
        {
            ListEntry<EventCategory> entry = new ListEntry<EventCategory>(eventCategory.Name)
            {
                Parent = listView,
                Width = listView.Width - 20,
                DragDrop = true,
                TextColor = Color.White,
                Data = eventCategory,
                Alignment = HorizontalAlignment.Center
                //BackgroundColor = new Color(random.Next(0, 256), random.Next(0, 256), random.Next(0, 256))
            };

            if (eventCategory.Icon != null)
            {
                entry.Icon = this.IconService.GetIcon(eventCategory.Icon);
            }
        }
    }

    protected override Task<bool> InternalLoad(IProgress<string> progress)
    {
        return Task.FromResult(true);
    }

    protected override void InternalBuild(Panel parent)
    {
        this.Panel = new Panel
        {
            Parent = parent,
            Location = new Point(MAIN_PADDING.X, MAIN_PADDING.Y),
            Width = parent.ContentRegion.Width - MAIN_PADDING.Y,
            Height = parent.ContentRegion.Height - MAIN_PADDING.X,
            CanScroll = true
        };

        Rectangle contentRegion = this.Panel.ContentRegion;

        ListView<EventCategory> listView = new ListView<EventCategory>
        {
            Parent = this.Panel,
            Location = new Point(Control.ControlStandard.ControlOffset.X, contentRegion.Y),
            WidthSizingMode = SizingMode.Standard,
            HeightSizingMode = SizingMode.Standard
        };

        listView.Size = new Point(contentRegion.Width - listView.Left - MAIN_PADDING.X, contentRegion.Height - (int)(StandardButton.STANDARD_CONTROL_HEIGHT * 1.25));

        Panel buttons = new Panel
        {
            Parent = this.Panel,
            Location = new Point(listView.Left, listView.Bottom),
            Size = new Point(listView.Width, StandardButton.STANDARD_CONTROL_HEIGHT)
        };

        StandardButton saveButton = new StandardButton
        {
            Text = "Save",
            Parent = buttons,
            Right = buttons.Width,
            Bottom = buttons.Height
        };
        saveButton.Click += async (s, e) =>
        {
            Logger.Debug("Save reordered categories.");
            List<EventCategory> orderedCategories = listView.Children.ToList().Select(child =>
            {
                return ((ListEntry<EventCategory>)child).Data;
            }).ToList();

            // Get copy of current categories;
            List<EventCategory> currentCategories = this._allEvents;

            foreach (EventCategory category in orderedCategories)
            {
                int oldIndex = currentCategories.IndexOf(currentCategories.Where(ec => ec.Key == category.Key).First());
                int newIndex = orderedCategories.IndexOf(category);

                currentCategories.RemoveAt(oldIndex);
                if (newIndex > oldIndex)
                {
                    newIndex--;
                }

                currentCategories.Insert(newIndex, category);
            }

            this.SaveClicked?.Invoke(this, (this._areaConfiguration, currentCategories.Select(x => x.Key).ToArray()));

            /*Logger.Debug("Load current external file.");
            EventSettingsFile eventSettingsFile = await EventTableModule.ModuleInstance.EventFileService.GetLocalFile();
            eventSettingsFile.EventCategories = currentCategories;
            Logger.Debug("Export updated file.");
            await EventTableModule.ModuleInstance.EventFileService.ExportFile(eventSettingsFile);
            Logger.Debug("Reload events.");
            await EventTableModule.ModuleInstance.LoadEvents();
            Shared.Controls.ScreenNotification.ShowNotification(Strings.ReorderEventsView_Save_Success);*/
        };

        StandardButton resetButton = new StandardButton
        {
            Text = "Reset",
            Parent = buttons,
            Right = saveButton.Left,
            Bottom = buttons.Height
        };
        resetButton.Click += (s, e) =>
        {
            Logger.Debug("Reset current view");
            this.DrawEntries(listView);
        };

        this.DrawEntries(listView);
    }
}