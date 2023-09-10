namespace Estreya.BlishHUD.Shared.Extensions
{
    using Microsoft.Xna.Framework.Graphics;
    using MonoGame.Extended.BitmapFonts;
    using MonoGame.Extended.TextureAtlases;
    using System;
    using System.Collections.Generic;
    using System.Text;

    public static class FontExtensions
    {

        /// <summary>
        ///     Converts the <see cref="SpriteFont"/> to a <see cref="BitmapFont"/>.
        /// </summary>
        /// <param name="spriteFont">The font to convert.</param>
        public static BitmapFont ToBitmapFont(this SpriteFont spriteFont)
        {
            var texture = spriteFont.Texture;

            var regions = new List<BitmapFontRegion>();

            var glyphs = spriteFont.GetGlyphs();

            foreach (var glyph in glyphs.Values)
            {
                var glyphTextureRegion = new TextureRegion2D(texture,
                    glyph.BoundsInTexture.Left,
                    glyph.BoundsInTexture.Top,
                    glyph.BoundsInTexture.Width,
                    glyph.BoundsInTexture.Height);

                var region = new BitmapFontRegion(glyphTextureRegion,
                    glyph.Character,
                    glyph.Cropping.Left,
                    glyph.Cropping.Top,
                    (int)glyph.WidthIncludingBearings);

                regions.Add(region);
            }

            return new BitmapFont(Guid.NewGuid().ToString(), regions, spriteFont.LineSpacing);
        }
    }
}
