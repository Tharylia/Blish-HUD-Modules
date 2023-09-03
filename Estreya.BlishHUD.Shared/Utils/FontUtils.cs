namespace Estreya.BlishHUD.Shared.Utils
{
    using Blish_HUD;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using SpriteFontPlus;
    using System.IO;

    public static class FontUtils
    {
        public static SpriteFont FromTrueTypeFont(byte[] ttfData, int fontSize, int bitmapWidth, int bitmapHeight)
        {
            TtfFontBakerResult fontBakeResult = TtfFontBaker.Bake(ttfData,
                fontSize,
                bitmapWidth,
                bitmapHeight,
                new[]
                {
                    SpriteFontPlus.CharacterRange.BasicLatin,
                    SpriteFontPlus.CharacterRange.Latin1Supplement,
                    SpriteFontPlus.CharacterRange.LatinExtendedA,
                    SpriteFontPlus.CharacterRange.LatinExtendedB,
                }
            );

            using Blish_HUD.Graphics.GraphicsDeviceContext ctx = GameService.Graphics.LendGraphicsDeviceContext();
            SpriteFont font = fontBakeResult.CreateSpriteFont(ctx.GraphicsDevice);

            return font;
        }

        public static SpriteFont FromBMFont(string fontPath)
        {
            string fontDirectory = Path.GetFullPath(fontPath);
            using Stream stream = TitleContainer.OpenStream(fontPath);
            using StreamReader reader = new StreamReader(stream);
            string fontData = reader.ReadToEnd();

            using Blish_HUD.Graphics.GraphicsDeviceContext ctx = GameService.Graphics.LendGraphicsDeviceContext();
            return BMFontLoader.Load(fontData, name => TitleContainer.OpenStream(Path.Combine(fontDirectory, name)), ctx.GraphicsDevice);
        }
    }
}
