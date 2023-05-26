namespace Estreya.BlishHUD.TradingPostWatcher.Models;

using Blish_HUD.Settings;
using Gw2Sharp.WebApi.V2.Models;
using Shared.Models.Drawers;

public class TransactionAreaConfiguration : DrawerConfiguration
{
    public SettingEntry<int> MaxTransactions { get; set; }
    public SettingEntry<bool> ShowBuyTransactions { get; set; }
    public SettingEntry<bool> ShowSellTransactions { get; set; }
    public SettingEntry<bool> ShowHighestTransactions { get; set; }
    public SettingEntry<bool> ShowPrice { get; set; }
    public SettingEntry<bool> ShowPriceAsTotal { get; set; }
    public SettingEntry<bool> ShowRemaining { get; set; }
    public SettingEntry<bool> ShowCreated { get; set; }

    public SettingEntry<bool> ShowTooltips { get; set; }

    public SettingEntry<Color> HighestTransactionColor { get; set; }
    public SettingEntry<Color> OutbidTransactionColor { get; set; }

    public SettingEntry<int> TransactionHeight { get; set; }

    public SettingEntry<bool> ShowNoDataInfo { get; set; }

    public SettingEntry<Color> NoDataTextColor { get; set; }

    public SettingEntry<int> NoDataHeight { get; set; }
}