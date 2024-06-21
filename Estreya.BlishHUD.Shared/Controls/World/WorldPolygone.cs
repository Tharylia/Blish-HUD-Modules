namespace Estreya.BlishHUD.Shared.Controls.World;

using Blish_HUD;
using Blish_HUD.Entities;
using Blish_HUD.Graphics;
using Estreya.BlishHUD.Shared.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

public class WorldPolygone : WorldEntity
{
    private readonly Color _color;
    private VertexPositionColor[] _vertexData;

    public WorldPolygone(Vector3 position, Vector3[] points, Color color) : base(position, 1)
    {
        if (points.Length < 2 || points.Length % 2 != 0)
        {
            throw new ArgumentOutOfRangeException("points");
        }

        this._color = color;
        this.Points = points; // This triggers rebuild of vertices
    }

    public WorldPolygone(Vector3 position, Vector3[] points) : this(position, points, Color.White)
    {
    }

    private Vector3[] _points;

    public Vector3[] Points { get => this._points;
        set
        {
            this._points = value;
            this._vertexData = this.BuildVertices();
        }
    }

    private VertexPositionColor[] BuildVertices()
    {
        VertexPositionColor[] verts = new VertexPositionColor[this.Points.Length];

        for (int i = 0; i < this.Points.Length; i++)
        {
            verts[i] = new VertexPositionColor(this.Points[i], this._color);
        }

        using GraphicsDeviceContext ctx = GameService.Graphics.LendGraphicsDeviceContext();
        using VertexBuffer sectionBuffer = new VertexBuffer(ctx.GraphicsDevice, VertexPositionColor.VertexDeclaration, verts.Length, BufferUsage.WriteOnly);
        sectionBuffer.SetData(verts);

        return verts;
    }

    protected override void InternalRender(GraphicsDevice graphicsDevice, IWorld world, ICamera camera)
    {
        var modelMatrix = this.GetMatrix(graphicsDevice, world, camera);

        this.RenderEffect.View = GameService.Gw2Mumble.PlayerCamera.View;
        this.RenderEffect.Projection = GameService.Gw2Mumble.PlayerCamera.Projection;
        this.RenderEffect.World = modelMatrix;

        //graphicsDevice.SetVertexBuffer(_vertexBuffer);
        foreach (EffectPass pass in this.RenderEffect.CurrentTechnique.Passes)
        {
            pass.Apply();

            graphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, this._vertexData, 0, this._vertexData.Length / 2);
        }
    }

    public Vector3[] GetAbsolutePoints()
    {
        List<Vector3> newPoints = new List<Vector3>();
        foreach (Vector3 point in this.Points)
        {
            newPoints.Add(this.Position + point);
        }

        return newPoints.ToArray();
    }

    public override bool IsPlayerInside(bool includeZAxis = true)
    {
        Vector3 playerPosition = GameService.Gw2Mumble.PlayerCharacter.Position;
        Vector3[] points = this.GetAbsolutePoints();

        float maxZ = points.Max(p => p.Z);
        float minZ = points.Min(p => p.Z);

        if (includeZAxis && (playerPosition.Z > maxZ || playerPosition.Z < minZ))
        {
            return false;
        }

        bool result = false;
        int j = points.Length - 1;
        for (int i = 0; i < points.Length; i++)
        {
            if ((points[i].Y < playerPosition.Y && points[j].Y >= playerPosition.Y) || (points[j].Y < playerPosition.Y && points[i].Y >= playerPosition.Y))
            {
                if (points[i].X + ((playerPosition.Y - points[i].Y) / (points[j].Y - points[i].Y) * (points[j].X - points[i].X)) < playerPosition.X)
                {
                    result = !result;
                }
            }

            j = i;
        }

        return result;
    }
}