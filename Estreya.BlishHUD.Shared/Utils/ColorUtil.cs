namespace Estreya.BlishHUD.Shared.Utils
{
    using Blish_HUD;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class ColorUtil
    {
        private static Color[][] CreateColorGradientRows(Color start, Color end, int width, int height)
        {
            List<Color[]> bgc = new List<Color[]>();
            float stepA = (end.A - start.A) / (width - 1f);
            float stepR = (end.R - start.R) / (width - 1f);
            float stepG = (end.G - start.G) / (width - 1f);
            float stepB = (end.B - start.B) / (width - 1f);

            for (int h = 0; h < height; h++)
            {
                var colorRow = new Color[width];
                for (int w = 0; w < width; w++)
                {

                    colorRow[w] = new Color(
                        (start.R + (stepR * w)) / 255,
                        (start.G + (stepG * w)) / 255,
                        (start.B + (stepB * w)) / 255,
                        (start.A + (stepA * w)) / 255);
                }

                bgc.Add(colorRow);
            }

            return bgc.ToArray();
        }

        public static Color[] CreateColorGradient(Color start, Color end, int width, int height)
        {
            return CreateColorGradientRows(start, end, width, height).SelectMany(r => r).ToArray();
        }

        public static Color[] CreateColorGradients(Color[] colors, int width, int height)
        {
            if (colors == null || colors.Length < 2) throw new ArgumentOutOfRangeException(nameof(colors), "A color gradient needs at least 2 colors.");

            if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException("size", "Width and Height must be at least 1.");

            List<Color[]> rows = new List<Color[]>();

            var sectionWidth = width / (colors.Length - 1);

            for (int i = 0; i < colors.Length - 1; i++)
            {
                var start = colors[i];
                var end = colors[i + 1];

                var newRows = CreateColorGradientRows(start, end, sectionWidth, height);
                if (i != 0 && newRows.Length != rows.Count) throw new ArgumentOutOfRangeException("rowCount", "A subsequential run has generated more rows than expected.");

                for (int r = 0; r < newRows.Length; r++)
                {
                    if (r == rows.Count)
                    {
                        rows.Add(newRows[r]);
                    }
                    else
                    {
                        rows[r] = rows[r].Concat(newRows[r]).ToArray();
                    }
                }
            }

            if (rows.Count > 0 && rows[0].Length != width)
            {
                // We have a rounding error and need to push the last few pixels
                var missingWidth = width - rows[0].Length;
                for (int r = 0; r < rows.Count; r++)
                {
                    rows[r] = rows[r].Concat(Enumerable.Repeat(rows[r].Last(), missingWidth)).ToArray();
                }
            }

            return rows.SelectMany(b => b).ToArray();
        }

        public static Texture2D CreateColorGradientTexture(Color start, Color end, int width, int height)
        {
            using var ctx = GameService.Graphics.LendGraphicsDeviceContext();
            var backgroundTex = new Texture2D(ctx.GraphicsDevice, width, height);

            var colors = CreateColorGradient(start, end, width, height);

            backgroundTex.SetData(colors);
            return backgroundTex;
        }

        public static Texture2D CreateColorGradientsTexture(Color[] colors, int width, int height)
        {
            using var ctx = GameService.Graphics.LendGraphicsDeviceContext();
            var backgroundTex = new Texture2D(ctx.GraphicsDevice, width, height);

            var gradients = CreateColorGradients(colors, width, height);

            backgroundTex.SetData(gradients);
            return backgroundTex;
        }
    }
}
