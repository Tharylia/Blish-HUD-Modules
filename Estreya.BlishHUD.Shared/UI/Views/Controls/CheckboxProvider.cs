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

    internal class CheckboxProvider : ControlProvider<bool,bool>
    {
        public override Control CreateControl(BoxedValue<bool> value, Func<bool, bool> isEnabled, Func<bool, bool> isValid, (float Min, float Max)? range, int width, int heigth, int x, int y)
        {
            Checkbox checkbox = new Checkbox()
            {
                Width = width,
                Location = new Point(x, y),
                Checked = value?.Value ?? false,
                Enabled = isEnabled?.Invoke(value?.Value ?? false) ?? true
            };

            if (value != null)
            {
                checkbox.CheckedChanged += (s, e) =>
                {
                    if (isValid?.Invoke(e.Checked) ?? true)
                    {
                        value.Value = e.Checked;
                    }
                    else
                    {
                        checkbox.Checked = !e.Checked;
                    }
                };
            }

            return checkbox;
        }
    }
}
