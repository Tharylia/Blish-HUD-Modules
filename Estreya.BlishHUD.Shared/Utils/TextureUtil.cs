namespace Estreya.BlishHUD.Shared.Utils
{
    using Microsoft.Xna.Framework.Graphics;
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Text;

    public static class TextureUtil
    {
        public static Image ToImage(this Texture2D texture)
        {
            Image img;
            using (MemoryStream ms = new MemoryStream())
            {
                texture.SaveAsPng(ms, texture.Width, texture.Height);
                //Go To the  beginning of the stream.
                ms.Seek(0, SeekOrigin.Begin);
                //Create the image based on the stream.
                img = Bitmap.FromStream(ms);
            }
            return img;
        }
    }
}
