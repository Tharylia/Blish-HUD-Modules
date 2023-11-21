namespace Estreya.BlishHUD.PortalDistance.Controls;

using Blish_HUD;
using Blish_HUD.Controls;
using Estreya.BlishHUD.Shared.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class DistanceMessageControl : Control
{
    private float _distance;
    private Color _color;

    public DistanceMessageControl()
    {
        this.Parent = GameService.Graphics.SpriteScreen;
    }

    protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds)
    {
        spriteBatch.DrawStringOnCtrl(this, $"Distance: {Math.Round(this._distance, 2)}", GameService.Content.DefaultFont32, bounds, _color, horizontalAlignment: HorizontalAlignment.Center, verticalAlignment: VerticalAlignment.Middle);
    }

    public override void DoUpdate(GameTime gameTime)
    {
        this.Size = new Point(this.Parent.Width, this.Parent.Height/2);
    }

    protected override CaptureType CapturesInput()
    {
        return CaptureType.None;
    }

    public void UpdateDistance(float distance)
    {
        this._distance = distance;
    }

    public void UpdateColor(Color color)
    {
        this._color = color;
    }
}
