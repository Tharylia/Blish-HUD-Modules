﻿namespace Estreya.BlishHUD.Shared.Controls.Input;

using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.BitmapFonts;
using Services;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DialogResult = System.Windows.Forms.DialogResult;

public class InputDialog<TControl> : Container where TControl : Control, new()
{
    private static readonly BitmapFont _titleFont = GameService.Content.DefaultFont32;
    private static readonly BitmapFont _messageFont = GameService.Content.DefaultFont18;
    private readonly string _message;
    private readonly string _title;

    private TControl _inputControl;
    private FlowPanel _buttonPanel;

    private (StandardButton Button, DialogResult Result)[] _buttons =
    {
        (new StandardButton { Text = "OK" }, DialogResult.OK),
        (new StandardButton { Text = "Cancel" }, DialogResult.Cancel)
    };

    private Rectangle _confirmRect;
    private DialogResult _dialogResult = DialogResult.None;
    private IconService _iconService;
    private Rectangle _messageRect;
    private string _parsedMessage;
    private string _parsedTitle;

    private int _selectedButtonIndex;
    private Rectangle _shadowRect;
    private Rectangle _titleRect;

    private EventWaitHandle _waitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

    public DialogResult ESC_Result = DialogResult.None;

    public InputDialog(TControl control, string title, string message, IconService iconService, ButtonDefinition[] buttons = null)
    {
        this._inputControl = control;
        this._title = title;
        this._parsedTitle = this._title;
        this._message = message;
        this._parsedMessage = this._message;
        this._iconService = iconService;

        if (buttons != null)
        {
            this._buttons = buttons.Select(x =>
            {
                return (new StandardButton { Text = x.Title }, x.Result);
            }).ToArray();
        }

        this.ApplyControlSettings();
        this.BuildButtons();
        this.SelectButton();

        this.Parent = GameService.Graphics.SpriteScreen;
        this.Width = this.Parent.Width;
        this.Height = this.Parent.Height;
        this.ZIndex = int.MaxValue / 3;
        this.Visible = false;

        GameService.Input.Keyboard.KeyPressed += this.Keyboard_KeyPressed;
    }

    public int SelectedButtonIndex
    {
        get => this._selectedButtonIndex;
        set
        {
            this._selectedButtonIndex = value;
            this.SelectButton();
        }
    }

    public object Input {
        get
        {
            var controlType = typeof(TControl);
            if (controlType.Name == typeof(Shared.Controls.Dropdown<>).Name){
                var genericTypeBase = controlType.GetGenericTypeDefinition();
                var genericArgs = controlType.GetGenericArguments();
                var genericType = genericTypeBase.MakeGenericType(genericArgs);
                var dropdown = Convert.ChangeType(this._inputControl, genericType);
                var selectedItemProperty = dropdown.GetType().GetProperty(nameof(Shared.Controls.Dropdown<object>.SelectedItem), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                return selectedItemProperty.GetValue(dropdown);
            }

            return this._inputControl switch
            {
                TextBox textBox => textBox.Text?.Trim(),
                Checkbox checkbox => checkbox.Checked,
                Blish_HUD.Controls.Dropdown dropdown => dropdown.SelectedItem,
                _ => throw new NotSupportedException($"The control {typeof(TControl).FullName} is currently not supported.")
            };
        }
    }

