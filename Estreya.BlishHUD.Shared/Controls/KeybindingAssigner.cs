namespace Estreya.BlishHUD.Shared.Controls;

using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

public class KeybindingAssigner : LabelBase
{
    private const int UNIVERSAL_PADDING = 2;

    private Rectangle _hotkeyRegion;

    private KeyBinding _keyBinding;

    private Rectangle _nameRegion;

    private int _nameWidth = 183;

    private bool _overHotkey;

    public KeybindingAssigner(KeyBinding keyBinding, bool withName)
    {
        this.KeyBinding = keyBinding ?? new KeyBinding();
        this.WithName = withName;
        this._font = Content.DefaultFont14;
        this._showShadow = true;
        this._cacheLabel = false;
        this.Size = new Point(340, 16);
    }

    public KeybindingAssigner(bool withName)
        : this(null, withName)
    {
    }

    public int NameWidth
    {
        get => this._nameWidth;
        set => this.SetProperty(ref this._nameWidth, value, true);
    }

    public string KeyBindingName
    {
        get => this._text;
        set => this.SetProperty(ref this._text, value);
    }

    public KeyBinding KeyBinding
    {
        get => this._keyBinding;
        set
        {
            if (this.SetProperty(ref this._keyBinding, value))
            {
                this.Enabled = this._keyBinding != null;
            }
        }
    }

    public bool WithName { get; }

    public event EventHandler<EventArgs> BindingChanged;

    protected void OnBindingChanged(EventArgs e)
    {
        this.BindingChanged?.Invoke(this, e);
    }

    protected override void OnClick(MouseEventArgs e)
    {
        if (this._overHotkey && e.IsDoubleClick)
        {
            this.SetupNewAssignmentWindow();
        }

        base.OnClick(e);
    }

    protected override void OnMouseMoved(MouseEventArgs e)
    {
        this._overHotkey = this.RelativeMousePosition.X >= this._hotkeyRegion.Left;
        base.OnMouseMoved(e);
    }

    protected override void OnMouseLeft(MouseEventArgs e)
    {
        this._overHotkey = false;
        base.OnMouseLeft(e);
    }

    public override void RecalculateLayout()
    {
        this._nameRegion = new Rectangle(0, 0, this._nameWidth, this._size.Y);
        this._hotkeyRegion = new Rectangle(this.WithName ? this._nameWidth + 2 : 0, 0, this._size.X - (this.WithName ? this._nameWidth - 2 : 0), this._size.Y);
    }

    private void SetupNewAssignmentWindow()
    {
        KeybindingAssignmentWindow newHkAssign = new KeybindingAssignmentWindow(this._text, this._keyBinding.ModifierKeys, this._keyBinding.PrimaryKey) { Parent = Graphics.SpriteScreen };
        newHkAssign.AssignmentAccepted += delegate
        {
            this._keyBinding.ModifierKeys = newHkAssign.ModifierKeys;
            this._keyBinding.PrimaryKey = newHkAssign.PrimaryKey;
            this.OnBindingChanged(EventArgs.Empty);
        };
        newHkAssign.Show();
    }

    protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds)
    {
        if (this.WithName)
        {
            spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, this._nameRegion, Color.White * 0.15f);
            this.DrawText(spriteBatch, this._nameRegion);
        }

        spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, this._hotkeyRegion, Color.White * (this._enabled && this._overHotkey ? 0.2f : 0.15f));
        if (this._enabled)
        {
            spriteBatch.DrawStringOnCtrl(this, this._keyBinding.GetBindingDisplayText(), Content.DefaultFont14, this._hotkeyRegion.OffsetBy(1, 1), Color.Black, false, HorizontalAlignment.Center);
            spriteBatch.DrawStringOnCtrl(this, this._keyBinding.GetBindingDisplayText(), Content.DefaultFont14, this._hotkeyRegion, Color.White, false, HorizontalAlignment.Center);
        }
    }
}