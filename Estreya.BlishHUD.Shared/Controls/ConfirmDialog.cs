namespace Estreya.BlishHUD.Shared.Controls
{
    using Blish_HUD;
    using Blish_HUD.Content;
    using Blish_HUD.Controls;
    using Estreya.BlishHUD.Shared.Extensions;
    using Estreya.BlishHUD.Shared.State;
    using Estreya.BlishHUD.Shared.Utils;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using MonoGame.Extended.BitmapFonts;
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    public class ConfirmDialog : Container
    {
        private readonly string _title;
        private string _parsedTitle;
        private string _parsedMessage;
        private readonly string _message;
        private IconState _iconState;
        private DialogResult _dialogResult = DialogResult.None;

        private static readonly BitmapFont _titleFont = GameService.Content.DefaultFont32;
        private static readonly BitmapFont _messageFont = GameService.Content.DefaultFont18;

        private EventWaitHandle _waitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
        private AsyncTexture2D _backgroundImage;
        private Rectangle _shadowRect;
        private Rectangle _confirmRect;
        private Rectangle _titleRect;
        private Rectangle _messageRect;

        private FlowPanel _buttonPanel;
        private (StandardButton Button, DialogResult Result)[] _buttons = new[]
        {
            (new StandardButton() {Text = "OK"}, DialogResult.OK),
            (new StandardButton() {Text = "Cancel"}, DialogResult.Cancel)
        };

        private int _selectedButtonIndex = 0;

        public int SelectedButtonIndex
        {
            get => this._selectedButtonIndex; 
            set
            {
                this._selectedButtonIndex = value;
                this.SelectButton();
            }
        }

        public DialogResult ESC_Result = DialogResult.None;

        public ConfirmDialog(string title, string message, IconState iconState, ButtonDefinition[] buttons = null)
        {
            this._title = title;
            this._parsedTitle = this._title;
            this._message = message;
            this._parsedMessage = this._message;
            this._iconState = iconState;

            if (buttons != null)
            {
                this._buttons = buttons.Select(x =>
                {
                    return (new StandardButton()
                    {
                        Text = x.Title
                    }, x.Result);
                }).ToArray();
            }

            this.BuildButtons();
            this.SelectButton();

            this.Parent = GameService.Graphics.SpriteScreen;
            this.Width = this.Parent.Width;
            this.Height = this.Parent.Height;
            this.ZIndex = int.MaxValue;
            this.Visible = false;

            this._backgroundImage = this._iconState.GetIcon("155963.png");

            GameService.Input.Keyboard.KeyPressed += this.Keyboard_KeyPressed;

        }

        private void Keyboard_KeyPressed(object sender, Blish_HUD.Input.KeyboardEventArgs e)
        {
            switch (e.Key)
            {
                case Microsoft.Xna.Framework.Input.Keys.Escape:
                    this._dialogResult = this.ESC_Result;
                    _ = this._waitHandle.Set();
                    break;
                case Microsoft.Xna.Framework.Input.Keys.Enter:
                    this._dialogResult = this._buttons[this.SelectedButtonIndex].Result;
                    _ = this._waitHandle.Set();
                    break;
                case Microsoft.Xna.Framework.Input.Keys.Left:
                    this.SelectButton(-1);
                    break;
                case Microsoft.Xna.Framework.Input.Keys.Right:
                    this.SelectButton(1);
                    break;
            }
        }

        private void SelectButton(int direction = 0)
        {
            switch (direction)
            {
                case -1:
                    if (this.SelectedButtonIndex > 0)
                    {
                        this.SelectedButtonIndex--;
                    }

                    break;
                case 1:
                    if (this.SelectedButtonIndex < this._buttons.Length - 1)
                    {
                        this.SelectedButtonIndex++;
                    }

                    break;
            }

            this._buttons.ToList().ForEach(b => b.Button.BackgroundColor = Color.Transparent);
            this._buttons[this.SelectedButtonIndex].Button.BackgroundColor = Color.White;
        }

        private void BuildButtons()
        {
            this._buttonPanel = new FlowPanel()
            {
                FlowDirection = ControlFlowDirection.SingleLeftToRight,
                WidthSizingMode = SizingMode.AutoSize,
                HeightSizingMode = SizingMode.AutoSize,
            };

            foreach (var button in _buttons)
            {
                button.Button.Parent = this._buttonPanel;
                button.Button.Click += this.Button_Click;
            }

            this._buttonPanel.Parent = this;
        }

        private void Button_Click(object sender, Blish_HUD.Input.MouseEventArgs e)
        {
            var button = sender as StandardButton;
            this._dialogResult = this._buttons.Where(b => b.Button == button).First().Result;
            _ = this._waitHandle.Set();
        }

        private void CancelButton_Click(object sender, Blish_HUD.Input.MouseEventArgs e)
        {
            this._dialogResult = DialogResult.Cancel;
            _ = this._waitHandle.Set();
        }

        private void OKButton_Click(object sender, Blish_HUD.Input.MouseEventArgs e)
        {
            this._dialogResult = DialogResult.OK;
            _ = this._waitHandle.Set();
        }

        protected override CaptureType CapturesInput()
        {
            return CaptureType.Mouse;
        }

        /// <summary>
        /// Shows the <see cref="ConfirmDialog"/> as an async operation waiting on its completion.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to abort the form.</param>
        /// <returns>The dialog result of <see cref="DialogResult.OK"/> or <see cref="DialogResult.Cancel"/>. A result of <see cref="DialogResult.None"/> indicates a timeout after 5 minutes.</returns>
        public async Task<DialogResult> ShowDialog(CancellationToken cancellationToken = default)
        {
            return await this.ShowDialog(TimeSpan.FromMinutes(5), cancellationToken);
        }

        /// <summary>
        /// Shows the <see cref="ConfirmDialog"/> as an async operation waiting on its completion.
        /// </summary>
        /// <param name="timeout">The timespan until the dialog times out.</param>
        /// <param name="cancellationToken">A cancellation token to abort the form.</param>
        /// <returns>The dialog result of <see cref="DialogResult.OK"/> or <see cref="DialogResult.Cancel"/>. A result of <see cref="DialogResult.None"/> indicates a timeout.</returns>
        public async Task<DialogResult> ShowDialog(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            this.Show();

            bool waitResult = await this._waitHandle.WaitOneAsync(timeout, cancellationToken);

            if (!waitResult)
            {
                this._dialogResult = DialogResult.None;
            }

            this.Hide();

            return this._dialogResult;
        }

        public override void RecalculateLayout()
        {
            this._shadowRect = new Rectangle(0, 0, this.Width, this.Height);

            int width = this.Width / 3;
            int height = this.Height / 3;
            int x = (this.Width / 2) - width / 2;
            int y = (this.Height / 2) - height / 2;

            this._confirmRect = new Rectangle(x, y, width, height);

            int textPadding = 50;

            int maxTextWidth = this._confirmRect.Width - textPadding * 2;
            this._parsedTitle = DrawUtil.WrapText(_titleFont, this._title, maxTextWidth);
            this._parsedMessage = DrawUtil.WrapText(_messageFont, this._message, maxTextWidth);

            this._titleRect = new Rectangle(this._confirmRect.X + textPadding, this._confirmRect.Y + this._confirmRect.Height / 4,
                this._confirmRect.Width - textPadding * 2, (int)Math.Ceiling(_titleFont.MeasureString(this._parsedTitle).Height));

            this._messageRect = new Rectangle(this._confirmRect.X + textPadding, this._titleRect.Bottom + 50,
                this._confirmRect.Width - textPadding * 2, (int)Math.Ceiling(_messageFont.MeasureString(this._parsedMessage).Height));
        }

        public override void UpdateContainer(GameTime gameTime)
        {
            // Has to be in update to get _buttonPanel.Width
            int buttonY = this._confirmRect.Y + (this._confirmRect.Height / 2) + this._confirmRect.Height / 4;
            this._buttonPanel.Location = new Point((this._confirmRect.X + this._confirmRect.Width / 2) - this._buttonPanel.Width / 2, buttonY);
        }

        public override void PaintBeforeChildren(SpriteBatch spriteBatch, Rectangle bounds)
        {
            spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, this._shadowRect, Color.LightGray * 0.8f);

            if (this._backgroundImage != null && this._backgroundImage.HasSwapped)
            {
                spriteBatch.DrawOnCtrl(this, this._backgroundImage, this._confirmRect);
            }

            spriteBatch.DrawStringOnCtrl(this, this._parsedTitle, _titleFont, this._titleRect, Color.White, horizontalAlignment: Blish_HUD.Controls.HorizontalAlignment.Center, verticalAlignment: VerticalAlignment.Top);

            spriteBatch.DrawStringOnCtrl(this, this._parsedMessage, _messageFont, this._messageRect, Color.White, horizontalAlignment: Blish_HUD.Controls.HorizontalAlignment.Center, verticalAlignment: VerticalAlignment.Top);
        }

        protected override void DisposeControl()
        {
            if (_buttons != null)
            {
                foreach (var buttonPair in _buttons)
                {
                    buttonPair.Button.Click -= this.Button_Click;
                }
            }

            this._buttonPanel?.ClearChildren();
            this.ClearChildren();

            if (_buttons != null)
            {
                foreach (var buttonPair in _buttons)
                {
                    buttonPair.Button.Dispose();
                }
            }

            _buttons = null;
            this._buttonPanel?.Dispose();
            this._buttonPanel = null;

            GameService.Input.Keyboard.KeyPressed -= this.Keyboard_KeyPressed;
            this._iconState = null;
            _ = this._waitHandle?.Set();
            this._waitHandle?.Dispose();
            this._waitHandle = null;
            this._backgroundImage = null;
        }
    }

    public class ButtonDefinition
    {
        public string Title { get; set; }
        public DialogResult Result { get; set; }

        public ButtonDefinition(string title, DialogResult result)
        {
            this.Title = title;
            this.Result = result;
        }
    }
}
