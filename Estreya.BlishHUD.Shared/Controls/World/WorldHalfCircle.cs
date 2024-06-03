namespace Estreya.BlishHUD.Shared.Controls.World
{
    using Blish_HUD.Entities;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using System;
    using Extensions;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class WorldHalfCircle : WorldPolygone
    {
        public WorldHalfCircle(Vector3 position, float radius, int tessellation = 50) : this(position, radius, Color.White, tessellation) { }

        public WorldHalfCircle(Vector3 position, float radius, Color color, int tessellation = 50) : base(position, Enumerable.Range(0, tessellation + 1).SelectWithIndex((t, index, sourceList) =>
        {
            float circumferenceProgress = (float)index / tessellation;
            float currentRadian = (float)(circumferenceProgress * 1 * Math.PI);

            float xScaled = (float)Math.Cos(currentRadian);
            float yScaled = (float)Math.Sin(currentRadian);

            float x = xScaled * radius;
            float y = yScaled * radius;

            return new Vector3(x, y, 0);
        }).SelectManyWithIndex((t, index, sourceList, first, last) =>
        {
            IEnumerable<Vector3> arr = Enumerable.Repeat(t, first || last ? 1 : 2);
            first = false;
            return arr;
        }).ToArray(), color)
        {
        }
    }
}
