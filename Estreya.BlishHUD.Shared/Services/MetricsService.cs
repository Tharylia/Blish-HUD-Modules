namespace Estreya.BlishHUD.Shared.Services
{
    using Estreya.BlishHUD.Shared.Settings;
    using Estreya.BlishHUD.Shared.Controls.Input;
    using Estreya.BlishHUD.Shared.Threading;
    using Estreya.BlishHUD.Shared.Utils;
    using Flurl.Http;
    using Microsoft.Xna.Framework;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;

    public class MetricsService : ManagedService
    {
        private static SemVer.Version CURRENT_VERSION = new SemVer.Version("4.0.0"); // Starting at 4 to fix bug with event table

        private readonly IFlurlClient _flurlClient;
        private readonly string _apiBaseUrl;
        private readonly string _moduleName;
        private readonly string _moduleNamespace;
        private readonly BaseModuleSettings _moduleSettings;
        private readonly IconService _iconService;
        private ConcurrentQueue<string> _metricsQueue;

        private static TimeSpan _metricsQueueInterval = TimeSpan.FromSeconds(10);
        private AsyncRef<double> _timeSincelastMetricQueueInterval = new AsyncRef<double>(0);

        public bool ConsentGiven => this._moduleSettings.AskedMetricsConsent.Value && this._moduleSettings.SendMetrics.Value;

        public bool NeedsConsentRenewal => this._moduleSettings.SendMetrics.Value
            && this._moduleSettings.MetricsConsentGivenVersion.Value is not null
            && this._moduleSettings.MetricsConsentGivenVersion.Value < CURRENT_VERSION;

        public MetricsService(ServiceConfiguration configuration, IFlurlClient flurlClient, string apiBaseUrl, string moduleName, string moduleNamespace, BaseModuleSettings moduleSettings, IconService iconService) : base(configuration)
        {
            this._flurlClient = flurlClient;
            this._apiBaseUrl = apiBaseUrl;
            this._moduleName = moduleName;
            this._moduleNamespace = moduleNamespace;
            this._moduleSettings = moduleSettings;
            this._iconService = iconService;
        }

        protected override Task Initialize()
        {
            this._metricsQueue = new ConcurrentQueue<string>();
            return Task.CompletedTask;
        }

        protected override void InternalUnload()
        {
            this._metricsQueue = null;
        }

        protected override void InternalUpdate(GameTime gameTime)
        {
            _ = UpdateUtil.UpdateAsync(this.HandleQueue, gameTime, _metricsQueueInterval.TotalMilliseconds, _timeSincelastMetricQueueInterval, false);
        }

        protected override Task Load() => Task.CompletedTask;

        private async Task HandleQueue()
        {
            int max = 50;
            int handled = 0;
            while (this._metricsQueue.TryDequeue(out var metricKey) && handled <= max)
            {
                await this.SendMetricAsync(metricKey);
                handled++;
            }
        }

        public void QueueMetric(string key)
        {
            if (!this.ConsentGiven) return;

            this._metricsQueue.Enqueue(key);
        }

        public async Task SendMetricAsync(string key)
        {
            if (!this.ConsentGiven) return;

            try
            {
                var request = this._flurlClient.Request(this._apiBaseUrl, "metrics/modules", this._moduleNamespace, key);
                await request.SendAsync(System.Net.Http.HttpMethod.Post);
            }
            catch (Exception ex)
            {
                this.Logger.Debug(ex, $"Could not send metric \"{key}\".");
            }
        }

        public async Task AskMetricsConsent(bool forceAsk = false)
        {
            var needNewConsent = this.NeedsConsentRenewal;
            if (!forceAsk && !needNewConsent && this._moduleSettings.AskedMetricsConsent.Value) return;

            var confirmDialog = new ConfirmDialog("Allow Metrics?", $"The module \"{this._moduleName}\" ({this._moduleNamespace}) would like to collect anonymous metric data.\n\n" +
                $"The collected data will be used for advanced usage statistics for specific module functions to see how often and in which combination they are used.\n\n" +
                $"All data is completely anonymous and a reference to your profile/account can't be created.", this._iconService,
                new ButtonDefinition[]
                {
                    new ButtonDefinition("Yes", System.Windows.Forms.DialogResult.Yes),
                    new ButtonDefinition("No", System.Windows.Forms.DialogResult.No)
                })
            {
                SelectedButtonIndex = 1
            };

            var result = await confirmDialog.ShowDialog();

            var consentGiven = result == System.Windows.Forms.DialogResult.Yes;

            this._moduleSettings.AskedMetricsConsent.Value = true;

            this._moduleSettings.SendMetrics.Value = consentGiven;
            this._moduleSettings.MetricsConsentGivenVersion.Value = consentGiven ? CURRENT_VERSION : new SemVer.Version("0.0.0");
        }
    }
}
