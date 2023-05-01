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
    using Estreya.BlishHUD.Shared.Service;
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

    [Export(typeof(Blish_HUD.Modules.Module))]
    public class TradingPostWatcherModule : BaseModule<TradingPostWatcherModule, ModuleSettings>
    {
        public override string UrlModuleName => "trading-post-watcher";

        internal static TradingPostWatcherModule ModuleInstance => Instance;

        private TransactionDrawer Drawer { get; set; }

        internal TransactionDrawerConfiguration DrawerConfiguration { get; set; }

        private TrackedTransactionView _trackedTransactionView;

        #region Services
        public TrackedTransactionService TrackedTransactionService { get; private set; }

        protected override string API_VERSION_NO => "1";
        #endregion

        [ImportingConstructor]
        public TradingPostWatcherModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) { }

        protected override async Task LoadAsync()
        {
            await base.LoadAsync();

            this.ModuleSettings.ModuleSettingsChanged += (sender, eventArgs) =>
            {
                switch (eventArgs.Name)
                {
                    case nameof(this.ModuleSettings.GlobalDrawerVisible):
                        this.ToggleContainer(this.ModuleSettings.GlobalDrawerVisible.Value);
                        break;
                    default:
                        break;
                }
            };
        }

        private void TradingPostService_TransactionsUpdated(object sender, EventArgs e)
        {
            this.Drawer.ClearTransactions();
            foreach (PlayerTransaction transaction in this.TradingPostService.OwnTransactions)
            {
                this.Drawer.AddTransaction(transaction);
            }
        }

        protected override void OnBeforeServicesStarted()
        {
            this.Drawer = new TransactionDrawer(this.DrawerConfiguration, this.IconService, this.TradingPostService, this.TranslationService)
            {
                Parent = GameService.Graphics.SpriteScreen,
                Opacity = 0f,
                Visible = false,
                FlowDirection = ControlFlowDirection.SingleTopToBottom,
                HeightSizingMode = SizingMode.AutoSize
            };

            this.TradingPostService.Updated += this.TradingPostService_TransactionsUpdated;
        }

        protected override Collection<ManagedService> GetAdditionalServices(string directoryPath)
        {
            Collection<ManagedService> states = new Collection<ManagedService>();

            this.TrackedTransactionService = new TrackedTransactionService(new APIServiceConfiguration()
            {
                AwaitLoading = true,
                Enabled = true,
                SaveInterval = TimeSpan.FromSeconds(30),
                UpdateInterval = TimeSpan.FromSeconds(30)
            }, this.Gw2ApiManager, this.ItemService, directoryPath);
            this.TrackedTransactionService.TransactionEnteredRange += this.TrackedTransactionService_TransactionEnteredRange;
            this.TrackedTransactionService.TransactionLeftRange += this.TrackedTransactionService_TransactionLeftRange;

            states.Add(this.TrackedTransactionService);

            return states;
        }

        private void TrackedTransactionService_TransactionLeftRange(object sender, Shared.Models.GW2API.Commerce.Transaction e)
        {
            Shared.Controls.ScreenNotification.ShowNotification($"{e.Item.Name} is not best price anymore");
        }

        private void TrackedTransactionService_TransactionEnteredRange(object sender, Shared.Models.GW2API.Commerce.Transaction e)
        {
            Shared.Controls.ScreenNotification.ShowNotification($"{e.Item.Name} reached best {e.Type.Humanize(LetterCasing.LowerCase)} price of {GW2Utils.FormatCoins(e.Price)}");
        }

        private void ToggleContainer(bool show)
        {
            if (this.Drawer == null)
            {
                return;
            }

            if (!this.ModuleSettings.GlobalDrawerVisible.Value)
            {
                if (this.Drawer.Visible)
                {
                    this.Drawer.Hide();
                }

                return;
            }

            if (show)
            {
                if (!this.Drawer.Visible)
                {
                    this.Drawer.Show();
                }
            }
            else
            {
                if (this.Drawer.Visible)
                {
                    this.Drawer.Hide();
                }
            }
        }

        protected override void OnModuleLoaded(EventArgs e)
        {
            // Base handler must be called
            base.OnModuleLoaded(e);

            if (this.ModuleSettings.GlobalDrawerVisible.Value)
            {
                this.ToggleContainer(true);
            }
        }

        protected override AsyncTexture2D GetEmblem()
        {
            return this.IconService?.GetIcon("102495.png");
        }

        protected override AsyncTexture2D GetCornerIcon()
        {
            return this.IconService?.GetIcon("255379.png");
        }

        protected override void OnSettingWindowBuild(TabbedWindow2 settingWindow)
        {
            this._trackedTransactionView = new TrackedTransactionView(() => this.TrackedTransactionService.TrackedTransactions, this.Gw2ApiManager, this.IconService, this.ItemService, this.TranslationService, this.Font)
            {
                DefaultColor = this.ModuleSettings.DefaultGW2Color
            };

            this._trackedTransactionView.AddTracking += this.TrackedTransactionView_AddTracking;
            this._trackedTransactionView.RemoveTracking += this.TrackedTransactionView_RemoveTracking;


            this.SettingsWindow.Tabs.Add(new Tab(this.IconService.GetIcon("156736.png"), () => new UI.Views.Settings.GeneralSettingsView(this.Gw2ApiManager, this.IconService, this.TranslationService, this.SettingEventService, this.Font) { DefaultColor = this.ModuleSettings.DefaultGW2Color }, "General"));
            this.SettingsWindow.Tabs.Add(new Tab(this.IconService.GetIcon("255379.png"), () => new UI.Views.Settings.TransactionSettingsView(this.Gw2ApiManager, this.IconService, this.TranslationService, this.SettingEventService, this.Font) { DefaultColor = this.ModuleSettings.DefaultGW2Color }, "Transactions"));
            this.SettingsWindow.Tabs.Add(new Tab(this.IconService.GetIcon("156740.png"), () => new UI.Views.Settings.GraphicsSettingsView(this.Gw2ApiManager, this.IconService,this.TranslationService, this.SettingEventService, this.Font) { DefaultColor = this.ModuleSettings.DefaultGW2Color }, "Graphic"));
            this.SettingsWindow.Tabs.Add(new Tab(this.IconService.GetIcon("255379.png"), () => this._trackedTransactionView, "Tracked Transactions"));
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

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            this.ToggleContainer(this.ShowUI);

            this.ModuleSettings.CheckDrawerSizeAndPosition(this.DrawerConfiguration);
        }

        /// <inheritdoc />
        protected override void Unload()
        {
            this.Logger.Debug("Unload drawer...");

            if (this.Drawer != null)
            {
                this.Drawer.Dispose();
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
            this.TradingPostService.Updated -= this.TradingPostService_TransactionsUpdated;
            this.TrackedTransactionService.TransactionEnteredRange -= this.TrackedTransactionService_TransactionEnteredRange;
            this.TrackedTransactionService.TransactionLeftRange -= this.TrackedTransactionService_TransactionLeftRange;
            this.Logger.Debug("Finished unloading states.");

            this.Logger.Debug("Unload base...");

            base.Unload();

            this.Logger.Debug("Unloaded base.");
        }

        protected override BaseModuleSettings DefineModuleSettings(SettingCollection settings)
        {
            ModuleSettings moduleSettings = new ModuleSettings(settings);

            this.DrawerConfiguration = moduleSettings.AddDrawer("currentTransactions");

            return moduleSettings;
        }

        protected override string GetDirectoryName()
        {
            return "tradingpost";
        }

        protected override void ConfigureServices(ServiceConfigurations configurations)
        {
            configurations.TradingPost.Enabled = true;
            configurations.Items.Enabled = true;
        }
    }
}

