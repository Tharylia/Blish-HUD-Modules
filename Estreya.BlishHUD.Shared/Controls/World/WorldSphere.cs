namespace Estreya.BlishHUD.Shared.Controls.World
{
    using Blish_HUD;
    using Blish_HUD.Entities;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using System;
    using System.Collections.Generic;

    public class WorldSphere : WorldEntity
    {
        private const int CIRCLE_AMOUNT = 90;
        private const int VERTICES_AMOUNT = 90;
        private readonly Color _color;
        private VertexBuffer vertexBuffer;
        public IndexBuffer indexBuffer;
        private List<VertexPositionNormal> _vertices =  new List<VertexPositionNormal>();
        private List<short> _indices = new List<short>();

        /// <summary>
        /// Queries the index of the current vertex. This starts at
        /// zero, and increments every time AddVertex is called.
        /// </summary>
        private int CurrentVertex => _vertices.Count;

        public WorldSphere(Vector3 position, float radius, Color color, int tessellation = 16) : base(position, 1f)
        {
            if (tessellation < 3)
                throw new ArgumentOutOfRangeException("tessellation");

            int verticalSegments = tessellation;
            int horizontalSegments = tessellation * 2;

            // Start with a single vertex at the bottom of the sphere.
            AddVertex(Vector3.Down * radius, Vector3.Down);

            // Create rings of vertices at progressively higher latitudes.
            for (int i = 0; i < verticalSegments - 1; i++)
            {
                float latitude = ((i + 1) * MathHelper.Pi / verticalSegments) - MathHelper.PiOver2;

                float dy = (float)Math.Sin(latitude);
                float dxz = (float)Math.Cos(latitude);

                // Create a single ring of vertices at this latitude.
                for (int j = 0; j < horizontalSegments; j++)
                {
                    float longitude = j * MathHelper.TwoPi / horizontalSegments;

                    float dx = (float)Math.Cos(longitude) * dxz;
                    float dz = (float)Math.Sin(longitude) * dxz;

                    Vector3 normal = new Vector3(dx, dy, dz);

                    AddVertex(normal * radius, normal);
                }
            }

            // Finish with a single vertex at the top of the sphere.
            AddVertex(Vector3.Up * radius, Vector3.Up);

            // Create a fan connecting the bottom vertex to the bottom latitude ring.
            for (int i = 0; i < horizontalSegments; i++)
            {
                AddIndex(0);
                AddIndex(1 + (i + 1) % horizontalSegments);
                AddIndex(1 + i);
            }

            // Fill the sphere body with triangles joining each pair of latitude rings.
            for (int i = 0; i < verticalSegments - 2; i++)
            {
                for (int j = 0; j < horizontalSegments; j++)
                {
                    int nextI = i + 1;
                    int nextJ = (j + 1) % horizontalSegments;

                    AddIndex(1 + i * horizontalSegments + j);
                    AddIndex(1 + i * horizontalSegments + nextJ);
                    AddIndex(1 + nextI * horizontalSegments + j);

                    AddIndex(1 + i * horizontalSegments + nextJ);
                    AddIndex(1 + nextI * horizontalSegments + nextJ);
                    AddIndex(1 + nextI * horizontalSegments + j);
                }
            }

            // Create a fan connecting the top vertex to the top latitude ring.
            for (int i = 0; i < horizontalSegments; i++)
            {
                AddIndex(CurrentVertex - 1);
                AddIndex(CurrentVertex - 2 - (i + 1) % horizontalSegments);
                AddIndex(CurrentVertex - 2 - i);
            }

            using var ctx = GameService.Graphics.LendGraphicsDeviceContext();

            // Create a vertex declaration, describing the format of our vertex data.

            // Create a vertex buffer, and copy our vertex data into it.
            vertexBuffer = new VertexBuffer(ctx.GraphicsDevice,
                                            typeof(VertexPositionNormal),
                                            _vertices.Count, BufferUsage.None);

            vertexBuffer.SetData(_vertices.ToArray());

            // Create an index buffer, and copy our index data into it.
            indexBuffer = new IndexBuffer(ctx.GraphicsDevice, typeof(short),
                                          _indices.Count, BufferUsage.None);

            indexBuffer.SetData(_indices.ToArray());

            this._color = color;
        }

        /// <summary>
        /// Adds a new vertex to the primitive model. This should only be called
        /// during the initialization process, before InitializePrimitive.
        /// </summary>
        private void AddVertex(Vector3 position, Vector3 normal)
        {
            _vertices.Add(new VertexPositionNormal(position, normal));
        }


        /// <summary>
        /// Adds a new index to the primitive model. This should only be called
        /// during the initialization process, before InitializePrimitive.
        /// </summary>
        private void AddIndex(int index)
        {
            if (index > short.MaxValue)
                throw new ArgumentOutOfRangeException("index");

            _indices.Add((short)index);
        }

        protected override void InternalRender(GraphicsDevice graphicsDevice, IWorld world, ICamera camera)
        {
            Matrix modelMatrix = Matrix.CreateScale(this.Scale);

            Vector3 position = this.Position + new Vector3(0, 0, /*this.HeightOffset*/0);

            modelMatrix *= Matrix.CreateTranslation(position);

            this.RenderEffect.View = GameService.Gw2Mumble.PlayerCamera.View;
            this.RenderEffect.Projection = GameService.Gw2Mumble.PlayerCamera.Projection;
            this.RenderEffect.World = modelMatrix;
            //this.RenderEffect.Texture = ContentService.Textures.Pixel;
            this.RenderEffect.DiffuseColor = _color.ToVector3();
            this.RenderEffect.Alpha = _color.A / 255.0f;

            graphicsDevice.SetVertexBuffer(vertexBuffer);
            graphicsDevice.Indices = indexBuffer;

            foreach (EffectPass pass in this.RenderEffect.CurrentTechnique.Passes)
            {
                pass.Apply();

                /*graphicsDevice.DrawPrimitives(
                    PrimitiveType.TriangleStrip,
                    0,
                    2);*/

                graphicsDevice.DrawUserIndexedPrimitives<VertexPositionNormal>(Microsoft.Xna.Framework.Graphics.PrimitiveType.TriangleList, this._vertices.ToArray(), 0, this._vertices.Count, this._indices.ToArray(), 0, this._indices.Count / 3);
            }
        }

        public override bool IsPlayerInside(bool includeZAxis = true)
        {
            return true;
        }
    }
}
