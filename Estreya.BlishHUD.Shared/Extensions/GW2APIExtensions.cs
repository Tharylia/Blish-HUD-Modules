namespace Estreya.BlishHUD.Shared.Extensions;

using Gw2Sharp.Models;
using Microsoft.Xna.Framework;

public static class GW2APIExtensions
{
    public static Vector2 ToVector2(this Coordinates2 coords)
    {
        return new Vector2((float)coords.X, (float)coords.Y);
    }
}