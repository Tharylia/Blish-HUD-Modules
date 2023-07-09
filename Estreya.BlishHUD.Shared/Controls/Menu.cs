namespace Estreya.BlishHUD.Shared.Controls;

using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using System.Linq;

public class Menu : Blish_HUD.Controls.Menu
{
    public override void RecalculateLayout()
    {
        base.RecalculateLayout();

        int lastBottom = 0;

        foreach (Control child in this._children.Where(c => c.Visible))
        {
            child.Location = new Point(0, lastBottom);
            child.Width = this.Width;

            lastBottom = child.Bottom;
        }
    }
}