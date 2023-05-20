namespace Estreya.BlishHUD.TradingPostWatcher
{
    using Blish_HUD;
    using Blish_HUD.Content;
    using Blish_HUD.Controls;
    using Blish_HUD.Modules;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.Shared.Helpers;
    using Estreya.BlishHUD.Shared.Models.GW2API.Commerce;
    using Estreya.BlishHUD.Shared.Modules;
    using Estreya.BlishHUD.Shared.Settings;
    using Estreya.BlishHUD.Shared.Utils;
    using Estreya.BlishHUD.TradingPostWatcher.Controls;
    using Estreya.BlishHUD.TradingPostWatcher.Models;
    using Estreya.BlishHUD.TradingPostWatcher.Service;
    using Estreya.BlishHUD.TradingPostWatcher.UI.Views;
    using Humanizer;
    using Microsoft.Xna.Framework;
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel.Composition;
    using System.Threading;
    using System.Threading.Tasks;
    using Estreya.BlishHUD.Shared.Services;
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    using System.Linq;
    using Estreya.BlishHUD.Shared.Extensions;

    [Export(typeof(Blish_HUD.Modules.Module))]
    public class TradingPostWatcherModule : BaseModule<TradingPostWatcherModule, ModuleSettings>
    {
        public override string UrlModuleName => "trading-post-watcher";

        private ConcurrentDictionary<string, TransactionArea> _areas;

        private TrackedTransactionView _trackedTransactionView;

        #region Services
        public TrackedTransactionService TrackedTransactionService { get; private set; }

        protected override string API_VERSION_NO => "1";
        #endregion

        [ImportingConstructor]
        public TradingPostWatcherModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) { }

        protected override BaseModuleSettings DefineModuleSettings(SettingCollection settings) => new ModuleSettings(settings);

        protected override string GetDirectoryName() => "tradingpost";

        protected override AsyncTexture2D GetEmblem() => this.IconService?.GetIcon("102495.png");

        protected override AsyncTexture2D GetCornerIcon() => this.IconService?.GetIcon("255379.png");

        protected override void Initialize()
        {
            base.Initialize();
            this._areas = new ConcurrentDictionary<string, TransactionArea>();
        }

        protected override async Task LoadAsync()
        {
            await base.LoadAsync();

            this.AddAllAreas();
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            this.ToggleContainers(this.ShowUI);

            foreach (TransactionArea area in this._areas.Values)
            {
                this.ModuleSettings.CheckDrawerSizeAndPosition(area.Configuration);
            }
        }

        protected override void ConfigureServices(ServiceConfigurations configurations)
        {
            configurations.TradingPost.Enabled = true;
            configurations.TradingPost.AwaitLoading = false;

            configurations.Items.Enabled = true;
            configurations.Items.AwaitLoading = false;
        }


        private void TradingPostService_TransactionsUpdated(object sender, EventArgs e)
        {
            foreach (var transactionArea in this._areas.Values)
            {
                transactionArea?.ClearTransactions();
                transactionArea?.AddTransactions(this.TradingPostService.OwnTransactions);
            }
        }

        protected override Collection<ManagedService> GetAdditionalServices(string directoryPath)
        {
            Collection<ManagedService> states = new Collection<ManagedService>();

            this.TrackedTransactionService = new TrackedTransactionService(new APIServiceConfiguration()
            {
                AwaitLoading = false,
                Enabled = true,
                SaveInterval = TimeSpan.FromSeconds(30),
                UpdateInterval = TimeSpan.FromSeconds(30)
            }, this.Gw2ApiManager, this.ItemService, directoryPath);
            this.TrackedTransactionService.TransactionEnteredRange += this.TrackedTransactionService_TransactionEnteredRange;
            this.TrackedTransactionService.TransactionLeftRange += this.TrackedTransactionService_TransactionLeftRange;

            states.Add(this.TrackedTransactionService);

            return states;
        }

        protected override void OnBeforeServicesStarted()
        {
            this.TradingPostService.Updated += this.TradingPostService_TransactionsUpdated;
        }

        protected override void OnSettingWindowBuild(TabbedWindow2 settingWindow)
        {
            this.SettingsWindow.Tabs.Add(new Tab(this.IconService.GetIcon("156736.png"), () => new UI.Views.Settings.GeneralSettingsView(this.ModuleSettings, this.Gw2ApiManager, this.IconService, this.TranslationService, this.SettingEventService, this.Font) { DefaultColor = this.ModuleSettings.DefaultGW2Color }, "General"));

            UI.Views.Settings.AreaSettingsView areaSettingsView = new UI.Views.Settings.AreaSettingsView(
               () => this._areas.Values.Select(area => area.Configuration),
               this.ModuleSettings,
               this.Gw2ApiManager,
               this.IconService,
               this.TranslationService,
               this.SettingEventService,
               GameService.Content.DefaultFont16)
            { DefaultColor = this.ModuleSettings.DefaultGW2Color };
            areaSettingsView.AddArea += (s, e) =>
            {
                e.AreaConfiguration = this.AddArea(e.Name);
            };

            areaSettingsView.RemoveArea += (s, e) =>
            {
                this.RemoveArea(e);
            };

            this.SettingsWindow.Tabs.Add(new Tab(
                this.IconService.GetIcon("605018.png"),
                () => areaSettingsView,
                this.TranslationService.GetTranslation("areaSettingsView-title", "Transaction Areas")));

            this._trackedTransactionView = new TrackedTransactionView(() => this.TrackedTransactionService.TrackedTransactions, this.Gw2ApiManager, this.IconService, this.ItemService, this.TranslationService, this.Font)
            {
                DefaultColor = this.ModuleSettings.DefaultGW2Color
            };

            this._trackedTransactionView.AddTracking += this.TrackedTransactionView_AddTracking;
            this._trackedTransactionView.RemoveTracking += this.TrackedTransactionView_RemoveTracking;

            this.SettingsWindow.Tabs.Add(new Tab(this.IconService.GetIcon("255379.png"), () => this._trackedTransactionView, "Tracked Transactions"));
        }

        private void TrackedTransactionService_TransactionLeftRange(object sender, TrackedTransaction e)
        {
            Shared.Controls.ScreenNotification.ShowNotification($"{e.Item.Name} is not best price anymore.");
        }

        private void TrackedTransactionService_TransactionEnteredRange(object sender, TrackedTransaction e)
        {
            List<string> messages = new List<string>();
            switch (e.Type)
            {
                case TrackedTransactionType.BuyGT:
                    messages.Add($"{e.Item.Name} reached best buy price!");
                    messages.Add($"It is now more than {GW2Utils.FormatCoins(e.ActualPrice)}!");
                    break;
                case TrackedTransactionType.BuyLT:
                    messages.Add($"{e.Item.Name} reached best buy price!");
                    messages.Add($"It is now less than {GW2Utils.FormatCoins(e.ActualPrice)}!");
                    break;
                case TrackedTransactionType.SellGT:
                    messages.Add($"{e.Item.Name} reached best sell price!");
                    messages.Add($"It is now more than {GW2Utils.FormatCoins(e.ActualPrice)}!");
                    break;
                case TrackedTransactionType.SellLT:
                    messages.Add($"{e.Item.Name} reached best sell price!");
                    messages.Add($"It is now less than {GW2Utils.FormatCoins(e.ActualPrice)}!");
                    break;
            }

            if (messages.Count == 0) return;

            Shared.Controls.ScreenNotification.ShowNotification(messages.ToArray());
        }

        private void ToggleContainers(bool show)
        {
            if (!this.ModuleSettings.GlobalDrawerVisible.Value)
            {
                show = false;
            }

            this._areas.Values.ToList().ForEach(area =>
            {
                // Don't show if disabled.
                bool showArea = show && area.Enabled && area.CalculateVisibility();

                if (showArea)
                {
                    if (!area.Visible)
                    {
                        area.Show();
                    }
                }
                else
                {
                    if (area.Visible)
                    {
                        area.Hide();
                    }
                }
            });
        }

        private void TrackedTransactionView_RemoveTracking(object sender, TrackedTransaction e)
        {
            this.TrackedTransactionService.Remove(e.ItemId, e.Type);
        }

        private void TrackedTransactionView_AddTracking(object sender, TrackedTransaction e)
        {
            AsyncHelper.RunSync(async () =>
            {
                bool added = await this.TrackedTransactionService.Add(e.ItemId, e.WishPrice, e.Type);
                if (!added)
                {
                    throw new Exception("Item could not be added to tracking list.");
                }
            });
        }

        private void AddAllAreas()
        {
            if (this.ModuleSettings.AreaNames.Value.Count == 0)
            {
                this.ModuleSettings.AreaNames.Value.Add("Main");
            }

            foreach (string areaName in this.ModuleSettings.AreaNames.Value)
            {
                _ = this.AddArea(areaName);
            }
        }

        private TransactionAreaConfiguration AddArea(string name)
        {
            var config = this.ModuleSettings.AddDrawer(name);
            this.AddArea(config);

            return config;
        }

        private void AddArea(TransactionAreaConfiguration configuration)
        {
            if (!this.ModuleSettings.AreaNames.Value.Contains(configuration.Name))
            {
                this.ModuleSettings.AreaNames.Value = new List<string>(this.ModuleSettings.AreaNames.Value) { configuration.Name };
            }

            this.ModuleSettings.UpdateDrawerLocalization(configuration, this.TranslationService);

            TransactionArea area = new TransactionArea(
                configuration,
                this.IconService,
                this.TradingPostService,
                this.TranslationService)
            {
                Parent = GameService.Graphics.SpriteScreen,
                DrawInterval = TimeSpan.FromMilliseconds(1)
            };

            area.RequestedNewData += this.Area_RequestedNewData;
            this.SetAreaTransactions(area);

            _ = this._areas.AddOrUpdate(configuration.Name, area, (name, prev) => area);
        }

        private void Area_RequestedNewData(object sender, EventArgs e)
        {
            var area = sender as TransactionArea;
            this.SetAreaTransactions(area);
        }

        private void SetAreaTransactions(TransactionArea area)
        {
            area.ClearTransactions();
            area.AddTransactions(this.TradingPostService.OwnTransactions);
        }

        private void RemoveArea(TransactionAreaConfiguration configuration)
        {
            this.ModuleSettings.AreaNames.Value = new List<string>(this.ModuleSettings.AreaNames.Value.Where(areaName => areaName != configuration.Name));

            this._areas[configuration.Name]?.Dispose();
            _ = this._areas.TryRemove(configuration.Name, out _);

            this.ModuleSettings.RemoveDrawer(configuration.Name);
        }

        /// <inheritdoc />
        protected override void Unload()
        {
            this.Logger.Debug("Unload drawer...");

            if (this._areas != null)
            {
                foreach (TransactionArea area in this._areas.Values)
                {
                    area.RequestedNewData -= this.Area_RequestedNewData;
                    area?.Dispose();
                }
            }

            this.Logger.Debug("Unloaded drawer.");

            this.Logger.Debug("Unload views...");

            if (this._trackedTransactionView != null)
            {
                this._trackedTransactionView.AddTracking -= this.TrackedTransactionView_AddTracking;
                this._trackedTransactionView.RemoveTracking -= this.TrackedTransactionView_RemoveTracking;
            }

            this.Logger.Debug("Unloaded views.");

            this.Logger.Debug("Unloading states...");
            if (this.TradingPostService != null)
            {
                this.TradingPostService.Updated -= this.TradingPostService_TransactionsUpdated;
            }

            if (this.TrackedTransactionService != null)
            {
                this.TrackedTransactionService.TransactionEnteredRange -= this.TrackedTransactionService_TransactionEnteredRange;
                this.TrackedTransactionService.TransactionLeftRange -= this.TrackedTransactionService_TransactionLeftRange;
            }
            this.Logger.Debug("Finished unloading states.");

            this.Logger.Debug("Unload base...");

            base.Unload();

            this.Logger.Debug("Unloaded base.");
        }
    }
}

