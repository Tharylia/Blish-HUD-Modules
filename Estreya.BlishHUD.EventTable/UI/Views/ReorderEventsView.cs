namespace Estreya.BlishHUD.EventTable.UI.Views
{
    using Blish_HUD;
    using Blish_HUD.Content;
    using Blish_HUD.Controls;
    using Blish_HUD.Graphics.UI;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.EventTable.Controls;
    using Estreya.BlishHUD.EventTable.Models;
    using Estreya.BlishHUD.EventTable.Models.Settings;
    using Estreya.BlishHUD.EventTable.Resources;
    using Microsoft.Xna.Framework;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class ReorderEventsView : View
    {
        private static Point MAIN_PADDING = new Point(20, 20);

        private static readonly Logger Logger = Logger.GetLogger<ReorderEventsView>();

        public Panel Panel { get; private set; }

        protected override void Build(Container buildPanel)
        {
            this.Panel = new Panel
            {
                Parent = buildPanel,
                Location = new Point(MAIN_PADDING.X, MAIN_PADDING.Y),
                Width = buildPanel.ContentRegion.Width - MAIN_PADDING.Y,
                Height = buildPanel.ContentRegion.Height - MAIN_PADDING.X,
                CanScroll = true
            };

            Rectangle contentRegion = this.Panel.ContentRegion;

            ListView<EventCategory> listView = new ListView<EventCategory>()
            {
                Parent = Panel,
                Location = new Point(Panel.ControlStandard.ControlOffset.X, contentRegion.Y),
                WidthSizingMode = SizingMode.Standard,
                HeightSizingMode = SizingMode.Standard,
            };

            listView.Size = new Point(contentRegion.Width - listView.Left - MAIN_PADDING.X, contentRegion.Height - (int)(StandardButton.STANDARD_CONTROL_HEIGHT * 1.25));

            Panel buttons = new Panel()
            {
                Parent = Panel,
                Location = new Point(listView.Left, listView.Bottom),
                Size = new Point(listView.Width, StandardButton.STANDARD_CONTROL_HEIGHT),
            };

            StandardButton saveButton = new StandardButton()
            {
                Text =  Strings.ReorderEventsView_Save,
                Parent = buttons,
                Right = buttons.Width,
                Bottom = buttons.Height
            };
            saveButton.Click += async (s, e) =>
            {
                Logger.Debug("Save reordered categories.");
                var orderedCategories = listView.Children.ToList().Select(child =>
                {
                    return ((ListEntry<EventCategory>)child).Data;
                }).ToList();

                // Get copy of current categories;
                var currentCategories = EventTableModule.ModuleInstance.EventCategories.ToArray().ToList();

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

                Logger.Debug("Load current external file.");
                EventSettingsFile eventSettingsFile = await EventTableModule.ModuleInstance.EventFileState.GetExternalFile();
                eventSettingsFile.EventCategories = currentCategories;
                Logger.Debug("Export updated file.");
                await EventTableModule.ModuleInstance.EventFileState.ExportFile(eventSettingsFile);
                Logger.Debug("Reload events.");
                await EventTableModule.ModuleInstance.LoadEvents();
                EventTable.Controls.ScreenNotification.ShowNotification(Strings.ReorderEventsView_Save_Success);
            };

            StandardButton resetButton = new StandardButton()
            {
                Text = Strings.ReorderEventsView_Reset,
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

        private void DrawEntries(ListView<EventCategory> listView)
        {
            listView.ClearChildren();

            //Random random = new Random();

            foreach (EventCategory eventCategory in EventTableModule.ModuleInstance.EventCategories.GroupBy(ec => ec.Key).Select(g => g.First()))
            {
                ListEntry<EventCategory> entry = new(eventCategory.Name)
                {
                    Parent = listView,
                    Width = listView.Width - 20,
                    DragDrop = true,
                    TextColor = Color.White,
                    Data = eventCategory,
                    Alignment = HorizontalAlignment.Center,
                    //BackgroundColor = new Color(random.Next(0, 256), random.Next(0, 256), random.Next(0, 256))
                };

                if (eventCategory.Icon != null)
                {
                    GameService.Graphics.QueueMainThreadRender(graphicsDevice =>
                    {
                        entry.Icon = EventTableModule.ModuleInstance.IconState.GetIcon(eventCategory.Icon);
                    });
                }
            }
        }
    }
}
