namespace Estreya.BlishHUD.Browser.Controls;

using Blish_HUD;
using Blish_HUD.Controls;
using CefSharp;
using Estreya.BlishHUD.Browser.CEF;
using Estreya.BlishHUD.Shared.Helpers;
using Flurl.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public class BrowserControl : Control
{
    private OffscreenBrowserRenderer BrowserRenderer;
    private MouseState LastMouseState;

    public bool Focused { get; private set; }

    public event EventHandler<AddressChangedEventArgs> AddressChanged
    {
        add { this.BrowserRenderer.AddressChanged += value; }
        remove { this.BrowserRenderer.AddressChanged -= value; }
    }
    public BrowserControl(string homepage)
    {
        this.BrowserRenderer = new OffscreenBrowserRenderer();

        AsyncHelper.RunSync(() =>
        {
            using Blish_HUD.Graphics.GraphicsDeviceContext ctx = GameService.Graphics.LendGraphicsDeviceContext();
            int height = Math.Min(GameService.Graphics.WindowHeight, this.Height);
            int width = Math.Min(GameService.Graphics.WindowWidth, this.Width);
            return this.BrowserRenderer.MainAsync(ctx.GraphicsDevice, GameService.GameIntegration.Gw2Instance.Gw2WindowHandle, homepage, null, new System.Drawing.Size(width, height));
        });

        GameService.Input.Mouse.LeftMouseButtonPressed += this.Global_LeftMouseButtonPressed;
        GameService.Input.Mouse.LeftMouseButtonReleased += this.Global_LeftMouseButtonReleased;
        this.MouseWheelScrolled += this.BrowserControl_MouseWheelScrolled;
        GameService.Input.Keyboard.KeyPressed += this.Keyboard_KeyPressed;
        GameService.Input.Keyboard.KeyReleased += this.Keyboard_KeyReleased;
    }

    private void Keyboard_KeyReleased(object sender, Blish_HUD.Input.KeyboardEventArgs e)
    {
        if (!this.Focused) return;

        KeyEvent ke = new KeyEvent() { Type = KeyEventType.KeyUp, NativeKeyCode = (int)e.Key, WindowsKeyCode = (int)e.Key };
        this.BrowserRenderer.HandleKeyEvent(ke);
    }

    private void Keyboard_KeyPressed(object sender, Blish_HUD.Input.KeyboardEventArgs e)
    {
        if (!this.Focused) return;

        if (e.Key is Keys.LeftControl or Keys.LeftAlt or Keys.LeftShift or Keys.LeftWindows or Keys.RightControl or Keys.RightAlt or Keys.RightShift or Keys.RightWindows) return;

        if (e.Key == Keys.A && GameService.Input.Keyboard.ActiveModifiers.HasFlag(ModifierKeys.Ctrl))
        {
            // Select all
            this.BrowserRenderer.HandleSelectAll();
        }
        else
        {
            // Normal presses
            int code = (int)e.Key;
            if (code >= 65 && code <= 90 && !GameService.Input.Keyboard.ActiveModifiers.HasFlag(ModifierKeys.Shift))
            {
                code += 32;
            };

            KeyEvent ke = new KeyEvent() { Type = KeyEventType.KeyDown, NativeKeyCode = code, WindowsKeyCode = code };
            this.BrowserRenderer.HandleKeyEvent(ke);
            KeyEvent ke2 = new KeyEvent() { Type = KeyEventType.Char, NativeKeyCode = code, WindowsKeyCode = code };
            this.BrowserRenderer.HandleKeyEvent(ke2);
        }
    }

    private void BrowserControl_MouseWheelScrolled(object sender, Blish_HUD.Input.MouseEventArgs e)
    {
        if (!this.Focused) return;
        this.BrowserRenderer.HandleMouseWheel(new MouseEvent(this.RelativeMousePosition.X, this.RelativeMousePosition.Y, CefEventFlags.None), e.MouseState.ScrollWheelValue);
    }

    private void Global_LeftMouseButtonReleased(object sender, Blish_HUD.Input.MouseEventArgs e)
    {
        if (!this.Focused) return;

        this.BrowserRenderer.HandleMouseUp(this.RelativeMousePosition.X, this.RelativeMousePosition.Y, MouseButtonType.Left);
    }

    private void Global_LeftMouseButtonPressed(object sender, Blish_HUD.Input.MouseEventArgs e)
    {
        var focused = this.MouseOver && this.Enabled;

        this.UpdateFocusState(focused);

        if (!this.Focused) return;

        this.BrowserRenderer.HandleMouseDown(this.RelativeMousePosition.X, this.RelativeMousePosition.Y, MouseButtonType.Left);
    }

    protected override CaptureType CapturesInput()
    {
        return CaptureType.Mouse | CaptureType.MouseWheel;
    }

    private void UpdateFocusState(bool focused)
    {
        this.Focused = focused;

        if (this.Focused)
        {
            GameService.Input.Keyboard.SetTextInputListner(this.OnTextInput);
        }
        else
        {
            GameService.Input.Keyboard.UnsetTextInputListner(this.OnTextInput);
        }
    }

    private void OnTextInput(string value)
    {

    }

    public override void DoUpdate(GameTime gameTime)
    {
        int height = Math.Min(GameService.Graphics.WindowHeight, this.Height);
        int width = Math.Min(GameService.Graphics.WindowWidth, this.Width);
        this.BrowserRenderer.Resize(new System.Drawing.Size(width, height));
    }

    protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds)
    {
        if (this.BrowserRenderer.CurrentFrame != null)
        {
            spriteBatch.DrawOnCtrl(this, this.BrowserRenderer.CurrentFrame, new Rectangle(0, 0, this.Width, this.Height), Color.White);// new Color(Color.White, 0.5f));
        }
    }

    public void HandleBackNavigation()
    {
        this.BrowserRenderer.HandleBackNavigation();
    }

    public void HandleForwardNavigation()
    {
        this.BrowserRenderer.HandleForwardNavigation();
    }

    public async Task<LoadUrlAsyncResponse> HandleAddressChange(string newAddress)
    {
       return await this.BrowserRenderer.HandleAddressChange(newAddress);
    }

    public string GetCurrentAddress()
    {
        return this.BrowserRenderer.GetCurrentAddress();
    }

    protected override void DisposeControl()
    {
        GameService.Input.Mouse.LeftMouseButtonPressed -= this.Global_LeftMouseButtonPressed;
        GameService.Input.Mouse.LeftMouseButtonReleased -= this.Global_LeftMouseButtonReleased;
        this.MouseWheelScrolled -= this.BrowserControl_MouseWheelScrolled;
        GameService.Input.Keyboard.KeyPressed -= this.Keyboard_KeyPressed;
        GameService.Input.Keyboard.KeyReleased -= this.Keyboard_KeyReleased;

        this.BrowserRenderer?.Dispose();
    }
}
