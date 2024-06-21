namespace Estreya.BlishHUD.EventTable.Controls.World;

using Blish_HUD.Entities;
using Estreya.BlishHUD.Shared.Controls.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

public class EventWorldTimer : WorldPolygone
{
    public EventWorldTimer(Vector3 position, Vector3[] points) : this(position, points, Color.White)
    {
    }

    public EventWorldTimer(Vector3 position, Vector3[] points, Color color) : base(position, points, color)
    {
    }
}
