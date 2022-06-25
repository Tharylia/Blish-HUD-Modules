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

    public RenderTargetControl()
    {
        this.CreateRenderTarget();
    }

    protected sealed override void Paint(SpriteBatch spriteBatch, Rectangle bounds)
    {
        spriteBatch.GraphicsDevice.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;
        spriteBatch.End();

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
        spriteBatch.DrawOnCtrl(this, _renderTarget, bounds , Color.White);
        spriteBatch.End();

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

        if (this._renderTarget != null && (this._renderTarget.Width != width || this._renderTarget.Height != height))
        {
            this._renderTarget.Dispose();
            this._renderTarget = null;
        }

        if (this._renderTarget == null)
        {
            this._renderTarget = new RenderTarget2D(
            GameService.Graphics.GraphicsDevice,
            width,
            height,
            false,
            GameService.Graphics.GraphicsDevice.PresentationParameters.BackBufferFormat,
            GameService.Graphics.GraphicsDevice.PresentationParameters.DepthStencilFormat,
            1,
            RenderTargetUsage.PreserveContents);

            _renderTargetIsEmpty = true;
        }
    }

    protected override void DisposeControl()
    {
        if (this._renderTarget != null)
        {
            this._renderTarget?.Dispose();
            this._renderTarget = null;
        }

        base.DisposeControl();
    }
}
