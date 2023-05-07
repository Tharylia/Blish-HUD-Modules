namespace Estreya.BlishHUD.Shared.Models
{
    using Estreya.BlishHUD.Shared.Attributes;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Text;

    public enum CornerIconClickAction
    {
        [Translation("cornerIconClickAction-none", "None")]
        None,
        [Translation("cornerIconClickAction-settings", "Toggle Settingswindow")]
        Settings,
        [Translation("cornerIconClickAction-visibility", "Toggle Visibility")]
        Visibility
    }
}
