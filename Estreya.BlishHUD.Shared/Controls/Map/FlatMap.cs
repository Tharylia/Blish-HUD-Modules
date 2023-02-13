namespace Estreya.BlishHUD.Shared.Controls.Map
{
    using Blish_HUD;
    using Blish_HUD.Controls;
    using Blish_HUD.Input;
    using Estreya.BlishHUD.Shared.Utils;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Microsoft.Xna.Framework.Input;
    using System;
    using System.Collections.Generic;

    public class FlatMap : Control
    {

        private const int MAPWIDTH_MAX = 362;
        private const int MAPHEIGHT_MAX = 338;
        private const int MAPWIDTH_MIN = 170;
        private const int MAPHEIGHT_MIN = 170;
        private const int MAPOFFSET_MIN = 19;

        private double _lastMapViewChanged = 0;

        private AsyncLock _entityLock = new AsyncLock();

        private MapEntity _activeEntity;
        private List<MapEntity> _entities = new List<MapEntity>();

        public FlatMap()
        {
            this.ZIndex = int.MinValue / 10;
            this.Location = new Point(1);
            this.SpriteBatchParameters = new SpriteBatchParameters(SpriteSortMode.Deferred, BlendState.Opaque);

            this.UpdateBounds();
        }

        public void AddEntity(MapEntity mapEntity)
        {
            using (this._entityLock.Lock())
            {
                this._entities.Add(mapEntity);
                mapEntity.Disposed += this.MapEntity_Disposed;
            }
        }

        private void MapEntity_Disposed(object sender, EventArgs e)
        {
            this.RemoveEntity(sender as MapEntity);
        }

        public void RemoveEntity(MapEntity mapEntity)
        {
            using (this._entityLock.Lock())
            {
                this._entities.Remove(mapEntity);
            }
        }

        public void ClearEntities()
        {
            using (this._entityLock.Lock())
            {
                this._entities.Clear();
            }
        }

        private int GetOffset(float curr, float max, float min, float val)
        {
            return (int)Math.Round((curr - min) / (max - min) * (val - MAPOFFSET_MIN) + MAPOFFSET_MIN, 0);
        }

        private void UpdateBounds()
        {
            if (GameService.Gw2Mumble.UI.CompassSize.Width < 1 ||
                GameService.Gw2Mumble.UI.CompassSize.Height < 1)
            {
                return;
            }

            Point newSize;

            if (GameService.Gw2Mumble.UI.IsMapOpen)
            {
                this.Location = Point.Zero;

                newSize = GameService.Graphics.SpriteScreen.Size;
            }
            else
            {
                int offsetWidth = this.GetOffset(GameService.Gw2Mumble.UI.CompassSize.Width, MAPWIDTH_MAX, MAPWIDTH_MIN, 40);
                int offsetHeight = this.GetOffset(GameService.Gw2Mumble.UI.CompassSize.Height, MAPHEIGHT_MAX, MAPHEIGHT_MIN, 40);

                if (GameService.Gw2Mumble.UI.IsCompassTopRight)
                {
                    this.Location = new Point(GameService.Graphics.SpriteScreen.ContentRegion.Width - GameService.Gw2Mumble.UI.CompassSize.Width - offsetWidth + 1, 1);
                }
                else
                {
                    this.Location = new Point(GameService.Graphics.SpriteScreen.ContentRegion.Width - GameService.Gw2Mumble.UI.CompassSize.Width - offsetWidth,
                                              GameService.Graphics.SpriteScreen.ContentRegion.Height - GameService.Gw2Mumble.UI.CompassSize.Height - offsetHeight - 40);
                }

                newSize = new Point(GameService.Gw2Mumble.UI.CompassSize.Width + offsetWidth,
                                    GameService.Gw2Mumble.UI.CompassSize.Height + offsetHeight);
            }

            this.Size = newSize;
        }

        protected override CaptureType CapturesInput()
        {
            return CaptureType.Mouse | CaptureType.DoNotBlock;
        }

        public override void DoUpdate(GameTime gameTime)
        {
            this.UpdateBounds();
        }

        protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds)
        {
            if (!GameService.GameIntegration.Gw2Instance.IsInGame)
            {
                return;
            }

            bounds = new Rectangle(this.Location, bounds.Size);

            double scale = GameService.Gw2Mumble.UI.MapScale * 0.897d; //Workaround to fix pixel to coordinate scaling - Blish HUD scale of 1 is "Larger" but game is "Normal".
            double offsetX = bounds.X + (bounds.Width / 2d);
            double offsetY = bounds.Y + (bounds.Height / 2d);

            float opacity = MathHelper.Clamp((float)(GameService.Overlay.CurrentGameTime.TotalGameTime.TotalSeconds - this._lastMapViewChanged) / 0.65f, 0f, 1f);

            this._activeEntity = null;

            if (this._entityLock.IsFree())
            {
                using (this._entityLock.Lock())
                {
                    foreach (MapEntity entity in this._entities)
                    {
                        MonoGame.Extended.RectangleF? hint = entity.RenderToMiniMap(spriteBatch, bounds, offsetX, offsetY, scale, opacity);

                        if (/*this.MouseOver && */hint.HasValue && hint.Value.Contains(GameService.Input.Mouse.Position))
                        {
                            this._activeEntity = entity;
                        }
                    }
                }
            }

            this.UpdateTooltip();
        }

        public override Control TriggerMouseInput(MouseEventType mouseEventType, MouseState ms)
        {
            return this._activeEntity != null ? base.TriggerMouseInput(mouseEventType, ms) : null;
        }

        private void UpdateTooltip()
        {
            this.BasicTooltipText = this._activeEntity?.TooltipText;
        }

        protected override void DisposeControl()
        {
            for (int i = this._entities.Count - 1; i >= 0; i--)
            {
                this._entities[i]?.Dispose();
            }

            this._entities?.Clear();
            this._entities = null;
        }
    }
}
