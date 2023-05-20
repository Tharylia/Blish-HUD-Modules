namespace Estreya.BlishHUD.TradingPostWatcher.Models;

using Estreya.BlishHUD.Shared.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public enum TrackedTransactionType
{
    [Translation("trackedTransactionType-buyGT", "Buy >=")]
    BuyGT,

    [Translation("trackedTransactionType-buyLT", "Buy <=")]
    BuyLT,

    [Translation("trackedTransactionType-sellGT", "Sell >=")]
    SellGT,

    [Translation("trackedTransactionType-sellLT", "Sell <=")]
    SellLT
}
