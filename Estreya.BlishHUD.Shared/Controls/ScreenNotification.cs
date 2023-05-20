namespace Estreya.BlishHUD.Shared.Controls
{
    using Blish_HUD;
    using Blish_HUD.Controls;
    using Glide;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using MonoGame.Extended.BitmapFonts;
    using System.Collections.Generic;
    using System.Linq;

    public class ScreenNotification : Control
    {

        private const int DURATION_DEFAULT = 4;

        private const int NOTIFICATION_WIDTH = 1024;
        private const int NOTIFICATION_HEIGHT = 256;

        #region Load Static

        private static readonly SynchronizedCollection<ScreenNotification> _activeScreenNotifications = new SynchronizedCollection<ScreenNotification>();

        private static readonly BitmapFont _fontMenomonia36Regular = Content.GetFont(ContentService.FontFace.Menomonia, ContentService.FontSize.Size36, ContentService.FontStyle.Regular);

        private static readonly Texture2D _textureGrayBackground = Content.GetTexture(@"controls/notification/notification-gray");
        private static readonly Texture2D _textureBlueBackground = Content.GetTexture(@"controls/notification/notification-blue");
        private static readonly Texture2D _textureGreenBackground = Content.GetTexture(@"controls/notification/notification-green");
        private static readonly Texture2D _textureRedBackground = Content.GetTexture(@"controls/notification/notification-red");

        #endregion

        public enum NotificationType
        {
            Info,
            Warning,
            Error,

            Gray,
            Blue,
            Green,
            Red,
        }

        private NotificationType _type;
        public NotificationType Type
        {
            get => this._type;
            set => this.SetProperty(ref this._type, value, true);
        }

        private int _duration;
        public int Duration
        {
            get => this._duration;
            set => this.SetProperty(ref this._duration, value);
        }

        private Texture2D _icon;

        public Texture2D Icon
        {
            get => this._icon;
            set => this.SetProperty(ref this._icon, value);
        }

        private string _message;
        public string Message
        {
            get => this._message;
            set => this.SetProperty(ref this._message, value);
        }

        private int _targetTop = 0;
        private Tween _slideDownTween;

        private Rectangle _layoutMessageBounds;

        public ScreenNotification(string message, NotificationType type = NotificationType.Info, Texture2D icon = null, int duration = DURATION_DEFAULT)
        {
            this._message = message;
            this._type = type;
            this._icon = icon;
            this._duration = duration;

            this.Opacity = 0f;
            this.Size = new Point(NOTIFICATION_WIDTH, NOTIFICATION_HEIGHT);
            this.ZIndex = Screen.TOOLTIP_BASEZINDEX;
            this.Location = new Point((Graphics.SpriteScreen.Width / 2) - (this.Size.X / 2), (Graphics.SpriteScreen.Height / 4) - (this.Size.Y / 2));

            this._targetTop = this.Top;
        }

        public override void DoUpdate(GameTime gameTime)
        {
            // Calculate new top location. Fixes the wrong location before blish finishes resizing.
            var calculatedNewTop = (Graphics.SpriteScreen.Height / 4) - (this.Size.Y / 2);
            if (calculatedNewTop > this._targetTop)
            {
                this._targetTop += calculatedNewTop;

                this._slideDownTween?.Cancel();
                // Can't cancel a Tween inside Update loop and manually setting the Tween property as the tween will override it after current Update has finished and cancel afterwards.
                GameService.Animation.Tweener.Update((float)gameTime.ElapsedGameTime.TotalSeconds); // Force above tween to be canceled before next Update loop.
                this.Top = this._targetTop;
            }

            this.Left = (Graphics.SpriteScreen.Width / 2) - (this.Size.X / 2);
        }

        protected override CaptureType CapturesInput()
        {
            return CaptureType.Filter;
        }

        public override void RecalculateLayout()
        {
            switch (this._type)
            {
                case NotificationType.Info:
                case NotificationType.Warning:
                case NotificationType.Error:
                    this._layoutMessageBounds = this.LocalBounds;
                    break;

                case NotificationType.Gray:
                case NotificationType.Blue:
                case NotificationType.Green:
                case NotificationType.Red:
                    this._layoutMessageBounds = this.LocalBounds;
                    break;
            }
        }

        protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds)
        {
            if (string.IsNullOrEmpty(this._message)) return;

            Color messageColor = Color.White;
            Texture2D notificationBackground = null;

            switch (this._type)
            {
                case NotificationType.Info:
                    messageColor = Color.White;
                    break;

                case NotificationType.Warning:
                    messageColor = StandardColors.Yellow;
                    break;

                case NotificationType.Error:
                    messageColor = StandardColors.Red;
                    break;

                case NotificationType.Gray:
                    notificationBackground = _textureGrayBackground;
                    break;

                case NotificationType.Blue:
                    notificationBackground = _textureBlueBackground;
                    break;

                case NotificationType.Green:
                    notificationBackground = _textureGreenBackground;
                    break;

                case NotificationType.Red:
                    notificationBackground = _textureRedBackground;
                    break;
            }

            if (notificationBackground != null)
                spriteBatch.DrawOnCtrl(this, notificationBackground, this._layoutMessageBounds);

            // TODO: Add back drawing icon: (something like) spriteBatch.Draw(this.Icon, new Rectangle(64, 32, 128, 128).OffsetBy(bounds.Location), Color.White);

            spriteBatch.DrawStringOnCtrl(this,
                                         this.Message,
                                         _fontMenomonia36Regular,
                                         bounds.OffsetBy(1, 1),
                                         Color.Black,
                                         false,
                                         HorizontalAlignment.Center);

            spriteBatch.DrawStringOnCtrl(this,
                                         this.Message,
                                         _fontMenomonia36Regular,
                                         bounds,
                                         messageColor,
                                         false,
                                         HorizontalAlignment.Center);
        }

        /// <inheritdoc />
        public override void Show()
        {
            Animation.Tweener
                .Tween(this, new { Opacity = 1f }, 0.2f)
                .Repeat(1)
                .RepeatDelay(this.Duration)
                .Reflect()
                .OnComplete(this.Dispose);

            base.Show();
        }

        private void SlideDown(int distance)
        {
            this._targetTop += distance;

            this._slideDownTween?.Cancel();
            this._slideDownTween = Animation.Tweener.Tween(this, new { Top = this._targetTop }, 0.1f);

            if (this._opacity < 1f) return;

            Animation.Tweener
                .Tween(this, new { Opacity = 0f }, 1f)
                .OnComplete(this.Dispose);
        }

        /// <inheritdoc />
        protected override void DisposeControl()
        {
            this._slideDownTween?.Cancel();
            this._slideDownTween = null;

            _activeScreenNotifications.Remove(this);

            base.DisposeControl();
        }

        public static void ShowNotification(string message, NotificationType type = NotificationType.Info, Texture2D icon = null, int duration = DURATION_DEFAULT)
        {
            var nNot = new ScreenNotification(message, type, icon, duration)
            {
                Parent = Graphics.SpriteScreen
            };

            nNot.ZIndex = _activeScreenNotifications.DefaultIfEmpty(nNot).Max(n => n.ZIndex) + 1;

            foreach (var activeScreenNotification in _activeScreenNotifications)
            {
                activeScreenNotification.SlideDown((int)(_fontMenomonia36Regular.LineHeight * 0.75f));
            }

            _activeScreenNotifications.Add(nNot);

            nNot.Show();
        }

        public static void ShowNotification(string[] messages, NotificationType type = NotificationType.Info, Texture2D icon = null, int duration = 4)
        {
            for (int i = messages.Length - 1; i >= 0; i--)
            {
                ShowNotification(messages[i], type, icon, duration);
            }
        }
    }
}
