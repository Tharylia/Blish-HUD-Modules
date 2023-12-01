namespace Estreya.BlishHUD.Browser.CEF;
using CefSharp.Internals;
using CefSharp.OffScreen;
using CefSharp;
using Microsoft.Xna.Framework.Graphics;

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using Estreya.BlishHUD.Shared.Helpers;

public class OffscreenBrowserRenderer : IDisposable
{
    public const string BaseUrl = "custom://cefsharp";
    public const string DefaultUrl = BaseUrl + "/index.html";
    private const bool DebuggingSubProcess = false;

    private MonogameBrowser _monogameBrowser;
    public Texture2D CurrentFrame;

    public event Action<object> DataChanged;
    public event EventHandler<AddressChangedEventArgs> AddressChanged
    {
        add { this._monogameBrowser.AddressChanged += value; }
        remove { this._monogameBrowser.AddressChanged -= value; }
    }

    public static void Shutdown()
    {
        Cef.Shutdown();
    }

    public static void Init(string cefRootPath, string gameCefRootPath, string cachePath, bool multiThreadedMessageLoop = true, IBrowserProcessHandler browserProcessHandler = null)
    {
        browserProcessHandler ??= new BrowserProcessHandler();
        CefSharpSettings.ShutdownOnExit = true;
        CefSharpSettings.FocusedNodeChangedEnabled = true;

        // Set Google API keys, used for Geolocation requests sans GPS.  See http://www.chromium.org/developers/how-tos/api-keys
        // Environment.SetEnvironmentVariable("GOOGLE_API_KEY", "");
        // Environment.SetEnvironmentVariable("GOOGLE_DEFAULT_CLIENT_ID", "");
        // Environment.SetEnvironmentVariable("GOOGLE_DEFAULT_CLIENT_SECRET", "");

        // Widevine CDM registration - pass in directory where Widevine CDM binaries and manifest.json are located.
        // For more information on support for DRM content with Widevine see: https://github.com/cefsharp/CefSharp/issues/1934
        //Cef.RegisterWidevineCdm(@".\WidevineCdm");

        //Chromium Command Line args
        //http://peter.sh/experiments/chromium-command-line-switches/
        //NOTE: Not all relevant in relation to `CefSharp`, use for reference purposes only.

        CefSettings settings = new CefSettings
        {
            LocalesDirPath = Path.Combine(cefRootPath, "locales"),
            ResourcesDirPath = cefRootPath.TrimEnd('\\'),
            //settings.ResourcesDirPath = cefRootPath;
            RemoteDebuggingPort = 8088,
            CachePath = cachePath,
        };

        var result = CefSharp.DependencyChecker.CheckDependencies(false, false, cefRootPath.TrimEnd('\\'), cefRootPath.TrimEnd('\\'), Path.Combine(cefRootPath, "CefSharp.BrowserSubprocess.exe"));

        settings.CefCommandLineArgs.Add("transparent-painting-enabled", "1");
        //settings.UserAgent = "CefSharp Browser" + Cef.CefSharpVersion; // Example User Agent
        //settings.CefCommandLineArgs.Add("renderer-process-limit", "1");
        //settings.CefCommandLineArgs.Add("renderer-startup-dialog", "1");
        //settings.CefCommandLineArgs.Add("enable-media-stream", "1"); //Enable WebRTC
        //settings.CefCommandLineArgs.Add("no-proxy-server", "1"); //Don't use a proxy server, always make direct connections. Overrides any other proxy server flags that are passed.
        //settings.CefCommandLineArgs.Add("debug-plugin-loading", "1"); //Dumps extra logging about plugin loading to the log file.
        //settings.CefCommandLineArgs.Add("disable-plugins-discovery", "1"); //Disable discovering third-party plugins. Effectively loading only ones shipped with the browser plus third-party ones as specified by --extra-plugin-dir and --load-plugin switches
        //settings.CefCommandLineArgs.Add("enable-system-flash", "1"); //Automatically discovered and load a system-wide installation of Pepper Flash.
        //settings.CefCommandLineArgs.Add("allow-running-insecure-content", "1"); //By default, an https page cannot run JavaScript, CSS or plugins from http URLs. This provides an override to get the old insecure behavior. Only available in 47 and above.

        //settings.CefCommandLineArgs.Add("enable-logging", "1"); //Enable Logging for the Renderer process (will open with a cmd prompt and output debug messages - use in conjunction with setting LogSeverity = LogSeverity.Verbose;)
        //settings.LogSeverity = LogSeverity.Verbose; // Needed for enable-logging to output messages

        //settings.CefCommandLineArgs.Add("disable-extensions", "1"); //Extension support can be disabled
        //settings.CefCommandLineArgs.Add("disable-pdf-extension", "1"); //The PDF extension specifically can be disabled

        //NOTE: For OSR best performance you should run with GPU disabled:
        // `--disable-gpu --disable-gpu-compositing --enable-begin-frame-scheduling`
        // (you'll loose WebGL support but gain increased FPS and reduced CPU usage).
        // http://magpcss.org/ceforum/viewtopic.php?f=6&t=13271#p27075
        //https://bitbucket.org/chromiumembedded/cef/commits/e3c1d8632eb43c1c2793d71639f3f5695696a5e8

        //NOTE: The following function will set all three params
        settings.SetOffScreenRenderingBestPerformanceArgs();
        //settings.CefCommandLineArgs.Add("disable-gpu", "1");
        //settings.CefCommandLineArgs.Add("disable-gpu-compositing", "1");
        //settings.CefCommandLineArgs.Add("enable-begin-frame-scheduling", "1");

        //settings.CefCommandLineArgs.Add("disable-gpu-vsync", "1"); //Disable Vsync

        //Disables the DirectWrite font rendering system on windows.
        //Possibly useful when experiencing blury fonts.
        //settings.CefCommandLineArgs.Add("disable-direct-write", "1");

        settings.MultiThreadedMessageLoop = multiThreadedMessageLoop;
        settings.ExternalMessagePump = !multiThreadedMessageLoop;

        // Off Screen rendering (WPF/Offscreen)
        settings.WindowlessRenderingEnabled = true;

        //Disable Direct Composition to test https://github.com/cefsharp/CefSharp/issues/1634
        //settings.CefCommandLineArgs.Add("disable-direct-composition", "1");

        // DevTools doesn't seem to be working when this is enabled
        // http://magpcss.org/ceforum/viewtopic.php?f=6&t=14095
        //settings.CefCommandLineArgs.Add("enable-begin-frame-scheduling", "1");

        InternetProxyInfo proxy = ProxyConfig.GetProxyInformation();
        switch (proxy.AccessType)
        {
            case InternetOpenType.Direct:
                {
                    //Don't use a proxy server, always make direct connections.
                    settings.CefCommandLineArgs.Add("no-proxy-server", "1");
                    break;
                }
            case InternetOpenType.Proxy:
                {
                    settings.CefCommandLineArgs.Add("proxy-server", proxy.ProxyAddress);
                    break;
                }
            case InternetOpenType.PreConfig:
                {
                    settings.CefCommandLineArgs.Add("proxy-auto-detect", "1");
                    break;
                }
        }

        //settings.LogSeverity = LogSeverity.Verbose;

        if (DebuggingSubProcess)
        {
            string architecture = Environment.Is64BitProcess ? "x64" : "x86";
            settings.BrowserSubprocessPath = "..\\..\\..\\..\\CefSharp.BrowserSubprocess\\bin\\" + architecture + "\\Debug\\CefSharp.BrowserSubprocess.exe";
        }

        settings.RegisterScheme(new CefCustomScheme
        {
            SchemeName = SchemeHandlerFactory.SchemeName,
            SchemeHandlerFactory = new SchemeHandlerFactory(),
            IsSecure = true //treated with the same security rules as those applied to "https" URLs
        });

        if (!Cef.Initialize(settings, performDependencyCheck: !DebuggingSubProcess, browserProcessHandler: browserProcessHandler))
        {
            throw new Exception("Unable to Initialize Cef");
        }

        Cef.AddCrossOriginWhitelistEntry(BaseUrl, "https", "cefsharp.com", false);

        //Experimental option where bound async methods are queued on TaskScheduler.Default.
        //CefSharpSettings.ConcurrentTaskExecution = true;
    }

