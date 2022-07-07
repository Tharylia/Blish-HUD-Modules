namespace Estreya.BlishHUD.TradingPostWatcher.Controls;

using Blish_HUD;
using Blish_HUD._Extensions;
using Blish_HUD.Controls;
using Estreya.BlishHUD.Shared.Models;
using Estreya.BlishHUD.Shared.Models.Drawers;
using Glide;
using Microsoft.Xna.Framework;

public class TradingPostWatcherDrawer : FlowPanel
{
    private bool _currentVisibilityDirection = false;
    private Tween _currentVisibilityAnimation { get; set; }

    public DrawerConfiguration Configuration { get; private set; }

    public TradingPostWatcherDrawer(DrawerConfiguration configuration)
    {
        this.Configuration = configuration;

        this.BackgroundColor_SettingChanged(this, new ValueChangedEventArgs<Gw2Sharp.WebApi.V2.Models.Color>(null, this.Configuration.BackgroundColor.Value));
        this.Opacity_SettingChanged(this, new ValueChangedEventArgs<float>(0f, this.Configuration.Opacity.Value));

        this.Configuration.BackgroundColor.SettingChanged += this.BackgroundColor_SettingChanged;
        this.Configuration.Opacity.SettingChanged += this.Opacity_SettingChanged;
        this.Configuration.Location.X.SettingChanged += this.Location_X_SettingChanged;
        this.Configuration.Location.Y.SettingChanged += this.Location_Y_SettingChanged;
        this.Configuration.Size.X.SettingChanged += this.Size_X_SettingChanged;
        this.Configuration.Size.Y.SettingChanged += this.Size_Y_SettingChanged;
    }
    private void Size_Y_SettingChanged(object sender, ValueChangedEventArgs<int> e)
    {
        this.Size = new Point(this.Size.X, e.NewValue);
    }

    private void Size_X_SettingChanged(object sender, ValueChangedEventArgs<int> e)
    {
        this.Size = new Point(e.NewValue, this.Size.Y);
    }

    private void Location_Y_SettingChanged(object sender, ValueChangedEventArgs<int> e)
    {
        this.Location = new Point(this.Location.X, e.NewValue);
    }

    private void Location_X_SettingChanged(object sender, ValueChangedEventArgs<int> e)
    {
        this.Location = new Point(e.NewValue, this.Location.Y);
    }

    private void Opacity_SettingChanged(object sender, ValueChangedEventArgs<float> e)
    {
        this.Opacity = e.NewValue;
    }

    private void BackgroundColor_SettingChanged(object sender, ValueChangedEventArgs<Gw2Sharp.WebApi.V2.Models.Color> e)
    {
        Color backgroundColor = Color.Transparent;

        if (e.NewValue != null && e.NewValue.Id != 1)
        {
            backgroundColor = e.NewValue.Cloth.ToXnaColor();
        }

        this.BackgroundColor = backgroundColor;
    }

    public new bool Visible
    {
        get
        {
            if (this._currentVisibilityDirection && this._currentVisibilityAnimation != null)
            {
                return true;
            }

            if (!this._currentVisibilityDirection && this._currentVisibilityAnimation != null)
            {
                return false;
            }

            return base.Visible;
        }
        set => base.Visible = value;
    }

    public new void Show()
    {
        if (this.Visible && this._currentVisibilityAnimation == null)
        {
            return;
        }

        if (this._currentVisibilityAnimation != null)
        {
            this._currentVisibilityAnimation.Cancel();
        }

        this._currentVisibilityDirection = true;
        this.Visible = true;
        this._currentVisibilityAnimation = Animation.Tweener.Tween(this, new { Opacity = 1f }, 0.2f);
        this._currentVisibilityAnimation.OnComplete(() =>
        {
            this._currentVisibilityAnimation = null;
        });
    }

    public new void Hide()
    {
        if (!this.Visible && this._currentVisibilityAnimation == null)
        {
            return;
        }

        if (this._currentVisibilityAnimation != null)
        {
            this._currentVisibilityAnimation.Cancel();
        }

        this._currentVisibilityDirection = false;
        this._currentVisibilityAnimation = Animation.Tweener.Tween(this, new { Opacity = 0f }, 0.2f);
        this._currentVisibilityAnimation.OnComplete(() =>
        {
            this.Visible = false;
            this._currentVisibilityAnimation = null;
        });
    }

    public void UpdateSize(int width, int height, bool overrideHeight = false)
    {
        if (height == -1)
        {
            height = this.Size.Y;
        }

        this.Size = new Point(width, !overrideHeight ? this.Size.Y : height);
    }

    public void UpdatePosition(int x, int y)
    {
        bool buildFromBottom = this.Configuration.BuildDirection.Value == BuildDirection.Bottom;

        this.Location = buildFromBottom ? new Point(x, y - this.Height) : new Point(x, y);
    }
}
