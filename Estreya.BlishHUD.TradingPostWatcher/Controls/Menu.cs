namespace Estreya.BlishHUD.TradingPostWatcher.Controls;

using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Menu : Blish_HUD.Controls.Menu
{
    public override void RecalculateLayout()
    {
        base.RecalculateLayout();

        int lastBottom = 0;

        foreach (var child in _children.Where(c => c.Visible))
        {
            child.Location = new Point(0, lastBottom);
            child.Width = this.Width;

            lastBottom = child.Bottom;
        }
    }
}
