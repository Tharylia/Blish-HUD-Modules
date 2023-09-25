namespace Estreya.BlishHUD.ValuableItems.Controls;

using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Estreya.BlishHUD.Shared.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class OCRSelection : Control
{
    private readonly AsyncTexture2D _textureWindowResizableCorner = AsyncTexture2D.FromAssetId(156009);
    private readonly AsyncTexture2D _textureWindowResizableCornerActive = AsyncTexture2D.FromAssetId(156010);

    private bool _isDragging = false;
    private Point _dragStartLocation = new Point();
    private Point _resizeStartLocation = new Point();
    private bool _mouseOverResizeIcon = false;
    private bool _isResizing = false;
    private Rectangle _resizeHandleBounds;

    public event EventHandler SelectionConfirmed;
    public event EventHandler SelectionCanceled;

    public OCRSelection()
    {
        this.Parent = GameService.Graphics.SpriteScreen;

        this.LeftMouseButtonPressed += this.OCRSelection_LeftMouseButtonPressed;
        this.LeftMouseButtonReleased += this.OCRSelection_LeftMouseButtonReleased;
        this.MouseMoved += this.OCRSelection_MouseMoved;

        Input.Keyboard.KeyPressed += this.Keyboard_KeyPressed;
    }

    private void OCRSelection_MouseMoved(object sender, Blish_HUD.Input.MouseEventArgs e)
    {
        this._mouseOverResizeIcon = this._resizeHandleBounds.Contains(this.RelativeMousePosition);
    }

    public override void RecalculateLayout()
    {

        // Corner bounds
        this._resizeHandleBounds = new Rectangle(this.Width - _textureWindowResizableCorner.Width,
                                                this.Height - _textureWindowResizableCorner.Height,
                                                _textureWindowResizableCorner.Width,
                                                _textureWindowResizableCorner.Height);
    }

    private void Keyboard_KeyPressed(object sender, Blish_HUD.Input.KeyboardEventArgs e)
    {
        if (e.Key == Microsoft.Xna.Framework.Input.Keys.Enter)
        {
            this.SelectionConfirmed?.Invoke(this, EventArgs.Empty);
        }
        else if (e.Key == Microsoft.Xna.Framework.Input.Keys.Escape)
        {
            this.SelectionCanceled?.Invoke(this, EventArgs.Empty);
        }
    }

    protected override CaptureType CapturesInput()
    {
        return CaptureType.Mouse;
    }

    private void OCRSelection_LeftMouseButtonReleased(object sender, Blish_HUD.Input.MouseEventArgs e)
    {
        this._isDragging = false;
        this._isResizing = false;
    }

    private void OCRSelection_LeftMouseButtonPressed(object sender, Blish_HUD.Input.MouseEventArgs e)
    {
        this._dragStartLocation = Input.Mouse.Position - this.Location;
        _resizeStartLocation = Input.Mouse.Position - this.Size;
        this._isResizing = this._mouseOverResizeIcon;
        this._isDragging = !this._isResizing;
    }

    public override void DoUpdate(GameTime gameTime)
    {
        this.Top = Math.Max(this.Top, 0);
        this.Left = Math.Max(this.Left, 0);
        this.Right = Math.Min(this.Right, (int)( GameService.Graphics.Resolution.X / GameService.Graphics.UIScaleMultiplier));
        this.Bottom = Math.Min(this.Bottom, (int)(GameService.Graphics.Resolution.Y / GameService.Graphics.UIScaleMultiplier));

        if (_isDragging)
        {
            this.Location = Input.Mouse.Position - this._dragStartLocation;
        }

        if (this._isResizing)
        {
            this.Size = Input.Mouse.Position - this._resizeStartLocation;
        }
    }

    protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds)
    {
        spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, bounds, Color.LightGray * 0.5f);
        spriteBatch.DrawOnCtrl(this, this._mouseOverResizeIcon ? _textureWindowResizableCornerActive : _textureWindowResizableCorner, this._resizeHandleBounds);

    }

    protected override void DisposeControl()
    {
        this.LeftMouseButtonPressed -= this.OCRSelection_LeftMouseButtonPressed;
        this.LeftMouseButtonReleased -= this.OCRSelection_LeftMouseButtonReleased;

        Input.Keyboard.KeyPressed -= this.Keyboard_KeyPressed;
    }
}
