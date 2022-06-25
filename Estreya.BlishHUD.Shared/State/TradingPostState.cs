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

    public event EventHandler TransactionsUpdated;

    public TradingPostState(Gw2ApiManager apiManager) :
        base(apiManager,
        new List<TokenPermission>() { TokenPermission.Account, TokenPermission.Tradingpost },
        updateInterval: TimeSpan.FromMinutes(2))
    {
        this.Updated += this.TradingPostState_TransactionsUpdated;
    }

    private void TradingPostState_TransactionsUpdated(object sender, EventArgs e)
    {
        this.TransactionsUpdated?.Invoke(this, EventArgs.Empty);
    }

    protected override Task DoClear()
    {
        return Task.CompletedTask;
    }

    protected override void DoUnload()
    {
        this.Updated -= this.TradingPostState_TransactionsUpdated;
    }

    protected override Task Save()
    {
        return Task.CompletedTask;
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

        List<Task<IReadOnlyList<Item>>> itemIdTasks = new List<Task<IReadOnlyList<Item>>>();

        foreach (IEnumerable<int> itemIdChunk in itemIds.ChunkBy(200))
        {
            itemIdTasks.Add(apiManager.Gw2ApiClient.V2.Items.ManyAsync(itemIdChunk));
        }

        IReadOnlyList<Item>[] rawItemList = await Task.WhenAll(itemIdTasks);
        Dictionary<int, Item> itemLookup = rawItemList.SelectMany(rawItemList => rawItemList).ToList().ToDictionary(item => item.Id);

        transactions.ForEach(transaction =>
        {
            transaction.Item = itemLookup[transaction.ItemId];
        });

        #endregion

        #region Is Highest

        List<Task<IReadOnlyList<CommercePrices>>> itemPriceTasks = new List<Task<IReadOnlyList<CommercePrices>>>();
        foreach (IEnumerable<int> itemIdChunk in itemIds.ChunkBy(200))
        {
            itemPriceTasks.Add(apiManager.Gw2ApiClient.V2.Commerce.Prices.ManyAsync(itemIdChunk));
        }

        IReadOnlyList<CommercePrices>[] rawItemPriceList = await Task.WhenAll(itemPriceTasks);
        Dictionary<int, CommercePrices> itemPriceLookup = rawItemPriceList.SelectMany(rawItemPriceList => rawItemPriceList).ToList().ToDictionary(item => item.Id);

        transactions.ForEach(transaction =>
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
        });

        #endregion

        return transactions;
    }
}
