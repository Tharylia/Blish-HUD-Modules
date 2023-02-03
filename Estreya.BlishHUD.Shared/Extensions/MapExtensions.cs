namespace Estreya.BlishHUD.Shared.Extensions
{
    using Gw2Sharp.WebApi.V2.Models;
    using Microsoft.Xna.Framework;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;

    public static class MapExtensions
    {
        public static Vector2 WorldInchCoordsToMapCoords(this Map map, Vector3 world)
            => new Vector2(
                (float)(map.ContinentRect.TopLeft.X + (world.X - map.MapRect.TopLeft.X) / map.MapRect.Width * map.ContinentRect.Width),
                (float)(map.ContinentRect.TopLeft.Y - (world.Y - map.MapRect.TopLeft.Y) / map.MapRect.Height * map.ContinentRect.Height));
        public static Vector2 WorldMeterCoordsToMapCoords(this Map map, Vector3 world)
            => map.WorldInchCoordsToMapCoords(world * 39.3700787f);
    }
}
