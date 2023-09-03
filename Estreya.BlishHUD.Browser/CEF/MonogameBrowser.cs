namespace Estreya.BlishHUD.Browser.CEF;
using CefSharp;
using CefSharp.Internals;
using CefSharp.OffScreen;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;

public class MonogameBrowser : ChromiumWebBrowser
{
    private readonly GraphicsDevice Graphics;

    Stopwatch TotalTime = new Stopwatch();
    byte[] ImageData;
    private Texture2D _texture;

    public event EventHandler<NewFrameEventArgs> NewFrame;
    public MonogameBrowser(GraphicsDevice gd, string address, BrowserSettings browserSettings, RequestContext requestContext) : base(address, browserSettings, requestContext, false)
    {
        this.Graphics = gd;
        this.Paint += this.MonoCefBrowser_Paint;
    }

    private void MonoCefBrowser_Paint(object sender, OnPaintEventArgs e)
    {
        if (e.DirtyRect.Width == 0 || e.DirtyRect.Height == 0)
        {
            Debug.WriteLine("Failed render because size is 0");
        }

        this.TotalTime.Start();

        Bitmap bmp = this.ScreenshotOrNull(PopupBlending.Main);
        if (bmp != null)
        {
            var texture = this.GetTexture(bmp, e);
            if (texture != null)
            {
                this.NewFrame?.Invoke(this, new NewFrameEventArgs(texture));
            }
            else
            {
                Debug.WriteLine("Failed render because texture null");
            }
            //Console.WriteLine($"{TotalTime.ElapsedMilliseconds / (double)RenderCount}");
        }
        else
        {
            Debug.WriteLine("Failed render because screenshot null");
        }

        this.TotalTime.Stop();

        bmp?.Dispose();
    }

    private Texture2D GetTexture(Bitmap bmp, OnPaintEventArgs args)
    {
        if (bmp.Size.Width != args.Width || bmp.Size.Height != args.Height) return null;

        if (this.ImageData == null || bmp.Width != this._texture.Width || bmp.Height != this._texture.Height)
        {
            this.ImageData = new byte[bmp.Width * bmp.Height * 4];
            this._texture = new Texture2D(this.Graphics, bmp.Width, bmp.Height);
        }

        unsafe
        {
            BitmapData origdata = bmp.LockBits(new Rectangle(args.DirtyRect.X, args.DirtyRect.Y, args.DirtyRect.Width, args.DirtyRect.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            int absStride = Math.Abs(origdata.Stride);

            for (int i = 0; i < args.DirtyRect.Height; i++)
            {
                IntPtr pointer = new IntPtr(origdata.Scan0.ToInt64() + (origdata.Stride * i));
                int y = i + args.DirtyRect.Y;
                int x = args.DirtyRect.X;
                System.Runtime.InteropServices.Marshal.Copy(pointer, this.ImageData, (x * 4) + (bmp.Width * 4 * y), absStride);
            }

            bmp.UnlockBits(origdata);
        }

        this._texture.SetData(this.ImageData);

        return this._texture;
    }
}

public class NewFrameEventArgs : EventArgs
{
    public readonly Texture2D Frame;
    public NewFrameEventArgs(Texture2D frame)
    {
        this.Frame = frame;
    }
}