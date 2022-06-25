namespace Estreya.BlishHUD.Shared.UI.Views.Controls
{
    using Blish_HUD.Controls;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.Shared.Extensions;
    using Microsoft.Xna.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    internal class IntTrackBarProvider : ControlProvider<int, int>
    {
        public override Control CreateControl(BoxedValue<int> value, Func<int, bool> isEnabled, Func<int, bool> isValid, (float Min, float Max)? range, int width, int heigth, int x, int y)
        {
            TrackBar trackBar = new TrackBar()
            {
                Width = width,
                Location = new Point(x, y)
            };

            trackBar.Enabled = isEnabled?.Invoke((int)trackBar.Value) ?? true;

            trackBar.MinValue = range.HasValue ? range.Value.Min : 0;
            trackBar.MaxValue = range.HasValue ? range.Value.Max : 100;

            trackBar.Value = value?.Value ?? 50;

            if (value != null)
            {
                trackBar.ValueChanged += (s, e) =>
                {
                    if (isValid?.Invoke((int)e.Value) ?? true)
                    {
                        value.Value = (int)e.Value;
                    }
                    else
                    {
                        trackBar.Value = value.Value;
                    }
                };
            }

            return trackBar;
        }
    }
}
