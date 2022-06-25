namespace Estreya.BlishHUD.Shared.UI.Views
{
    using Blish_HUD.Controls;
    using Blish_HUD.Graphics.UI;
    using Microsoft.Xna.Framework;
    using MonoGame.Extended.BitmapFonts;
    using System;
    using System.Threading.Tasks;

    public class ModuleSettingsView : View
    {
        private readonly string _openSettingsText;

        public BitmapFont Font { get; set; }

        public event EventHandler OpenClicked;

        public ModuleSettingsView(string openSettingsText)
        {
            this._openSettingsText = openSettingsText;
        }

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

            string buttonText = _openSettingsText;

            StandardButton button = new StandardButton()
            {
                Parent = settingContainer,
                Text = buttonText,
                            };

            if (this.Font != null)
            {
                button.Width = (int)this.Font.MeasureString(buttonText).Width;
            }

            button.Location = new Point(Math.Max(buildPanel.Width / 2 - button.Width / 2, 20), Math.Max(buildPanel.Height / 2 - button.Height, 20));

            button.Click += (s, e) => this.OpenClicked?.Invoke(this, EventArgs.Empty);
        }
    }
}
