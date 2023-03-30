namespace Estreya.BlishHUD.UniversalSearch.Controls.SearchResults;
using Blish_HUD.Content;

using Blish_HUD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blish_HUD.Input;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Estreya.BlishHUD.Shared.State;

public abstract class SearchResultItem : Control
{
    private const int ICON_SIZE = 32;
    private const int ICON_PADDING = 2;

    private const int DEFAULT_WIDTH = 100;
    private const int DEFAULT_HEIGHT = ICON_SIZE + ICON_PADDING * 2;

    public event EventHandler<bool> ClickActionExecuted;

    #region Load Static

    private AsyncTexture2D _textureItemHover;

    public SearchResultItem(IconState iconState)
    {
        _textureItemHover = iconState.GetIcon("1234875.png");
        this.Size = new Point(DEFAULT_WIDTH, DEFAULT_HEIGHT);
    }

    #endregion

    private AsyncTexture2D _icon;
    public AsyncTexture2D Icon
    {
        get => _icon;
        set => SetProperty(ref _icon, value);
    }

    private string _name;
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    private string _description;
    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    protected abstract string ChatLink { get; }

    protected override void OnClick(MouseEventArgs e)
    {
        Task.Run(async () =>
        {
            await ClickAction();
        });

        base.OnClick(e);
    }

    protected virtual async Task ClickAction()
    {
        if (ChatLink != null)
        {
            var clipboardResult = await ClipboardUtil.WindowsClipboardService.SetTextAsync(ChatLink);

            this.SignalClickActionExecuted(clipboardResult);
        }
    }

    protected void SignalClickActionExecuted(bool success)
    {
        this.ClickActionExecuted?.Invoke(this, success);
    }

    protected virtual Tooltip BuildTooltip()
        => null;

    protected override void OnMouseEntered(MouseEventArgs e)
    {
        if (Tooltip == null)
        {
            Tooltip = BuildTooltip();
        }

        Tooltip?.Show(AbsoluteBounds.Location + new Point(Width + 5, 0));

        base.OnMouseEntered(e);
    }

    private Rectangle _layoutIconBounds;
    private Rectangle _layoutNameBounds;
    private Rectangle _layoutDescriptionBounds;

    public override void RecalculateLayout()
    {
        _layoutIconBounds = new Rectangle(ICON_PADDING, ICON_PADDING, ICON_SIZE, ICON_SIZE);

        int iconRight = _layoutIconBounds.Right + ICON_PADDING;

        _layoutNameBounds = new Rectangle(iconRight, 0, _size.X - iconRight, 20);
        _layoutDescriptionBounds = new Rectangle(iconRight, _layoutNameBounds.Bottom, _size.X - iconRight, 16);
    }

    /// <inheritdoc />
    protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds)
    {
        if (_mouseOver)
        {
            spriteBatch.DrawOnCtrl(this, _textureItemHover, bounds, Color.White * 0.5f);
        }

        if (_icon != null)
        {
            spriteBatch.DrawOnCtrl(this, _icon, _layoutIconBounds);
        }

        spriteBatch.DrawStringOnCtrl(this, _name, Content.DefaultFont14, _layoutNameBounds, Color.White, false, false, verticalAlignment: VerticalAlignment.Bottom);
        spriteBatch.DrawStringOnCtrl(this, _description, Content.DefaultFont14, _layoutDescriptionBounds, ContentService.Colors.Chardonnay, false, false, verticalAlignment: VerticalAlignment.Top);
    }

}
