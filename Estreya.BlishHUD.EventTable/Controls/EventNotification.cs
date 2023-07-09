namespace Estreya.BlishHUD.EventTable.Controls;

using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.BitmapFonts;
using Shared.Controls;
using Shared.Services;
using Shared.Utils;
using System;

public class EventNotification : RenderTarget2DControl
{
    public const int NOTIFICATION_WIDTH = 350;
    public const int NOTIFICATION_HEIGHT = 96;
    private const int ICON_SIZE = 64;

    private static EventNotification _lastShown;

    private static readonly BitmapFont _titleFont = GameService.Content.DefaultFont18;
    private static readonly BitmapFont _messageFont = GameService.Content.DefaultFont16;

    private static readonly Rectangle _fullRect = new Rectangle(0, 0, NOTIFICATION_WIDTH, NOTIFICATION_HEIGHT);
    private static readonly Rectangle _iconRect = new Rectangle(10, (NOTIFICATION_HEIGHT / 2) - (ICON_SIZE / 2), ICON_SIZE, ICON_SIZE);
    private static readonly Rectangle _titleRect = new Rectangle(_iconRect.Right + 5, 10, _fullRect.Width - (_iconRect.Right + 5), _titleFont.LineHeight);
    private static readonly Rectangle _messageRect = new Rectangle(_iconRect.Right + 5, _titleRect.Bottom, _fullRect.Width - (_iconRect.Right + 5), NOTIFICATION_HEIGHT - _titleRect.Height);
    private readonly string _message;

    private Models.Event _event;
    private AsyncTexture2D _eventIcon;
    private IconService _iconService;

    private readonly int _x;
    private readonly int _y;

    public EventNotification(Models.Event ev, string message, int x, int y, IconService iconService)
    {
        this._event = ev;
        this._message = message;
        this._x = x;
        this._y = y;
        this._iconService = iconService;

        this._eventIcon = this._iconService?.GetIcon(ev.Icon);

        this.Width = NOTIFICATION_WIDTH;
        this.Height = NOTIFICATION_HEIGHT;
        this.Visible = false;
        this.Opacity = 0f;
        this.Parent = GameService.Graphics.SpriteScreen;
    }

    public float BackgroundOpacity { get; set; } = 1f;

    public void Show(TimeSpan duration)
    {
        this.Location = new Point(this._x, _lastShown != null ? _lastShown.Bottom + 15 : this._y);
        base.Show();
        _lastShown = this;

        _ = GameService.Animation.Tweener.Tween(this, new { Opacity = 1f }, 0.2f)
                       .Repeat(1)
                       .RepeatDelay((float)duration.TotalSeconds)
                       .Reflect()
                       .OnComplete(() =>
                       {
                           this.Hide();
                       });
    }

    public new void Hide()
    {
        base.Hide();

        _ = GameService.Animation.Tweener.Tween(this, new { Opacity = 0f }, 0.4f)
                       .OnComplete(() =>
                       {
                           this.Dispose();
                           if (_lastShown == this)
                           {
                               _lastShown = null;
                           }
                       });
    }

    protected override void DoPaint(SpriteBatch spriteBatch, Rectangle bounds)
    {
        spriteBatch.Draw(ContentService.Textures.Pixel, _fullRect, Color.Black * this.BackgroundOpacity);

        if (this._eventIcon != null && this._eventIcon.HasSwapped)
        {
            spriteBatch.Draw(this._eventIcon, _iconRect, Color.White);
        }

        spriteBatch.DrawString(this._event.Name, _titleFont, _titleRect, Color.White);
        spriteBatch.DrawString(this._message, _messageFont, _messageRect, Color.White);
    }

    protected override CaptureType CapturesInput()
    {
        return CaptureType.None;
    }

    protected override void InternalDispose()
    {
        this._event = null;
        this._iconService = null;
        this._eventIcon = null;
    }
}