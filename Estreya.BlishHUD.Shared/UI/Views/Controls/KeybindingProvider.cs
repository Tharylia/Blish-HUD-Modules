namespace Estreya.BlishHUD.Shared.UI.Views.Controls
{
    using Blish_HUD.Controls;
    using Blish_HUD.Input;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.Shared.Extensions;
    using Microsoft.Xna.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    internal class KeybindingProvider : ControlProvider<KeyBinding, KeyBinding>
    {

        public override Control CreateControl(BoxedValue<KeyBinding> value, Func<KeyBinding, bool> isEnabled, Func<KeyBinding, bool> isValid, (float Min, float Max)? range, int width, int heigth, int x, int y)
        {
            Shared.Controls.KeybindingAssigner keybindingAssigner = new Shared.Controls.KeybindingAssigner(value.Value, false)
            {
                Width = width,
                Location = new Point(x, y),
                Enabled = isEnabled?.Invoke(value?.Value) ?? true
            };

            if (value != null)
            {
                keybindingAssigner.BindingChanged += (s, e) =>
            {
                if (isValid?.Invoke(keybindingAssigner.KeyBinding) ?? true)
                {
                    value.Value = keybindingAssigner.KeyBinding;
                }
                else
                {
                    keybindingAssigner.KeyBinding = value.Value;
                }
            };
            }

            return keybindingAssigner;
        }
    }
}
