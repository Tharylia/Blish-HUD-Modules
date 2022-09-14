namespace Estreya.BlishHUD.Shared.State;

using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.Shared.Extensions;
using Estreya.BlishHUD.Shared.Models;
using Estreya.BlishHUD.Shared.Models.GW2API.Commerce;
using Gw2Sharp.WebApi.V2;
using Gw2Sharp.WebApi.V2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class TradingPostState : APIState<CurrentTransaction>
{
    public List<CurrentTransaction> Transactions => this.APIObjectList;

    public TradingPostState(APIStateConfiguration configuration, Gw2ApiManager apiManager) :
        base(apiManager, configuration)
    {
    }

    protected override async Task<List<CurrentTransaction>> Fetch(Gw2ApiManager apiManager)
    {
        List<CurrentTransaction> transactions = new List<CurrentTransaction>();

        // Buys
        IApiV2ObjectList<CommerceTransactionCurrent> apiBuys = await apiManager.Gw2ApiClient.V2.Commerce.Transactions.Current.Buys.GetAsync();
        IEnumerable<CurrentTransaction> buys = apiBuys.ToList().Select(x =>
        {
            CurrentTransaction transaction = new CurrentTransaction()
            {
                ItemId = x.ItemId,
                Price = x.Price,
                Quantity = x.Quantity,
                Created = x.Created.UtcDateTime,
                Type = TransactionType.Buy
            };

            return transaction;
        });

        transactions.AddRange(buys);

        // Sells
        IApiV2ObjectList<CommerceTransactionCurrent> apiSells = await apiManager.Gw2ApiClient.V2.Commerce.Transactions.Current.Sells.GetAsync();
        IEnumerable<CurrentTransaction> sells = apiSells.ToList().Select(x =>
        {
            CurrentTransaction transaction = new CurrentTransaction()
            {
                ItemId = x.ItemId,
                Price = x.Price,
                Quantity = x.Quantity,
                Created = x.Created.UtcDateTime,
                Type = TransactionType.Sell
            };

            return transaction;
        });

        transactions.AddRange(sells);

        IEnumerable<int> itemIds = transactions.Select(transaction => transaction.ItemId).Distinct();

        #region Set Item

        IReadOnlyList<Item> rawItemList = await apiManager.Gw2ApiClient.V2.Items.ManyAsync(itemIds);
        Dictionary<int, Item> itemLookup = rawItemList.ToDictionary(item => item.Id);

        foreach (var transaction in transactions)
        {
            transaction.Item = itemLookup[transaction.ItemId];
        }

        #endregion

        #region Is Highest

        IReadOnlyList<CommercePrices> rawItemPriceList = await apiManager.Gw2ApiClient.V2.Commerce.Prices.ManyAsync(itemIds);
        Dictionary<int, CommercePrices> itemPriceLookup = rawItemPriceList.ToDictionary(item => item.Id);

        foreach (var transaction in transactions)
        {
            switch (transaction.Type)
            {
                case TransactionType.Buy:
                    transaction.IsHighest = itemPriceLookup[transaction.ItemId].Buys.UnitPrice == transaction.Price;
                    break;
                case TransactionType.Sell:
                    transaction.IsHighest = itemPriceLookup[transaction.ItemId].Sells.UnitPrice == transaction.Price;
                    break;
                default:
                    break;
            }
        }

        #endregion

        return transactions;
    }
}
