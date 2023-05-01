namespace Estreya.BlishHUD.EventTable.Controls;

using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD._Extensions;
using Estreya.BlishHUD.Shared.Controls;
using Estreya.BlishHUD.Shared.Services;
using Estreya.BlishHUD.Shared.Utils;
using Estreya.BlishHUD.Shared.Extensions;
using Glide;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.BitmapFonts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Blish_HUD.Controls;

public class EventNotification : RenderTargetControl
{
    public const int NOTIFICATION_WIDTH = 350;
    public const int NOTIFICATION_HEIGHT = 96;
    private const int ICON_SIZE = 64;

    private static EventNotification _lastShown = null;

    private Models.Event _event;
    private readonly string _message;
    private IconService _iconService;
    private AsyncTexture2D _eventIcon;

    private int _x;
    private int _y;

    private static BitmapFont _titleFont = GameService.Content.DefaultFont18;
    private static BitmapFont _messageFont = GameService.Content.DefaultFont16;

    private static Rectangle _fullRect = new Rectangle(0, 0, NOTIFICATION_WIDTH, NOTIFICATION_HEIGHT);
    private static Rectangle _iconRect = new Rectangle(10, NOTIFICATION_HEIGHT / 2 - ICON_SIZE / 2, ICON_SIZE, ICON_SIZE);
    private static Rectangle _titleRect = new Rectangle(_iconRect.Right + 5, 10, _fullRect.Width - (_iconRect.Right + 5), _titleFont.LineHeight);
    private static Rectangle _messageRect = new Rectangle(_iconRect.Right + 5, _titleRect.Bottom, _fullRect.Width - (_iconRect.Right + 5), NOTIFICATION_HEIGHT - _titleRect.Height);

    public float BackgroundOpacity { get; set; } = 1f;

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

    public void Show(TimeSpan duration)
    {
        this.Location = new Microsoft.Xna.Framework.Point (this._x, _lastShown != null ? _lastShown.Bottom + 15 : this._y);
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
        spriteBatch.DrawString(_message, _messageFont, _messageRect, Color.White);
    }

    protected override CaptureType CapturesInput()
    {
        return CaptureType.None;
    }

    protected override void InternalDispose()
    {
        _event = null;
        _iconService = null;
        _eventIcon = null;
    }
}
