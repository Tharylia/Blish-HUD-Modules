namespace Estreya.BlishHUD.Shared.Services.TradingPost
{
    using Blish_HUD.Modules.Managers;
    using Estreya.BlishHUD.Shared.Models.GW2API.Commerce;
    using Gw2Sharp.WebApi.V2.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class PlayerTransactionsService : APIService<PlayerTransaction>
    {
        private readonly ItemService _itemService;
        public List<PlayerTransaction> Buys => this.APIObjectList?.Where(x => x.Type == TransactionType.Buy).ToList();
        public List<PlayerTransaction> Sells => this.APIObjectList?.Where(x => x.Type == TransactionType.Sell).ToList();
        public List<PlayerTransaction> Transactions => this.APIObjectList?.ToArray().ToList();

        public PlayerTransactionsService(APIServiceConfiguration configuration, ItemService itemService, Gw2ApiManager apiManager) : base(apiManager, configuration)
        {
            this._itemService = itemService;
        }

        protected override async Task<List<PlayerTransaction>> Fetch(Gw2ApiManager apiManager, IProgress<string> progress, CancellationToken cancellationToken)
        {
            var transactions = new List<PlayerTransaction>();

            progress.Report("Loading player buy orders...");
            // Buys
            var apiBuys = await apiManager.Gw2ApiClient.V2.Commerce.Transactions.Current.Buys.GetAsync(cancellationToken);
            List<PlayerTransaction> buys = apiBuys.ToList().Select(x =>
            {
                PlayerTransaction transaction = new PlayerTransaction
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
                transactions.AddRange(buys);
            }

            progress.Report("Loading player sell offers...");
            // Sells
            var apiSells = await apiManager.Gw2ApiClient.V2.Commerce.Transactions.Current.Sells.GetAsync(cancellationToken);
            List<PlayerTransaction> sells = apiSells.ToList().Select(x =>
            {
                PlayerTransaction transaction = new PlayerTransaction
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
                transactions.AddRange(sells);
            }

            IEnumerable<int> itemIds = transactions.Select(t => t.ItemId).Distinct();

            #region Is Highest
            progress.Report("Check highest transactions...");

            IReadOnlyList<CommercePrices> rawItemPriceList = await apiManager.Gw2ApiClient.V2.Commerce.Prices.ManyAsync(itemIds, cancellationToken);
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
                }
            }
            #endregion

            if (transactions.Count > 0)
            {
                progress.Report($"Waiting for {this._itemService.GetType().Name} to complete...");
                bool itemServiceCompleted = await this._itemService.WaitForCompletion(TimeSpan.FromMinutes(10));
                if (!itemServiceCompleted)
                {
                    this.Logger.Warn("ItemService did not complete in the predefined timespan.");
                }
            }

            #region Set Item
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
            #endregion


            return transactions;
        }
    }
}
