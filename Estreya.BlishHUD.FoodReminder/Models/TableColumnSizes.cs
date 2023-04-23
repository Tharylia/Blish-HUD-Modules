namespace Estreya.BlishHUD.FoodReminder.Models;

using Blish_HUD.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public struct TableColumnSizes
{
    public SettingEntry<float> Name { get; set; }

    public SettingEntry<float> Food { get; set; }

    public SettingEntry<float> Utility { get; set; }

    public SettingEntry<float> Reinforced { get; set; }
}
