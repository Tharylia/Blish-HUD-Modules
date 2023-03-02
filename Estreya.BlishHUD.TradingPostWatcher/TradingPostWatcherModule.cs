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
    using Estreya.BlishHUD.Shared.State;
    using Estreya.BlishHUD.Shared.Utils;
    using Estreya.BlishHUD.TradingPostWatcher.Controls;
    using Estreya.BlishHUD.TradingPostWatcher.Models;
    using Estreya.BlishHUD.TradingPostWatcher.State;
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
        public override string WebsiteModuleName => "trading-post-watcher";

        internal static TradingPostWatcherModule ModuleInstance => Instance;

        private TransactionDrawer Drawer { get; set; }

        internal TransactionDrawerConfiguration DrawerConfiguration { get; set; }

        private TrackedTransactionView _trackedTransactionView;

        #region States
        public TrackedTransactionState TrackedTransactionState { get; private set; }

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

        private void TradingPostState_TransactionsUpdated(object sender, EventArgs e)
        {
            this.Drawer.ClearTransactions();
            foreach (PlayerTransaction transaction in this.TradingPostState.OwnTransactions)
            {
                this.Drawer.AddTransaction(transaction);
            }
        }

        protected override void OnBeforeStatesStarted()
        {
            this.Drawer = new TransactionDrawer(this.DrawerConfiguration, this.IconState, this.TradingPostState, this.TranslationState)
            {
                Parent = GameService.Graphics.SpriteScreen,
                Opacity = 0f,
                Visible = false,
                FlowDirection = ControlFlowDirection.SingleTopToBottom,
                HeightSizingMode = SizingMode.AutoSize
            };

            this.TradingPostState.Updated += this.TradingPostState_TransactionsUpdated;
        }

        protected override Collection<ManagedState> GetAdditionalStates(string directoryPath)
        {
            Collection<ManagedState> states = new Collection<ManagedState>();

            this.TrackedTransactionState = new TrackedTransactionState(new APIStateConfiguration()
            {
                AwaitLoading = true,
                Enabled = true,
                SaveInterval = TimeSpan.FromSeconds(30),
                UpdateInterval = TimeSpan.FromSeconds(30)
            }, this.Gw2ApiManager, this.ItemState, directoryPath);
            this.TrackedTransactionState.TransactionEnteredRange += this.TrackedTransactionState_TransactionEnteredRange;
            this.TrackedTransactionState.TransactionLeftRange += this.TrackedTransactionState_TransactionLeftRange;

            states.Add(this.TrackedTransactionState);

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

            if (this.ModuleSettings.GlobalDrawerVisible.Value)
            {
                this.ToggleContainer(true);
            }
        }

        protected override AsyncTexture2D GetEmblem()
        {
            return this.IconState?.GetIcon("102495.png");
        }

        protected override AsyncTexture2D GetCornerIcon()
        {
            return this.IconState?.GetIcon("255379.png");
        }

        protected override void OnSettingWindowBuild(TabbedWindow2 settingWindow)
        {
            this._trackedTransactionView = new TrackedTransactionView(() => this.TrackedTransactionState.TrackedTransactions, this.Gw2ApiManager, this.IconState, this.ItemState, this.TranslationState, this.Font)
            {
                DefaultColor = this.ModuleSettings.DefaultGW2Color
            };

            this._trackedTransactionView.AddTracking += this.TrackedTransactionView_AddTracking;
            this._trackedTransactionView.RemoveTracking += this.TrackedTransactionView_RemoveTracking;


            this.SettingsWindow.Tabs.Add(new Tab(this.IconState.GetIcon("156736.png"), () => new UI.Views.Settings.GeneralSettingsView(this.Gw2ApiManager, this.IconState, this.TranslationState, this.SettingEventState, this.Font) { DefaultColor = this.ModuleSettings.DefaultGW2Color }, "General"));
            this.SettingsWindow.Tabs.Add(new Tab(this.IconState.GetIcon("255379.png"), () => new UI.Views.Settings.TransactionSettingsView(this.Gw2ApiManager, this.IconState, this.TranslationState, this.SettingEventState, this.Font) { DefaultColor = this.ModuleSettings.DefaultGW2Color }, "Transactions"));
            this.SettingsWindow.Tabs.Add(new Tab(this.IconState.GetIcon("156740.png"), () => new UI.Views.Settings.GraphicsSettingsView(this.Gw2ApiManager, this.IconState,this.TranslationState, this.SettingEventState, this.Font) { DefaultColor = this.ModuleSettings.DefaultGW2Color }, "Graphic"));
            this.SettingsWindow.Tabs.Add(new Tab(this.IconState.GetIcon("255379.png"), () => this._trackedTransactionView, "Tracked Transactions"));
        }

        private void TrackedTransactionView_RemoveTracking(object sender, TrackedTransaction e)
        {
            this.TrackedTransactionState.Remove(e.ItemId, e.Type);
        }

        private void TrackedTransactionView_AddTracking(object sender, TrackedTransaction e)
        {
            AsyncHelper.RunSync(async () =>
            {
                bool added = await this.TrackedTransactionState.Add(e.ItemId, e.WishPrice, e.Type);
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
            this.TradingPostState.Updated -= this.TradingPostState_TransactionsUpdated;
            this.TrackedTransactionState.TransactionEnteredRange -= this.TrackedTransactionState_TransactionEnteredRange;
            this.TrackedTransactionState.TransactionLeftRange -= this.TrackedTransactionState_TransactionLeftRange;
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

        protected override void ConfigureStates(StateConfigurations configurations)
        {
            configurations.TradingPost.Enabled = true;
            configurations.Items.Enabled = true;
        }
    }
}

