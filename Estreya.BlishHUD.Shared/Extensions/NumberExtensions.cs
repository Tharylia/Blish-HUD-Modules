namespace Estreya.BlishHUD.Shared.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Text;

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
    }
}
