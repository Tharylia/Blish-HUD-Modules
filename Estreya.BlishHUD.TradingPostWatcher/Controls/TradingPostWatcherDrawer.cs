namespace Estreya.BlishHUD.TradingPostWatcher.Controls;

using Blish_HUD;
using Blish_HUD._Extensions;
using Blish_HUD.Controls;
using Estreya.BlishHUD.Shared.Controls;
using Estreya.BlishHUD.Shared.Models;
using Estreya.BlishHUD.Shared.Utils;
using Glide;
using Humanizer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class TradingPostWatcherDrawer : FlowPanel
{
    private bool _currentVisibilityDirection = false;
    private Tween _currentVisibilityAnimation { get; set; }

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

    public void UpdateBackgroundColor()
    {
        Color backgroundColor = Color.Transparent;
        if (TradingPostWatcherModule.ModuleInstance.ModuleSettings.BackgroundColor.Value != null && TradingPostWatcherModule.ModuleInstance.ModuleSettings.BackgroundColor.Value.Id != 1)
        {
            backgroundColor = TradingPostWatcherModule.ModuleInstance.ModuleSettings.BackgroundColor.Value.Cloth.ToXnaColor();
        }

        this.BackgroundColor = backgroundColor * TradingPostWatcherModule.ModuleInstance.ModuleSettings.BackgroundColorOpacity.Value;
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
        bool buildFromBottom = TradingPostWatcherModule.ModuleInstance.ModuleSettings.BuildDirection.Value == BuildDirection.Bottom;

        this.Location = buildFromBottom ? new Point(x, y - this.Height) : new Point(x, y);
    }
}
