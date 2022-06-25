namespace Estreya.BlishHUD.Shared.UI.Views
{
    using Blish_HUD;
    using Blish_HUD.Common.UI.Views;
    using Blish_HUD.Controls;
    using Blish_HUD.Graphics.UI;
    using Microsoft.Xna.Framework;
    using System;

    public class StringTooltipView : View, ITooltipView, IView
    {
        private string Message { get; set; }
        private int MaxWidth { get; } = 200;
        public StringTooltipView(string message)
        {
            this.Message = message;
        }

        public StringTooltipView(string message, int maxWidth)
        {
            this.Message = message;
            this.MaxWidth = maxWidth;
        }

        protected override void Build(Container buildPanel)
        {
            //buildPanel.Size = new Point(300, 256);
            buildPanel.HeightSizingMode = SizingMode.AutoSize;
            buildPanel.WidthSizingMode = SizingMode.AutoSize;

            _ = new Label()
            {
                AutoSizeHeight = true,
                AutoSizeWidth = false,
                Width = this.MaxWidth,
                Padding = new Thickness(0, 8, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Middle,
                TextColor = Control.StandardColors.DisabledText,
                WrapText = true,
                Text = this.Message,
                Parent = buildPanel
            };
        }
    }
}
