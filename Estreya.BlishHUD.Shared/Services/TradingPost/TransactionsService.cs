namespace Estreya.BlishHUD.Shared.Services.TradingPost;

using Blish_HUD.Modules.Managers;
using Extensions;
using Gw2Sharp.WebApi.V2;
using Gw2Sharp.WebApi.V2.Models;
using Models.GW2API.Commerce;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;
using Item = Models.GW2API.Items.Item;

public class TransactionsService : APIService<Transaction>
{
    private readonly ItemService _itemService;

    private readonly SynchronizedCollection<int> _subscribedItemIds = new SynchronizedCollection<int>();

    public List<Transaction> Buys => this.APIObjectList?.Where(x => x.Type == TransactionType.Buy).ToList();
    public List<Transaction> Sells => this.APIObjectList?.Where(x => x.Type == TransactionType.Sell).ToList();
    public List<Transaction> Transactions => this.APIObjectList?.ToArray().ToList();

    public TransactionsService(APIServiceConfiguration configuration, ItemService itemService, Gw2ApiManager apiManager) : base(apiManager, configuration)
    {
        this._itemService = itemService;
    }

    public async Task AddItemSubscribtions(params int[] ids)
    {
        foreach (var id in ids)
        {
            if (this._subscribedItemIds.Contains(id)) continue;

            _ = await this._apiManager.Gw2ApiClient.V2.Commerce.Listings.GetAsync(id);
            this._subscribedItemIds.Add(id);

            this.Logger.Debug($"Added item {id} to subscribers.");
        }

        await this.Reload();
    }

    protected override async Task<List<Transaction>> Fetch(Gw2ApiManager apiManager, IProgress<string> progress, CancellationToken cancellationToken)
    {
        var tradeableItems = this._subscribedItemIds.ToArray().ToList();
        //this._itemService.Items.Where(item => !item.Flags.Any(flag => flag is ItemFlag.AccountBound or ItemFlag.SoulbindOnAcquire)).ToList();

        var loadedItemIds = 0;
        progress.Report($"Loading buys/sells for {loadedItemIds}/{tradeableItems.Count} items: 0 transactions.");

        var itemChunks = tradeableItems.OrderBy(x => x).ChunkBy(200).ToList();

        var parallelItemChunks = itemChunks.ChunkBy(1).ToList();

        List<Transaction> transactions = new List<Transaction>();
        foreach (var parallelItemChunk in parallelItemChunks)
        {
            var itemChunkLoadTasks = parallelItemChunk.Select(x => this.LoadTransactions(x, apiManager, cancellationToken)).ToArray();
            var itemChunkTransactions = await Task.WhenAll(itemChunkLoadTasks);

            transactions.AddRange(itemChunkTransactions.SelectMany(x => x));
            Interlocked.Add(ref loadedItemIds, parallelItemChunk.SelectMany(x => x).Count());
            progress.Report($"Loading buys/sells for {loadedItemIds}/{tradeableItems.Count} items: {transactions.Count} transactions.");

        }

        bool loadItems = transactions.Count > 0;

        if (loadItems)
        {
            progress.Report($"Waiting for {this._itemService.GetType().Name} to complete...");
            bool itemServiceCompleted = await this._itemService.WaitForCompletion(TimeSpan.FromMinutes(10));
            if (!itemServiceCompleted)
            {
                loadItems = false;
                this.Logger.Warn("ItemService did not complete in the predefined timespan.");
            }
        }

        #region Set Item
        if (loadItems)
        {
            progress.Report("Loading items...");
            var itemGroups = transactions.GroupBy(x => x.ItemId);
            foreach (var itemGroup in itemGroups)
            {
                var item = this._itemService.GetItemById(itemGroup.Key);
                foreach (var transaction in itemGroup)
                {
                    transaction.Item = item;
                }
            }
        }
        #endregion

        return transactions;
    }