    public void Resize(System.Drawing.Size size)
    {
        this._monogameBrowser.Size = size;
    }

    public void SetMarshalledData(object data)
    {
        this._monogameBrowser.ExecuteScriptAsync("pushData", Newtonsoft.Json.JsonConvert.SerializeObject(data));
    }
    public async Task<T> GetMarshalledData<T>()
    {
        JavascriptResponse response = await this._monogameBrowser.EvaluateScriptAsync("pullData()");
        try
        {
            return JsonConvert.DeserializeObject<T>(response.Result.ToString());
        }
        catch
        {
            return default(T);
        }
    }

    int ChangeCount = 0;
    public async Task<bool> CheckChanged()
    {
        JavascriptResponse response = await this._monogameBrowser.EvaluateScriptAsync("changeCount()");
        int newChangeCount = (int)response.Result;
        bool different = newChangeCount != this.ChangeCount;
        this.ChangeCount = newChangeCount;
        return different;
    }

    public void PullLatestDataIfChanged<T>()
    {
        this.CheckChanged().ContinueWith(task =>
        {
            if (task.Result)
            {
                this.GetMarshalledData<T>().ContinueWith(task2 =>
                {
                    if (task2.Result != null)
                    {
                        this.DataChanged?.Invoke(task2.Result);
                    }
                });
            }
        });
    }

