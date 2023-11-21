namespace Estreya.BlishHUD.EventTable.Controls;

using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Estreya.BlishHUD.EventTable.Models.Reminders;
using Estreya.BlishHUD.Shared.Extensions;
using Glide;
using Humanizer;
using Humanizer.Localisation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.BitmapFonts;
using Shared.Controls;
using Shared.Services;
using Shared.Utils;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using static Blish_HUD.ContentService;

public class EventNotification : RenderTarget2DControl
{
    private static ToastNotifier _toastNotifier = ToastNotificationManager.CreateToastNotifier("Estreya BlishHUD Event Table");

    private static EventNotification _lastShown;

    private readonly BitmapFont _titleFont;
    private readonly BitmapFont _messageFont;

    private Rectangle _fullRect;
    private Rectangle _iconRect;
    private Rectangle _titleRect;
    private Rectangle _messageRect;
    private readonly string _title;
    private readonly string _message;
    private string _formattedTitle;
    private string _formattedMessage;

    public Models.Event Model { get; private set; }
    private AsyncTexture2D _icon;
    private IconService _iconService;
    private readonly bool _captureMouseClicks;
    private readonly int _x;
    private readonly int _y;
    private readonly int _iconSize;
    private readonly EventReminderStackDirection _stackDirection;
    private readonly EventReminderStackDirection _overflowStackDirection;
    private Tween _showAnimation;

