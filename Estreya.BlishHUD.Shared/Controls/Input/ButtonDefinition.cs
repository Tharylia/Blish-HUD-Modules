namespace Estreya.BlishHUD.Shared.Controls.Input
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Windows.Forms;

    public class ButtonDefinition
    {
        public ButtonDefinition(string title, DialogResult result)
        {
            this.Title = title;
            this.Result = result;
        }

        public string Title { get; set; }
        public DialogResult Result { get; set; }
    }
}