    public async Task MainAsync(GraphicsDevice gd, IntPtr windowHandle, string url, object data, System.Drawing.Size size, double zoomLevel = 1.0)
    {
        if (this._monogameBrowser != null)
        {
            this._monogameBrowser.NewFrame -= this.Browser_NewFrame;
            this._monogameBrowser.Dispose();
        }

        BrowserSettings browserSettings = new BrowserSettings
        {
            //Reduce rendering speed to one frame per second so it's easier to take screen shots
            WindowlessFrameRate = 30,
        };
        RequestContextSettings requestContextSettings = new RequestContextSettings { };

        // RequestContext can be shared between browser instances and allows for custom settings
        // e.g. CachePath
       var requestContext = new RequestContext(requestContextSettings);
        this._monogameBrowser = new MonogameBrowser(gd, url, browserSettings, requestContext);
        await this._monogameBrowser.CreateBrowserAsync(new WindowInfo()
        {
            WindowHandle = windowHandle,
            Width = size.Width,
            Height = size.Height,
            WindowlessRenderingEnabled = true,
        }, browserSettings);
        this._monogameBrowser.NewFrame += this.Browser_NewFrame;
        this._monogameBrowser.Size = size;
        if (zoomLevel > 1)
        {
            this._monogameBrowser.FrameLoadStart += (s, argsi) =>
            {
                ChromiumWebBrowser b = (ChromiumWebBrowser)s;
                if (argsi.Frame.IsMain)
                {
                    b.SetZoomLevel(zoomLevel);
                }
            };
        }

        await this.LoadPageAsync(this._monogameBrowser);

        //Check preferences on the CEF UI Thread
        await Cef.UIThreadTaskFactory.StartNew(delegate
        {
            IDictionary<string, object> preferences = requestContext.GetAllPreferences(true);

            //Check do not track status
            bool doNotTrack = (bool)preferences["enable_do_not_track"];

            Debug.WriteLine("DoNotTrack:" + doNotTrack);
        });

        bool onUi = Cef.CurrentlyOnThread(CefThreadIds.TID_UI);

        await this.LoadPageAsync(this._monogameBrowser, url);

        //Gets a wrapper around the underlying CefBrowser instance
        IBrowser cefBrowser = this._monogameBrowser.GetBrowser();
        // Gets a warpper around the CefBrowserHost instance
        // You can perform a lot of low level browser operations using this interface
        IBrowserHost cefHost = cefBrowser.GetHost();
        cefHost.SendFocusEvent(true);

        this.SetMarshalledData(data);

        //You can call Invalidate to redraw/refresh the image
        cefHost.Invalidate(PaintElementType.View);
    }

