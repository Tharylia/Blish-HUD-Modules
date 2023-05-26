namespace Estreya.BlishHUD.UniversalSearch.Controls.SearchResults;

using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared.Services;
using System;
using System.Threading.Tasks;

public abstract class SearchResultItem : Control
{
    private const int ICON_SIZE = 32;
    private const int ICON_PADDING = 2;

    private const int DEFAULT_WIDTH = 100;
    private const int DEFAULT_HEIGHT = ICON_SIZE + (ICON_PADDING * 2);

    private string _description;

    private AsyncTexture2D _icon;
    private Rectangle _layoutDescriptionBounds;

    private Rectangle _layoutIconBounds;
    private Rectangle _layoutNameBounds;

    private string _name;

    protected IconService IconService { get; }

    public AsyncTexture2D Icon
    {
        get => this._icon;
        set => this.SetProperty(ref this._icon, value);
    }

    public string Name
    {
        get => this._name;
        set => this.SetProperty(ref this._name, value);
    }

    public string Description
    {
        get => this._description;
        set => this.SetProperty(ref this._description, value);
    }

    protected abstract string ChatLink { get; }

    public event EventHandler<bool> ClickActionExecuted;

    protected override void OnClick(MouseEventArgs e)
    {
        Task.Run(async () =>
        {
            await this.ClickAction();
        });

        base.OnClick(e);
    }

    protected virtual async Task ClickAction()
    {
        if (this.ChatLink != null)
        {
            bool clipboardResult = await ClipboardUtil.WindowsClipboardService.SetTextAsync(this.ChatLink);

            this.SignalClickActionExecuted(clipboardResult);
        }
    }

    protected void SignalClickActionExecuted(bool success)
    {
        this.ClickActionExecuted?.Invoke(this, success);
    }

    protected virtual Tooltip BuildTooltip()
    {
        return null;
    }

    protected override void OnMouseEntered(MouseEventArgs e)
    {
        this.Tooltip ??= this.BuildTooltip();
        this.Tooltip?.Show(this.AbsoluteBounds.Location + new Point(this.Width + 5, 0));

        base.OnMouseEntered(e);
    }

    public override void RecalculateLayout()
    {
        this._layoutIconBounds = new Rectangle(ICON_PADDING, ICON_PADDING, ICON_SIZE, ICON_SIZE);

        int iconRight = this._layoutIconBounds.Right + ICON_PADDING;

        this._layoutNameBounds = new Rectangle(iconRight, 0, this._size.X - iconRight, 20);
        this._layoutDescriptionBounds = new Rectangle(iconRight, this._layoutNameBounds.Bottom, this._size.X - iconRight, 16);
    }

    /// <inheritdoc />
    protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds)
    {
        if (this._mouseOver)
        {
            spriteBatch.DrawOnCtrl(this, this._textureItemHover, bounds, Color.White * 0.5f);
        }

        if (this._icon != null)
        {
            spriteBatch.DrawOnCtrl(this, this._icon, this._layoutIconBounds);
        }

        spriteBatch.DrawStringOnCtrl(this, this._name, Content.DefaultFont14, this._layoutNameBounds, Color.White, false, false, verticalAlignment: VerticalAlignment.Bottom);
        spriteBatch.DrawStringOnCtrl(this, this._description, Content.DefaultFont14, this._layoutDescriptionBounds, ContentService.Colors.Chardonnay, false, false, verticalAlignment: VerticalAlignment.Top);
    }

    #region Load Static

    private readonly AsyncTexture2D _textureItemHover;

    public SearchResultItem(IconService iconState)
    {
        this.IconService = iconState;
        this._textureItemHover = this.IconService.GetIcon("1234875.png");
        this.Size = new Point(DEFAULT_WIDTH, DEFAULT_HEIGHT);
    }

    #endregion
}