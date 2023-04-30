namespace Estreya.BlishHUD.Shared.Controls
{
    using Blish_HUD;
    using Blish_HUD.Content;
    using Blish_HUD.Controls;
    using Blish_HUD.Graphics;
    using Blish_HUD.Graphics.UI;
    using Blish_HUD.Input;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.Shared.Settings;
    using Glide;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public abstract class Window : Container, IWindow, IViewContainer
    {
        private const int STANDARD_TITLEBAR_HEIGHT = 40;

        private const int STANDARD_TITLEBAR_VERTICAL_OFFSET = 11;

        private const int STANDARD_LEFTTITLEBAR_HORIZONTAL_OFFSET = 2;

        private const int STANDARD_RIGHTTITLEBAR_HORIZONTAL_OFFSET = 16;

        private const int STANDARD_TITLEOFFSET = 80;

        private const int STANDARD_SUBTITLEOFFSET = 20;

        private const int STANDARD_MARGIN = 16;

        private const int SIDEBAR_WIDTH = 46;

        private const int SIDEBAR_OFFSET = 3;

        private const int RESIZEHANDLE_SIZE = 16;

        private const string WINDOW_SETTINGS = "WindowSettings2";

        private static readonly Texture2D _textureTitleBarLeft = Control.Content.GetTexture("titlebar-inactive");

        private static readonly Texture2D _textureTitleBarRight = Control.Content.GetTexture("window-topright");

        private static readonly Texture2D _textureTitleBarLeftActive = Control.Content.GetTexture("titlebar-active");

        private static readonly Texture2D _textureTitleBarRightActive = Control.Content.GetTexture("window-topright-active");

        private static readonly Texture2D _textureExitButton = Control.Content.GetTexture("button-exit");

        private static readonly Texture2D _textureExitButtonActive = Control.Content.GetTexture("button-exit-active");

        private static readonly Texture2D _textureBlackFade = Control.Content.GetTexture("fade-down-46");

        private readonly SettingCollection _windowSettings;

        private readonly AsyncTexture2D _textureWindowCorner = AsyncTexture2D.FromAssetId(156008);

        private readonly AsyncTexture2D _textureWindowResizableCorner = AsyncTexture2D.FromAssetId(156009);

        private readonly AsyncTexture2D _textureWindowResizableCornerActive = AsyncTexture2D.FromAssetId(156010);

        private readonly AsyncTexture2D _textureSplitLine = AsyncTexture2D.FromAssetId(605026);

        private string _title = "No Title";

        private string _subtitle = "";

        private bool _canClose = true;

        private bool _canCloseWithEscape = true;

        private bool _canResize;

        private Point _maxSize = Point.Zero;

        private Point _minSize = Point.Zero;

        private bool _rebuildViewAfterResize;

        private bool _unloadOnRebuild = true;

        private AsyncTexture2D _emblem;

        private bool _topMost;

        protected bool _savesPosition;

        protected bool _savesSize;

        private string _id;

        private bool _dragging;

        private bool _resizing;

        private readonly Tween _animFade;

        private bool _savedVisibility;

        private bool _showSideBar;

        private int _sideBarHeight = 100;

        private double _lastWindowInteract;

        private Rectangle _leftTitleBarDrawBounds = Rectangle.Empty;

        private Rectangle _rightTitleBarDrawBounds = Rectangle.Empty;

        private Rectangle _subtitleDrawBounds = Rectangle.Empty;

        private Rectangle _emblemDrawBounds = Rectangle.Empty;

        private Rectangle _sidebarInactiveDrawBounds = Rectangle.Empty;

        private Point _dragStart = Point.Zero;

        private Point _resizeStart = Point.Zero;

        private Point _contentMargin;

        private float _windowToTextureWidthRatio;

        private float _windowToTextureHeightRatio;

        private float _windowLeftOffsetRatio;

        private float _windowTopOffsetRatio;

        //
        // Summary:
        //     Gets or sets the active window. Returns null if no window is visible.
        public static IWindow ActiveWindow
        {
            get => (from w in GetWindows()
                    where w.Visible
                    select w).OrderByDescending(GetZIndex).FirstOrDefault();
            set => value.BringWindowToFront();
        }

        public override int ZIndex
        {
            get => this._zIndex + GetZIndex(this);
            set => this.SetProperty(ref this._zIndex, value, invalidateLayout: false, "ZIndex");
        }

        //
        // Summary:
        //     The text shown at the top of the window.
        public string Title
        {
            get => this._title;
            set => this.SetProperty(ref this._title, value, invalidateLayout: true, "Title");
        }

        //
        // Summary:
        //     The text shown to the right of the title in the title bar. This text is smaller
        //     and is normally used to show the current tab name and/or hotkey used to open
        //     the window.
        public string Subtitle
        {
            get => this._subtitle;
            set => this.SetProperty(ref this._subtitle, value, invalidateLayout: true, "Subtitle");
        }

        //
        // Summary:
        //     If true, draws an X icon on the window's titlebar and allows the user to close
        //     it by pressing it.
        //     Default: true
        public bool CanClose
        {
            get => this._canClose;
            set => this.SetProperty(ref this._canClose, value, invalidateLayout: false, "CanClose");
        }

        //
        // Summary:
        //     If true, the window will close when the user presses the escape key. Blish_HUD.Controls.WindowBase2.CanClose
        //     must also be set to true.
        //     Default: true
        public bool CanCloseWithEscape
        {
            get => this._canCloseWithEscape;
            set => this.SetProperty(ref this._canCloseWithEscape, value, invalidateLayout: false, "CanCloseWithEscape");
        }

        //
        // Summary:
        //     If true, allows the window to be resized by dragging the bottom right corner.
        //     Default: false
        public bool CanResize
        {
            get => this._canResize;
            set => this.SetProperty(ref this._canResize, value, invalidateLayout: false, "CanResize");
        }

        public Point MaxSize
        {
            get => this._maxSize;
            set => this.SetProperty(ref this._maxSize, value, invalidateLayout: false, nameof(this.MaxSize));
        }

        public Point MinSize
        {
            get => this._minSize;
            set => this.SetProperty(ref this._minSize, value, true, nameof(this.MinSize));
        }

        public bool RebuildViewAfterResize
        {
            get => this._rebuildViewAfterResize;
            set => this.SetProperty(ref this._rebuildViewAfterResize, value, false, nameof(this.RebuildViewAfterResize));
        }

        public bool UnloadOnRebuild
        {
            get => this._unloadOnRebuild;
            set => this.SetProperty(ref this._unloadOnRebuild, value, false, nameof(this.UnloadOnRebuild));
        }

        //
        // Summary:
        //     The emblem/badge displayed in the top left corner of the window.
        public Texture2D Emblem
        {
            get => this._emblem;
            set => this.SetProperty(ref this._emblem, value, invalidateLayout: true, "Emblem");
        }

        //
        // Summary:
        //     If true, this window will show on top of all other windows, regardless of which
        //     one had focus last.
        //     Default: false
        public bool TopMost
        {
            get => this._topMost;
            set => this.SetProperty(ref this._topMost, value, invalidateLayout: false, "TopMost");
        }

        //
        // Summary:
        //     If true, the window will remember its position between Blish HUD sessions. Requires
        //     that Blish_HUD.Controls.WindowBase2.Id be set.
        //     Default: false
        public bool SavesPosition
        {
            get => this._savesPosition;
            set => this.SetProperty(ref this._savesPosition, value, invalidateLayout: false, "SavesPosition");
        }

        //
        // Summary:
        //     If true, the window will remember its size between Blish HUD sessions. Requires
        //     that Blish_HUD.Controls.WindowBase2.Id be set.
        //     Default: false
        public bool SavesSize
        {
            get => this._savesSize;
            set => this.SetProperty(ref this._savesSize, value, invalidateLayout: false, "SavesSize");
        }

        //
        // Summary:
        //     A unique id to identify the window. Used with Blish_HUD.Controls.WindowBase2.SavesPosition
        //     and Blish_HUD.Controls.WindowBase2.SavesSize as a unique identifier to remember
        //     where the window is positioned and its size.
        public string Id
        {
            get => this._id;
            set => this.SetProperty(ref this._id, value, invalidateLayout: false, "Id");
        }

        //
        // Summary:
        //     Indicates if the window is actively being dragged.
        public bool Dragging
        {
            get => this._dragging;
            private set => this.SetProperty(ref this._dragging, value, invalidateLayout: false, "Dragging");
        }

        //
        // Summary:
        //     Indicates if the window is actively being resized.
        public bool Resizing
        {
            get => this._resizing;
            private set => this.SetProperty(ref this._resizing, value, invalidateLayout: false, "Resizing");
        }

        public ViewState ViewState { get; protected set; }

        public IView CurrentView { get; protected set; }

        protected bool ShowSideBar
        {
            get => this._showSideBar;
            set => this.SetProperty(ref this._showSideBar, value, invalidateLayout: false, "ShowSideBar");
        }

        protected int SideBarHeight
        {
            get => this._sideBarHeight;
            set => this.SetProperty(ref this._sideBarHeight, value, invalidateLayout: true, "SideBarHeight");
        }

        double IWindow.LastInteraction => this._lastWindowInteract;

        protected Rectangle TitleBarBounds { get; private set; } = Rectangle.Empty;


        protected Rectangle ExitButtonBounds { get; private set; } = Rectangle.Empty;


        protected Rectangle ResizeHandleBounds { get; private set; } = Rectangle.Empty;


        protected Rectangle SidebarActiveBounds { get; private set; } = Rectangle.Empty;


        protected Rectangle BackgroundDestinationBounds { get; private set; } = Rectangle.Empty;


        protected bool MouseOverTitleBar { get; private set; }

        protected bool MouseOverExitButton { get; private set; }

        protected bool MouseOverResizeHandle { get; private set; }

        protected AsyncTexture2D WindowBackground { get; set; }

        protected Rectangle WindowRegion { get; set; }

        protected Rectangle WindowRelativeContentRegion { get; set; }

        public event EventHandler ManualResized;

        public static IEnumerable<IWindow> GetWindows()
        {
            return GameService.Graphics.SpriteScreen.GetChildrenOfType<IWindow>();
        }

        //
        // Summary:
        //     Returns the calculated zindex offset. This should be added to the base zindex
        //     (typically Blish_HUD.Controls.Screen.WINDOW_BASEZINDEX) and returned as the zindex.
        public static int GetZIndex(IWindow thisWindow)
        {
            IWindow[] source = GetWindows().ToArray();
            if (!source.Contains(thisWindow))
            {
                throw new InvalidOperationException("thisWindow must be a direct child of GameService.Graphics.SpriteScreen before ZIndex can automatically be calculated.");
            }

            return 41 + (from window in source
                         orderby window.TopMost, window.LastInteraction
                         select window).TakeWhile((IWindow window) => window != thisWindow).Count();
        }

        private Window()
        {
            base.Opacity = 0f;
            base.Visible = false;
            this._zIndex = 41;
            base.ClipsBounds = false;
            GameService.Input.Mouse.LeftMouseButtonReleased += this.OnGlobalMouseRelease;
            GameService.Gw2Mumble.PlayerCharacter.IsInCombatChanged += delegate
            {
                UpdateWindowBaseDynamicHUDCombatState(this);
            };
            GameService.GameIntegration.Gw2Instance.IsInGameChanged += delegate
            {
                UpdateWindowBaseDynamicHUDLoadingState(this);
            };
            this._animFade = Control.Animation.Tweener.Tween(this, new
            {
                Opacity = 1f
            }, 0.2f).Repeat().Reflect();
            this._animFade.Pause();
            this._animFade.OnComplete(delegate
            {
                this._animFade.Pause();
                if (this._opacity <= 0f)
                {
                    base.Visible = false;
                }
            });
        }

        public Window(BaseModuleSettings settings) : this()
        {
            this._windowSettings = settings.GlobalSettings.AddSubCollection("WindowSettings");
        }

        public static void UpdateWindowBaseDynamicHUDCombatState(Window wb)
        {
            if (GameService.Overlay.DynamicHUDWindows == DynamicHUDMethod.ShowPeaceful && GameService.Gw2Mumble.PlayerCharacter.IsInCombat)
            {
                wb._savedVisibility = wb.Visible;
                if (wb._savedVisibility)
                {
                    wb.Hide();
                }
            }
            else if (wb._savedVisibility)
            {
                wb.Show();
            }
        }

        public static void UpdateWindowBaseDynamicHUDLoadingState(Window wb)
        {
            if (GameService.Overlay.DynamicHUDLoading == DynamicHUDMethod.NeverShow && !GameService.GameIntegration.Gw2Instance.IsInGame)
            {
                wb._savedVisibility = wb.Visible;
                if (wb._savedVisibility)
                {
                    wb.Hide();
                }
            }
            else if (wb._savedVisibility)
            {
                wb.Show();
            }
        }

        public override void UpdateContainer(GameTime gameTime)
        {
            if (this.Dragging)
            {
                Point point = Control.Input.Mouse.Position - this._dragStart;
                base.Location += point;
                this._dragStart = Control.Input.Mouse.Position;
            }
            else if (this.Resizing)
            {
                Point point2 = Control.Input.Mouse.Position - this._dragStart;
                base.Size = this.HandleWindowResize(this._resizeStart + point2);
            }
        }

        //
        // Summary:
        //     Shows the window if it is hidden. Hides the window if it is currently showing.
        public void ToggleWindow()
        {
            if (base.Visible)
            {
                this.Hide();
            }
            else
            {
                this.Show();
            }
        }

        //
        // Summary:
        //     Shows the window.
        public override void Show()
        {
            this.BringWindowToFront();
            if (base.Visible)
            {
                return;
            }

            if (this.Id != null)
            {
                if (this.SavesPosition && this._windowSettings.TryGetSetting(this.Id, out var settingEntry))
                {
                    base.Location = ((settingEntry as SettingEntry<Point>) ?? new SettingEntry<Point>()).Value;
                }

                if (this.SavesSize && this._windowSettings.TryGetSetting(this.Id + "_size", out var settingEntry2))
                {
                    var savedSize = ((settingEntry2 as SettingEntry<Point>) ?? new SettingEntry<Point>()).Value;
                    if (savedSize.X < this.MinSize.X || savedSize.Y < this.MinSize.Y)
                    {
                        this.Size = this.MinSize;
                    }else if (this.MaxSize != Point.Zero && (savedSize.X > this.MaxSize.X || savedSize.Y > this.MaxSize.Y))
                    {
                        this.Size = this.MaxSize;
                    }
                    else
                    {
                        base.Size = savedSize;
                    }
                }
            }

            base.Location = new Point(MathHelper.Clamp(this._location.X, 0, GameService.Graphics.SpriteScreen.Width - 64), MathHelper.Clamp(this._location.Y, 0, GameService.Graphics.SpriteScreen.Height - 64));
            base.Opacity = 0f;
            base.Visible = true;
            this._animFade.Resume();
        }

        //
        // Summary:
        //     Hides the window.
        public override void Hide()
        {
            if (base.Visible)
            {
                this.Dragging = false;
                this._animFade.Resume();
                Control.Content.PlaySoundEffectByName("window-close");
            }
        }

        public void SetView(IView view)
        {
            this.SetView(view, true);
        }

        private void SetView(IView view, bool unloadCurrent = true)
        {
            this.ClearView(view == null || unloadCurrent);
            if (view != null)
            {
                this.ViewState = ViewState.Loading;
                this.CurrentView = view;
                Progress<string> progress = new Progress<string>(delegate
                {
                });
                view.Loaded += this.OnViewBuilt;
                view.DoLoad(progress).ContinueWith(this.BuildView);
            }
        }

        //
        // Summary:
        //     Shows the window with the provided view.
        public void Show(IView view)
        {
            this.SetView(view);
            this.Show();
        }

        protected void ClearView(bool unload = true)
        {
            if (this.CurrentView != null)
            {
                this.CurrentView.Loaded -= this.OnViewBuilt;

                if (unload)
                {
                    this.CurrentView.DoUnload();
                }
            }

            this.ClearChildren();
            this.ViewState = ViewState.None;
        }

        private void OnViewBuilt(object sender, EventArgs e)
        {
            this.CurrentView.Loaded -= this.OnViewBuilt;
            this.ViewState = ViewState.Loaded;
        }

        private void BuildView(Task<bool> loadResult)
        {
            if (loadResult.Result)
            {
                this.CurrentView.DoBuild(this);
            }
        }

        public override void RecalculateLayout()
        {
            this._rightTitleBarDrawBounds = new Rectangle(this.TitleBarBounds.Width - _textureTitleBarRight.Width + 16, this.TitleBarBounds.Y - 11, _textureTitleBarRight.Width, _textureTitleBarRight.Height);
            this._leftTitleBarDrawBounds = new Rectangle(this.TitleBarBounds.Location.X - 2, this.TitleBarBounds.Location.Y - 11, Math.Min(_textureTitleBarLeft.Width, this._rightTitleBarDrawBounds.Left - 2), _textureTitleBarLeft.Height);
            if (!string.IsNullOrWhiteSpace(this.Title) && !string.IsNullOrWhiteSpace(this.Subtitle))
            {
                int num = (int)Control.Content.DefaultFont32.MeasureString(this.Title).Width;
                this._subtitleDrawBounds = this._leftTitleBarDrawBounds.OffsetBy(80 + num + 20, 0);
            }

            if (this._emblem != null)
            {
                this._emblemDrawBounds = new Rectangle(this._leftTitleBarDrawBounds.X + 40 - (this._emblem.Width / 2) - 16, this._leftTitleBarDrawBounds.Bottom - (_textureTitleBarLeft.Height / 2) - (this._emblem.Height / 2), this._emblem.Width, this._emblem.Height);
            }

            this.ExitButtonBounds = new Rectangle(this._rightTitleBarDrawBounds.Right - 32 - _textureExitButton.Width, this._rightTitleBarDrawBounds.Y + 16, _textureExitButton.Width, _textureExitButton.Height);
            int num2 = this._leftTitleBarDrawBounds.Bottom - 11;
            int num3 = base.Size.Y - num2;
            this.SidebarActiveBounds = new Rectangle(this._leftTitleBarDrawBounds.X + 3, num2 - 3, 46, this.SideBarHeight);
            this._sidebarInactiveDrawBounds = new Rectangle(this._leftTitleBarDrawBounds.X + 3, num2 - 3 + this.SideBarHeight, 46, num3 - this.SideBarHeight);
            this.ResizeHandleBounds = new Rectangle(base.Width - this._textureWindowCorner.Width, base.Height - this._textureWindowCorner.Height, this._textureWindowCorner.Width, this._textureWindowCorner.Height);
        }

        protected override void OnMouseMoved(MouseEventArgs e)
        {
            this.ResetMouseRegionStates();
            if (base.RelativeMousePosition.Y < this.TitleBarBounds.Bottom)
            {
                if (this.ExitButtonBounds.Contains(base.RelativeMousePosition))
                {
                    this.MouseOverExitButton = true;
                }
                else
                {
                    this.MouseOverTitleBar = true;
                }
            }
            else if (this._canResize && this.ResizeHandleBounds.Contains(base.RelativeMousePosition) && base.RelativeMousePosition.X > this.ResizeHandleBounds.Right - 16 && base.RelativeMousePosition.Y > this.ResizeHandleBounds.Bottom - 16)
            {
                this.MouseOverResizeHandle = true;
            }

            base.OnMouseMoved(e);
        }

        private void OnGlobalMouseRelease(object sender, MouseEventArgs e)
        {
            if (!base.Visible)
            {
                return;
            }

            if (this.Id != null)
            {
                if (this.SavesPosition && this.Dragging)
                {
                    ((this._windowSettings[this.Id] as SettingEntry<Point>) ?? this._windowSettings.DefineSetting(this.Id, base.Location)).Value = base.Location;
                }
                else if (this.SavesSize && this.Resizing)
                {
                    ((this._windowSettings[this.Id + "_size"] as SettingEntry<Point>) ?? this._windowSettings.DefineSetting(this.Id + "_size", base.Size)).Value = base.Size;
                }
            }

            if (this.Resizing && this._resizeStart != this.Size)
            {
                this.OnManualResized();
            }

            this.Dragging = false;
            this.Resizing = false;
        }

        private void OnManualResized()
        {
            try
            {
                this.ManualResized?.Invoke(this, EventArgs.Empty);

                if (this.RebuildViewAfterResize && this.CurrentView != null)
                {
                    this.SetView(this.CurrentView, this.UnloadOnRebuild);
                }
            }
            catch (Exception)
            {
                // Don't let this crash.
            }
        }

        protected override void OnMouseLeft(MouseEventArgs e)
        {
            this.ResetMouseRegionStates();
            base.OnMouseLeft(e);
        }

        protected override void OnLeftMouseButtonPressed(MouseEventArgs e)
        {
            this.BringWindowToFront();
            if (this.MouseOverTitleBar)
            {
                this.Dragging = true;
                this._dragStart = Control.Input.Mouse.Position;
            }
            else if (this.MouseOverResizeHandle)
            {
                this.Resizing = true;
                this._resizeStart = base.Size;
                this._dragStart = Control.Input.Mouse.Position;
            }
            else if (this.MouseOverExitButton && this.CanClose)
            {
                this.Hide();
            }

            base.OnLeftMouseButtonPressed(e);
        }

        protected override void OnClick(MouseEventArgs e)
        {
            if (this.MouseOverResizeHandle && e.IsDoubleClick)
            {
                base.Size = new Point(this.WindowRegion.Width, this.WindowRegion.Height + 40);
            }

            base.OnClick(e);
        }

        private void ResetMouseRegionStates()
        {
            this.MouseOverTitleBar = false;
            this.MouseOverExitButton = false;
            this.MouseOverResizeHandle = false;
        }

        //
        // Summary:
        //     Modifies the window size as it's being resized. Override to lock the window size
        //     at specific intervals or implement other resize behaviors.
        protected virtual Point HandleWindowResize(Point newSize)
        {
            var minX = Math.Max(
                Math.Max(base.ContentRegion.X + this._contentMargin.X + 16, this._subtitleDrawBounds.Left + 16),
                this.MinSize.X);
            var maxX = this.MaxSize != Point.Zero ? this.MaxSize.X : int.MaxValue;

            var minY = Math.Max(
                this.ShowSideBar ? (this._sidebarInactiveDrawBounds.Top + 16) : (base.ContentRegion.Y + this._contentMargin.Y + 16),
                this.MinSize.Y);
            var maxY = this.MaxSize != Point.Zero ? this.MaxSize.Y : int.MaxValue;

            return new Point(MathHelper.Clamp(newSize.X, minX, maxX), MathHelper.Clamp(newSize.Y, minY, maxY));
        }

        public void BringWindowToFront()
        {
            this._lastWindowInteract = GameService.Overlay.CurrentGameTime.TotalGameTime.TotalMilliseconds;
        }

        protected void ConstructWindow(AsyncTexture2D background, Rectangle windowRegion, Rectangle contentRegion)
        {
            this.ConstructWindow(background, windowRegion, contentRegion, new Point(windowRegion.Width, windowRegion.Height + 40));
        }

        protected void ConstructWindow(Texture2D background, Rectangle windowRegion, Rectangle contentRegion)
        {
            this.ConstructWindow((AsyncTexture2D)background, windowRegion, contentRegion);
        }

        protected void ConstructWindow(AsyncTexture2D background, Rectangle windowRegion, Rectangle contentRegion, Point windowSize)
        {
            this.WindowBackground = background;
            this.WindowRegion = windowRegion;
            this.WindowRelativeContentRegion = contentRegion;
            base.Padding = new Thickness(Math.Max(windowRegion.Top - 40, 11), background.Width - windowRegion.Right, background.Height - windowRegion.Bottom + 40, windowRegion.Left);
            base.ContentRegion = new Rectangle(contentRegion.X - (int)base.Padding.Left, contentRegion.Y + 40 - (int)base.Padding.Top, contentRegion.Width, contentRegion.Height);
            this._contentMargin = new Point(windowRegion.Right - contentRegion.Right, windowRegion.Bottom - contentRegion.Bottom);
            this._windowToTextureWidthRatio = (float)(base.ContentRegion.Width + this._contentMargin.X + base.ContentRegion.X) / (float)background.Width;
            this._windowToTextureHeightRatio = (float)(base.ContentRegion.Height + this._contentMargin.Y + base.ContentRegion.Y - 40) / (float)background.Height;
            this._windowLeftOffsetRatio = (float)-windowRegion.Left / (float)background.Width;
            this._windowTopOffsetRatio = (float)-windowRegion.Top / (float)background.Height;
            base.Size = windowSize;
        }

        protected void ConstructWindow(Texture2D background, Rectangle windowRegion, Rectangle contentRegion, Point windowSize)
        {
            this.ConstructWindow((AsyncTexture2D)background, windowRegion, contentRegion, windowSize);
        }

        protected override void OnResized(ResizedEventArgs e)
        {
            base.ContentRegion = new Rectangle(base.ContentRegion.X, base.ContentRegion.Y, base.Width - base.ContentRegion.X - this._contentMargin.X, base.Height - base.ContentRegion.Y - this._contentMargin.Y);
            this.CalculateWindow();
            base.OnResized(e);
        }

        private void CalculateWindow()
        {
            this.TitleBarBounds = new Rectangle(0, 0, base.Size.X, 40);
            int num = (int)((float)(base.ContentRegion.Width + this._contentMargin.X + base.ContentRegion.X) / this._windowToTextureWidthRatio);
            int num2 = (int)((float)(base.ContentRegion.Height + this._contentMargin.Y + base.ContentRegion.Y - 40) / this._windowToTextureHeightRatio);
            this.BackgroundDestinationBounds = new Rectangle((int)Math.Floor(this._windowLeftOffsetRatio * (float)num), (int)Math.Floor((this._windowTopOffsetRatio * (float)num2) + 40f), num, num2);
        }

        public override void PaintBeforeChildren(SpriteBatch spriteBatch, Rectangle bounds)
        {
            this.PaintWindowBackground(spriteBatch);
            this.PaintSideBar(spriteBatch);
            this.PaintTitleBar(spriteBatch);
        }

        public override void PaintAfterChildren(SpriteBatch spriteBatch, Rectangle bounds)
        {
            this.PaintEmblem(spriteBatch);
            this.PaintTitleText(spriteBatch);
            this.PaintExitButton(spriteBatch);
            this.PaintCorner(spriteBatch);
        }

        private void PaintCorner(SpriteBatch spriteBatch)
        {
            if (this.CanResize)
            {
                spriteBatch.DrawOnCtrl(this, (this.MouseOverResizeHandle || this.Resizing) ? this._textureWindowResizableCornerActive : this._textureWindowResizableCorner, this.ResizeHandleBounds);
            }
            else
            {
                spriteBatch.DrawOnCtrl(this, this._textureWindowCorner, this.ResizeHandleBounds);
            }
        }

        private void PaintSideBar(SpriteBatch spriteBatch)
        {
            if (this.ShowSideBar)
            {
                spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, this.SidebarActiveBounds, Color.Black);
                spriteBatch.DrawOnCtrl(this, _textureBlackFade, this._sidebarInactiveDrawBounds);
                spriteBatch.DrawOnCtrl(this, this._textureSplitLine, new Rectangle(this.SidebarActiveBounds.Right - (this._textureSplitLine.Width / 2), this.SidebarActiveBounds.Top, this._textureSplitLine.Width, this._sidebarInactiveDrawBounds.Bottom - this.SidebarActiveBounds.Top));
            }
        }

        private void PaintWindowBackground(SpriteBatch spriteBatch)
        {
            spriteBatch.DrawOnCtrl(this, this.WindowBackground, this.BackgroundDestinationBounds);
        }

        private void PaintTitleBar(SpriteBatch spriteBatch)
        {
            if (base.MouseOver && this.MouseOverTitleBar)
            {
                spriteBatch.DrawOnCtrl(this, _textureTitleBarLeftActive, this._leftTitleBarDrawBounds);
                spriteBatch.DrawOnCtrl(this, _textureTitleBarRightActive, this._rightTitleBarDrawBounds);
            }
            else
            {
                spriteBatch.DrawOnCtrl(this, _textureTitleBarLeft, this._leftTitleBarDrawBounds);
                spriteBatch.DrawOnCtrl(this, _textureTitleBarRight, this._rightTitleBarDrawBounds);
            }
        }

        private void PaintTitleText(SpriteBatch spriteBatch)
        {
            if (!string.IsNullOrWhiteSpace(this.Title))
            {
                spriteBatch.DrawStringOnCtrl(this, this.Title, Control.Content.DefaultFont32, this._leftTitleBarDrawBounds.OffsetBy(80, 0), ContentService.Colors.ColonialWhite);
                if (!string.IsNullOrWhiteSpace(this.Subtitle))
                {
                    spriteBatch.DrawStringOnCtrl(this, this.Subtitle, Control.Content.DefaultFont16, this._subtitleDrawBounds, Color.White);
                }
            }
        }

        private void PaintExitButton(SpriteBatch spriteBatch)
        {
            if (this.CanClose)
            {
                spriteBatch.DrawOnCtrl(this, this.MouseOverExitButton ? _textureExitButtonActive : _textureExitButton, this.ExitButtonBounds);
            }
        }

        private void PaintEmblem(SpriteBatch spriteBatch)
        {
            if (this._emblem != null)
            {
                spriteBatch.DrawOnCtrl(this, this.Emblem, this._emblemDrawBounds);
            }
        }

        protected override void DisposeControl()
        {
            if (this.CurrentView != null)
            {
                this.CurrentView.Loaded -= this.OnViewBuilt;
                this.CurrentView.DoUnload();
            }

            GameService.Input.Mouse.LeftMouseButtonReleased -= this.OnGlobalMouseRelease;
            base.DisposeControl();
        }
    }
}
