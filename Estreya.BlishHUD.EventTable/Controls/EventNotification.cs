namespace Estreya.BlishHUD.EventTable.Controls;

using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD._Extensions;
using Estreya.BlishHUD.Shared.Controls;
using Estreya.BlishHUD.Shared.State;
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

public class EventNotification : RenderTargetControl
{
    private const int NOTIFICATION_WIDTH = 512;
    private const int NOTIFICATION_HEIGHT = 128;
    private const int ICON_SIZE = 64;

    private static int _shownNotifications = 0;

    private Models.Event _event;
    private readonly string _message;
    private IconState _iconState;
    private AsyncTexture2D _backgroundTexture;
    private AsyncTexture2D _eventIcon;

    private static BitmapFont _titleFont = GameService.Content.DefaultFont18;
    private static BitmapFont _messageFont = GameService.Content.DefaultFont16;

    private static Rectangle _fullRect = new Rectangle(0, 0, NOTIFICATION_WIDTH, NOTIFICATION_HEIGHT);
    private static Rectangle _iconRect = new Rectangle(0, NOTIFICATION_HEIGHT / 2 - ICON_SIZE / 2, ICON_SIZE, ICON_SIZE);
    private static Rectangle _titleRect = new Rectangle(_iconRect.Right + 5, 10, _fullRect.Width - (_iconRect.Right + 5), _titleFont.LineHeight);
    private static Rectangle _messageRect = new Rectangle(_iconRect.Right + 5, _titleRect.Bottom, _fullRect.Width - (_iconRect.Right + 5), NOTIFICATION_HEIGHT - _titleRect.Height);

    public EventNotification(Models.Event ev, string message, IconState iconState)
    {
        this._event = ev;
        this._message = message;
        this._iconState = iconState;

        this._eventIcon = this._iconState?.GetIcon(ev.Icon);

        this._backgroundTexture = this._iconState?.GetIcon("604997.png");

        this.Width = NOTIFICATION_WIDTH;
        this.Height = NOTIFICATION_HEIGHT;
        this.Visible = false;
        this.Opacity = 0f;
        this.Parent = GameService.Graphics.SpriteScreen;
    }

    public void Show(TimeSpan duration, int x, int y)
    {
        _shownNotifications++;

        this.Location = new Microsoft.Xna.Framework.Point (x, y +( (NOTIFICATION_HEIGHT+ 15) * _shownNotifications));
        base.Show();

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
                _shownNotifications--;
                this.Dispose();
            });
    }

    protected override void DoPaint(SpriteBatch spriteBatch, Rectangle bounds)
    {
        if (this._backgroundTexture != null && this._backgroundTexture.HasSwapped)
        {
            spriteBatch.Draw(this._backgroundTexture, _fullRect, Color.White);
        }

        if (this._eventIcon != null && this._eventIcon.HasSwapped)
        {
            spriteBatch.Draw(this._eventIcon, _iconRect, Color.White);
        }

        spriteBatch.DrawString(this._event.Name, _titleFont, _titleRect, Color.White);
        spriteBatch.DrawString(_message, _messageFont, _messageRect, Color.White);
    }

    protected override void InternalDispose()
    {
        _event = null;
        _iconState = null;
        _eventIcon = null;
    }
}
