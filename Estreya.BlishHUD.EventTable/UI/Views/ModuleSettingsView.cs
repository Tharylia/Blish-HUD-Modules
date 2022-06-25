namespace Estreya.BlishHUD.EventTable.UI.Views
{
    using Blish_HUD.Controls;
    using Blish_HUD.Graphics.UI;
    using Estreya.BlishHUD.EventTable.Resources;
    using Microsoft.Xna.Framework;
    using System;
    using System.Threading.Tasks;

    public class ModuleSettingsView : View
    {
        protected override Task<bool> Load(IProgress<string> progress)
        {
            return Task.FromResult(true);
        }

        protected override void Build(Container buildPanel)
        {
            Rectangle bounds = buildPanel.ContentRegion;

            FlowPanel parentPanel = new FlowPanel()
            {
                Size = bounds.Size,
                FlowDirection = ControlFlowDirection.SingleTopToBottom,
                ControlPadding = new Vector2(5, 2),
                OuterControlPadding = new Vector2(10, 15),
                WidthSizingMode = SizingMode.Fill,
                HeightSizingMode = SizingMode.AutoSize,
                AutoSizePadding = new Point(0, 15),
                Parent = buildPanel
            };

            ViewContainer settingContainer = new ViewContainer()
            {
                WidthSizingMode = SizingMode.Fill,
                HeightSizingMode = SizingMode.AutoSize,
                Parent = parentPanel
            };

            string buttonText = Strings.SettingsView_OpenSettings;

            StandardButton button = new StandardButton()
            {
                Parent = settingContainer,
                Text = buttonText,
                Width = (int)EventTableModule.ModuleInstance.Font.MeasureString(buttonText).Width,
            };

            button.Location = new Point(Math.Max(buildPanel.Width / 2 - button.Width / 2, 20), Math.Max(buildPanel.Height / 2 - button.Height, 20));

            button.Click += (s, e) => EventTableModule.ModuleInstance.SettingsWindow.ToggleWindow();
        }
    }
}
