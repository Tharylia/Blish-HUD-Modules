namespace Estreya.BlishHUD.Shared.Controls
{
    using Blish_HUD.Content;
    using Estreya.BlishHUD.Shared.Settings;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    public class StandardWindow : Window
    {
        public StandardWindow(BaseModuleSettings baseModuleSettings, AsyncTexture2D background, Rectangle windowRegion, Rectangle contentRegion) : base(baseModuleSettings)
        {
            this.ConstructWindow(background, windowRegion, contentRegion);
        }

        public StandardWindow(BaseModuleSettings baseModuleSettings, Texture2D background, Rectangle windowRegion, Rectangle contentRegion)
            : this(baseModuleSettings, (AsyncTexture2D)background, windowRegion, contentRegion)
        {
        }

        public StandardWindow(BaseModuleSettings baseModuleSettings, AsyncTexture2D background, Rectangle windowRegion, Rectangle contentRegion, Point windowSize) : base(baseModuleSettings)
        {
            this.ConstructWindow(background, windowRegion, contentRegion, windowSize);
        }

        public StandardWindow(BaseModuleSettings baseModuleSettings, Texture2D background, Rectangle windowRegion, Rectangle contentRegion, Point windowSize)
            : this(baseModuleSettings, (AsyncTexture2D)background, windowRegion, contentRegion, windowSize)
        {
        }
    }
}
