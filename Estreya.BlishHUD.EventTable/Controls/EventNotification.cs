namespace Estreya.BlishHUD.EventTable.Controls;

using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Estreya.BlishHUD.EventTable.Models.Reminders;
using Glide;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.BitmapFonts;
using Shared.Controls;
using Shared.Services;
using Shared.Utils;
using System;
using static Blish_HUD.ContentService;

public class EventNotification : RenderTarget2DControl
{
    private static EventNotification _lastShown;

    private readonly BitmapFont _titleFont;
    private readonly BitmapFont _messageFont;

    private Rectangle _fullRect;
    private Rectangle _iconRect;
    private Rectangle _titleRect;
    private Rectangle _messageRect;
    private readonly string _title;
    private readonly string _message;

    public Models.Event Model { get; private set; }
    private AsyncTexture2D _icon;
    private IconService _iconService;
    private readonly bool _captureMouseClicks;
    private readonly int _x;
    private readonly int _y;
    private readonly int _iconSize;
    private readonly EventReminderStackDirection _stackDirection;
    private Tween _showAnimation;

    public EventNotification(Models.Event ev, string title, string message, AsyncTexture2D icon, int x, int y, int width, int height,int iconSize, EventReminderStackDirection stackDirection, FontSize titleFontSize, FontSize messageFontSize , IconService iconService, bool captureMouseClicks = false)
    {
        this.Model = ev;
        this._title = title;
        this._message = message;
        this._x = x;
        this._y = y;
        this._iconSize = iconSize;
        this._stackDirection = stackDirection;
        this._iconService = iconService;
        this._captureMouseClicks = captureMouseClicks;

        if (icon != null)
        {
            this._icon = icon;
        }
        else if (ev?.Icon != null)
        {
            this._icon = ev?.Icon != null ? this._iconService?.GetIcon(ev.Icon) : null;
        }

        this._titleFont = GameService.Content.GetFont(FontFace.Menomonia, titleFontSize, FontStyle.Regular);
        this._messageFont = GameService.Content.GetFont(FontFace.Menomonia, messageFontSize, FontStyle.Regular);

        this.Width = width;
        this.Height = height;
        this.Visible = false;
        this.Opacity = 0f;
        this.Parent = GameService.Graphics.SpriteScreen;

        if (this._iconSize > this.Height) throw new ArgumentOutOfRangeException(nameof(iconSize), "The icon size can't be higher than the total height.");
    }

    public EventNotification(Models.Event ev, string message, int x, int y, int width, int height,int iconSize, EventReminderStackDirection stackDirection, FontSize titleFontSize, FontSize messageFontSize, IconService iconService, bool captureMouseClicks = false) :
        this(ev, ev?.Name, message, null, x, y,width,height,iconSize, stackDirection, titleFontSize, messageFontSize, iconService, captureMouseClicks)
    { }

    public float BackgroundOpacity { get; set; } = 1f;

    public override void RecalculateLayout()
    {
        this._fullRect = new Rectangle(0, 0, this.Width, this.Height);
        this._iconRect = new Rectangle(10, (this.Height / 2) - (this._iconSize/ 2), this._iconSize, this._iconSize);
        this._titleRect = new Rectangle(_iconRect.Right + 5, 10, _fullRect.Width - (_iconRect.Right + 5), _titleFont.LineHeight);
        this._messageRect = new Rectangle(_iconRect.Right + 5, _titleRect.Bottom, _fullRect.Width - (_iconRect.Right + 5), this.Height - _titleRect.Height);
    }

    public void Show(TimeSpan duration)
    {
        this.Location = this._stackDirection switch
        {
            EventReminderStackDirection.Top => new Point(_lastShown != null ? _lastShown.Left : this._x, _lastShown != null ? _lastShown.Top - this.Height - 15 : this._y),
            EventReminderStackDirection.Down => new Point(_lastShown != null ? _lastShown.Left : this._x, _lastShown != null ? _lastShown.Bottom + 15 : this._y),
            EventReminderStackDirection.Left => new Point(_lastShown != null ? _lastShown.Left - this.Width - 15 : this._x, _lastShown != null ? _lastShown.Top : this._y),
            EventReminderStackDirection.Right => new Point(_lastShown != null ? _lastShown.Right + 15 : this._x, _lastShown != null ? _lastShown.Top : this._y),
            _ => throw new ArgumentException($"Invalid stack direction: {this._stackDirection}"),
        };

        base.Show();
        _lastShown = this;

        this._showAnimation?.Cancel();
        this._showAnimation = GameService.Animation.Tweener.Tween(this, new { Opacity = 1f }, 0.2f)
                       .Repeat(1)
                       .RepeatDelay((float)duration.TotalSeconds)
                       .Reflect()
                       .OnComplete(() =>
                       {
                           base.Hide();
                           this.Dispose();
                           if (_lastShown == this)
                           {
                               _lastShown = null;
                           }
                       });
    }

    //public new void Hide()
    //{
    //    base.Hide();

    //    _ = GameService.Animation.Tweener.Tween(this, new { Opacity = 0f }, 0.4f)
    //                   .OnComplete(() =>
    //                   {
    //                       this.Dispose();
    //                       if (_lastShown == this)
    //                       {
    //                           _lastShown = null;
    //                       }
    //                   });
    //}

    protected override void DoPaint(SpriteBatch spriteBatch, Rectangle bounds)
    {
        spriteBatch.Draw(ContentService.Textures.Pixel, _fullRect, Color.Black * this.BackgroundOpacity);

        if (this._icon != null)
        {
            spriteBatch.Draw(this._icon, _iconRect, Color.White);
        }

        if (!string.IsNullOrWhiteSpace(this._title))
        {
            spriteBatch.DrawString(this._title, _titleFont, _titleRect, Color.White);
        }

        if (!string.IsNullOrWhiteSpace(this._message))
        {
            spriteBatch.DrawString(this._message, _messageFont, _messageRect, Color.White);
        }
    }

    protected override CaptureType CapturesInput()
    {
        return this._captureMouseClicks ? CaptureType.Mouse : CaptureType.None;
    }

    protected override void InternalDispose()
    {
        this._showAnimation?.Cancel();
        this._showAnimation = null;
        this.Model = null;
        this._iconService = null;
        this._icon = null;
    }
}