namespace Estreya.BlishHUD.Shared.UI.Views
{
    using Blish_HUD;
    using Blish_HUD.Common.UI.Views;
    using Blish_HUD.Content;
    using Blish_HUD.Controls;
    using Blish_HUD.Graphics.UI;
    using Microsoft.Xna.Framework;
    using System;

    public class TooltipView : View, ITooltipView, IView
    {
        private string Title { get; set; }
        private string Description { get; set; }
        private AsyncTexture2D Icon { get; set; }
        public TooltipView(string title, string description)
        {
            this.Title = title;
            this.Description = description;
        }
        public TooltipView(string title, string description, AsyncTexture2D icon) : this(title, description)
        {
            this.Icon = icon;
        }

        protected override void Build(Container buildPanel)
        {
            //buildPanel.Size = new Point(300, 256);
            buildPanel.HeightSizingMode = SizingMode.AutoSize;
            buildPanel.WidthSizingMode = SizingMode.AutoSize;

            Image image = new Image()
            {
                Size = new Point(48, 48),
                Location = new Point(8, 8),
                Parent = buildPanel,
                Texture = this.Icon
            };

            Label nameLabel = new Label()
            {
                AutoSizeHeight = false,
                AutoSizeWidth = true,
                Location = new Point(image.Right + 8, image.Top),
                Height = image.Height / 2,
                Padding = new Thickness(0, 8, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Middle,
                Font = GameService.Content.DefaultFont16,
                Text = this.Title,
                Parent = buildPanel
            };

            Label descriptionLabel = new Label()
            {
                AutoSizeHeight = true,
                AutoSizeWidth = false,
                Location = new Point(nameLabel.Left, image.Top + image.Height / 2),
                Padding = new Thickness(0, 8, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Middle,
                TextColor = Control.StandardColors.DisabledText,
                WrapText = false,
                Text = this.Description,
                Parent = buildPanel
            };

            descriptionLabel.Width = (int)Math.Ceiling(Math.Max(nameLabel.Width, descriptionLabel.Font.MeasureString(descriptionLabel.Text).Width));
        }
    }
}
