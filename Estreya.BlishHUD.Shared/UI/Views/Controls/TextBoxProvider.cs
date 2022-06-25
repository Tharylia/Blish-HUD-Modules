namespace Estreya.BlishHUD.Shared.UI.Views.Controls;

using Blish_HUD;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

internal class TextBoxProvider : ControlProvider<string, string>
{

    public override Control CreateControl(BoxedValue<string> value, Func<string, bool> isEnabled, Func<string, bool> isValid, (float Min, float Max)? range, int width, int height, int x, int y)
    {
        TextBox textBox = new TextBox()
        {
            Width = width,
            Location = new Point(x, y),
            Text = value?.Value ?? string.Empty,
            Enabled = isEnabled?.Invoke(value?.Value) ?? true
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
                    value.Value = eventArgs.NewValue;
                }
                else
                {
                    var cIndex = textBox.CursorIndex - 1; // -1 because of new last typed char
                    textBox.Text = eventArgs.PreviousValue;
                    textBox.CursorIndex = cIndex;
                }
            };
        }

        return textBox;
    }
}
