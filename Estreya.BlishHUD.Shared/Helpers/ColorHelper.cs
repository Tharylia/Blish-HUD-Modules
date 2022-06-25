namespace Estreya.BlishHUD.Shared.Helpers
{
    using Microsoft.Xna.Framework;
    using System;

    internal static class ColorHelper
    {
        public static Color GetComplementary(this Color color)
        {
            HSLColor hsl = HSLColor.FromColor(color);
            hsl.H += 0.5f;
            if (hsl.H > 1)
            {
                hsl.H -= 1f;
            }

            Color switchedColor = hsl.ToRgbColor();
            switchedColor.A = color.A;
            return switchedColor;

            // To HSL

            float r = color.R / 255f;
            float g = color.G / 255f;
            float b = color.B / 255f;
            float min = Math.Min(Math.Min(r, g), b);
            float max = Math.Max(Math.Max(r, g), b);

            float h = 0f;
            float s = 0f;
            float l = (max + min) / 2;

            if (max != 0)
            {
                if (l < 0.5)
                {
                    s = max / (max + min);
                }
                else
                {
                    s = max / (2 - max - min);
                }

                float delta_r = (((max - r) / 6) + (max / 2)) / max;
                float delta_g = (((max - g) / 6) + (max / 2)) / max;
                float delta_b = (((max - b) / 6) + (max / 2)) / max;

                if (r == max)
                {
                    h = delta_b - delta_g;
                }
                else if (g == max)
                {
                    h = (1 / 3) + delta_r - delta_b;
                }
                else if (b == max)
                {
                    h = (2 / 3) + delta_g - delta_r;
                }

                if (h < 0)
                {
                    h += 1;
                }

                if (h > 1)
                {
                    h -= 1;
                }
            }

            // Hue 180°

            h += 0.5f;
            if (h > 1)
            {
                h -= 1;
            }

            // Back to Color

            if (s == 0)
            {
                return new Color(l * 255, l * 255, l * 255);
            }

            float two = 0.0f;

            if (l < 0.5)
            {
                two = l * (1 + s);
            }
            else
            {
                two = (l + s) - (s * l);
            }

            float one = 2 * l - two;

            return new Color(255 * HueToRGB(one, two, h + (1 / 3)), 255 * HueToRGB(one, two, h), 255 * HueToRGB(one, two, h - (1 / 3)));

        }

        private static float HueToRGB(float one, float two, float hue)
        {
            if (hue < 0)
            {
                hue += 1;
            }

            if (hue > 1)
            {
                hue -= 1;
            }

            if ((6 * hue) < 1)
            {
                return one + (two - one) * 6 * hue;
            }

            if ((2 * hue) < 1)
            {
                return two;
            }

            if ((3 * hue) < 2)
            {
                return one + (two - one) * ((2 / 3) - hue) * 6;
            }

            return one;
        }

        private struct HSLColor
        {

            // HSL stands for Hue, Saturation and Luminance. HSL
            // color space makes it easier to do calculations
            // that operate on these channels
            // Helpful color math can be found here:
            // https://www.easyrgb.com/en/math.php

            /// <summary>
            /// Hue: the 'color' of the color!
            /// </summary>
            public float H;

            /// <summary>
            /// Saturation: How grey or vivid/colorful a color is
            /// </summary>
            public float S;

            /// <summary>
            /// Luminance: The brightness or lightness of the color
            /// </summary>
            public float L;

            public HSLColor(float h, float s, float l)
            {
                this.H = h;
                this.S = s;
                this.L = l;
            }

            public static HSLColor FromColor(Color color)
            {
                return FromRgb(color.R, color.G, color.B);
            }


            public static HSLColor FromRgb(byte R, byte G, byte B)
            {
                HSLColor hsl = new HSLColor
                {
                    H = 0,
                    S = 0,
                    L = 0
                };

                float r = R / 255f;
                float g = G / 255f;
                float b = B / 255f;
                float min = Math.Min(Math.Min(r, g), b);
                float max = Math.Max(Math.Max(r, g), b);
                float delta = max - min;

                // luminance is the ave of max and min
                hsl.L = (max + min) / 2f;


                if (delta > 0)
                {
                    if (hsl.L < 0.5f)
                    {
                        hsl.S = delta / (max + min);
                    }
                    else
                    {
                        hsl.S = delta / (2 - max - min);
                    }

                    float deltaR = (((max - r) / 6f) + (delta / 2f)) / delta;
                    float deltaG = (((max - g) / 6f) + (delta / 2f)) / delta;
                    float deltaB = (((max - b) / 6f) + (delta / 2f)) / delta;

                    if (r == max)
                    {
                        hsl.H = deltaB - deltaG;
                    }
                    else if (g == max)
                    {
                        hsl.H = (1f / 3f) + deltaR - deltaB;
                    }
                    else if (b == max)
                    {
                        hsl.H = (2f / 3f) + deltaG - deltaR;
                    }

                    if (hsl.H < 0)
                    {
                        hsl.H += 1;
                    }

                    if (hsl.H > 1)
                    {
                        hsl.H -= 1;
                    }
                }

                return hsl;
            }

            public HSLColor GetComplement()
            {

                // complementary colors are across the color wheel
                // which is 180 degrees or 50% of the way around the
                // wheel. Add 50% to our hue and wrap large/small values
                float h = this.H + 0.5f;
                if (h > 1)
                {
                    h -= 1;
                }

                return new HSLColor(h, this.S, this.L);
            }

            public Color ToRgbColor()
            {
                Color c = new Color();

                if (this.S == 0)
                {
                    c.R = (byte)(this.L * 255f);
                    c.G = (byte)(this.L * 255f);
                    c.B = (byte)(this.L * 255f);
                }
                else
                {
                    float v2 = (this.L + this.S) - (this.S * this.L);
                    if (this.L < 0.5f)
                    {
                        v2 = this.L * (1 + this.S);
                    }
                    float v1 = 2f * this.L - v2;

                    c.R = (byte)(255f * HueToRgb(v1, v2, this.H + (1f / 3f)));
                    c.G = (byte)(255f * HueToRgb(v1, v2, this.H));
                    c.B = (byte)(255f * HueToRgb(v1, v2, this.H - (1f / 3f)));
                }

                return c;
            }

            private static float HueToRgb(float v1, float v2, float vH)
            {
                vH += (vH < 0) ? 1 : 0;
                vH -= (vH > 1) ? 1 : 0;
                float ret = v1;

                if ((6 * vH) < 1)
                {
                    ret = v1 + (v2 - v1) * 6 * vH;
                }

                else if ((2 * vH) < 1)
                {
                    ret = v2;
                }

                else if ((3 * vH) < 2)
                {
                    ret = v1 + (v2 - v1) * ((2f / 3f) - vH) * 6f;
                }

                return Math.Max(Math.Min(ret, 1), 0);
            }
        }
    }


}