    public EventNotification(Models.Event ev, string title, string message, AsyncTexture2D icon, int x, int y, int width, int height, int iconSize, EventReminderStackDirection stackDirection, EventReminderStackDirection overflowStackDirection, FontSize titleFontSize, FontSize messageFontSize, IconService iconService, bool captureMouseClicks = false)
    {
        this.Model = ev;
        this._title = title;
        this._message = message;
        this._x = x;
        this._y = y;
        this._iconSize = iconSize;
        this._stackDirection = stackDirection;
        this._overflowStackDirection = overflowStackDirection;
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

    public EventNotification(Models.Event ev, string message, int x, int y, int width, int height, int iconSize, EventReminderStackDirection stackDirection, EventReminderStackDirection overflowStackDirection, FontSize titleFontSize, FontSize messageFontSize, IconService iconService, bool captureMouseClicks = false) :
        this(ev, ev?.Name, message, null, x, y, width, height, iconSize, stackDirection, overflowStackDirection, titleFontSize, messageFontSize, iconService, captureMouseClicks)
    { }

    public float BackgroundOpacity { get; set; } = 1f;

    public override void RecalculateLayout()
    {
        this._fullRect = new Rectangle(0, 0, this.Width, this.Height);
        this._iconRect = new Rectangle(10, (this.Height / 2) - (this._iconSize / 2), this._iconSize, this._iconSize);

        var maxTitleWidth = _fullRect.Width - (_iconRect.Right + 5);
        this._formattedTitle = DrawUtil.WrapText(this._titleFont, this._title, maxTitleWidth - 10);
        var titleSize = this._titleFont.MeasureString(this._formattedTitle);

        this._titleRect = new Rectangle(_iconRect.Right + 5, 10, maxTitleWidth, (int)Math.Ceiling(titleSize.Height));

        var maxMessageWidth = _fullRect.Width - (_iconRect.Right + 5);
        this._formattedMessage = DrawUtil.WrapText(this._messageFont, this._message, maxMessageWidth - 10);

        this._messageRect = new Rectangle(_iconRect.Right + 5, _titleRect.Bottom, maxMessageWidth, this.Height - _titleRect.Height);
    }

    private Point GetOverflowLocation(int spacing)
    {
        return this.Parent.AbsoluteBounds.Contains(this.AbsoluteBounds)
            ? this.Location
            : this._stackDirection switch
            {
                EventReminderStackDirection.Top => this._overflowStackDirection switch
                {
                    EventReminderStackDirection.Top => throw new InvalidOperationException("Can't overflow to same direction."),
                    EventReminderStackDirection.Down => throw new InvalidOperationException("Can't overflow to the bottom."),
                    EventReminderStackDirection.Left => new Point(this.Left - this.Width - spacing, this._y),
                    EventReminderStackDirection.Right => new Point(this.Right + spacing, this._y),
                    _ => throw new ArgumentException($"Invalid overflow stack direction: {this._stackDirection}"),
                },
                EventReminderStackDirection.Down => this._overflowStackDirection switch
                {
                    EventReminderStackDirection.Top => throw new InvalidOperationException("Can't overflow to the top."),
                    EventReminderStackDirection.Down => throw new InvalidOperationException("Can't overflow to same direction."),
                    EventReminderStackDirection.Left => new Point(this.Left - this.Width - spacing, this._y),
                    EventReminderStackDirection.Right => new Point(this.Right + spacing, this._y),
                    _ => throw new ArgumentException($"Invalid overflow stack direction: {this._stackDirection}"),
                },
                EventReminderStackDirection.Left => this._overflowStackDirection switch
                {
                    EventReminderStackDirection.Top => new Point(this._x, this.Top - this.Height - spacing),
                    EventReminderStackDirection.Down => new Point(this._x, this.Bottom + spacing),
                    EventReminderStackDirection.Left => throw new InvalidOperationException("Can't overflow to same direction."),
                    EventReminderStackDirection.Right => throw new InvalidOperationException("Can't overflow to the right."),
                    _ => throw new ArgumentException($"Invalid overflow stack direction: {this._stackDirection}"),
                },
                EventReminderStackDirection.Right => this._overflowStackDirection switch
                {
                    EventReminderStackDirection.Top => new Point(this._x, this.Top - this.Height - spacing),
                    EventReminderStackDirection.Down => new Point(this._x, this.Bottom + spacing),
                    EventReminderStackDirection.Left => throw new InvalidOperationException("Can't overflow to the left."),
                    EventReminderStackDirection.Right => throw new InvalidOperationException("Can't overflow to same direction."),
                    _ => throw new ArgumentException($"Invalid overflow stack direction: {this._stackDirection}"),
                },
                _ => throw new ArgumentException($"Invalid stack direction: {this._stackDirection}"),
            };
    }

    public void Show(TimeSpan duration)
    {
        int spacing = 15;

        this.Location = this._stackDirection switch
        {
            EventReminderStackDirection.Top => new Point(_lastShown != null ? _lastShown.Left : this._x, _lastShown != null ? _lastShown.Top - this.Height - spacing : this._y),
            EventReminderStackDirection.Down => new Point(_lastShown != null ? _lastShown.Left : this._x, _lastShown != null ? _lastShown.Bottom + spacing : this._y),
            EventReminderStackDirection.Left => new Point(_lastShown != null ? _lastShown.Left - this.Width - spacing : this._x, _lastShown != null ? _lastShown.Top : this._y),
            EventReminderStackDirection.Right => new Point(_lastShown != null ? _lastShown.Right + spacing : this._x, _lastShown != null ? _lastShown.Top : this._y),
            _ => throw new ArgumentException($"Invalid stack direction: {this._stackDirection}"),
        };

        this.Location = this.GetOverflowLocation(spacing);

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

    protected override void DoPaint(SpriteBatch spriteBatch, Rectangle bounds)
    {
        spriteBatch.Draw(ContentService.Textures.Pixel, _fullRect, Color.Black * this.BackgroundOpacity);

        if (this._icon != null)
        {
            spriteBatch.Draw(this._icon, _iconRect, Color.White);
        }

        if (!string.IsNullOrWhiteSpace(this._formattedTitle))
        {
            spriteBatch.DrawString(this._formattedTitle, _titleFont, _titleRect, Color.White);
        }

        if (!string.IsNullOrWhiteSpace(this._formattedMessage))
        {
            spriteBatch.DrawString(this._formattedMessage, _messageFont, _messageRect, Color.White);
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

    public static async Task ShowAsWindowsNotification(string title, string message, AsyncTexture2D icon)
    {
        var template = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastImageAndText02);
        var textNodes = template.GetElementsByTagName("text");

        textNodes[0].AppendChild(template.CreateTextNode(title));
        textNodes[1].AppendChild(template.CreateTextNode(message));

        XmlNodeList toastImageElements = template.GetElementsByTagName("image");

        await icon.WaitUntilSwappedAsync(TimeSpan.FromSeconds(10));

        var tempImagePath = Path.GetTempFileName();
        tempImagePath = Path.ChangeExtension(tempImagePath, "png");

        using var fs = new FileStream(tempImagePath, FileMode.OpenOrCreate);
        icon.Texture.SaveAsPng(fs, icon.Texture.Width, icon.Texture.Height);

        ((XmlElement)toastImageElements[0]).SetAttribute("src", tempImagePath);
        IXmlNode toastNode = template.SelectSingleNode("/toast");
        ((XmlElement)toastNode).SetAttribute("duration", "short");

        GameService.Graphics.QueueMainThreadRender((gd) =>
        {
            var notification = new ToastNotification(template)
            {
                ExpirationTime = DateTimeOffset.Now.AddMinutes(5),
                ExpiresOnReboot = true
            };

            notification.Dismissed += (s,e) =>
            {
                File.Delete(tempImagePath);
            };

            _toastNotifier.Show(notification);
        });

    }
}