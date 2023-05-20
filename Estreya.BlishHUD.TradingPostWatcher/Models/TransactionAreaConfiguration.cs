namespace Estreya.BlishHUD.TradingPostWatcher.Models
{

    using Blish_HUD.Settings;
    using Estreya.BlishHUD.Shared.Models.Drawers;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

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

        public SettingEntry<Gw2Sharp.WebApi.V2.Models.Color> HighestTransactionColor { get; set; }
        public SettingEntry<Gw2Sharp.WebApi.V2.Models.Color> OutbidTransactionColor { get; set; }

        public SettingEntry<int> TransactionHeight { get; set; }

        public SettingEntry<bool> ShowNoDataInfo { get; set; }

        public SettingEntry<Gw2Sharp.WebApi.V2.Models.Color> NoDataTextColor { get; set; }

        public SettingEntry<int> NoDataHeight { get; set; }
    }
}