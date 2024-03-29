﻿namespace Estreya.BlishHUD.Shared.UI.Views;

using Blish_HUD;
using Blish_HUD.Common.UI.Views;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Graphics.UI;
using Blish_HUD.Modules.Managers;
using Microsoft.Xna.Framework;
using Services;
using System;
using System.Threading.Tasks;

public class TooltipView : BaseView, ITooltipView, IView
{
    public TooltipView(string title, string description, TranslationService translationService, Gw2ApiManager apiManager = null, IconService iconService = null) : base(apiManager, iconService, translationService)
    {
        this.Title = title;
        this.Description = description;
    }

    public TooltipView(string title, string description, AsyncTexture2D icon, TranslationService translationService, Gw2ApiManager apiManager = null, IconService iconService = null) : this(title, description, translationService, apiManager, iconService)
    {
        this.Icon = icon;
    }

    private string Title { get; }
    private string Description { get; }
    private AsyncTexture2D Icon { get; }

    public Color NameTextColor { get; set; } = Color.White;
    public Color DescriptionTextColor { get; set; } = Control.StandardColors.DisabledText;

    protected override void InternalBuild(Panel parent)
    {
        //buildPanel.Size = new Point(300, 256);
        parent.HeightSizingMode = SizingMode.AutoSize;
        parent.WidthSizingMode = SizingMode.AutoSize;

        Image image = new Image
        {
            Size = new Point(48, 48),
            Location = new Point(8, 8),
            Parent = parent,
            Texture = this.Icon
        };

        Label nameLabel = new Label
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
            Parent = parent,
            TextColor = this.NameTextColor
        };

        Label descriptionLabel = new Label
        {
            AutoSizeHeight = true,
            AutoSizeWidth = false,
            Location = new Point(nameLabel.Left, image.Top + (image.Height / 2)),
            Padding = new Thickness(0, 8, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Middle,
            TextColor = this.DescriptionTextColor,
            WrapText = true,
            Text = this.Description,
            Parent = parent,
            Width = Math.Max(nameLabel.Width, 500)
        };
    }

    protected override Task<bool> InternalLoad(IProgress<string> progress)
    {
        return Task.FromResult(true);
    }
}