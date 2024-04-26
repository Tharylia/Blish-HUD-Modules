namespace Estreya.BlishHUD.Shared.Extensions;

public static class NumberExtensions
{
    private const float METER_TO_INCHES_RATIO = 39.3700787f;

    public static double ToInches(this double value)
    {
        return value * METER_TO_INCHES_RATIO;
    }

    public static float ToInches(this float value)
    {
        return value * METER_TO_INCHES_RATIO;
    }

    public static double ToMeters(this double value)
    {
        return value / METER_TO_INCHES_RATIO;
    }

    public static float ToMeters(this float value)
    {
        return value / METER_TO_INCHES_RATIO;
    }

    public static int Remap(this int from, int fromMin, int fromMax, int toMin, int toMax)
    {
        return (int)Remap((float)from, fromMin, fromMax, toMin, toMax);
    }

    public static float Remap(this float from, float fromMin, float fromMax, float toMin, float toMax)
    {
        return (float)Remap((double)from, fromMin, fromMax, toMin, toMax);
    }

    public static double Remap(this double from, double fromMin, double fromMax, double toMin, double toMax)
    {
        var fromAbs = from - fromMin;
        var fromMaxAbs = fromMax - fromMin;

        var normal = fromAbs / fromMaxAbs;

        var toMaxAbs = toMax - toMin;
        var toAbs = toMaxAbs * normal;

        var to = toAbs + toMin;

        return to;
    }
}