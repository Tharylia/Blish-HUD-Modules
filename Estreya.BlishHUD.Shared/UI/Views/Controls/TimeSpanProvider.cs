namespace Estreya.BlishHUD.Shared.UI.Views.Controls;

using Blish_HUD;
using Blish_HUD.Controls;
using Estreya.BlishHUD.Shared.UI.Views.Controls;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

internal class TimeSpanProvider : ControlProvider<TimeSpan, string>
{
    public override Control CreateControl(BoxedValue<TimeSpan> value, Func<TimeSpan, bool> isEnabled, Func<string, bool> isValid, (float Min, float Max)? range, int width, int heigth, int x, int y)
    {
        TextBox textBox = new TextBox()
        {
            Width = width,
            Location = new Point(x, y),
            Text = value?.Value.ToString(),
            Enabled = isEnabled?.Invoke(value?.Value ?? TimeSpan.Zero) ?? true
        };

        if (value != null)
        {
            textBox.TextChanged += (s, e) =>
            {
                ValueChangedEventArgs<string> eventArgs = (ValueChangedEventArgs<string>)e;

                bool rangeValid = true;

                if (range != null)
                {
                    if (eventArgs.NewValue.Length < range.Value.Min || eventArgs.NewValue.Length > range.Value.Max)
                    {
                        rangeValid = false;
                    }
                }

                

                if (rangeValid && (isValid?.Invoke(eventArgs.NewValue) ?? true))
                {
                    TimeSpan.TryParse(eventArgs.NewValue, out TimeSpan parsedTS);
                    value.Value = parsedTS;
                }
            };
        }

        return textBox;
    }
}
