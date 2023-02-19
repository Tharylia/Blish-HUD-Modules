namespace Estreya.BlishHUD.Shared.Controls.World
{
    using Blish_HUD;
    using Blish_HUD.Entities;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using System;
    using System.Collections.Generic;
    using System.Text;

    public abstract class WorldEntity : IEntity
    {
        public float DrawOrder => 1f;

        protected Vector3 Position { get; set; }

        private float _scale = 1f;
        protected float Scale => _scale;

        protected float DistanceToPlayer { get; private set; }

        protected BasicEffect RenderEffect { get; private set; }

        public WorldEntity(Vector3 position, float scale)
        {
            this.Position = position;
            this._scale = scale;
        }

        public void Render(GraphicsDevice graphicsDevice, IWorld world, ICamera camera)
        {
            this.RenderEffect ??=  new BasicEffect(graphicsDevice);

            this.InternalRender(graphicsDevice, world, camera);
        }

        protected abstract void InternalRender(GraphicsDevice graphicsDevice, IWorld world, ICamera camera);

        public virtual void Update(GameTime gameTime) {
            this.DistanceToPlayer = Vector3.Distance(GameService.Gw2Mumble.PlayerCharacter.Position, this.Position);
        }

        public abstract bool IsPlayerInside(bool includeZAxis = true);
    }
}
