namespace Estreya.BlishHUD.FoodReminder.Controls;

using Blish_HUD.Controls;
using Estreya.BlishHUD.FoodReminder.Models;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public abstract class TableControl : Control
{
    private readonly TableColumnSizes _sizes;

    public TableControl(TableColumnSizes sizes)
    {
        this._sizes = sizes;
    }

    public override void DoUpdate(GameTime gameTime)
    {
            this.Width = (int)(this._sizes.Name.Value + this._sizes.Food.Value + this._sizes.Utility.Value + this._sizes.Reinforced.Value);
    }
}
