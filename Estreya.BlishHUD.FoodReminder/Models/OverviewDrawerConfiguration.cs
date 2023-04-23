namespace Estreya.BlishHUD.FoodReminder.Models;

using Blish_HUD.Settings;
using Estreya.BlishHUD.Shared.Models.Drawers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class OverviewDrawerConfiguration : DrawerConfiguration
{
    public TableColumnSizes ColumnSizes { get; set; }

    public SettingEntry<int> HeaderHeight { get; set; }

    public SettingEntry<int> PlayerHeight { get; set; }
}
