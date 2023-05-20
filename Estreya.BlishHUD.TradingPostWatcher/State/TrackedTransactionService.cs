namespace Estreya.BlishHUD.TradingPostWatcher.Service
{

    using Blish_HUD;
    using Blish_HUD.Modules.Managers;
    using Estreya.BlishHUD.Shared.Extensions;
    using Estreya.BlishHUD.Shared.Models.GW2API.Commerce;
    using Estreya.BlishHUD.Shared.Services;
    using Estreya.BlishHUD.Shared.Utils;
    using Estreya.BlishHUD.TradingPostWatcher.Models;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public class TrackedTransactionService : APIService<TrackedTransaction>
    {
        private const string FOLDER_NAME = "tracked";
        private const string FILE_NAME = "transactions.txt";
        private const string COLUMN_SPLIT = "<-->";
        private const string DATE_TIME_FORMAT = "yyyy-MM-ddTHH:mm:ssZ";

        private List<TrackedTransaction> _trackedTransactions = new List<TrackedTransaction>();
        private AsyncLock _transactionLock = new AsyncLock();
        private readonly ItemService _itemService;
        private readonly string _baseFolder;

        private bool _loadedFiles = false;

        private string FullFolderPath => Path.Combine(this._baseFolder, FOLDER_NAME);

        public List<TrackedTransaction> TrackedTransactions => this._trackedTransactions;
        public List<TrackedTransaction> BestPriceTransactions => this.APIObjectList;

        public event EventHandler<TrackedTransaction> TransactionEnteredRange;
        public event EventHandler<TrackedTransaction> TransactionLeftRange;

        public TrackedTransactionService(APIServiceConfiguration configuration, Gw2ApiManager apiManager, ItemService itemService, string baseFolder) : base(apiManager, configuration)
        {
            this.APIObjectAdded += this.TrackedTransactionService_APIObjectAdded;
            this.APIObjectRemoved += this.TrackedTransactionService_APIObjectRemoved;
            this._itemService = itemService;
            this._baseFolder = baseFolder;
        }

        private void TrackedTransactionService_APIObjectRemoved(object sender, TrackedTransaction e)
        {
            this.TransactionLeftRange?.Invoke(this, e);
        }

        private void TrackedTransactionService_APIObjectAdded(object sender, TrackedTransaction e)
        {
            this.TransactionEnteredRange?.Invoke(this, e);
        }

        protected override Task DoClear()
        {
            using (this._transactionLock.Lock())
            {
                _trackedTransactions.Clear();
            }

            return Task.CompletedTask;
        }

        protected override void DoUnload()
        {
            this.APIObjectAdded -= this.TrackedTransactionService_APIObjectAdded;
            this.APIObjectRemoved -= this.TrackedTransactionService_APIObjectRemoved;
        }

        protected override async Task Load()
        {
            if (!this._loadedFiles)
            {
                List<string> trackedTransactionLines = new List<string>();
                if (File.Exists(Path.Combine(this.FullFolderPath, FILE_NAME)))
                {
                    var lines = await FileUtil.ReadLinesAsync(Path.Combine(this.FullFolderPath, FILE_NAME));
                    if (lines.Length > 0)
                    {
                        trackedTransactionLines.AddRange(lines);
                    }
                }

                if (trackedTransactionLines.Count > 0)
                {
                    foreach (string transactionLine in trackedTransactionLines)
                    {
                        string[] parts = transactionLine.Split(new string[] { COLUMN_SPLIT }, StringSplitOptions.None);

                        if (parts.Length == 0)
                        {
                            Logger.Warn("Line empty.");
                            continue;
                        }

                        string id = parts[0];
                        try
                        {
                            TrackedTransactionType type = (TrackedTransactionType)Enum.Parse(typeof(TrackedTransactionType), parts[1]);
                            int price = int.Parse(parts[2]);
                            DateTime created = DateTime.SpecifyKind(DateTime.ParseExact(parts[3], DATE_TIME_FORMAT, CultureInfo.InvariantCulture), DateTimeKind.Utc);

                            _ = await this.Add(int.Parse(id), price, type, created);
                        }
                        catch (Exception ex)
                        {
                            Logger.Warn(ex, "Could not load tracked transaction {0}", id);
                        }
                    }
                }

                this._loadedFiles = true;
            }

            await base.Load();
        }

        protected override async Task Save()
        {
            using (await this._transactionLock.LockAsync())
            {
                List<string> lines = new List<string>();

                foreach (TrackedTransaction trackedTransaction in this._trackedTransactions)
                {
                    lines.Add($"{trackedTransaction.ItemId}{COLUMN_SPLIT}{trackedTransaction.Type}{COLUMN_SPLIT}{trackedTransaction.WishPrice}{COLUMN_SPLIT}{trackedTransaction.Created.ToString(DATE_TIME_FORMAT)}");
                }

                await FileUtil.WriteLinesAsync(Path.Combine(this.FullFolderPath, FILE_NAME), lines.ToArray());
            }
        }

        private async Task<bool> Add(int id, int price, TrackedTransactionType type, DateTime created)
        {
            this.Remove(id, type);

            using (this._transactionLock.Lock())
            {
                try
                {
                    if (!await this._itemService.WaitForCompletion(TimeSpan.FromSeconds(2)))
                    {
                        Logger.Warn("ItemService did not complete loading.");
                        return false;
                    }

                    var item = this._itemService.GetItemById(id);

                    this._trackedTransactions.Add(new TrackedTransaction()
                    {
                        ItemId = id,
                        WishPrice = price,
                        Created = created.ToUniversalTime(),
                        Item = item,
                        Type = type
                    });

                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, "Could not add item {0}", id);
                    return false;
                }
            }
        }

        public async Task<bool> Add(int id, int price, TrackedTransactionType type)
        {
            return await this.Add(id, price, type, DateTime.UtcNow);
        }

        public void Remove(int id, TrackedTransactionType type)
        {
            using (this._transactionLock.Lock())
            {
                List<TrackedTransaction> transactionsToRemove = this._trackedTransactions.Where(t => t.ItemId == id && t.Type == type).ToList();

                if (transactionsToRemove.Count == 0)
                {
                    return;
                }

                for (int i = transactionsToRemove.Count - 1; i >= 0; i--)
                {
                    _ = this._trackedTransactions.Remove(transactionsToRemove[i]);
                }
            }
        }

        protected override async Task<List<TrackedTransaction>> Fetch(Gw2ApiManager apiManager, IProgress<string> progress, CancellationToken cancellationToken)
        {
            List<TrackedTransaction> transactions = new List<TrackedTransaction>();

            using (await this._transactionLock.LockAsync())
            {
                foreach (TrackedTransaction transaction in this._trackedTransactions)
                {
                    Gw2Sharp.WebApi.V2.Models.CommercePrices prices = await apiManager.Gw2ApiClient.V2.Commerce.Prices.GetAsync(transaction.ItemId, cancellationToken);

                    switch (transaction.Type)
                    {
                        case TrackedTransactionType.BuyGT:
                            transaction.ActualPrice = prices.Buys.UnitPrice; // Highest buy order

                            if (prices.Buys.UnitPrice >= transaction.WishPrice)
                            {
                                transactions.Add(transaction);
                            }

                            break;
                            case TrackedTransactionType.BuyLT:
                            transaction.ActualPrice = prices.Buys.UnitPrice; // Highest buy order

                            if (prices.Buys.UnitPrice <= transaction.WishPrice)
                            {
                                transactions.Add(transaction);
                            }

                            break;
                        case TrackedTransactionType.SellGT:
                            transaction.ActualPrice = prices.Sells.UnitPrice; // Lowest sell offer

                            if (prices.Sells.UnitPrice >= transaction.WishPrice)
                            {
                                transactions.Add(transaction);
                            }

                            break;
                        case TrackedTransactionType.SellLT:
                            transaction.ActualPrice = prices.Sells.UnitPrice; // Lowest sell offer

                            if (prices.Sells.UnitPrice <= transaction.WishPrice)
                            {
                                transactions.Add(transaction);
                            }

                            break;
                        default:
                            break;
                    }
                }
            }

            return transactions;
        }
    }
}