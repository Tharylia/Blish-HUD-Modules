namespace Estreya.BlishHUD.Shared.Controls
{
    using Blish_HUD;
    using Blish_HUD.Content;
    using Blish_HUD.Controls;
    using Blish_HUD.Input;
    using Glide;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using MonoGame.Extended;
    using MonoGame.Extended.BitmapFonts;
    using System.ComponentModel;

    public class Button : LabelBase
    {
        public const int STANDARD_CONTROL_HEIGHT = 26;

        public const int DEFAULT_CONTROL_WIDTH = 128;

        private const int ICON_SIZE = 16;

        private const int ICON_TEXT_OFFSET = 4;

        private const int ATLAS_SPRITE_WIDTH = 350;

        private const int ATLAS_SPRITE_HEIGHT = 20;

        private const int ANIM_FRAME_COUNT = 8;

        private const float ANIM_FRAME_TIME = 0.25f;

        private static readonly Texture2D _textureButtonIdle = Control.Content.GetTexture("common/button-states");

        private static readonly Texture2D _textureButtonBorder = Control.Content.GetTexture("button-border");

        private AsyncTexture2D _icon;

        private bool _resizeIcon;

        private Tween _animIn;

        private Tween _animOut;

        private Rectangle _layoutIconBounds = Rectangle.Empty;

        private Rectangle _layoutTextBounds = Rectangle.Empty;

        /// <summary>
        /// The text shown on the button.
        /// </summary>
        public string Text
        {
            get => this._text;
            set => this.SetProperty(ref this._text, value, invalidateLayout: true, "Text");
        }

        /// <summary>
        /// An icon to show on the Blish_HUD.Controls.StandardButton. For best results, the <see cref="Icon"/> should be 16x16.
        /// </summary>
        public AsyncTexture2D Icon
        {
            get => this._icon;
            set => this.SetProperty(ref this._icon, value, invalidateLayout: true, "Icon");
        }
        /// <summary>
        /// If true, the <see cref="Icon"/> texture will be resized to 16x16.
        /// </summary>
        public bool ResizeIcon
        {
            get => this._resizeIcon;
            set => this.SetProperty(ref this._resizeIcon, value, invalidateLayout: true, "ResizeIcon");
        }

        public BitmapFont Font
        {
            get => this._font;
            set => this.SetProperty(ref this._font, value, invalidateLayout: true, "Font");
        }

        //
        // Summary:
        //     Do not directly manipulate this property. It is only public because the animation
        //     library requires it to be public.
        [EditorBrowsable(EditorBrowsableState.Never)]
        public int AnimationState { get; set; }

        public Button()
        {
            this._textColor = Color.Black;
            this._horizontalAlignment = HorizontalAlignment.Left;
            this._verticalAlignment = VerticalAlignment.Middle;
            base.Size = new Point(128, 26);
        }

        private void TriggerAnimation(bool directionIn)
        {
            this._animIn?.Pause();
            this._animOut?.Pause();
            if (directionIn)
            {
                this._animIn = GameService.Animation.Tweener.Tween(this, new
                {
                    AnimationState = ANIM_FRAME_COUNT
                }, ANIM_FRAME_TIME - (this._animOut?.TimeRemaining ?? 0f));
            }
            else
            {
                this._animOut = GameService.Animation.Tweener.Tween(this, new
                {
                    AnimationState = 0
                }, ANIM_FRAME_TIME - (this._animIn?.TimeRemaining ?? 0f));
            }
        }

        protected override void OnMouseEntered(MouseEventArgs e)
        {
            this.TriggerAnimation(directionIn: true);
            base.OnMouseEntered(e);
        }

        protected override void OnMouseLeft(MouseEventArgs e)
        {
            this.TriggerAnimation(directionIn: false);
            base.OnMouseLeft(e);
        }

        protected override void OnClick(MouseEventArgs e)
        {
            Control.Content.PlaySoundEffectByName("audio\\button-click");
            base.OnClick(e);
        }

        public override void RecalculateLayout()
        {
            Size2 textDimensions = this.GetTextDimensions();
            int num = (int)(this._size.X / 2 - textDimensions.Width / 2f);
            if (this._icon != null)
            {
                num = (!(textDimensions.Width > 0f)) ? (num + 8) : (num + 10);
                Point point = this._resizeIcon ? new Point(ICON_SIZE) : this._icon.Texture.Bounds.Size;
                this._layoutIconBounds = new Rectangle(num - point.X - ICON_TEXT_OFFSET, this._size.Y / 2 - point.Y / 2, point.X, point.Y);
            }

            this._layoutTextBounds = new Rectangle(num, 0, this._size.X - num, this._size.Y);
        }

        protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds)
        {
            if (this._enabled)
            {
                spriteBatch.DrawOnCtrl(this, _textureButtonIdle, new Rectangle(3, 3, this._size.X - 6, this._size.Y - 5), new Rectangle(this.AnimationState * ATLAS_SPRITE_WIDTH, 0, ATLAS_SPRITE_WIDTH, ATLAS_SPRITE_HEIGHT));
            }
            else
            {
                spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new Rectangle(3, 3, this._size.X - 6, this._size.Y - 5), Color.FromNonPremultiplied(121, 121, 121, 255));
            }

            spriteBatch.DrawOnCtrl(this, _textureButtonBorder, new Rectangle(2, 0, base.Width - 5, 4), new Rectangle(0, 0, 1, 4));
            spriteBatch.DrawOnCtrl(this, _textureButtonBorder, new Rectangle(base.Width - 4, 2, 4, base.Height - 3), new Rectangle(0, 1, 4, 1));
            spriteBatch.DrawOnCtrl(this, _textureButtonBorder, new Rectangle(3, base.Height - 4, base.Width - 6, 4), new Rectangle(1, 0, 1, 4));
            spriteBatch.DrawOnCtrl(this, _textureButtonBorder, new Rectangle(0, 2, 4, base.Height - 3), new Rectangle(0, 3, 4, 1));
            if (this._icon != null)
            {
                spriteBatch.DrawOnCtrl(this, this._icon, this._layoutIconBounds);
            }

            this._textColor = this._enabled ? Color.Black : Color.FromNonPremultiplied(51, 51, 51, 255);
            this.DrawText(spriteBatch, this._layoutTextBounds);
        }
    }
}
