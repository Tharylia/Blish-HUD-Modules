namespace Estreya.BlishHUD.Shared.Services;

using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.Shared.Extensions;
using Estreya.BlishHUD.Shared.Models;
using Estreya.BlishHUD.Shared.Models.GW2API.Commerce;
using Gw2Sharp.WebApi.V2;
using Gw2Sharp.WebApi.V2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using static Estreya.BlishHUD.Shared.Services.TradingPostService;

public class TradingPostService : APIService<TransactionMapping>
{
    private readonly ItemService _itemService;

    public TransactionMappingType Scopes { get; set; } = TransactionMappingType.Own;

    public List<Transaction> Buys => this.APIObjectList.Where(mapping => mapping.Type == TransactionMappingType.Buy).SelectMany(mapping => mapping.Transactions).ToList();
    public List<Transaction> Sells => this.APIObjectList.Where(mapping => mapping.Type == TransactionMappingType.Sell).SelectMany(mapping => mapping.Transactions).ToList();
    public List<PlayerTransaction> OwnTransactions => this.APIObjectList.Where(mapping => mapping.Type == TransactionMappingType.Own).SelectMany(mapping => mapping.Transactions.Select(transactions => transactions as PlayerTransaction)).ToList();

    public TradingPostService(APIServiceConfiguration configuration, Gw2ApiManager apiManager, ItemService itemService) :
        base(apiManager, configuration)
    {
        this._itemService = itemService;
    }

