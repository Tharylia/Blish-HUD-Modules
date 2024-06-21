namespace Estreya.BlishHUD.Shared.Controls.World
{
    using Blish_HUD;
    using Blish_HUD.Entities;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using MonoGame.Extended.BitmapFonts;
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class WorldRectangle : WorldEntity
    {
        private readonly Color _color;
        private static DynamicVertexBuffer _sharedVertexBuffer;

        private static readonly Vector3[] _faceVerts = {
            new(-0.5f, -0.5f, 0), new(0.5f, -0.5f, 0), new(-0.5f, 0.5f, 0), new(0.5f, 0.5f, 0),
        };
        public WorldRectangle(Vector3 position, Color color, float scale = 1f) : base(position, scale)
        {
            this._color = color;

            this.CreateSharedVertexBuffer();
        }

        private void CreateSharedVertexBuffer()
        {
            using (var gdctx = GameService.Graphics.LendGraphicsDeviceContext())
            {
                _sharedVertexBuffer = new DynamicVertexBuffer(gdctx.GraphicsDevice, typeof(VertexPositionTexture), 4, BufferUsage.WriteOnly);
            }

            var verts = new VertexPositionTexture[_faceVerts.Length];

            for (int i = 0; i < _faceVerts.Length; i++)
            {
                ref var vert = ref _faceVerts[i];

                verts[i] = new VertexPositionTexture(vert, new Vector2(vert.X < 0 ? 1 : 0, vert.Y < 0 ? 1 : 0));
            }

            _sharedVertexBuffer.SetData(verts);
        }
        public override bool IsPlayerInside(bool includeZAxis = true) => false;

        private RenderTarget2D CreateTexture(SpriteBatch spriteBatch)
        {
            var texture = ContentService.Textures.Pixel;
            RenderTarget2D target = new RenderTarget2D(spriteBatch.GraphicsDevice, texture.Width, texture.Height);
            spriteBatch.GraphicsDevice.SetRenderTarget(target);// Now the spriteBatch will render to the RenderTarget2D

            try
            {
                spriteBatch.Begin();
                spriteBatch.GraphicsDevice.Clear(this._color);

                spriteBatch.End();
            }
            catch (Exception)
            {
            }

            spriteBatch.GraphicsDevice.SetRenderTarget(null);//This will set the spriteBatch to render to the screen again.

            return target;
        }

        protected override void InternalRender(GraphicsDevice graphicsDevice, IWorld world, ICamera camera)
        {
            var spriteBatch = new SpriteBatch(graphicsDevice);
            var renderTarget2D = this.CreateTexture(spriteBatch);

            var modelMatrix = this.GetMatrix(graphicsDevice, world, camera);

            this.RenderEffect.View = GameService.Gw2Mumble.PlayerCamera.View;
            this.RenderEffect.Projection = GameService.Gw2Mumble.PlayerCamera.Projection;
            this.RenderEffect.World = modelMatrix;
            this.RenderEffect.Texture = renderTarget2D;
            this.RenderEffect.TextureEnabled = true;
            this.RenderEffect.VertexColorEnabled = false;

            graphicsDevice.SetVertexBuffer(_sharedVertexBuffer);
            foreach (EffectPass pass in this.RenderEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
            }

            renderTarget2D.Dispose();
            spriteBatch.Dispose();
        }
    }
}
