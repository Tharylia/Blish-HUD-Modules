namespace Estreya.BlishHUD.FoodReminder.Models;

using Blish_HUD.ArcDps.Common;
using System;

public class Player
{
    private FoodDefinition _food;

    private DateTimeOffset _foodUpdatedAt;

    private UtilityDefinition _utility;

    private DateTimeOffset _utilityUpdatedAt;

    public Player(string name)
    {
        this.Name = name;
    }

    public string Name { get; private set; }

    public FoodDefinition Food
    {
        get => this._food;
        set
        {
            this._food = value;
            this._foodUpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    public UtilityDefinition Utility
    {
        get => this._utility;
        set
        {
            this._utility = value;
            this._utilityUpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    public bool Reinforced { get; set; }

    public bool Tracked { get; set; }

    public CommonFields.Player? ArcDPSPlayer { get; set; }

    public bool IsFoodRemoveable => this._food != null && DateTimeOffset.UtcNow - this._foodUpdatedAt >= TimeSpan.FromMilliseconds(500); // Remove events can be fired after add.
    public bool IsUtilityRemoveable => this._utility != null && DateTimeOffset.UtcNow - this._utilityUpdatedAt >= TimeSpan.FromMilliseconds(500); // Remove events can be fired after add.

    public void Clear()
    {
        this.Food = null;
        this.Utility = null;
        this.Reinforced = false;
        this.ArcDPSPlayer = null;
    }
}