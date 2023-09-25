namespace Estreya.BlishHUD.ValuableItems.Extensions;

using Estreya.BlishHUD.ValuableItems.Utils;
using Estreya.BlishHUD.Shared.Extensions;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Tesseract;

public static class TesseractEngineExtensions
{
    public static Page ProcessScreenRegion(this TesseractEngine tesseractEngine, Rectangle region)
    {
        using var screenshot = OCRUtils.TakeScreenshot(region);
        using var stream = new MemoryStream();
        screenshot.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);

        screenshot.Save("C:\\temp\\ocr.png", System.Drawing.Imaging.ImageFormat.Png);

        using var pix = Pix.LoadFromMemory(stream.ToByteArray());

        return tesseractEngine.Process(pix);
    }
}
