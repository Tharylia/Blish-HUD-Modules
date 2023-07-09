namespace Estreya.BlishHUD.TradingPostWatcher.Models;

using Shared.Attributes;

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