namespace Estreya.BlishHUD.ValuableItems.Utils;

using Blish_HUD;
using Estreya.BlishHUD.Shared.Utils;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

public static class OCRUtils
{
    public static Rectangle GetBlishWindowRectangle()
    {
        //var blishGameInstance = typeof(BlishHud).GetField("Instance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).GetValue(null);

        //var blishHudForm = typeof(BlishHud).GetProperty("Form").GetValue(blishGameInstance) as Form;

        //return new Rectangle(blishHudForm.Location.X, blishHudForm.Location.Y, blishHudForm.Size.Width, blishHudForm.Size.Height);

        WindowsUtil.RECT rect = new WindowsUtil.RECT();
        WindowsUtil.GetWindowRect(GameService.GameIntegration.Gw2Instance.Gw2WindowHandle, ref rect);

        return new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
    }

    public static System.Drawing.Bitmap TakeScreenshot(Rectangle rect)
    {
        var bmp = new System.Drawing.Bitmap(rect.Width, rect.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bmp);
        g.CopyFromScreen(new System.Drawing.Point(rect.Left, rect.Top), System.Drawing.Point.Empty, new System.Drawing.Size(rect.Width, rect.Height), System.Drawing.CopyPixelOperation.SourceCopy);
        return bmp;
    }
}
