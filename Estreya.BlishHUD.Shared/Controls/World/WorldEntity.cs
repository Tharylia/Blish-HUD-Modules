namespace Estreya.BlishHUD.Shared.Controls.World;

using Blish_HUD;
using Blish_HUD.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public abstract class WorldEntity : IEntity
{
    public WorldEntity(Vector3 position, float scale)
    {
        this.Position = position;
        this.Scale = scale;
    }

    protected Vector3 Position { get; set; }
    protected float Scale { get; } = 1f;

    public float DistanceToPlayer { get; private set; }

    protected BasicEffect RenderEffect { get; private set; }
    public float DrawOrder => 1f;

    public void Render(GraphicsDevice graphicsDevice, IWorld world, ICamera camera)
    {
        this.RenderEffect ??= new BasicEffect(graphicsDevice);
        this.RenderEffect.VertexColorEnabled = true;

        this.InternalRender(graphicsDevice, world, camera);
    }

    public virtual void Update(GameTime gameTime)
    {
        this.DistanceToPlayer = Vector3.Distance(GameService.Gw2Mumble.PlayerCharacter.Position, this.Position);
    }

    protected abstract void InternalRender(GraphicsDevice graphicsDevice, IWorld world, ICamera camera);

    public abstract bool IsPlayerInside(bool includeZAxis = true);
}