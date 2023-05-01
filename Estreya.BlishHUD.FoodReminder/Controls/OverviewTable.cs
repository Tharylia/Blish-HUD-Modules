namespace Estreya.BlishHUD.FoodReminder.Controls;

using Blish_HUD;
using Blish_HUD._Extensions;
using Blish_HUD.Controls;
using Estreya.BlishHUD.FoodReminder.Models;
using Estreya.BlishHUD.Shared.Extensions;
using Estreya.BlishHUD.Shared.Utils;
using Microsoft.Xna.Framework;
using MonoGame.Extended.BitmapFonts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Blish_HUD.ContentService;

public class OverviewTable : FlowPanel
{
    public OverviewDrawerConfiguration Configuration;
    private readonly Func<List<Models.Player>> _getPlayers;

    private static TimeSpan _sortingInterval = TimeSpan.FromSeconds(2);
    private double _lastSorted = 0;

    private List<Controls.Player> _playerControls = new List<Player>();

    private Header _header;

    private ConcurrentDictionary<FontSize, BitmapFont> _fonts = new ConcurrentDictionary<FontSize, BitmapFont>();

    public OverviewTable(OverviewDrawerConfiguration configuration, Func<List<Models.Player>> getPlayers)
    {
        this.FlowDirection = ControlFlowDirection.SingleTopToBottom;
        this.CanScroll = true;
        this.Configuration = configuration;
        this._getPlayers = getPlayers;

        this.Configuration.Location.X.SettingChanged += this.Location_SettingChanged;
        this.Configuration.Location.Y.SettingChanged += this.Location_SettingChanged;

        this.Configuration.Size.Y.SettingChanged += this.Size_SettingChanged;

        this.Configuration.BackgroundColor.SettingChanged += this.BackgroundColor_SettingChanged;

        this.Location_SettingChanged(this, null);
        this.Size_SettingChanged(this, null);
        this.BackgroundColor_SettingChanged(this, null);

        

        this.AddHeader();
    }

    private void BackgroundColor_SettingChanged(object sender, ValueChangedEventArgs<Gw2Sharp.WebApi.V2.Models.Color> e)
    {
        this.BackgroundColor = this.Configuration.BackgroundColor.Value.Id == 1
            ? Color.Transparent
            : this.Configuration.BackgroundColor.Value.Cloth.ToXnaColor();
    }

    private void Size_SettingChanged(object sender, ValueChangedEventArgs<int> e)
    {
        this.Size = new Point(this.Width, this.Configuration.Size.Y.Value);
    }

    private void Location_SettingChanged(object sender, ValueChangedEventArgs<int> e)
    {
        this.Location = new Point(this.Configuration.Location.X.Value, this.Configuration.Location.Y.Value);
    }

    private void AddHeader()
    {
        this._header = new Header(
            this.Configuration.ColumnSizes,
            this.GetFont,
            () => this.Configuration.HeaderHeight.Value,
            this.GetTextColor)
        {
            Parent = this
        };
    }

    private BitmapFont GetFont()
    {
        return this._fonts.GetOrAdd(this.Configuration.FontSize.Value, fontSize => GameService.Content.GetFont(FontFace.Menomonia, fontSize, FontStyle.Regular));
    }

    private Color GetTextColor()
    {
        return this.Configuration.TextColor.Value.Id == 1
        ? Color.Black
        : this.Configuration.TextColor.Value.Cloth.ToXnaColor();
    }

    public override void UpdateContainer(GameTime gameTime)
    {
        this.WidthSizingMode = SizingMode.AutoSize;

        var allPlayers = _getPlayers().Where(p => p.Tracked).ToList();

        var missing = allPlayers.Where(mp => !this._playerControls.Any(cp => cp.Model.Name == mp.Name)).ToList();

        var leftover = this._playerControls.Where(cp => !allPlayers.Any(mp => mp.Name == cp.Model.Name)).ToList();

        foreach (var player in missing)
        {
            this._playerControls.Add(new Player(
                player,
                this.Configuration.ColumnSizes,
                this.GetFont,
                () => this.Configuration.PlayerHeight.Value,
                this.GetTextColor)
            {
                Parent = this
            });
        }

        foreach (var player in leftover)
        {
            player?.Dispose();
            this._playerControls?.Remove(player);
        }

        UpdateUtil.Update(() => this.SortTable(allPlayers), gameTime, _sortingInterval.TotalMilliseconds, ref this._lastSorted);
    }

    private void SortTable(List<Models.Player> allPlayers)
    {
        var sortType = SortingType.Alphabetical;

        List<Models.Player> sortedPlayers = null;

        switch (sortType)
        {
            case SortingType.Alphabetical:
                sortedPlayers = new List<Models.Player>(allPlayers.OrderBy(p => p.Name));
                break;
            case SortingType.FoodOrUtility:
                sortedPlayers = new List<Models.Player>(allPlayers.OrderByDescending(p => p.Food != null || p.Utility != null));
                break;
            case SortingType.FoodAndUtilityAndReinforced:
                sortedPlayers = new List<Models.Player>(allPlayers.OrderByDescending(p => p.Food != null && p.Utility != null && p.Reinforced));
                break;
        }

        this.SortChildren(new Comparison<Player>((a, b) =>
        {
            var aIndex = sortedPlayers?.IndexOf(a.Model) ?? 0;
            var bIndex = sortedPlayers?.IndexOf(b.Model) ?? 0;

            if (aIndex < bIndex) return -1;
            if (aIndex > bIndex) return 1;

            return 0;
        }));
    }

    //
    // Zusammenfassung:
    //     Sorts children of the flow panel using the provided comparison function.
    //
    // Parameter:
    //   comparison:
    //
    // Typparameter:
    //   TControl:
    public new void SortChildren<TControl>(Comparison<TControl> comparison) where TControl : Blish_HUD.Controls.Control
    {
        List<TControl> list = _children.Where(c => c is TControl).Cast<TControl>().ToList();
        list.Sort(comparison);
        var children = new ControlCollection<Blish_HUD.Controls.Control>(list);
        children.Insert(0, _children[0]); // Insert header
        _children = children;
        this.Invalidate();
    }
}
