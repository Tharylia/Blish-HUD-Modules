namespace Estreya.BlishHUD.EventTable.Controls;

using Blish_HUD;
using Blish_HUD._Extensions;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Estreya.BlishHUD.EventTable.Models;
using Estreya.BlishHUD.EventTable.Models.Reminders;
using Estreya.BlishHUD.Shared.Extensions;
using Estreya.BlishHUD.Shared.Services.Audio;
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using static Blish_HUD.ContentService;

public class EventNotification : RenderTarget2DControl
{
    private static Logger Logger = Logger.GetLogger<EventNotification>();

    private static ToastNotifier _toastNotifier = ToastNotificationManager.CreateToastNotifier("Estreya BlishHUD Event Table");

    private static SynchronizedCollection<EventNotification> _activeNotifications = new SynchronizedCollection<EventNotification>();
    private static ConcurrentDictionary<FontSize, BitmapFont> _fonts = new ConcurrentDictionary<FontSize, BitmapFont>();

    private BitmapFont _titleFont;
    private BitmapFont _messageFont;

    private Rectangle _fullRect;
    private Rectangle _iconRect;
    private Rectangle _titleRect;
    private Rectangle _messageRect;
    private readonly string _title;
    private readonly string _message;
    private readonly ModuleSettings _moduleSettings;
    private string _formattedTitle;
    private string _formattedMessage;

    public Models.Event Model { get; private set; }
    private AsyncTexture2D _icon;
    private readonly bool _captureMouseClicks;
    private Tween _showAnimation;

    private EventNotification(Models.Event model, string title, string message, AsyncTexture2D icon, ModuleSettings moduleSettings)
    {
        this.Model = model;
        this._title = title;
        this._message = message;
        this._moduleSettings = moduleSettings;
        this._captureMouseClicks = moduleSettings.ReminderLeftClickAction.Value != LeftClickAction.None || moduleSettings.ReminderRightClickAction.Value != EventReminderRightClickAction.None;

        this._icon = icon;

        this.UpdateFonts();

        this.SetWidthAndHeight();
        this.SetLocation();

        this.Visible = false;
        this.Opacity = 0f;
        this.Parent = GameService.Graphics.SpriteScreen;

        this._moduleSettings.ReminderSize.Icon.SettingChanged += this.ReminderIconSize_SettingChanged;
    }

    private void ReminderIconSize_SettingChanged(object sender, ValueChangedEventArgs<int> e)
    {
        this.RecalculateLayout();
    }

    public override void RecalculateLayout()
    {
        var iconSize = this._moduleSettings.ReminderSize.Icon.Value;

        this._fullRect = new Rectangle(0, 0, this.Width, this.Height);
        this._iconRect = new Rectangle(10, (this.Height / 2) - (iconSize / 2), iconSize, iconSize);

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
        var initialXLocation = this._moduleSettings.ReminderPosition.X.Value;
        var initialYLocation = this._moduleSettings.ReminderPosition.Y.Value;
        var stackDirection = this._moduleSettings.ReminderStackDirection.Value;
        var overflowStackDirection = this._moduleSettings.ReminderOverflowStackDirection.Value;

        var parent = this.Parent;

        if (parent is null) return this.Location;

        return parent.AbsoluteBounds.Contains(this.AbsoluteBounds)
            ? this.Location
            : stackDirection switch
            {
                EventReminderStackDirection.Top => overflowStackDirection switch
                {
                    EventReminderStackDirection.Top => throw new InvalidOperationException("Can't overflow to same direction."),
                    EventReminderStackDirection.Down => throw new InvalidOperationException("Can't overflow to the bottom."),
                    EventReminderStackDirection.Left => new Point(this.Left - this.Width - spacing, initialYLocation),
                    EventReminderStackDirection.Right => new Point(this.Right + spacing, initialYLocation),
                    _ => throw new ArgumentException($"Invalid overflow stack direction: {overflowStackDirection}"),
                },
                EventReminderStackDirection.Down => overflowStackDirection switch
                {
                    EventReminderStackDirection.Top => throw new InvalidOperationException("Can't overflow to the top."),
                    EventReminderStackDirection.Down => throw new InvalidOperationException("Can't overflow to same direction."),
                    EventReminderStackDirection.Left => new Point(this.Left - this.Width - spacing, initialYLocation),
                    EventReminderStackDirection.Right => new Point(this.Right + spacing, initialYLocation),
                    _ => throw new ArgumentException($"Invalid overflow stack direction: {overflowStackDirection}"),
                },
                EventReminderStackDirection.Left => overflowStackDirection switch
                {
                    EventReminderStackDirection.Top => new Point(initialXLocation, this.Top - this.Height - spacing),
                    EventReminderStackDirection.Down => new Point(initialXLocation, this.Bottom + spacing),
                    EventReminderStackDirection.Left => throw new InvalidOperationException("Can't overflow to same direction."),
                    EventReminderStackDirection.Right => throw new InvalidOperationException("Can't overflow to the right."),
                    _ => throw new ArgumentException($"Invalid overflow stack direction: {overflowStackDirection}"),
                },
                EventReminderStackDirection.Right => overflowStackDirection switch
                {
                    EventReminderStackDirection.Top => new Point(initialXLocation, this.Top - this.Height - spacing),
                    EventReminderStackDirection.Down => new Point(initialXLocation, this.Bottom + spacing),
                    EventReminderStackDirection.Left => throw new InvalidOperationException("Can't overflow to the left."),
                    EventReminderStackDirection.Right => throw new InvalidOperationException("Can't overflow to same direction."),
                    _ => throw new ArgumentException($"Invalid overflow stack direction: {overflowStackDirection}"),
                },
                _ => throw new ArgumentException($"Invalid stack direction: {stackDirection}"),
            };
    }

