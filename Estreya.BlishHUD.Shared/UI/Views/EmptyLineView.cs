namespace Estreya.BlishHUD.Shared.UI.Views
{
    using Blish_HUD.Controls;
    using Blish_HUD.Graphics.UI;

    public class EmptyLineView : View
    {
        private int Height { get; set; }
        public EmptyLineView(int height)
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