    private void Keyboard_KeyPressed(object sender, KeyboardEventArgs e)
    {
        switch (e.Key)
        {
            case Keys.Escape:
                this._dialogResult = this.ESC_Result;
                _ = this._waitHandle.Set();
                break;
            case Keys.Enter:
            //case Keys.Space:
                this._dialogResult = this._buttons[this.SelectedButtonIndex].Result;
                _ = this._waitHandle.Set();
                break;
            //case Keys.Left:
            //    this.SelectButton(-1);
            //    break;
            //case Keys.Right:
            //    this.SelectButton(1);
            //    break;
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

    private void ApplyControlSettings()
    {
        this._inputControl.Parent = this;
    }

    private void BuildButtons()
    {
        this._buttonPanel = new FlowPanel
        {
            FlowDirection = ControlFlowDirection.SingleLeftToRight,
            WidthSizingMode = SizingMode.AutoSize,
            HeightSizingMode = SizingMode.AutoSize
        };

        foreach ((StandardButton Button, DialogResult Result) button in this._buttons)
        {
            button.Button.Parent = this._buttonPanel;
            button.Button.Click += this.Button_Click;
        }

        this._buttonPanel.Parent = this;
    }

    private void Button_Click(object sender, MouseEventArgs e)
    {
        StandardButton button = sender as StandardButton;
        this._dialogResult = this._buttons.Where(b => b.Button == button).First().Result;
        _ = this._waitHandle.Set();
    }

    private void CancelButton_Click(object sender, MouseEventArgs e)
    {
        this._dialogResult = DialogResult.Cancel;
        _ = this._waitHandle.Set();
    }

    private void OKButton_Click(object sender, MouseEventArgs e)
    {
        this._dialogResult = DialogResult.OK;
        _ = this._waitHandle.Set();
    }

    protected override CaptureType CapturesInput()
    {
        return CaptureType.Mouse;
    }

    /// <summary>
    ///     Shows the <see cref="ConfirmDialog" /> as an async operation waiting on its completion.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to abort the form.</param>
    /// <returns>
    ///     The dialog result of <see cref="DialogResult.OK" /> or <see cref="DialogResult.Cancel" />. A result of
    ///     <see cref="DialogResult.None" /> indicates a timeout after 5 minutes.
    /// </returns>
    public async Task<DialogResult> ShowDialog(CancellationToken cancellationToken = default)
    {
        return await this.ShowDialog(TimeSpan.FromMinutes(5), cancellationToken);
    }

    /// <summary>
    ///     Shows the <see cref="ConfirmDialog" /> as an async operation waiting on its completion.
    /// </summary>
    /// <param name="timeout">The timespan until the dialog times out.</param>
    /// <param name="cancellationToken">A cancellation token to abort the form.</param>
    /// <returns>
    ///     The dialog result of <see cref="DialogResult.OK" /> or <see cref="DialogResult.Cancel" />. A result of
    ///     <see cref="DialogResult.None" /> indicates a timeout.
    /// </returns>
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
        int x = (this.Width / 2) - (width / 2);
        int y = (this.Height / 2) - (height / 2);

        this._confirmRect = new Rectangle(x, y, width, height);

        int textPadding = 50;

        int maxTextWidth = this._confirmRect.Width - (textPadding * 2);
        this._parsedTitle = DrawUtil.WrapText(_titleFont, this._title, maxTextWidth);
        this._parsedMessage = DrawUtil.WrapText(_messageFont, this._message, maxTextWidth);

        this._titleRect = new Rectangle(this._confirmRect.X + textPadding, this._confirmRect.Y + (this._confirmRect.Height / 4),
            this._confirmRect.Width - (textPadding * 2), (int)Math.Ceiling(_titleFont.MeasureString(this._parsedTitle).Height));

        this._messageRect = new Rectangle(this._confirmRect.X + textPadding, this._titleRect.Bottom + 50,
            this._confirmRect.Width - (textPadding * 2), (int)Math.Ceiling(_messageFont.MeasureString(this._parsedMessage).Height));
    }

    public override void UpdateContainer(GameTime gameTime)
    {
        this.Height = this.Parent?.Height ?? 0;
        this.Width = this.Parent?.Width ?? 0;

        this._inputControl.Width = (int)(this._confirmRect.Width * 0.75);
        int inputControlY = this._confirmRect.Y + (this._confirmRect.Height / 2) + (this._confirmRect.Height / 6);
        this._inputControl.Location = new Point(this._confirmRect.X + (this._confirmRect.Width / 2) - (this._inputControl.Width / 2), inputControlY);

        // Has to be in update to get _buttonPanel.Width
        int buttonY = this._confirmRect.Y + (this._confirmRect.Height / 2) + (this._confirmRect.Height / 4);
        this._buttonPanel.Location = new Point(this._confirmRect.X + (this._confirmRect.Width / 2) - (this._buttonPanel.Width / 2), buttonY);
    }

    public override void PaintBeforeChildren(SpriteBatch spriteBatch, Rectangle bounds)
    {
        spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, this._shadowRect, Color.LightGray * 0.8f);

        spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, this._confirmRect, Color.Black * 0.9f);

        spriteBatch.DrawStringOnCtrl(this, this._parsedTitle, _titleFont, this._titleRect, Color.White, horizontalAlignment: HorizontalAlignment.Center, verticalAlignment: VerticalAlignment.Top);

        spriteBatch.DrawStringOnCtrl(this, this._parsedMessage, _messageFont, this._messageRect, Color.White, horizontalAlignment: HorizontalAlignment.Center, verticalAlignment: VerticalAlignment.Top);
    }

    protected override void DisposeControl()
    {
        if (this._buttons != null)
        {
            foreach ((StandardButton Button, DialogResult Result) buttonPair in this._buttons)
            {
                buttonPair.Button.Click -= this.Button_Click;
            }
        }

        this._buttonPanel?.ClearChildren();
        this.ClearChildren();

        if (this._buttons != null)
        {
            foreach ((StandardButton Button, DialogResult Result) buttonPair in this._buttons)
            {
                buttonPair.Button.Dispose();
            }
        }

        this._buttons = null;
        this._buttonPanel?.Dispose();
        this._buttonPanel = null;

        GameService.Input.Keyboard.KeyPressed -= this.Keyboard_KeyPressed;
        this._iconService = null;
        _ = this._waitHandle?.Set();
        this._waitHandle?.Dispose();
        this._waitHandle = null;
    }
}