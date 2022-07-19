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
using System.Threading.Tasks;

public abstract class RenderTargetControl : Control
{
    private RenderTarget2D _renderTarget;
    private bool _renderTargetIsEmpty;
    private readonly AsyncLock _renderTargetLock = new AsyncLock();

    private TimeSpan _lastDraw = TimeSpan.Zero;

    public TimeSpan DrawInterval { get; set; } = TimeSpan.FromMilliseconds(500);

    public new Point Size
    {
        get => base.Size;
        set
        {
            base.Size = value;
            this.CreateRenderTarget();
        }
    }

    public new int Height
    {
        get => base.Height;
        set
        {
            base.Height = value;
            this.CreateRenderTarget();
        }
    }

    public new int Width
    {
        get => base.Width;
        set
        {
            base.Width = value;
            this.CreateRenderTarget();
        }
    }

    public RenderTargetControl()
    {
        this.CreateRenderTarget();
    }

    protected sealed override void Paint(SpriteBatch spriteBatch, Rectangle bounds)
    {
        spriteBatch.GraphicsDevice.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;
        spriteBatch.End();

        using (this._renderTargetLock.Lock())
        {
            if (this._renderTarget != null)
            {
                if (this._renderTargetIsEmpty || this._lastDraw > this.DrawInterval)
                {
                    spriteBatch.GraphicsDevice.SetRenderTarget(this._renderTarget);

                    spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                    spriteBatch.GraphicsDevice.Clear(Color.Transparent); // Clear render target to transparent. Backgroundcolor is set on the control

                    this.DoPaint(spriteBatch, bounds);

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

        spriteBatch.Begin(this.SpriteBatchParameters);
    }

    public sealed override void DoUpdate(GameTime gameTime)
    {
        this._lastDraw += gameTime.ElapsedGameTime;
        this.InternalUpdate(gameTime);
    }

    protected virtual void InternalUpdate(GameTime gameTime) { /* NOOP */ }

    protected abstract void DoPaint(SpriteBatch spriteBatch, Rectangle bounds);

    private void CreateRenderTarget()
    {
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
                using var ctx = GameService.Graphics.LendGraphicsDeviceContext();

                this._renderTarget = new RenderTarget2D(
                ctx.GraphicsDevice,
                width,
                height,
                false,
                ctx.GraphicsDevice.PresentationParameters.BackBufferFormat,
                ctx.GraphicsDevice.PresentationParameters.DepthStencilFormat,
                1,
                RenderTargetUsage.PreserveContents);

                _renderTargetIsEmpty = true;
            }
        }
    }

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

    protected virtual void InternalDispose() { /* NOOP */ }
}
