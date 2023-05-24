namespace Estreya.BlishHUD.Shared.Controls;

using Blish_HUD;
using Blish_HUD.Controls;
using Estreya.BlishHUD.Shared.Utils;
using Glide;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Provides an abstraction to use a <see cref="RenderTarget2D"/> for a blish control.
/// </summary>
public abstract class RenderTarget2DControl : Control
{
    private static Logger Logger = Logger.GetLogger<RenderTarget2DControl>();

    /// <summary>
    /// The internal render target which caches the drawn texture.
    /// </summary>
    private RenderTarget2D _renderTarget;

    /// <summary>
    /// Specifies whether the render target is currently empty and needs content.
    /// </summary>
    private bool _renderTargetIsEmpty;

    /// <summary>
    /// The lock used to lock the render target.
    /// </summary>
    private readonly AsyncLock _renderTargetLock = new AsyncLock();

    private TimeSpan _lastDraw = TimeSpan.Zero;

    /// <summary>
    /// Specifies the refresh rate of the <see cref="RenderTarget2DControl"/>.
    /// <para/>
    /// Even a interval of 1ms is a massive performance boost.
    /// <para/>
    /// If only one draw in the control lifetime is needed, an interval of <see cref="Timeout.InfiniteTimeSpan"/> can be used.
    /// </summary>
    public TimeSpan DrawInterval { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Gets or sets the size of the control. Setting the value recreates the render target.
    /// </summary>
    public new Point Size
    {
        get => base.Size;
        set
        {
            base.Size = value;
            this.CreateRenderTarget();
        }
    }

    /// <summary>
    /// Gets or sets the height of the control. Setting the value recreates the render target.
    /// </summary>
    public new int Height
    {
        get => base.Height;
        set
        {
            base.Height = value;
            this.CreateRenderTarget();
        }
    }

    /// <summary>
    /// Gets or sets the width of the control. Setting the value recreates the render target.
    /// </summary>
    public new int Width
    {
        get => base.Width;
        set
        {
            base.Width = value;
            this.CreateRenderTarget();
        }
    }

    public RenderTarget2DControl()
    {
        this.CreateRenderTarget();
    }

    /// <summary>
    /// Invalidates the control and forces a redraw.
    /// </summary>
    public sealed override void Invalidate()
    {
        this._renderTargetIsEmpty = true;
        this._lastDraw = this.DrawInterval;

        base.Invalidate();
    }

    /// <summary>
    /// Draws the cached render target onto the screen and requests new data after the <see cref="DrawInterval"/> is passed.
    /// </summary>
    /// <param name="spriteBatch">The spritebatch used to </param>
    /// <param name="bounds"></param>
    protected sealed override void Paint(SpriteBatch spriteBatch, Rectangle bounds)
    {
        spriteBatch.GraphicsDevice.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;
        spriteBatch.End();

        if (this._renderTargetLock.IsFree())
        {
            using (this._renderTargetLock.Lock())
            {
                if (this._renderTarget != null)
                {
                    if (this._renderTargetIsEmpty || (this.DrawInterval != Timeout.InfiniteTimeSpan && this._lastDraw >= this.DrawInterval))
                    {
                        spriteBatch.GraphicsDevice.SetRenderTarget(this._renderTarget);

                        spriteBatch.Begin(samplerState: SamplerState.PointClamp); // This is needed for Anti-Aliasing. Drawing to float positions using RectangleF is not possible.
                        spriteBatch.GraphicsDevice.Clear(Color.Transparent); // Clear render target to transparent. Backgroundcolor is set on the control

                        // We can't let this fail or all subsequential calls using this graphics device will draw onto this render target.
                        try
                        {
                            this.DoPaint(spriteBatch, bounds);
                        }
                        catch (Exception ex)
                        {
                            Logger.Warn(ex, "Failed to draw onto the render target.");
                        }

                        spriteBatch.End();

                        spriteBatch.GraphicsDevice.SetRenderTarget(null);

                        this._renderTargetIsEmpty = false;
                        this._lastDraw = TimeSpan.Zero;
                    }

                    spriteBatch.Begin(this.SpriteBatchParameters);
                    spriteBatch.DrawOnCtrl(this, _renderTarget, bounds, Color.White);
                    spriteBatch.End();
                }
            }
        }

        spriteBatch.Begin(this.SpriteBatchParameters);
    }

    /// <summary>
    /// Updates the <see cref="_lastDraw"/> value used to trigger redraws.
    /// </summary>
    /// <param name="gameTime"></param>
    public sealed override void DoUpdate(GameTime gameTime)
    {
        this._lastDraw += gameTime.ElapsedGameTime;
        this.InternalUpdate(gameTime);
    }

    /// <summary>
    /// Used to perform update logic outside of the draw loop.
    /// </summary>
    /// <param name="gameTime">The current game time.</param>
    protected virtual void InternalUpdate(GameTime gameTime) { /* NOOP */ }

    /// <summary>
    /// Draws a texture onto the created render target.
    /// </summary>
    /// <param name="spriteBatch">The spritebatch used to draw textures.</param>
    /// <param name="bounds">The bounds of the render target.</param>
    protected abstract void DoPaint(SpriteBatch spriteBatch, Rectangle bounds);

    /// <summary>
    /// (Re-)Creates the internal render target if it is null or the height or width changed.
    /// </summary>
    private void CreateRenderTarget()
    {
        // This will most likely never happen in a released version. It will get spotted in development instantly.
        // In case that it will not be spotted it is still better to crash blish instead of locking everything in a deadlock.
        this._renderTargetLock.ThrowIfBusy("Deadlock detected."); 

        int width = Math.Max(this.Width, 1);
        int height = Math.Max(this.Height, 1);

        using (this._renderTargetLock.Lock())
        {
            if (this._renderTarget != null && (this._renderTarget.Width != width || this._renderTarget.Height != height))
            {
                this._renderTarget.Dispose();
                this._renderTarget = null;
            }

            if (this._renderTarget == null)
            {
                try
                {
                    using (var ctx = GameService.Graphics.LendGraphicsDeviceContext())
                    {
                        this._renderTarget = new RenderTarget2D(
                            ctx.GraphicsDevice,
                            width,
                            height,
                            false,
                            ctx.GraphicsDevice.PresentationParameters.BackBufferFormat,
                            ctx.GraphicsDevice.PresentationParameters.DepthStencilFormat,
                            1,
                            RenderTargetUsage.PreserveContents);
                    }

                    _renderTargetIsEmpty = true;
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, "Failed to create Render Target");
                }
            }
        }
    }

    /// <summary>
    /// Releases the render target.
    /// <para/>
    /// Failing to do so will result in a massive memory leak when recreating multiple <see cref="RenderTarget2DControl"/>.
    /// </summary>
    protected sealed override void DisposeControl()
    {
        using (this._renderTargetLock.Lock())
        {
            if (this._renderTarget != null)
            {
                this._renderTarget?.Dispose();
                this._renderTarget = null;
            }
        }

        base.DisposeControl();
        this.InternalDispose();
    }

    /// <summary>
    /// Used to release any created resources in the controls lifespan.
    /// </summary>
    protected virtual void InternalDispose() { /* NOOP */ }
}
