namespace Estreya.BlishHUD.Shared.Controls;

using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Settings;
using System;

public class TabbedWindow : Window, ITabOwner
{
    private const int TAB_VERTICALOFFSET = 40;

    private const int TAB_HEIGHT = 50;

    private const int TAB_WIDTH = 84;

    private static readonly Texture2D _textureTabActive = Content.GetTexture("window-tab-active");

    private Tab _selectedTab;

    public TabbedWindow(BaseModuleSettings baseModuleSettings, AsyncTexture2D background, Rectangle windowRegion, Rectangle contentRegion) : base(baseModuleSettings)
    {
        this.Tabs = new TabCollection(this);
        this.ShowSideBar = true;
        this.ConstructWindow(background, windowRegion, contentRegion);
    }

    public TabbedWindow(BaseModuleSettings baseModuleSettings, Texture2D background, Rectangle windowRegion, Rectangle contentRegion)
        : this(baseModuleSettings, (AsyncTexture2D)background, windowRegion, contentRegion)
    {
    }

    public TabbedWindow(BaseModuleSettings baseModuleSettings, AsyncTexture2D background, Rectangle windowRegion, Rectangle contentRegion, Point windowSize) : base(baseModuleSettings)
    {
        this.Tabs = new TabCollection(this);
        this.ShowSideBar = true;
        this.ConstructWindow(background, windowRegion, contentRegion, windowSize);
    }

    public TabbedWindow(BaseModuleSettings baseModuleSettings, Texture2D background, Rectangle windowRegion, Rectangle contentRegion, Point windowSize)
        : this(baseModuleSettings, (AsyncTexture2D)background, windowRegion, contentRegion, windowSize)
    {
    }

    private Tab HoveredTab { get; set; }

    public TabCollection Tabs { get; }

    public Tab SelectedTab
    {
        get => this._selectedTab;
        set
        {
            Tab selectedTab = this._selectedTab;
            if ((value == null || this.Tabs.Contains(value)) && this.SetProperty(ref this._selectedTab, value, true))
            {
                this.OnTabChanged(new ValueChangedEventArgs<Tab>(selectedTab, value));
            }
        }
    }

    //
    // Summary:
    //     Fires when a Blish_HUD.Controls.TabbedWindow2 Tab changes.
    public event EventHandler<ValueChangedEventArgs<Tab>> TabChanged;

    protected virtual void OnTabChanged(ValueChangedEventArgs<Tab> e)
    {
        this.SetView(e.NewValue?.View());
        if (this.Visible && e.PreviousValue != null)
        {
            Content.PlaySoundEffectByName($"tab-swap-{RandomUtil.GetRandom(1, 5)}");
        }

        this.TabChanged?.Invoke(this, e);
    }

    protected override void OnClick(MouseEventArgs e)
    {
        Tab hoveredTab = this.HoveredTab;
        if (hoveredTab != null && hoveredTab.Enabled)
        {
            this.SelectedTab = this.HoveredTab;
        }

        base.OnClick(e);
    }

    private void UpdateTabStates()
    {
        this.SideBarHeight = 40 + (50 * this.Tabs.Count);
        this.HoveredTab = this.MouseOver && this.SidebarActiveBounds.Contains(this.RelativeMousePosition) ? this.Tabs.FromIndex((this.RelativeMousePosition.Y - this.SidebarActiveBounds.Y - 40) / 50) : null;
        this.BasicTooltipText = this.HoveredTab?.Name;
    }

    public override void UpdateContainer(GameTime gameTime)
    {
        this.UpdateTabStates();
        base.UpdateContainer(gameTime);
    }

    public override void PaintAfterChildren(SpriteBatch spriteBatch, Rectangle bounds)
    {
        base.PaintAfterChildren(spriteBatch, bounds);
        int num = 0;
        foreach (Tab tab in this.Tabs)
        {
            int y = this.SidebarActiveBounds.Top + 40 + (num * 50);
            bool flag = tab == this.SelectedTab;
            bool hovered = tab == this.HoveredTab;
            if (flag)
            {
                Rectangle destinationRectangle = new Rectangle(this.SidebarActiveBounds.Left - (84 - this.SidebarActiveBounds.Width) + 2, y, 84, 50);
                spriteBatch.DrawOnCtrl(this, this.WindowBackground, destinationRectangle, new Rectangle(this.WindowRegion.Left + destinationRectangle.X, destinationRectangle.Y - (int)this.Padding.Top, destinationRectangle.Width, destinationRectangle.Height));
                spriteBatch.DrawOnCtrl(this, _textureTabActive, destinationRectangle);
            }

            tab.Draw(this, spriteBatch, new Rectangle(this.SidebarActiveBounds.X, y, this.SidebarActiveBounds.Width, 50), flag, hovered);
            num++;
        }
    }
}