    private void Browser_NewFrame(object sender, NewFrameEventArgs e)
    {
        this.CurrentFrame = e.Frame;
    }

    public Task LoadPageAsync(IWebBrowser browser, string address = null)
    {
        //If using .Net 4.6 then use TaskCreationOptions.RunContinuationsAsynchronously
        //and switch to tcs.TrySetResult below - no need for the custom extension method
        TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

        EventHandler<LoadingStateChangedEventArgs> handler = null;
        handler = (sender, args) =>
        {
            //Wait for while page to finish loading not just the first frame
            if (!args.IsLoading)
            {
                browser.LoadingStateChanged -= handler;
                //This is required when using a standard TaskCompletionSource
                //Extension method found in the CefSharp.Internals namespace
                tcs.TrySetResultAsync(true);
            }

            browser.GetBrowser().GetHost().Invalidate(PaintElementType.View);
        };

        browser.LoadingStateChanged += handler;

        if (!string.IsNullOrEmpty(address))
        {
            browser.Load(address);
        }

        return tcs.Task;
    }

    public void HandleMouseMove(int x, int y)
    {
        this._monogameBrowser?.GetBrowser().GetHost().SendMouseMoveEvent(x, y, false, CefEventFlags.None);
    }
    public void HandleMouseDown(int x, int y, MouseButtonType type)
    {
        if (this._monogameBrowser != null)
        {
            this._monogameBrowser.GetBrowser().GetHost().SendMouseClickEvent(x, y, type, false, 1, CefEventFlags.None);
            this._monogameBrowser.GetBrowser().GetHost().Invalidate(PaintElementType.View);
        }
    }
    public void HandleMouseUp(int x, int y, MouseButtonType type)
    {
        if (this._monogameBrowser != null)
        {
            this._monogameBrowser.GetBrowser().GetHost().SendMouseClickEvent(x, y, type, true, 1, CefEventFlags.None);
            this._monogameBrowser.GetBrowser().GetHost().Invalidate(PaintElementType.View);
        }
    }
    public void HandleKeyEvent(KeyEvent k)
    {
        if (this._monogameBrowser != null)
        {
            this._monogameBrowser.GetBrowser().GetHost().SendKeyEvent(k);
            this._monogameBrowser.GetBrowser().GetHost().Invalidate(PaintElementType.View);
        }
    }

    public void HandleSelectAll()
    {
        this._monogameBrowser?.SelectAll();
    }

    public async Task<LoadUrlAsyncResponse> HandleAddressChange(string newAddress)
    {
        return  await this._monogameBrowser?.LoadUrlAsync(newAddress);
    }

    public string GetCurrentAddress()
    {
        return this._monogameBrowser.Address;
    }

    public void HandleMouseWheel(MouseEvent mouseEvent, int scrollDistance)
    {
        if (this._monogameBrowser != null)
        {
            this._monogameBrowser.GetBrowser().GetHost().SendMouseWheelEvent(mouseEvent, 0, scrollDistance);
            this._monogameBrowser.GetBrowser().GetHost().Invalidate(PaintElementType.View);
        }
    }

    public void HandleKeyEvents(KeyEvent[] ke)
    {
        if (this._monogameBrowser != null)
        {
            foreach (KeyEvent k in ke)
            {
                this._monogameBrowser.GetBrowser().GetHost().SendKeyEvent(k);
            }

            this._monogameBrowser.GetBrowser().GetHost().Invalidate(PaintElementType.View);
        }
    }

    public void HandleBackNavigation()
    {
        if (!this._monogameBrowser?.CanGoBack ?? true) return;

        this._monogameBrowser?.Back();
    }

    public void HandleForwardNavigation()
    {
        if (!this._monogameBrowser?.CanGoForward ?? true) return;

        this._monogameBrowser?.Forward();
    }

    public void Dispose()
    {
        this._monogameBrowser?.Dispose();
    }
}

