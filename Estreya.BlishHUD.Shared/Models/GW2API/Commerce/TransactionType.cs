namespace Estreya.BlishHUD.Shared.Models.GW2API.Commerce;

using Estreya.BlishHUD.Shared.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public enum TransactionType
{
    [Translation("transactionType-buy", "Buy")]
    Buy,

    [Translation("transactionType-sell", "Sell")]
    Sell,
}
