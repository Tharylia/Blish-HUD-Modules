namespace Estreya.BlishHUD.EventTable.UI.Views
{
    using Blish_HUD.Controls;
    using Blish_HUD.Graphics.UI;

    public class EmptySettingsLineView : View
    {
        private int Height { get; set; }
        public EmptySettingsLineView(int height)
        {
            this.Height = height;
        }

        protected override void Build(Container buildPanel)
        {
            new Panel()
            {
                Parent = buildPanel,
                Height = this.Height,
            };
        }
    }
}
