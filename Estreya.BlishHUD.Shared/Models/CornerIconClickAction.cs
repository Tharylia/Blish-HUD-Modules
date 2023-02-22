namespace Estreya.BlishHUD.Shared.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Text;

    public enum CornerIconClickAction
    {
        None,
        [Description("Toggle Settingswindow")]
        Settings,
        [Description("Toggle Visibility")]
        Visibility
    }
}
