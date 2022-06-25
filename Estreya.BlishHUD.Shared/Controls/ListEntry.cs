using Blish_HUD;
using Blish_HUD.Controls;
using Estreya.BlishHUD.Shared.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.BitmapFonts;
using System;

namespace Estreya.BlishHUD.Shared.Controls
{
    public class ListEntry<T> : Control
    {
        private bool _dragDrop = false;

        public bool DragDrop
        {
            get => this._dragDrop;
            set => this.SetProperty(ref this._dragDrop, value, true);
        }

        private bool _dragging = false;
        public bool Dragging
        {
            get => this._dragging;
            internal set => this.SetProperty(ref this._dragging, value, true);
        }

        private string _text;

        public string Text
        {
            get => this._text;
            set => this.SetProperty(ref this._text, value, true);
        }

        private BitmapFont _font = GameService.Content.DefaultFont16;

        public BitmapFont Font
        {
            get => this._font;
            set => this.SetProperty(ref this._font, value, true);
        }

        private Color _textColor = Color.Black;
        public Color TextColor
        {
            get => this._textColor;
            set => this.SetProperty(ref this._textColor, value, true);
        }

        private Texture2D _icon;

        public Texture2D Icon
        {
            get => this._icon;
            set => this.SetProperty(ref this._icon, value, true);
        }

        private float _iconMinWidth = 32;
        public float IconMinWidth
        {
            get => this._iconMinWidth;
            set
            {
                if (value > this._iconMaxWidth)
                {
                    this._iconMaxWidth = value;
                }

                _ = this.SetProperty(ref this._iconMinWidth, value, true);
            }
        }

        private float _iconMaxWidth = 32;
        public float IconMaxWidth
        {
            get => this._iconMaxWidth;
            set
            {
                if (value < this._iconMinWidth)
                {
                    this._iconMinWidth = value;
                }

                _ = this.SetProperty(ref this._iconMaxWidth, value, true);
            }
        }

        private float _iconMinHeight = 32;
        public float IconMinHeight
        {
            get => this._iconMinHeight;
            set
            {
                if (value > this._iconMaxHeight)
                {
                    this._iconMaxHeight = value;
                }

                _ = this.SetProperty(ref this._iconMinHeight, value, true);
            }
        }

        private float _iconMaxHeight = 32;
        public float IconMaxHeight
        {
            get => this._iconMaxHeight;
            set
            {
                if (value < this._iconMinHeight)
                {
                    this._iconMinHeight = value;
                }

                _ = this.SetProperty(ref this._iconMaxHeight, value, true);
            }
        }

        private float IconWidth => this.Icon == null ? 0 : MathHelper.Clamp(this.Icon.Width, this.IconMinWidth, this.IconMaxWidth);
        private float IconHeight => this.Icon == null ? 0 : MathHelper.Clamp(this.Icon.Height, this.IconMinHeight, this.IconMaxHeight);

        private HorizontalAlignment _alignment = HorizontalAlignment.Center;
        public HorizontalAlignment Alignment
        {
            get => this._alignment;
            set => this.SetProperty(ref this._alignment, value, true);
        }

        private float IconRightPadding => 20;

        private RectangleF IconBounds
        {
            get
            {
                var textWidth = this.Font.MeasureString(this.Text).Width;

                return this.Alignment switch
                {
                    HorizontalAlignment.Left => new RectangleF(0, 0, this.IconWidth, this.IconHeight),
                    HorizontalAlignment.Center => new RectangleF((this.Size.X / 2) - (textWidth / 2) - (this.IconWidth / 2) - (this.IconRightPadding/2), 0, this.IconWidth, this.IconHeight),
                    HorizontalAlignment.Right => new RectangleF(this.Size.X - textWidth - this.IconWidth - this.IconRightPadding, 0, this.IconWidth, this.IconHeight),
                    _ => throw new InvalidOperationException($"Alignment \"{this.Alignment}\" is not supported."),
                };
            }
        }
        private RectangleF TextBounds => new RectangleF(this.IconBounds.Right + IconRightPadding, 0, this.Size.X - this.IconBounds.Width, this.Size.Y);

        public T Data { get; set; }

        public ListEntry(string title)
        {
            this.Text = title;
        }

        public ListEntry(string title, BitmapFont font) : this(title)
        {
            this.Font = font;
        }

        public override void RecalculateLayout()
        {
            base.RecalculateLayout();

            int height = (int)Math.Max(this.IconBounds.Height, this.TextBounds.Height);
            height = Math.Max(this.Size.Y, height);

            this.Size = new Point(this.Size.X, height);
        }

        protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds)
        {
            //RectangleF textBounds = new RectangleF(bounds.X, bounds.Y, this.DragDrop ? bounds.Width - DRAG_DROP_WIDTH : bounds.Width, bounds.Height);

            if (this.Icon != null)
            {
                spriteBatch.DrawOnCtrl(this, this.Icon, this.IconBounds);
            }

            if (!string.IsNullOrWhiteSpace(this.Text))
            {
                spriteBatch.DrawStringOnCtrl(this, this.Text, this.Font, this.TextBounds, this.TextColor, false);
            }
        }
    }
}