    private async Task<List<Transaction>> LoadTransactions(IEnumerable<int> ids, Gw2ApiManager apiManager, CancellationToken cancellationToken)
    {
        var transactions = new List<Transaction>();
        try
        {
            IReadOnlyList<CommerceListings> listings = await apiManager.Gw2ApiClient.V2.Commerce.Listings.ManyAsync(ids, cancellationToken);

            List<Transaction> mappedListings = listings.ToList().SelectMany(itemListing =>
            {
                List<Transaction> listingTransactions = new List<Transaction>();

                foreach (CommerceListing buyListing in itemListing.Buys)
                {
                    Transaction buyTransaction = new Transaction
                    {
                        ItemId = itemListing.Id,
                        Price = buyListing.UnitPrice,
                        Quantity = buyListing.Quantity,
                        Created = DateTime.MinValue,
                        Type = TransactionType.Buy
                    };
                    listingTransactions.Add(buyTransaction);
                }

                foreach (CommerceListing buyListing in itemListing.Sells)
                {
                    Transaction buyTransaction = new Transaction
                    {
                        ItemId = itemListing.Id,
                        Price = buyListing.UnitPrice,
                        Quantity = buyListing.Quantity,
                        Created = DateTime.MinValue,
                        Type = TransactionType.Sell
                    };
                    listingTransactions.Add(buyTransaction);
                }

                return listingTransactions;
            }).ToList();

            transactions.AddRange(mappedListings);
        }
        catch (Exception ex)
        {
            this.Logger.Warn(ex, $"Could not load global buys from items {ids.First()} - {ids.Last()}.");
        }

        return transactions;
    }

    public PriceRange GetBuyPricesForItem(int itemId)
    {
        IEnumerable<Transaction> transactions = this.Buys.Where(t => t.ItemId == itemId).OrderBy(b => b.Price);
        return transactions.Any()
            ? new PriceRange
            {
                Lowest = transactions.First().Price,
                Highest = transactions.Last().Price
            }
            : new PriceRange()
            {
                Lowest = 0,
                Highest = 0
            };
    }

    public int GetBuyQuantity(int itemId)
    {
        IEnumerable<Transaction> transactions = this.Buys.Where(t => t.ItemId == itemId).OrderBy(b => b.Price);
        return transactions.Sum(t => t.Quantity);
    }

    public int GetLowestBuyQuantity(int itemId)
    {
        IEnumerable<Transaction> transactions = this.Buys.Where(t => t.ItemId == itemId).OrderBy(b => b.Price);
        return transactions.FirstOrDefault()?.Quantity ?? 0;
    }

    public int GetHighestBuyQuantity(int itemId)
    {
        IEnumerable<Transaction> transactions = this.Buys.Where(t => t.ItemId == itemId).OrderBy(b => b.Price);
        return transactions.LastOrDefault()?.Quantity ?? 0;
    }

    public PriceRange GetSellPricesForItem(int itemId)
    {
        IEnumerable<Transaction> transactions = this.Sells.Where(t => t.ItemId == itemId).OrderBy(b => b.Price);
        return transactions.Any()
            ? new PriceRange
            {
                Lowest = transactions.First().Price,
                Highest = transactions.Last().Price
            }
            : new PriceRange()
            {
                Lowest = 0,
                Highest = 0
            };
    }

    public int GetSellQuantity(int itemId)
    {
        IEnumerable<Transaction> transactions = this.Sells.Where(t => t.ItemId == itemId).OrderBy(b => b.Price);
        return transactions.Sum(t => t.Quantity);
    }

    public int GetLowestSellQuantity(int itemId)
    {
        IEnumerable<Transaction> transactions = this.Sells.Where(s => s.ItemId == itemId).OrderBy(b => b.Price);
        return transactions.FirstOrDefault()?.Quantity ?? 0;
    }

    public int GetHighestSellQuantity(int itemId)
    {
        IEnumerable<Transaction> transactions = this.Sells.Where(s => s.ItemId == itemId).OrderBy(b => b.Price);
        return transactions.LastOrDefault()?.Quantity ?? 0;
    }
}