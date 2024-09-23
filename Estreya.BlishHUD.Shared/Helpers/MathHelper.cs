namespace Estreya.BlishHUD.Shared.Helpers;

using Microsoft.Xna.Framework;
using MonoGame.Extended;
using System;
using System.Numerics;

public static class MathHelper
{
    public static float CalculateAngle(Point start, Point arrival)
    {
        float radian = (float)Math.Atan2(arrival.Y - start.Y, arrival.X - start.X);

        return radian;
    }

    public static float CalculateAngle(Point2 start, Point2 arrival)
    {
        float radian = (float)Math.Atan2(arrival.Y - start.Y, arrival.X - start.X);

        return radian;
    }

    public static double CalculateDistance(Point start, Point arrival)
    {
        double deltaX = Math.Pow(arrival.X - start.X, 2);
        double deltaY = Math.Pow(arrival.Y - start.Y, 2);

        double distance = Math.Sqrt(deltaY + deltaX);

        return distance;
    }

    public static float CalculateDistance(Point2 start, Point2 arrival)
    {
        double deltaX = Math.Pow(arrival.X - start.X, 2);
        double deltaY = Math.Pow(arrival.Y - start.Y, 2);

        float distance = (float)Math.Sqrt(deltaY + deltaX);

        return distance;
    }

    public static double Scale(double value, double sourceScaleMin, double sourceScaleMax, double destScaleMin, double destScaleMax)
    {
        double normalised_value = (value - sourceScaleMin) / (sourceScaleMax - sourceScaleMin);
        double new_value = (normalised_value * (destScaleMax - destScaleMin)) + destScaleMin;
        return new_value;
    }
}