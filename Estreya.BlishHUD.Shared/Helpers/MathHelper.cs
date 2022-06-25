namespace Estreya.BlishHUD.Shared.Helpers
{
    using Microsoft.Xna.Framework;
    using MonoGame.Extended;
    using System;

    public static class MathHelper
    {
        public static float CalculeAngle(Point start, Point arrival)
        {
            float radian = (float)Math.Atan2(arrival.Y - start.Y, arrival.X - start.X);

            return radian;
        }

        public static float CalculeAngle(Point2 start, Point2 arrival)
        {
            float radian = (float)Math.Atan2(arrival.Y - start.Y, arrival.X - start.X);

            return radian;
        }
        public static double CalculeDistance(Point start, Point arrival)
        {
            double deltaX = Math.Pow(arrival.X - start.X, 2);
            double deltaY = Math.Pow(arrival.Y - start.Y, 2);

            double distance = Math.Sqrt(deltaY + deltaX);

            return distance;
        }

        public static float CalculeDistance(Point2 start, Point2 arrival)
        {
            double deltaX = Math.Pow(arrival.X - start.X, 2);
            double deltaY = Math.Pow(arrival.Y - start.Y, 2);

            float distance = (float)Math.Sqrt(deltaY + deltaX);

            return distance;
        }
    }
}
