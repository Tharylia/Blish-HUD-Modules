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

    internal class FloatTrackBarProvider : ControlProvider<float,float>
    {
        public override Control CreateControl(BoxedValue<float> value, Func<float, bool> isEnabled, Func<float, bool> isValid, (float Min, float Max)? range, int width, int heigth, int x, int y)
        {
            TrackBar trackBar = new TrackBar()
            {
                Width = width,
                Location = new Point(x, y),
                SmallStep = true
            };

            trackBar.Enabled = isEnabled?.Invoke(trackBar.Value) ?? true;

            trackBar.MinValue = range.HasValue ? range.Value.Min : 0;
            trackBar.MaxValue = range.HasValue ? range.Value.Max : 100;

            trackBar.Value = value?.Value ?? 50;

            if (value != null)
            {
                trackBar.ValueChanged += (s, e) =>
                {
                    if (isValid?.Invoke(e.Value) ?? true)
                    {
                        value.Value = e.Value;
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
