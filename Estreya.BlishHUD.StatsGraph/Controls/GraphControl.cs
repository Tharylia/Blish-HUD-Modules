namespace Estreya.BlishHUD.StatsGraph.Controls;

using Blish_HUD;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class GraphControl : Control
{
    private Texture2D _texture;

    public GraphControl()
    {
        this.Visible = false;
    }

    public void UpdateTexture(Texture2D texture)
    {
        this._texture?.Dispose();
        this._texture = texture;
        this.Visible = this._texture != null;

        this.Width = this._texture.Width;
        this.Height = this._texture.Height;
    }

    protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds)
    {
        if (this._texture == null) return;

        spriteBatch.DrawOnCtrl(this, _texture, bounds);
    }

    protected override void DisposeControl()
    {
        this._texture?.Dispose();
        this._texture = null;
    }
}
