namespace Estreya.BlishHUD.Shared.Controls.World
{
    using Blish_HUD;
    using Blish_HUD.Entities;
    using Blish_HUD.Graphics;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using MonoGame.Extended;
    using MonoGame.Extended.BitmapFonts;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using static Blish_HUD.ArcDps.ArcDpsEnums;

    public class WorldText : WorldEntity
    {
        private readonly Func<string> _getText;
        private readonly BitmapFont _font;
        private readonly Color _color;
        private static DynamicVertexBuffer _sharedVertexBuffer;

        private static readonly Vector3[] _faceVerts = {
            new(-0.5f, -0.5f, 0), new(0.5f, -0.5f, 0), new(-0.5f, 0.5f, 0), new(0.5f, 0.5f, 0),
        };

        public int TextureWidth { get; set; }
        public int TextureHeight { get; set; }

        public WorldText(Func<string> getText, BitmapFont font, Vector3 position, float scale, Color color) : base(position, scale)
        {
            this._getText = getText;
            this._font = font;
            this._color = color;

            CreateSharedVertexBuffer();
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

        private RenderTarget2D CreateTexture(GraphicsDevice graphicsDevice, string text, Size2 textSizes, Size2 textureSizes)
        {
            using var spriteBatch = new SpriteBatch(graphicsDevice);
            RenderTarget2D target = new RenderTarget2D(graphicsDevice, (int)Math.Ceiling(textureSizes.Width), (int)Math.Ceiling(textureSizes.Height + 2),
            false,
            graphicsDevice.PresentationParameters.BackBufferFormat,
            graphicsDevice.PresentationParameters.DepthStencilFormat,
            1,
            RenderTargetUsage.PreserveContents);

            spriteBatch.GraphicsDevice.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;
            spriteBatch.GraphicsDevice.SetRenderTarget(target);// Now the spriteBatch will render to the RenderTarget2D
            try
            {
                spriteBatch.Begin();
                spriteBatch.GraphicsDevice.Clear(Color.Transparent);

                spriteBatch.DrawString(this._font, text, new Vector2(textureSizes.Width / 2 - textSizes.Width / 2, textureSizes.Height / 2 - textSizes.Height / 2), this._color);//Do your stuff here

                spriteBatch.End();
            }
            catch (Exception)
            {
            }

            spriteBatch.GraphicsDevice.SetRenderTarget(null);//This will set the spriteBatch to render to the screen again.

            //using var stream = new FileStream("C:\\temp\\target.png", FileMode.Create);
            //target.SaveAsPng(stream, target.Width, target.Height);
            return target;
        }

        protected override void InternalRender(GraphicsDevice graphicsDevice, IWorld world, ICamera camera)
        {
            var text = this._getText();
            var textSizes = this._font.MeasureString(text);
            var textureSizes = new Size2(textSizes.Width, textSizes.Height);
            if (this.TextureWidth > textureSizes.Width) textureSizes.Width = this.TextureWidth;
            if (this.TextureHeight > textureSizes.Height) textureSizes.Height = this.TextureHeight;
            using var renderTarget2D = this.CreateTexture(graphicsDevice, text, textSizes, textureSizes);

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
