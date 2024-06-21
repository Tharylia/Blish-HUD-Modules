namespace Estreya.BlishHUD.Shared.Controls.World
{
    using Blish_HUD;
    using Blish_HUD.Content;
    using Blish_HUD.Entities;
    using Blish_HUD.Graphics;
    using Shared.Utils;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using MonoGame.Extended;
    using MonoGame.Extended.BitmapFonts;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using static Blish_HUD.ArcDps.ArcDpsEnums;

    public class WorldTexture : WorldEntity
    {
        private static DynamicVertexBuffer _sharedVertexBuffer;

        private static readonly Vector3[] _faceVerts = {
            new(-0.5f, -0.5f, 0), new(0.5f, -0.5f, 0), new(-0.5f, 0.5f, 0), new(0.5f, 0.5f, 0),
        };
        private readonly AsyncTexture2D _asyncTexture;

        public int ResizeWidth { get; set; } = -1;
        public int ResizeHeight { get; set; } = -1;

        public WorldTexture(AsyncTexture2D asyncTexture, Vector3 position, float scale) : base(position, scale)
        {
            this._asyncTexture = asyncTexture;
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

        public override bool IsPlayerInside(bool includeZAxis = true)
        {
            return false;
        }

        private RenderTarget2D CreateTexture(GraphicsDevice graphicsDevice)
        {
            using var spriteBatch = new SpriteBatch(graphicsDevice);

            var texture = this._asyncTexture.Texture;
            var doResize = !(this.ResizeWidth is -1 && this.ResizeHeight is -1);
            var resizedTexture = !doResize
                ? texture 
                : ImageUtil.ResizeImage(
                    texture.ToImage(), 
                    this.ResizeWidth is -1 
                    ? texture.Width 
                    : this.ResizeWidth, 
                    this.ResizeHeight is -1 
                    ? texture.Height 
                    : this.ResizeHeight)
                .ToTexture2D(spriteBatch.GraphicsDevice);
            RenderTarget2D target = new RenderTarget2D(spriteBatch.GraphicsDevice, resizedTexture.Width, resizedTexture.Height,
            false,
            graphicsDevice.PresentationParameters.BackBufferFormat,
            graphicsDevice.PresentationParameters.DepthStencilFormat,
            1,
            RenderTargetUsage.PreserveContents);

            spriteBatch.GraphicsDevice.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;
            spriteBatch.GraphicsDevice.SetRenderTarget(target);// Now the spriteBatch will render to the RenderTarget2D

            try
            {
                spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                spriteBatch.GraphicsDevice.Clear(Color.Transparent);

                spriteBatch.Draw(resizedTexture, Vector2.Zero, Color.White);//Do your stuff here

                spriteBatch.End();
            }
            catch (Exception)
            {
            }

            spriteBatch.GraphicsDevice.SetRenderTarget(null);//This will set the spriteBatch to render to the screen again.

            //using var stream = new FileStream("C:\\temp\\target.png", FileMode.Create);
            //target.SaveAsPng(stream, target.Width, target.Height);

            if (doResize)
            {
                resizedTexture.Dispose();

            }

            return target;
        }

        protected override void InternalRender(GraphicsDevice graphicsDevice, IWorld world, ICamera camera)
        {
            using var renderTarget2D = this.CreateTexture(graphicsDevice);

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
        }
    }
}
