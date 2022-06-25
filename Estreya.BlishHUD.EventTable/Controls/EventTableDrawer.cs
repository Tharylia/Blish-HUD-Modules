namespace Estreya.BlishHUD.EventTable.Controls
{
    using Blish_HUD;
    using Blish_HUD._Extensions;
    using Blish_HUD.Controls;
    using Estreya.BlishHUD.EventTable.Models;
    using Estreya.BlishHUD.EventTable.Utils;
    using Glide;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using MonoGame.Extended;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class EventTableDrawer : Control
    {
        private bool _currentVisibilityDirection = false;

        private static bool CursorVisible => GameService.Input.Mouse.CursorIsVisible;

        private TimeSpan _lastDraw = TimeSpan.Zero;

        private RenderTarget2D _renderTarget;
        private bool _renderTargetIsEmpty = true;

        public new bool Visible
        {
            get
            {
                if (this._currentVisibilityDirection && this.CurrentVisibilityAnimation != null)
                {
                    return true;
                }

                if (!this._currentVisibilityDirection && this.CurrentVisibilityAnimation != null)
                {
                    return false;
                }

                return base.Visible;
            }
            set => base.Visible = value;
        }

        private double PixelPerMinute
        {
            get
            {
                int pixels = this.Size.X;

                double pixelPerMinute = pixels / EventTableModule.ModuleInstance.EventTimeSpan.TotalMinutes;

                return pixelPerMinute;
            }
        }

        private Tween CurrentVisibilityAnimation { get; set; }

        public EventTableDrawer()
        {
            this.LeftMouseButtonPressed += this.EventTableContainer_Click;
            this.RightMouseButtonPressed += this.EventTableContainer_Click;
            this.MouseMoved += this.EventTableContainer_MouseMoved;

            this.CreateRenderTarget();
        }

        private void EventTableContainer_MouseMoved(object sender, Blish_HUD.Input.MouseEventArgs e)
        {
            if (!CursorVisible)
            {
                return;
            }

            Input.MouseEventArgs mouseEventArgs = new Input.MouseEventArgs(this.RelativeMousePosition, e.IsDoubleClick, e.EventType);
            foreach (EventCategory eventCategory in EventTableModule.ModuleInstance.EventCategories)
            {
                foreach (Event ev in eventCategory.Events.Where(ev => !ev.IsDisabled))
                {
                    if (ev.IsHovered(EventTableModule.ModuleInstance.EventTimeMin, this.AbsoluteBounds, this.RelativeMousePosition, this.PixelPerMinute))
                    {
                        ev.HandleHover(sender, mouseEventArgs, this.PixelPerMinute);
                    }
                    else
                    {
                        ev.HandleNonHover(sender, mouseEventArgs);
                    }
                }
            }
        }

        private void EventTableContainer_Click(object sender, Blish_HUD.Input.MouseEventArgs e)
        {
            if (!CursorVisible)
            {
                return;
            }

            foreach (EventCategory eventCategory in EventTableModule.ModuleInstance.EventCategories)
            {
                foreach (Event ev in eventCategory.Events.Where(ev => !ev.IsDisabled))
                {
                    if (ev.IsHovered(EventTableModule.ModuleInstance.EventTimeMin, this.AbsoluteBounds, this.RelativeMousePosition, this.PixelPerMinute))
                    {
                        ev.HandleClick(sender, e);
                        return;
                    }
                }
            }
        }

        protected override CaptureType CapturesInput()
        {
            return CaptureType.Mouse;
        }

        private void CreateRenderTarget()
        {
            int width = Math.Max(this.Width, 1);
            int height = Math.Max(this.Height,1);

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
                DepthFormat.Depth24Stencil8, 1, RenderTargetUsage.PreserveContents);

                _renderTargetIsEmpty = true;
            }
        }

        protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds)
        {
            spriteBatch.GraphicsDevice.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;
            //spriteBatch.GraphicsDevice.Clear(Color.Transparent);
            spriteBatch.End();

            int refreshInterval = EventTableModule.ModuleInstance.ModuleSettings.RefreshRateDelay.Value;

            if (this._renderTargetIsEmpty || this._lastDraw.TotalMilliseconds > refreshInterval)
            {
                spriteBatch.GraphicsDevice.SetRenderTarget(this._renderTarget);

                spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                spriteBatch.GraphicsDevice.Clear(Color.Transparent); // Clear render target to transparent. Backgroundcolor is set on the control

                List<EventCategory> eventCategories = EventTableModule.ModuleInstance.EventCategories; // Already checks for IsDisabled()

                int y = 0;
                DateTime now = EventTableModule.ModuleInstance.DateTimeNow;
                DateTime min = EventTableModule.ModuleInstance.EventTimeMin;
                DateTime max = EventTableModule.ModuleInstance.EventTimeMax;

                foreach (EventCategory eventCategory in eventCategories)
                {
                    bool categoryHasEvents = false;

                    foreach (Event ev in eventCategory.Events.Where(ev => !ev.IsDisabled))
                    {
                        categoryHasEvents = true;
                        if (!EventTableModule.ModuleInstance.ModuleSettings.UseFiller.Value && ev.Filler)
                        {
                            continue;
                        }

                        _ = ev.Draw(spriteBatch, bounds, ContentService.Textures.Pixel, y, this.PixelPerMinute, now, min, max, EventTableModule.ModuleInstance.Font);
                    }

                    if (categoryHasEvents)
                    {
                        y += EventTableModule.ModuleInstance.EventHeight;
                    }
                }

                this.UpdateSize(bounds.Width, y, true);

                float middleLineX = this.Size.X * EventTableModule.ModuleInstance.EventTimeSpanRatio;
                spriteBatch.DrawLine(ContentService.Textures.Pixel, new RectangleF(middleLineX, 0, 2, this.Size.Y), Color.LightGray);

                spriteBatch.End();

                spriteBatch.GraphicsDevice.SetRenderTarget(null);

                this._renderTargetIsEmpty = false;
                this._lastDraw = TimeSpan.Zero;
            }

            spriteBatch.Begin(this.SpriteBatchParameters);
            spriteBatch.DrawOnCtrl(this, _renderTarget, bounds, Color.White);
            spriteBatch.End();

            spriteBatch.Begin(this.SpriteBatchParameters);
        }

        public new void Show()
        {
            if (this.Visible && this.CurrentVisibilityAnimation == null)
            {
                return;
            }

            if (this.CurrentVisibilityAnimation != null)
            {
                this.CurrentVisibilityAnimation.Cancel();
            }

            this._currentVisibilityDirection = true;
            this.Visible = true;
            this.CurrentVisibilityAnimation = Animation.Tweener.Tween(this, new { Opacity = 1f }, 0.2f);
            this.CurrentVisibilityAnimation.OnComplete(() =>
            {
                this.CurrentVisibilityAnimation = null;
            });
        }

        public new void Hide()
        {
            if (!this.Visible && this.CurrentVisibilityAnimation == null)
            {
                return;
            }

            if (this.CurrentVisibilityAnimation != null)
            {
                this.CurrentVisibilityAnimation.Cancel();
            }

            this._currentVisibilityDirection = false;
            this.CurrentVisibilityAnimation = Animation.Tweener.Tween(this, new { Opacity = 0f }, 0.2f);
            this.CurrentVisibilityAnimation.OnComplete(() =>
            {
                this.Visible = false;
                this.CurrentVisibilityAnimation = null;
            });
        }

        public void UpdatePosition(int x, int y)
        {
            bool buildFromBottom = EventTableModule.ModuleInstance.ModuleSettings.BuildDirection.Value == BuildDirection.Bottom;

            this.Location = buildFromBottom ? new Point(x, y - this.Height) : new Point(x, y);
        }

        public void UpdateSize(int width, int height, bool overrideHeight = false)
        {
            if (height == -1)
            {
                height = this.Size.Y;
            }

            this.Size = new Point(width, !overrideHeight ? this.Size.Y : height);

            this.CreateRenderTarget();
        }

        protected override void DisposeControl()
        {
            this.Hide();

            this.LeftMouseButtonPressed -= this.EventTableContainer_Click;
            this.RightMouseButtonPressed -= this.EventTableContainer_Click;
            this.MouseMoved -= this.EventTableContainer_MouseMoved;

            this._renderTarget.Dispose();

            base.DisposeControl();
        }

        public override void DoUpdate(GameTime gameTime)
        {
            this._lastDraw += gameTime.ElapsedGameTime;
        }

        public void UpdateBackgroundColor()
        {
            Color backgroundColor = Color.Transparent;
            if (EventTableModule.ModuleInstance.ModuleSettings.BackgroundColor.Value != null && EventTableModule.ModuleInstance.ModuleSettings.BackgroundColor.Value.Id != 1)
            {
                backgroundColor = EventTableModule.ModuleInstance.ModuleSettings.BackgroundColor.Value.Cloth.ToXnaColor();
            }

            this.BackgroundColor = backgroundColor * EventTableModule.ModuleInstance.ModuleSettings.BackgroundColorOpacity.Value;
        }

        public Task LoadAsync()
        {
            this.UpdateBackgroundColor();
            return Task.CompletedTask;
        }
    }
}
