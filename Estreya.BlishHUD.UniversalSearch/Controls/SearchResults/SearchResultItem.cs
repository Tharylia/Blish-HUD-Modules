namespace Estreya.BlishHUD.UniversalSearch.Controls.SearchResults;
using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Estreya.BlishHUD.Shared.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Threading.Tasks;

public abstract class SearchResultItem : Control
{
    private const int ICON_SIZE = 32;
    private const int ICON_PADDING = 2;

    private const int DEFAULT_WIDTH = 100;
    private const int DEFAULT_HEIGHT = ICON_SIZE + (ICON_PADDING * 2);

    protected IconService IconService { get; private set; }

    public event EventHandler<bool> ClickActionExecuted;

    #region Load Static

    private AsyncTexture2D _textureItemHover;

    public SearchResultItem(IconService iconState)
    {
        this.IconService = iconState;
        this._textureItemHover = this.IconService.GetIcon("1234875.png");
        this.Size = new Point(DEFAULT_WIDTH, DEFAULT_HEIGHT);
    }

    #endregion

    private AsyncTexture2D _icon;
    public AsyncTexture2D Icon
    {
        get => this._icon;
        set => this.SetProperty(ref this._icon, value);
    }

    private string _name;
    public string Name
    {
        get => this._name;
        set => this.SetProperty(ref this._name, value);
    }

    private string _description;
    public string Description
    {
        get => this._description;
        set => this.SetProperty(ref this._description, value);
    }

    protected abstract string ChatLink { get; }

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
            var clipboardResult = await ClipboardUtil.WindowsClipboardService.SetTextAsync(this.ChatLink);

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

    private Rectangle _layoutIconBounds;
    private Rectangle _layoutNameBounds;
    private Rectangle _layoutDescriptionBounds;

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
}
