namespace Estreya.BlishHUD.Shared.Controls.World;

using Blish_HUD;
using Blish_HUD.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Threading.Tasks;

public abstract class WorldEntity : IEntity
{
    public WorldEntity(Vector3 position, float scale)
    {
        this.Position = position;
        this.ScaleX = scale;
        this.ScaleY = scale;
        this.ScaleZ = scale;
    }

    protected Vector3 Position { get; set; }
    public float ScaleX { get; set; } = 1f;
    public float ScaleY { get; set; } = 1f;
    public float ScaleZ { get; set; } = 1f;

    public float RotationX { get; set; } = 0f;
    public float RotationY { get; set; } = 0f;
    public float RotationZ { get; set; } = 0f;

    public float DistanceToPlayer { get; private set; }

    public Func<WorldEntity, bool> RenderCondition { get; set; }

    //private SmallInteract _smallInteract;

    //public Func<Task> InteractionAction { get; set; }

    //public float InteractionMaxDistance { get; set; } = 2f;

    protected BasicEffect RenderEffect { get; private set; }
    public float DrawOrder => 1f;

    public void Render(GraphicsDevice graphicsDevice, IWorld world, ICamera camera)
    {
        if (this.RenderCondition is not null && !this.RenderCondition(this)) return;

        this.RenderEffect ??= new BasicEffect(graphicsDevice);
        this.RenderEffect.VertexColorEnabled = true;

        this.InternalRender(graphicsDevice, world, camera);
    }

    public virtual void Update(GameTime gameTime)
    {
        this.DistanceToPlayer = Vector3.Distance(GameService.Gw2Mumble.PlayerCharacter.Position, this.Position);

        //this.CheckInteract();
    }

    //private void CheckInteract()
    //{
    //    if (this.InteractionAction is null || this.DistanceToPlayer > this.InteractionMaxDistance)
    //    {
    //        this._smallInteract?.Dispose();
    //        this._smallInteract = null;

    //        return;
    //    }

    //    if (this._smallInteract is null)
    //    {
    //        this._smallInteract = new SmallInteract()
    //        {
    //            Parent = GameService.Graphics.SpriteScreen
    //        };
    //        this._smallInteract.ShowInteract("Copy Waypoint");
    //    }

    //}

    protected virtual Matrix GetMatrix(GraphicsDevice graphicsDevice, IWorld world, ICamera camera)
    {
        var matrix = Matrix.CreateScale(this.ScaleX, this.ScaleY, this.ScaleZ)
            * Matrix.CreateRotationX(MathHelper.ToRadians(this.RotationX))
            * Matrix.CreateRotationY(MathHelper.ToRadians(this.RotationY))
            * Matrix.CreateRotationZ(MathHelper.ToRadians(this.RotationZ))
            * Matrix.CreateTranslation(this.Position);
        return matrix;
    }

    protected abstract void InternalRender(GraphicsDevice graphicsDevice, IWorld world, ICamera camera);

    public abstract bool IsPlayerInside(bool includeZAxis = true);
}