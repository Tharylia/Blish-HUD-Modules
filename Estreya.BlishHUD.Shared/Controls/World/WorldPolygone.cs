namespace Estreya.BlishHUD.Shared.Controls.World
{
    using Blish_HUD;
    using Blish_HUD.Entities;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using MonoGame.Extended.Shapes;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Windows.Shapes;

    public class WorldPolygone : WorldEntity
    {
        public Vector3[] Points { get; private set; }
        private VertexPositionColor[] _vert;
        private readonly Color _color;
        private readonly Func<WorldEntity, bool> _renderCondition;

        public WorldPolygone(Vector3 position, Vector3[] points, Color color, Func<WorldEntity, bool> renderCondition = null) : base(position, 1)
        {
            if (points.Length < 2 || points.Length % 2 != 0)
            {
                throw new ArgumentOutOfRangeException("points");
            }

            this.Points = points;
            this._color = color;
            this._renderCondition = renderCondition;
            _vert = this.BuildVertices();
        }

        public WorldPolygone(Vector3 position, Vector3[] points) : this(position, points, Color.White)
        {
        }

        private VertexPositionColor[] BuildVertices()
        {
            var verts = new VertexPositionColor[this.Points.Length];

            var curPoint = this.Points[0];

            for (int i = 0; i < this.Points.Length - 1; i++)
            {
                var nextPoint = this.Points[i + 1];

                verts[i] = new VertexPositionColor(curPoint, _color);
                verts[i +1] = new VertexPositionColor(nextPoint, _color);

                curPoint = nextPoint;
            }

            using var ctx = GameService.Graphics.LendGraphicsDeviceContext();
            var sectionBuffer = new VertexBuffer(ctx.GraphicsDevice, VertexPositionColor.VertexDeclaration, verts.Length, BufferUsage.WriteOnly);
            sectionBuffer.SetData(verts);

            return verts;
        }

        protected override void InternalRender(GraphicsDevice graphicsDevice, IWorld world, ICamera camera)
        {
            if (this._renderCondition != null && !this._renderCondition(this)) return;

            Matrix modelMatrix = Matrix.CreateScale(this.Scale);

            Vector3 position = this.Position + new Vector3(0, 0, /*this.HeightOffset*/0);

            modelMatrix *= Matrix.CreateTranslation(position);

            this.RenderEffect.View = GameService.Gw2Mumble.PlayerCamera.View;
            this.RenderEffect.Projection = GameService.Gw2Mumble.PlayerCamera.Projection;
            this.RenderEffect.World = modelMatrix;

            //graphicsDevice.SetVertexBuffer(_vertexBuffer);
            foreach (var pass in this.RenderEffect.CurrentTechnique.Passes)
            {
                pass.Apply();

                graphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, _vert, 0, _vert.Length / 2);
            }
        }

        public Vector3[] GetAbsolutePoints()
        {
            var newPoints = new List<Vector3>();
            foreach (var point in this.Points)
            {
                newPoints.Add(this.Position + point);
            }

            return newPoints.ToArray();
        }

        public override bool IsPlayerInside(bool includeZAxis = true)
        {
            var playerPosition = GameService.Gw2Mumble.PlayerCharacter.Position;
            var points = this.GetAbsolutePoints();

            var maxZ = points.Max(p => p.Z);
            var minZ = points.Min(p => p.Z);

            if (includeZAxis && (playerPosition.Z > maxZ || playerPosition.Z < minZ)) return false;

            bool result = false;
            int j = points.Length - 1;
            for (int i = 0; i < points.Length; i++)
            {
                if (points[i].Y < playerPosition.Y && points[j].Y >= playerPosition.Y || points[j].Y < playerPosition.Y && points[i].Y >= playerPosition.Y)
                {
                    if (points[i].X + (playerPosition.Y - points[i].Y) / (points[j].Y - points[i].Y) * (points[j].X - points[i].X) < playerPosition.X)
                    {
                        result = !result;
                    }
                }

                j = i;
            }

            return result;
        }
    }
}
