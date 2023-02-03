namespace Estreya.BlishHUD.Shared.Extensions
{
    using Gw2Sharp.Models;
    using Microsoft.Xna.Framework;
    using System;
    using System.Collections.Generic;
    using System.Text;

    public static class GW2APIExtensions
    {
        public static Vector2 ToVector2(this Coordinates2 coords) => new Vector2((float)coords.X, (float)coords.Y);
    }
}
