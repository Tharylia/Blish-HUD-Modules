namespace Estreya.BlishHUD.Shared.Controls;

using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Models;
using System.Collections.Generic;
using System.Linq;

public class ControlMover : Control, IWindow
{
    // This is all very kludgy.  I wouldn't use it as a reference for anything.

    private const int HANDLE_SIZE = 40;

    private readonly SpriteBatchParameters _clearDrawParameters;
    private readonly string _description;

    private readonly Texture2D _handleTexture;

    private readonly ScreenRegion[] _screenRegions;

    private ScreenRegion _activeScreenRegion;

    private Point _grabPosition = Point.Zero;

    public ControlMover(string description, Texture2D handleTexture, params ScreenRegion[] screenPositions) : this(description, screenPositions.ToList(), handleTexture)
    {
        /* NOOP */
    }

    public ControlMover(string description, IEnumerable<ScreenRegion> screenPositions, Texture2D handleTexture)
    {
        //WindowBase2.RegisterWindow(this);

        this.ZIndex = int.MaxValue - 10;

        this._clearDrawParameters = new SpriteBatchParameters(SpriteSortMode.Deferred, BlendState.Opaque);
        this._screenRegions = screenPositions.ToArray();

        this._handleTexture = handleTexture;
        this._description = description;
    }

    public override void Hide()
    {
        this.Dispose();
    }

    public void BringWindowToFront()
    {
        /* NOOP */
    }

    public bool TopMost => true;
    public double LastInteraction { get; }
    public bool CanClose => true;

    public bool CanCloseWithEscape => true;

    protected override void OnLeftMouseButtonPressed(MouseEventArgs e)
    {
        if (this._activeScreenRegion == null)
        {
            // Only start drag if we were moused over one.
            return;
        }

        this._grabPosition = this.RelativeMousePosition;
    }

    protected override void OnLeftMouseButtonReleased(MouseEventArgs e)
    {
        this._grabPosition = Point.Zero;
    }

    protected override void OnMouseMoved(MouseEventArgs e)
    {
        if (this._grabPosition != Point.Zero && this._activeScreenRegion != null)
        {
            Point lastPos = this._grabPosition;
            this._grabPosition = this.RelativeMousePosition;

            this._activeScreenRegion.Location += this._grabPosition - lastPos;
        }
        else
        {
            // Update which screen region the mouse is over.
            foreach (ScreenRegion region in this._screenRegions)
            {
                if (region.Bounds.Contains(this.RelativeMousePosition))
                {
                    this._activeScreenRegion = region;
                    return;
                }
            }

            this._activeScreenRegion = null;
        }
    }

    protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds)
    {
        spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, bounds, Color.Black * 0.8f);
        spriteBatch.End();
        spriteBatch.Begin(this._clearDrawParameters);

        foreach (ScreenRegion region in this._screenRegions)
        {
            spriteBatch.DrawOnCtrl(this, ContentService.Textures.TransparentPixel, region.Bounds, Color.Transparent);
        }

        spriteBatch.End();
        spriteBatch.Begin(this.SpriteBatchParameters);

        foreach (ScreenRegion region in this._screenRegions)
        {
            if (region == this._activeScreenRegion)
            {
                spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new Rectangle(region.Location, region.Size), Color.White * 0.5f);
            }

            spriteBatch.DrawOnCtrl(this, this._handleTexture, new Rectangle(region.Bounds.Left, region.Bounds.Top, HANDLE_SIZE, HANDLE_SIZE), this._handleTexture.Bounds, Color.White * 0.6f);
            spriteBatch.DrawOnCtrl(this, this._handleTexture, new Rectangle(region.Bounds.Right - (HANDLE_SIZE / 2), region.Bounds.Top + (HANDLE_SIZE / 2), HANDLE_SIZE, HANDLE_SIZE), this._handleTexture.Bounds, Color.White * 0.6f, MathHelper.PiOver2, new Vector2(HANDLE_SIZE / 2f, HANDLE_SIZE / 2f));
            spriteBatch.DrawOnCtrl(this, this._handleTexture, new Rectangle(region.Bounds.Left + (HANDLE_SIZE / 2), region.Bounds.Bottom - (HANDLE_SIZE / 2), HANDLE_SIZE, HANDLE_SIZE), this._handleTexture.Bounds, Color.White * 0.6f, MathHelper.PiOver2 * 3, new Vector2(HANDLE_SIZE / 2f, HANDLE_SIZE / 2f));
            spriteBatch.DrawOnCtrl(this, this._handleTexture, new Rectangle(region.Bounds.Right - (HANDLE_SIZE / 2), region.Bounds.Bottom - (HANDLE_SIZE / 2), HANDLE_SIZE, HANDLE_SIZE), this._handleTexture.Bounds, Color.White * 0.6f, MathHelper.Pi, new Vector2(HANDLE_SIZE / 2f, HANDLE_SIZE / 2f));

            //spriteBatch.DrawStringOnCtrl(this,
            //                             region.RegionName,
            //                             GameService.Content.DefaultFont32,
            //                             region.Bounds,
            //                             Color.Black,
            //                             false,
            //                             HorizontalAlignment.Center);
        }

        spriteBatch.DrawStringOnCtrl(this, $"{(!string.IsNullOrWhiteSpace(this._description) ? this._description + "\n" : "Press ESC to close.")}", GameService.Content.DefaultFont32, bounds, Color.White, false, HorizontalAlignment.Center);
    }
}