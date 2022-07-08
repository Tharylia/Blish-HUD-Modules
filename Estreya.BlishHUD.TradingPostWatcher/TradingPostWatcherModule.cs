namespace Estreya.BlishHUD.TradingPostWatcher
{
    using Blish_HUD;
    using Blish_HUD.Controls;
    using Blish_HUD.Modules;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.Shared.Helpers;
    using Estreya.BlishHUD.Shared.Models.Drawers;
    using Estreya.BlishHUD.Shared.Models.GW2API.Commerce;
    using Estreya.BlishHUD.Shared.Modules;
    using Estreya.BlishHUD.Shared.Settings;
    using Estreya.BlishHUD.Shared.State;
    using Estreya.BlishHUD.Shared.Utils;
    using Estreya.BlishHUD.TradingPostWatcher.Controls;
    using Estreya.BlishHUD.TradingPostWatcher.Resources;
    using Estreya.BlishHUD.TradingPostWatcher.State;
    using Humanizer;
    using Microsoft.Xna.Framework;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel.Composition;
    using System.Linq;
    using System.Threading.Tasks;

    [Export(typeof(Blish_HUD.Modules.Module))]
    public class TradingPostWatcherModule : BaseModule<TradingPostWatcherModule, ModuleSettings>
    {
        public override string WebsiteModuleName => "trading-post-watcher";

        internal static TradingPostWatcherModule ModuleInstance => Instance;

        private TradingPostWatcherDrawer Drawer { get; set; }

        internal DrawerConfiguration DrawerConfiguration { get; set; }

        internal DateTime DateTimeNow => DateTime.Now;

        #region States
        public TrackedTransactionState TrackedTransactionState { get; private set; }
        #endregion

        [ImportingConstructor]
        public TradingPostWatcherModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) { }

        protected override void Initialize()
        {
            this.Drawer = new TradingPostWatcherDrawer(this.DrawerConfiguration)
            {
                Parent = GameService.Graphics.SpriteScreen,
                Opacity = 0f,
                Visible = false,
                FlowDirection = ControlFlowDirection.SingleTopToBottom,
                HeightSizingMode = SizingMode.AutoSize
            };

            GameService.Overlay.UserLocaleChanged += (s, e) =>
            {
            };
        }

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
                    case nameof(this.ModuleSettings.MaxTransactions):
                    case nameof(this.ModuleSettings.ShowBuyTransactions):
                    case nameof(this.ModuleSettings.ShowSellTransactions):
                    case nameof(this.ModuleSettings.ShowHighestTransactions):
                        this.AddTransactions();
                        break;
                    default:
                        break;
                }
            };
        }

        private void TradingPostState_TransactionsUpdated(object sender, EventArgs e)
        {
            this.AddTransactions();
        }

        private void AddTransactions()
        {
            this.Logger.Debug("Clear current transactions from drawer.");
            this.Drawer.SuspendLayout();
            this.Drawer.Children.ToList().ForEach(transaction => transaction.Dispose());
            this.Drawer.ClearChildren();
            this.Drawer.ResumeLayout(true);

            this.Logger.Debug("Filter new transactions.");
            IEnumerable<CurrentTransaction> filteredTransactions = this.TradingPostState.Transactions.Where(transaction =>
            {
                if (!this.ModuleSettings.ShowBuyTransactions.Value && transaction.Type == TransactionType.Buy)
                {
                    return false;
                }

                if (!this.ModuleSettings.ShowSellTransactions.Value && transaction.Type == TransactionType.Sell)
                {
                    return false;
                }

                if (!this.ModuleSettings.ShowHighestTransactions.Value && transaction.IsHighest)
                {
                    return false;
                }

                return true;
            });

            foreach (CurrentTransaction transaction in filteredTransactions.Take(this.ModuleSettings.MaxTransactions.Value))
            {
                this.Logger.Debug("Add new transaction: {0}", transaction);
                new Controls.Transaction(transaction,
                    () => this.DrawerConfiguration.Opacity.Value,
                    () => this.ModuleSettings.ShowPrice.Value,
                    () => this.ModuleSettings.ShowPriceAsTotal.Value,
                    () => this.ModuleSettings.ShowRemaining.Value,
                    () => this.ModuleSettings.ShowCreated.Value)
                {
                    Parent = this.Drawer,
                    HeightSizingMode = SizingMode.AutoSize,
                    WidthSizingMode = SizingMode.Fill
                };
            }
        }

        protected override void HandleDefaultStates()
        {
            this.TradingPostState.TransactionsUpdated += this.TradingPostState_TransactionsUpdated;
        }

        protected override Collection<ManagedState> GetAdditionalStates(string directoryPath)
        {
            Collection<ManagedState> states = new Collection<ManagedState>();

            this.TrackedTransactionState = new TrackedTransactionState(this.Gw2ApiManager, directoryPath);
            this.TrackedTransactionState.TransactionEnteredRange += this.TrackedTransactionState_TransactionEnteredRange;
            this.TrackedTransactionState.TransactionLeftRange += this.TrackedTransactionState_TransactionLeftRange;

            //states.Add(this.TrackedTransactionState);

            return states;
        }

        private void TrackedTransactionState_TransactionLeftRange(object sender, Shared.Models.GW2API.Commerce.Transaction e)
        {
            Shared.Controls.ScreenNotification.ShowNotification($"{e.Item.Name} is not best price anymore");
        }

        private void TrackedTransactionState_TransactionEnteredRange(object sender, Shared.Models.GW2API.Commerce.Transaction e)
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

            this.Drawer.UpdatePosition(this.DrawerConfiguration.Location.X.Value, this.DrawerConfiguration.Location.Y.Value);
            this.Drawer.UpdateSize(this.DrawerConfiguration.Size.X.Value, -1);

            if (this.ModuleSettings.GlobalDrawerVisible.Value)
            {
                this.ToggleContainer(true);
            }
        }

        protected override void OnSettingWindowBuild(TabbedWindow2 settingWindow)
        {
            this.SettingsWindow.Tabs.Add(new Tab(this.IconState.GetIcon(@"textures\tradingpost.png"), () =>
            {
                UI.Views.TrackedTransactionView trackedTransactionView = new UI.Views.TrackedTransactionView(this.TrackedTransactionState.TrackedTransactions)
                {
                    APIManager = this.Gw2ApiManager,
                    IconState = this.IconState,
                    DefaultColor = this.ModuleSettings.DefaultGW2Color
                };

                trackedTransactionView.AddTracking += (s, e) =>
                {
                    AsyncHelper.RunSync(async () =>
                    {
                        bool added = await this.TrackedTransactionState.Add(e.ItemId, e.WishPrice, e.Type);
                    });
                };
                trackedTransactionView.RemoveTracking += (s, e) =>
                {
                    this.TrackedTransactionState.Remove(e.ItemId, e.Type);
                };

                return trackedTransactionView;
            }, "Tracked Transactions"));

            this.SettingsWindow.Tabs.Add(new Tab(this.IconState.GetIcon(@"156736"), () => new UI.Views.Settings.GeneralSettingsView() { APIManager = this.Gw2ApiManager, IconState = this.IconState, DefaultColor = this.ModuleSettings.DefaultGW2Color }, Strings.SettingsWindow_GeneralSettings_Title));
            this.SettingsWindow.Tabs.Add(new Tab(this.IconState.GetIcon(@"textures\tradingpost.png"), () => new UI.Views.Settings.TransactionSettingsView() { APIManager = this.Gw2ApiManager, IconState = this.IconState, DefaultColor = this.ModuleSettings.DefaultGW2Color }, "Transactions"));
            this.SettingsWindow.Tabs.Add(new Tab(this.IconState.GetIcon(@"textures\graphics_settings.png"), () => new UI.Views.Settings.GraphicsSettingsView() { APIManager = this.Gw2ApiManager, IconState = this.IconState, DefaultColor = this.ModuleSettings.DefaultGW2Color }, Strings.SettingsWindow_GraphicSettings_Title));

        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            this.ToggleContainer(this.ShowUI);

            this.Drawer.UpdatePosition(this.DrawerConfiguration.Location.X.Value, this.DrawerConfiguration.Location.Y.Value); // Handle windows resize

            this.ModuleSettings.CheckDrawerSizeAndPosition(this.DrawerConfiguration);
        }

        /// <inheritdoc />
        protected override void Unload()
        {
            this.Logger.Debug("Unload base.");

            base.Unload();

            this.Logger.Debug("Unloaded base.");

            this.Logger.Debug("Unload drawer.");

            if (this.Drawer != null)
            {
                this.Drawer.Dispose();
            }

            this.Logger.Debug("Unloaded drawer.");

            this.Logger.Debug("Unloading states...");
            this.TradingPostState.TransactionsUpdated -= this.TradingPostState_TransactionsUpdated;
            this.TrackedTransactionState.TransactionEnteredRange -= this.TrackedTransactionState_TransactionEnteredRange;
            this.TrackedTransactionState.TransactionLeftRange -= this.TrackedTransactionState_TransactionLeftRange;
            this.Logger.Debug("Finished unloading states.");
        }

        protected override BaseModuleSettings DefineModuleSettings(SettingCollection settings)
        {
            var moduleSettings = new ModuleSettings(settings);

            this.DrawerConfiguration = moduleSettings.AddDrawer("currentTransactions");

            return moduleSettings;
        }

        protected override string GetDirectoryName()
        {
            return "tradingpost";
        }

        protected override void ConfigureStates(StateConfigurations configurations)
        {
            configurations.Skills = false;
            configurations.Worldbosses = false;
            configurations.Mapchests = false;
            configurations.ArcDPS = false;
            configurations.PointOfInterests = false;
        }
    }
}

