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
        public static Vector3 MapCoordsToWorldInches(this Map map, Vector2 mapCoords)
        => new Vector3(
            (float)((mapCoords.X - map.ContinentRect.TopLeft.X) / map.ContinentRect.Width * map.MapRect.Width + map.MapRect.TopLeft.X),
            (float)((map.ContinentRect.TopLeft.Y- mapCoords.Y) / map.ContinentRect.Height * map.MapRect.Height + map.MapRect.TopLeft.Y),
            0);
        public static Vector3 MapCoordsToWorldMeters(this Map map, Vector2 mapCoords)
            => map.MapCoordsToWorldInches(mapCoords) / 39.3700787f;

        public static Vector2 WorldInchCoordsToMapCoords(this Map map, Vector2 coords)
            => new Vector2(
                (float)(map.ContinentRect.TopLeft.X + (coords.X - map.MapRect.TopLeft.X) / map.MapRect.Width * map.ContinentRect.Width),
                (float)(map.ContinentRect.TopLeft.Y - (coords.Y - map.MapRect.TopLeft.Y) / map.MapRect.Height * map.ContinentRect.Height));

        public static Vector2 WorldInchCoordsToMapCoords(this Map map, Vector3 world)
            => WorldInchCoordsToMapCoords(map, new Vector2(world.X, world.Y));

        public static Vector2 WorldMeterCoordsToMapCoords(this Map map, Vector3 world)
            => map.WorldInchCoordsToMapCoords(world * 39.3700787f);

        public static double GetDynamicEventMapLengthScale(this Map map, double length)
        {
            var map_rect = map.MapRect;

            length /= 1d / 24d;

            var scalex = (length - map_rect.BottomLeft.X) / (map_rect.TopRight.X - map_rect.BottomLeft.X);
            var scaley = (length - map_rect.BottomLeft.Y) / (map_rect.TopRight.Y - map_rect.BottomLeft.Y);
            return Math.Sqrt((scalex * scalex) + (scaley * scaley));
        }

        public static Vector2 EventMapCoordinatesToMapCoordinates(this Map map, Vector2 coordinates)
        {
            var continent_rect = map.ContinentRect;
            var map_rect = map.MapRect;

            var x = (float)Math.Round(continent_rect.TopLeft.X + (1 * (coordinates.X - map_rect.BottomLeft.X) / (map_rect.TopRight.X - map_rect.BottomLeft.X) * (continent_rect.BottomRight.X - continent_rect.TopLeft.X)));
            var y = (float)Math.Round(continent_rect.TopLeft.Y + (-1 * (coordinates.Y - map_rect.TopRight.Y) / (map_rect.TopRight.Y - map_rect.BottomLeft.Y) * (continent_rect.BottomRight.Y - continent_rect.TopLeft.Y)));

            return new Vector2(x, y);
        }

    }
}
