namespace Estreya.BlishHUD.Shared.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Text;

    public enum CornerIconRightClickAction
    {
        None,
        [Description("Toggle Settingswindow")]
        Settings,
        [Description("Toggle Visibility")]
        Visibility
    }
}
