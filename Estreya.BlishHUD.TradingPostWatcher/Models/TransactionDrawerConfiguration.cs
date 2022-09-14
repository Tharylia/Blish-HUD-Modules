namespace Estreya.BlishHUD.TradingPostWatcher.Models;

using Blish_HUD.Settings;
using Estreya.BlishHUD.Shared.Models.Drawers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class TransactionDrawerConfiguration : DrawerConfiguration
{
    public SettingEntry<int> MaxTransactions { get; init; }
    public SettingEntry<bool> ShowBuyTransactions { get; init; }
    public SettingEntry<bool> ShowSellTransactions { get; init; }
    public SettingEntry<bool> ShowHighestTransactions { get; init; }
    public SettingEntry<bool> ShowPrice { get; init; }
    public SettingEntry<bool> ShowPriceAsTotal { get; init; }
    public SettingEntry<bool> ShowRemaining { get; init; }
    public SettingEntry<bool> ShowCreated { get; init; }
}
