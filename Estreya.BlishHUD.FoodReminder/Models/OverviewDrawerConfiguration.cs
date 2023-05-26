namespace Estreya.BlishHUD.FoodReminder.Models;

using Blish_HUD.Settings;
using Shared.Models.Drawers;

public class OverviewDrawerConfiguration : DrawerConfiguration
{
    public TableColumnSizes ColumnSizes { get; set; }

    public SettingEntry<int> HeaderHeight { get; set; }

    public SettingEntry<int> PlayerHeight { get; set; }
}