    private BitmapFont GetTitleFont()
    {
        return _fonts.GetOrAdd(this._moduleSettings.ReminderFonts.TitleSize.Value, (size) => GameService.Content.GetFont(FontFace.Menomonia, size, FontStyle.Regular));
    }

    private BitmapFont GetMessageFont()
    {
        return _fonts.GetOrAdd(this._moduleSettings.ReminderFonts.MessageSize.Value, (size) => GameService.Content.GetFont(FontFace.Menomonia, size, FontStyle.Regular));
    }

    private void SetWidthAndHeight()
    {
        this.Width = this._moduleSettings.ReminderSize.X.Value;
        this.Height = this._moduleSettings.ReminderSize.Y.Value;
    }

    private void SetLocation()
    {
        int spacing = 15;

        var initialXLocation = this._moduleSettings.ReminderPosition.X.Value;
        var initialYLocation = this._moduleSettings.ReminderPosition.Y.Value;

        var notifications = _activeNotifications.ToList();
        var indexInNotifications = notifications.IndexOf(this);
        var lastShown = indexInNotifications is -1 or 0 ? null : notifications[indexInNotifications - 1];

        this.Location = this._moduleSettings.ReminderStackDirection.Value switch
        {
            EventReminderStackDirection.Top => new Point(lastShown != null ? lastShown.Left : initialXLocation, lastShown != null ? lastShown.Top - this.Height - spacing : initialYLocation),
            EventReminderStackDirection.Down => new Point(lastShown != null ? lastShown.Left : initialXLocation, lastShown != null ? lastShown.Bottom + spacing : initialYLocation),
            EventReminderStackDirection.Left => new Point(lastShown != null ? lastShown.Left - this.Width - spacing : initialXLocation, lastShown != null ? lastShown.Top : initialYLocation),
            EventReminderStackDirection.Right => new Point(lastShown != null ? lastShown.Right + spacing : initialXLocation, lastShown != null ? lastShown.Top : initialYLocation),
            _ => throw new ArgumentException($"Invalid stack direction: {this._moduleSettings.ReminderStackDirection.Value}"),
        };

        this.Location = this.GetOverflowLocation(spacing);
    }

    private void UpdateFonts()
    {
        var newTitleFont = this.GetTitleFont();
        var newMessageFont = this.GetMessageFont();

        var recalculate = false;
        if (newTitleFont != this._titleFont || newMessageFont != this._messageFont)
        {
            recalculate = true;
        }

        this._titleFont = newTitleFont;
        this._messageFont = newMessageFont;

        if (recalculate)
        {
            this.RecalculateLayout();
        }
    }

    protected override void InternalUpdate(GameTime gameTime)
    {
        this.UpdateFonts();
        this.SetWidthAndHeight();
        this.SetLocation();

    }

    private void Show(TimeSpan duration)
    {
        base.Show();
        _activeNotifications.Add(this);

        this._showAnimation?.Cancel();
        this._showAnimation = GameService.Animation.Tweener.Tween(this, new { Opacity = 1f }, 0.2f)
                       .Repeat(1)
                       .RepeatDelay((float)duration.TotalSeconds)
                       .Reflect()
                       .OnComplete(() =>
                       {
                           this.Dispose();
                       });
    }

