namespace Estreya.BlishHUD.Shared.UI.Views.Controls
{
    using Blish_HUD;
    using Blish_HUD.Controls;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.Shared.Extensions;
    using Microsoft.Xna.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    internal class FloatTextBoxProvider : ControlProvider<float,string>
    {
        private static readonly Logger Logger = Logger.GetLogger<IntTextBoxProvider>();

        public override Control CreateControl(BoxedValue<float> value, Func<float, bool> isEnabled, Func<string, bool> isValid, (float Min, float Max)? range, int width, int heigth, int x, int y)
        {
            TextBox textBox = new TextBox()
            {
                Width = width,
                Location = new Point(x, y),
                Text = (value?.Value ?? 50).ToString()
            };

            textBox.Enabled = isEnabled?.Invoke(value?.Value ?? 50) ?? true;

            if (value != null)
            {
                textBox.TextChanged += (s, e) =>
                {
                    ValueChangedEventArgs<string> eventArgs = (ValueChangedEventArgs<string>)e;

                    string newValue = string.IsNullOrWhiteSpace(eventArgs.NewValue) ? "0" : eventArgs.NewValue;
                    bool parsed = int.TryParse(newValue, out int parsedNewValue);

                    Logger.Debug("Value \"{0}\" could be parsed: {1}", newValue, parsed);
                    if (!parsed)
                    {
                        Logger.Debug("Range check not available.");
                    }

                    bool rangeValid = true;

                    if (range != null && parsed)
                    {
                        if (parsedNewValue < range.Value.Min || parsedNewValue > range.Value.Max)
                        {
                            rangeValid = false;
                        }
                    }

                    if ((isValid?.Invoke(newValue) ?? true) && rangeValid )
                    {
                        int.TryParse(newValue, out int parsedValue);
                        value.Value = parsedValue;
                    }
                };
            }

            return textBox;
        }
    }
}
