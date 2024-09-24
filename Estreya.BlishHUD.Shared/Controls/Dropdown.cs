namespace Estreya.BlishHUD.Shared.Controls;

using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.BitmapFonts;
using MonoGame.Extended.TextureAtlases;
using System;
using System.Collections.ObjectModel;
using System.Linq;

/// <summary>
///     Represents a Guild Wars 2 Dropdown control.
/// </summary>
public class Dropdown<TItem> : Control
{
    public static readonly DesignStandard Standard = new DesignStandard( /*          Size */ new Point(250, 27),
        /*   PanelOffset */ new Point(5, 2),
        /* ControlOffset */ ControlStandard.ControlOffset);

    private bool _hadPanel;

    private DropdownPanel _lastPanel;

    private TItem _selectedItem;

    /// <summary>
    ///     Initializes a new instance of the <see cref="Dropdown" /> class.
    /// </summary>
    public Dropdown()
    {
        this.Items = new ObservableCollection<TItem>();

        this.Items.CollectionChanged += delegate
        {
            this.ItemsUpdated();
            this.Invalidate();
        };

        this.Size = Standard.Size;
    }

    /// <summary>
    ///     The collection of items contained in this <see cref="Dropdown" />.
    /// </summary>
    public ObservableCollection<TItem> Items { get; }

    /// <summary>
    ///     Gets or sets the currently selected item in the <see cref="Dropdown" />.
    /// </summary>
    public TItem SelectedItem
    {
        get => this._selectedItem;
        set
        {
            TItem previousValue = this._selectedItem;

            if (this.SetProperty(ref this._selectedItem, value))
            {
                this.OnValueChanged(new ValueChangedEventArgs<TItem>(previousValue, this._selectedItem));
            }
        }
    }

    /// <summary>
    ///     Returns <c>true</c> if this <see cref="Dropdown" /> is actively
    ///     showing the dropdown panel of options.
    /// </summary>
    public bool PanelOpen => this._lastPanel != null;

    /// <summary>
    ///     Gets or sets the height of the dropdown panel. A value of -1 indicates that all values should be shown.
    /// </summary>
    public int PanelHeight { get; set; } = -1;

    public int ItemHeight { get; set; } = -1;

    public BitmapFont Font { get; set; } = GameService.Content.DefaultFont14;

    public bool PreselectOnItemsChange { get; set; } = true;

    /// <summary>
    ///     If the Dropdown box items are currently being shown, they are hidden.
    /// </summary>
    public void HideDropdownPanel()
    {
        this._hadPanel = this._mouseOver;
        this._lastPanel?.Dispose();
    }

    private void HideDropdownPanelWithoutDebounce()
    {
        this.HideDropdownPanel();
        this._hadPanel = false;
    }

    protected override void OnClick(MouseEventArgs e)
    {
        base.OnClick(e);

        if (this._lastPanel == null && !this._hadPanel)
        {
            this._lastPanel = DropdownPanel.ShowPanel(this, Math.Min(this.PanelHeight, this.Items.Sum(x => this.Height)));
            if (this.PanelHeight != -1)
            {
                this._lastPanel.CanScroll = true;
            }
        }
        else
        {
            this._hadPanel = false;
        }
    }

    private void ItemsUpdated()
    {
        if (this.PreselectOnItemsChange)
        {
            this.SelectedItem ??= this.Items.FirstOrDefault();
        }
    }

    protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds)
    {
        // Draw dropdown
        spriteBatch.DrawOnCtrl(this,
            _textureInputBox,
            new Rectangle(Point.Zero, this._size).Subtract(new Rectangle(0, 0, 5, 0)),
            new Rectangle(0, 0,
                Math.Min(_textureInputBox.Width - 5, this.Width - 5),
                _textureInputBox.Height));

        // Draw right side of dropdown
        spriteBatch.DrawOnCtrl(this,
            _textureInputBox,
            new Rectangle(this._size.X - 5, 0, 5, this._size.Y),
            new Rectangle(_textureInputBox.Width - 5, 0,
                5, _textureInputBox.Height));

        // Draw dropdown arrow
        spriteBatch.DrawOnCtrl(this,
            this.Enabled && this.MouseOver ? _textureArrowActive : _textureArrow,
            new Rectangle(this._size.X - _textureArrow.Width - 5, (this._size.Y / 2) - (_textureArrow.Height / 2),
                _textureArrow.Width,
                _textureArrow.Height));

        // Draw text
        spriteBatch.DrawStringOnCtrl(this, this.SelectedItem?.ToString(),
            this.Font,
            new Rectangle(5, 0, this._size.X - 10 - _textureArrow.Width, this._size.Y),
            this.Enabled
                ? Color.FromNonPremultiplied(239, 240, 239, 255)
                : StandardColors.DisabledText);
    }

    private class DropdownPanel : FlowPanel
    {
        private const int SCROLL_CLOSE_THRESHOLD = 20;

        private readonly int _startTop;

        private Dropdown<TItem> _assocDropdown;

        private DropdownPanel(Dropdown<TItem> assocDropdown, int panelHeight = -1)
        {
            this._assocDropdown = assocDropdown;
            this._size = new Point(this._assocDropdown.Width, panelHeight != -1 ? panelHeight:  this._assocDropdown.Height * this._assocDropdown.Items.Count);
            this._location = this.GetPanelLocation();
            this._zIndex = Screen.TOOLTIP_BASEZINDEX;
            this.BackgroundColor = Color.Black; // Needed as some items have white lines between them otherwise.
            this.FlowDirection = ControlFlowDirection.SingleTopToBottom;

            this._startTop = this._location.Y;

            this.Parent = Graphics.SpriteScreen;

            Input.Mouse.LeftMouseButtonPressed += this.InputOnMousedOffDropdownPanel;
            Input.Mouse.RightMouseButtonPressed += this.InputOnMousedOffDropdownPanel;

            this.AddItems();
        }

        private void AddItems()
        {
            foreach (TItem itemValue in this._assocDropdown.Items)
            {
                DropdownPanelItem dropdownPanelItem = new DropdownPanelItem(itemValue)
                {
                    Parent = this,
                    Height = this._assocDropdown.ItemHeight == -1 ? this._assocDropdown.Height : this._assocDropdown.ItemHeight,
                    Width = this._assocDropdown.Width,
                    Font = this._assocDropdown.Font,
                };

                dropdownPanelItem.Click += this.DropdownPanelItem_Click;
            }
        }

        private void DropdownPanelItem_Click(object sender, MouseEventArgs e)
        {
            if (sender is DropdownPanelItem panelItem)
            {
                this._assocDropdown.SelectedItem = panelItem.Value;

                this.Dispose();
            }
        }

        private Point GetPanelLocation()
        {
            Point dropdownLocation = this._assocDropdown.AbsoluteBounds.Location;

            int yUnderDef = Graphics.SpriteScreen.Bottom - (dropdownLocation.Y + this._assocDropdown.Height + this._size.Y);
            int yAboveDef = Graphics.SpriteScreen.Top + (dropdownLocation.Y - this._size.Y);

            return yUnderDef > 0 || yUnderDef > yAboveDef
                // flip down
                ? dropdownLocation + new Point(0, this._assocDropdown.Height - 1)
                // flip up
                : dropdownLocation - new Point(0, this._size.Y + 1);
        }

        public static DropdownPanel ShowPanel(Dropdown<TItem> assocDropdown, int panelHeight = -1)
        {
            return new DropdownPanel(assocDropdown, panelHeight);
        }

        private void InputOnMousedOffDropdownPanel(object sender, MouseEventArgs e)
        {
            if (!this.MouseOver)
            {
                if (e.EventType == MouseEventType.RightMouseButtonPressed)
                {
                    // Required to prevent right-click exiting the menu from eating the next left click
                    this._assocDropdown.HideDropdownPanelWithoutDebounce();
                }
                else
                {
                    this._assocDropdown.HideDropdownPanel();
                }
            }
        }

        public void UpdateDropdownLocation()
        {
            this._location = this.GetPanelLocation();

            if (Math.Abs(this._location.Y - this._startTop) > SCROLL_CLOSE_THRESHOLD)
            {
                this.Dispose();
            }
        }

        public override void UpdateContainer(GameTime gameTime)
        {
            this.UpdateDropdownLocation();
        }

        protected override void DisposeControl()
        {
            this.Children?.ToList().ForEach(child =>
            {
                if (child is DropdownPanelItem panelItem)
                {
                    panelItem.Click -= this.DropdownPanelItem_Click;
                }

                child?.Dispose();
            });

            if (this._assocDropdown != null)
            {
                this._assocDropdown._lastPanel = null;
                this._assocDropdown = null;
            }

            Input.Mouse.LeftMouseButtonPressed -= this.InputOnMousedOffDropdownPanel;
            Input.Mouse.RightMouseButtonPressed -= this.InputOnMousedOffDropdownPanel;

            base.DisposeControl();
        }
    }

    private class DropdownPanelItem : Control
    {
        private const int TOOLTIP_HOVER_DELAY = 800;

        private double _hoverTime;

        public BitmapFont Font { get; set; } = GameService.Content.DefaultFont14;

        public DropdownPanelItem(TItem value)
        {
            this.Value = value;
            this.BackgroundColor = Color.Black;
        }

        public TItem Value { get; }

        private void UpdateHoverTimer(double elapsedMilliseconds)
        {
            if (this._mouseOver)
            {
                this._hoverTime += elapsedMilliseconds;
            }
            else
            {
                this._hoverTime = 0;
            }

            this.BasicTooltipText = this._hoverTime > TOOLTIP_HOVER_DELAY
                ? this.Value?.ToString()
                : string.Empty;
        }

        public override void DoUpdate(GameTime gameTime)
        {
            this.UpdateHoverTimer(gameTime.ElapsedGameTime.TotalMilliseconds);
        }

        protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds)
        {
            if (this.MouseOver)
            {
                spriteBatch.DrawOnCtrl(this,
                    ContentService.Textures.Pixel,
                    new Rectangle(2,
                        2, this._size.X - 12 - _textureArrow.Width,
                        this.Height - 4),
                    new Color(45, 37, 25, 255));

                spriteBatch.DrawStringOnCtrl(this, this.Value?.ToString(),
                    this.Font,
                    new Rectangle(8,
                        0,
                        bounds.Width - 13 - _textureArrow.Width,
                        this.Height),
                    ContentService.Colors.Chardonnay);
            }
            else
            {
                spriteBatch.DrawStringOnCtrl(this, this.Value?.ToString(),
                    this.Font,
                    new Rectangle(8,
                        0,
                        bounds.Width - 13 - _textureArrow.Width,
                        this.Height),
                    Color.FromNonPremultiplied(239, 240, 239, 255));
            }
        }
    }

    #region Load Static

    private static readonly Texture2D _textureInputBox = Content.GetTexture("input-box");

    private static readonly TextureRegion2D _textureArrow = Blish_HUD.Controls.Resources.Control.TextureAtlasControl.GetRegion("inputboxes/dd-arrow");
    private static readonly TextureRegion2D _textureArrowActive = Blish_HUD.Controls.Resources.Control.TextureAtlasControl.GetRegion("inputboxes/dd-arrow-active");

    #endregion

    #region Events

    /// <summary>
    ///     Occurs when the <see cref="SelectedItem" /> property has changed.
    /// </summary>
    public event EventHandler<ValueChangedEventArgs<TItem>> ValueChanged;

    protected virtual void OnValueChanged(ValueChangedEventArgs<TItem> e)
    {
        this.ValueChanged?.Invoke(this, e);
    }

    #endregion
}