    protected override void DoPaint(SpriteBatch spriteBatch, Rectangle bounds)
    {
        var backgroundColor = this._moduleSettings.ReminderColors.Background.Value.Id == 1 ? Color.Black : this._moduleSettings.ReminderColors.Background.Value.Cloth.ToXnaColor();

        spriteBatch.Draw(ContentService.Textures.Pixel, _fullRect, backgroundColor * this._moduleSettings.ReminderBackgroundOpacity.Value);

        if (this._icon != null)
        {
            spriteBatch.Draw(this._icon, _iconRect, Color.White);
        }

        if (!string.IsNullOrWhiteSpace(this._formattedTitle))
        {
            var titleColor = this._moduleSettings.ReminderColors.TitleText.Value.Id == 1 ? Color.White : this._moduleSettings.ReminderColors.TitleText.Value.Cloth.ToXnaColor();
            spriteBatch.DrawString(this._formattedTitle, _titleFont, _titleRect, titleColor * this._moduleSettings.ReminderTitleOpacity.Value);
        }

        if (!string.IsNullOrWhiteSpace(this._formattedMessage))
        {
            var messageColor = this._moduleSettings.ReminderColors.MessageText.Value.Id == 1 ? Color.White : this._moduleSettings.ReminderColors.MessageText.Value.Cloth.ToXnaColor();
            spriteBatch.DrawString(this._formattedMessage, _messageFont, _messageRect, messageColor * this._moduleSettings.ReminderMessageOpacity.Value);
        }
    }

    protected override CaptureType CapturesInput()
    {
        return this._captureMouseClicks ? CaptureType.Mouse : CaptureType.None;
    }

    protected override void InternalDispose()
    {
        this._moduleSettings.ReminderSize.Icon.SettingChanged -= this.ReminderIconSize_SettingChanged;
        base.Hide();
        _activeNotifications.Remove(this);

        this._showAnimation?.Cancel();
        this._showAnimation = null;
        this.Model = null;
        this._icon = null;
    }

    public static EventNotification ShowAsControl(string title, string message, AsyncTexture2D icon, IconService iconService, ModuleSettings moduleSettings)
    {
        return ShowAsControl(null, title, message, icon, iconService, moduleSettings);
    }

    public static EventNotification ShowAsControl(Models.Event ev, string title, string message, AsyncTexture2D icon, IconService iconService, ModuleSettings moduleSettings)
    {
        return ShowAsControl(ev, title, message, icon, iconService, moduleSettings, TimeSpan.FromSeconds(moduleSettings.ReminderDuration.Value));
    }

    public static EventNotification ShowAsControl(Models.Event ev, string title, string message, AsyncTexture2D icon, IconService iconService, ModuleSettings moduleSettings, TimeSpan timeout)
    {
        var notification = new EventNotification(
            ev,
            title,
            message,
            icon ?? (!string.IsNullOrWhiteSpace(ev.Icon) ? iconService.GetIcon(ev.Icon) : null),
            moduleSettings);

        notification.Show(timeout);

        return notification;
    }

    public static EventNotification ShowAsControlTest(string title, string message, AsyncTexture2D icon, IconService iconService, ModuleSettings moduleSettings)
    {
        return ShowAsControl(null, title, message, icon, iconService, moduleSettings, TimeSpan.FromHours(1));
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

            notification.Dismissed += (s, e) =>
            {
                File.Delete(tempImagePath);
            };

            _toastNotifier.Show(notification);
        });
    }

    public static string GetAudioServiceBaseSubfolder()
    {
        return "reminders";
    }

    public static string GetAudioServiceEventsSubfolder()
    {
        return $"{GetAudioServiceBaseSubfolder()}/events";
    }

    public static string GetSoundFileName()
    {
        return "reminder";
    }

    public static async Task PlaySound(AudioService audioService, Models.Event ev = null)
    {
        if (ev is not null)
        {
            var result = await audioService.PlaySoundFromFile(ev.SettingKey, GetAudioServiceEventsSubfolder(), true);
            if (result is AudioService.AudioPlaybackResult.Success)
            {
                return;
            }
        }

        await audioService.PlaySoundFromFile(GetSoundFileName(), GetAudioServiceBaseSubfolder(), true);
    }
}