    protected override async Task<List<TransactionMapping>> Fetch(Gw2ApiManager apiManager, IProgress<string> progress, CancellationToken cancellationToken)
    {
        var loadOwn = (this.Scopes & TransactionMappingType.Own) != 0;
        var loadBuy = (this.Scopes & TransactionMappingType.Buy) != 0;
        var loadSell = (this.Scopes & TransactionMappingType.Sell) != 0;

        List<TransactionMapping> transactions = new List<TransactionMapping>();

        if (loadOwn)
        {
            progress.Report("Loading player buy orders...");
            // Buys
            IApiV2ObjectList<CommerceTransactionCurrent> apiBuys = await apiManager.Gw2ApiClient.V2.Commerce.Transactions.Current.Buys.GetAsync(cancellationToken);
            List<Transaction> buys = apiBuys.ToList().Select(x =>
            {
                Transaction transaction = new PlayerTransaction()
                {
                    ItemId = x.ItemId,
                    Price = x.Price,
                    Quantity = x.Quantity,
                    Created = x.Created.UtcDateTime,
                    Type = TransactionType.Buy
                };

                return transaction;
            }).ToList();

            if (buys.Count > 0)
            {
                transactions.Add(new TransactionMapping()
                {
                    Type = TransactionMappingType.Own,
                    Transactions = buys
                });
            }

            progress.Report("Loading player sell offers...");
            // Sells
            IApiV2ObjectList<CommerceTransactionCurrent> apiSells = await apiManager.Gw2ApiClient.V2.Commerce.Transactions.Current.Sells.GetAsync(cancellationToken);
            List<Transaction> sells = apiSells.ToList().Select(x =>
            {
                Transaction transaction = new PlayerTransaction()
                {
                    ItemId = x.ItemId,
                    Price = x.Price,
                    Quantity = x.Quantity,
                    Created = x.Created.UtcDateTime,
                    Type = TransactionType.Sell
                };

                return transaction;
            }).ToList();


            if (sells.Count > 0)
            {
                transactions.Add(new TransactionMapping()
                {
                    Type = TransactionMappingType.Own,
                    Transactions = sells
                });
            }

            IEnumerable<int> itemIds = transactions.SelectMany(transaction => transaction.Transactions.Select(transaction => transaction.ItemId)).Distinct();

            #region Is Highest
            progress.Report("Check highest transactions...");

            IReadOnlyList<CommercePrices> rawItemPriceList = await apiManager.Gw2ApiClient.V2.Commerce.Prices.ManyAsync(itemIds, cancellationToken);
            Dictionary<int, CommercePrices> itemPriceLookup = rawItemPriceList.ToDictionary(item => item.Id);

            foreach (var transactionMapping in transactions.Where(mapping => mapping.Type == TransactionMappingType.Own))
            {
                foreach (var transaction in transactionMapping.Transactions)
                {
                    if (transaction is PlayerTransaction playerTransaction)
                    {
                        switch (transaction.Type)
                        {
                            case TransactionType.Buy:
                                playerTransaction.IsHighest = itemPriceLookup[transaction.ItemId].Buys.UnitPrice == transaction.Price;
                                break;
                            case TransactionType.Sell:
                                playerTransaction.IsHighest = itemPriceLookup[transaction.ItemId].Sells.UnitPrice == transaction.Price;
                                break;
                            default:
                                break;
                        }

                    }
                }
            }
            #endregion

        }

        if (transactions.Count > 0 || loadBuy || loadSell)
        {
            var itemServiceCompleted = await this._itemService.WaitForCompletion(TimeSpan.FromMinutes(10));
            if (!itemServiceCompleted)
            {
                Logger.Warn("ItemService did not complete in the predefined timespan.");
            }
        }

        if (loadBuy || loadSell)
        {
            var tradeableItems = this._itemService.Items.Where(item => !item.Flags.Any(flag => flag is ItemFlag.AccountBound or ItemFlag.SoulbindOnAcquire)).ToList();

            #region Buys/Sells
            progress.Report("Loading global buys/sells...");

            foreach (var itemChunk in tradeableItems.ChunkBy(200))
            {
                progress.Report($"Loading global buys/sells from items {itemChunk.First().Id} - {itemChunk.Last().Id}...");
                var listings = await apiManager.Gw2ApiClient.V2.Commerce.Listings.ManyAsync(itemChunk.Select(item => item.Id), cancellationToken);

                List<Transaction> mappedListings = listings.ToList().SelectMany(itemListing =>
                {
                    List<Transaction> listingTransactions = new List<Transaction>();

                    if (loadBuy)
                    {
                        progress.Report($"Loading global buys: {itemListing.Buys.Count}...");
                        foreach (var buyListing in itemListing.Buys)
                        {
                            Transaction buyTransaction = new Transaction()
                            {
                                ItemId = itemListing.Id,
                                Price = buyListing.UnitPrice,
                                Quantity = buyListing.Quantity,
                                Created = DateTime.MinValue,
                                Type = TransactionType.Buy
                            };
                            listingTransactions.Add(buyTransaction);
                        }
                    }

                    if (loadSell)
                    {
                        progress.Report($"Loading global sells: {itemListing.Sells.Count}...");
                        foreach (var sellListing in itemListing.Sells)
                        {
                            Transaction sellTransaction = new Transaction()
                            {
                                ItemId = itemListing.Id,
                                Price = sellListing.UnitPrice,
                                Quantity = sellListing.Quantity,
                                Created = DateTime.MinValue,
                                Type = TransactionType.Sell
                            };
                            listingTransactions.Add(sellTransaction);
                        }
                    }

                    return listingTransactions;
                }).ToList();

                transactions.Add(new TransactionMapping()
                {
                    Type = TransactionMappingType.Buy,
                    Transactions = mappedListings.Where(mappedListing => mappedListing.Type == TransactionType.Buy).ToList()
                });

                transactions.Add(new TransactionMapping()
                {
                    Type = TransactionMappingType.Sell,
                    Transactions = mappedListings.Where(mappedListing => mappedListing.Type == TransactionType.Sell).ToList()
                });
            }
            #endregion
        }

        #region Set Item
        progress.Report("Loading items...");
        foreach (var transactionMapping in transactions)
        {
            foreach (var transaction in transactionMapping.Transactions)
            {
                transaction.Item = this._itemService.GetItemById(transaction.ItemId);
            }
        }
        #endregion

        return transactions;
    }

    public async Task<int> GetPriceForItem(int itemId, TransactionType transactionType)
    {
        switch (transactionType)
        {
            case TransactionType.Buy:
                var itemBuys = this.Buys.Where(buy => buy.ItemId == itemId);
                if (itemBuys.Any())
                {
                    return itemBuys.First().Price;
                }
                else
                {
                    var itemPrices = await this._apiManager.Gw2ApiClient.V2.Commerce.Prices.GetAsync(itemId);
                    return itemPrices.Buys.UnitPrice;
                }
            case TransactionType.Sell:
                var itemSells = this.Sells.Where(sell => sell.ItemId == itemId);
                if (itemSells.Any())
                {
                    return itemSells.First().Price;
                }
                else
                {
                    var itemPrices = await this._apiManager.Gw2ApiClient.V2.Commerce.Prices.GetAsync(itemId);
                    return itemPrices.Sells.UnitPrice;
                }
            default:
                throw new ArgumentException($"Invalid transaction type: {transactionType}");
        }
    }

    public enum TransactionMappingType
    {
        Sell,
        Buy,
        Own
    }

    public struct TransactionMapping
    {
        public TransactionMappingType Type;
        public List<Transaction> Transactions;
    }
}
