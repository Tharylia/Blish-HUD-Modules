namespace Estreya.BlishHUD.EventTable.Services;

using Blish_HUD;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.EventTable;
using Estreya.BlishHUD.EventTable.Models.SelfHosting;
using Estreya.BlishHUD.Shared.Helpers;
using Estreya.BlishHUD.Shared.Services;
using Estreya.BlishHUD.Shared.Threading;
using Estreya.BlishHUD.Shared.Utils;
using Flurl.Http;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class SelfHostingEventService : APIService<SelfHostingEventEntry>
{
    private readonly ModuleSettings _moduleSettings;
    private readonly IFlurlClient _flurlClient;
    private readonly string _apiBaseUrl;
    private readonly AccountService _accountService;
    private readonly BlishHudApiService _blishHudApiService;

    private static TimeSpan _lastServerAddressCheckInterval = TimeSpan.FromSeconds(10);
    private AsyncRef<double> _lastServerAddressCheck = new AsyncRef<double>(_lastServerAddressCheckInterval.TotalMilliseconds);

    private string _lastServerAddress;
    private bool _lastServerAddressFirstCheck = true;

    public List<SelfHostingEventEntry> Events
    {
        get
        {
            using (this._apiObjectListLock.Lock())
            {
                return new List<SelfHostingEventEntry>(this.APIObjectList);
            }
        }
    }

    public SelfHostingEventService(APIServiceConfiguration configuration, Gw2ApiManager apiManager, IFlurlClient flurlClient, string apiBaseUrl, AccountService accountService, BlishHudApiService blishHudApiService) : base(apiManager, configuration)
    {
        this._flurlClient = flurlClient;
        this._apiBaseUrl = apiBaseUrl;
        this._accountService = accountService;
        this._blishHudApiService = blishHudApiService;
    }

    protected override void DoUpdate(GameTime gameTime)
    {
        base.DoUpdate(gameTime);

        _ = UpdateUtil.UpdateAsync(this.CheckServerAddress, gameTime, _lastServerAddressCheckInterval.TotalMilliseconds, this._lastServerAddressCheck, false);
    }

    private async Task CheckServerAddress()
    {
        try
        {
            var currentServerAddress = GameService.Gw2Mumble.Info.ServerAddress;

            if (!this._lastServerAddressFirstCheck && this._lastServerAddress != currentServerAddress)
            {
                var hasSelfHostingEntry = this.HasSelfHostingEntry();

                if (hasSelfHostingEntry)
                {
                    this.Logger.Info($"Instance IP changed from {this._lastServerAddress} to {currentServerAddress}. Deleting self hosting entry.");
                    await this.DeleteEntry();
                }
            };

            this._lastServerAddressFirstCheck = false;
            this._lastServerAddress = currentServerAddress;
        }
        catch (Exception ex)
        {
            this.Logger.Warn(ex, "Could not check server address.");
        }
    }

    protected override async Task<List<SelfHostingEventEntry>> Fetch(Gw2ApiManager apiManager, IProgress<string> progress, CancellationToken cancellationToken)
    {
        var request = this._flurlClient.Request(this._apiBaseUrl, "/self-hosting");

        var selfHostingEntries = await request.GetJsonAsync<List<SelfHostingEventEntry>>();

        return selfHostingEntries;
    }

    public async Task<double> GetMaxHostingDuration()
    {
        var request = this._flurlClient.Request(this._apiBaseUrl, $"/self-hosting/duration");

        var response = await request.GetJsonAsync();

        return (double)response.duration;
    }

    public async Task<List<SelfHostingCategoryDefinition>> GetDefinitions()
    {
        var request = this._flurlClient.Request(this._apiBaseUrl, $"/self-hosting/definitions");

        var definitions = await request.GetJsonAsync<List<SelfHostingCategoryDefinition>>();

        return definitions;
    }

    public async Task<List<SelfHostingCategoryDefinition>> GetCategories()
    {
        var request = this._flurlClient.Request(this._apiBaseUrl, $"/self-hosting/categories");

        var categories = await request.GetJsonAsync<List<SelfHostingCategoryDefinition>>();

        return categories;
    }

    public async Task<SelfHostingCategoryDefinition> GetCategory(string categoryKey)
    {
        var request = this._flurlClient.Request(this._apiBaseUrl, $"/self-hosting/categories/{categoryKey}");

        var categoryEvents = await request.GetJsonAsync<SelfHostingCategoryDefinition>();

        return categoryEvents;
    }
    public async Task<List<SelfHostingZoneDefinition>> GetCategoryZones(string categoryKey)
    {
        var request = this._flurlClient.Request(this._apiBaseUrl, $"/self-hosting/categories/{categoryKey}/zones");

        var zones = await request.GetJsonAsync<List<SelfHostingZoneDefinition>>();

        return zones;
    }

    public async Task<List<SelfHostingEventDefinition>> GetCategoryZoneEvents(string categoryKey, string zoneKey)
    {
        var request = this._flurlClient.Request(this._apiBaseUrl, $"/self-hosting/categories/{categoryKey}/zones/{zoneKey}/events");

        var categoryEvents = await request.GetJsonAsync<List<SelfHostingEventDefinition>>();

        return categoryEvents;
    }

    public bool HasSelfHostingEntry()
    {
        var accountName = this._accountService.Account?.Name;

        return !string.IsNullOrWhiteSpace(accountName) && this.Events.Any(e => e.AccountName == accountName);
    }

    /// <summary>
    /// Deletes the current hosted entry for the user.
    /// </summary>
    /// <returns></returns>
    public async Task DeleteEntry()
    {
        await this.DeleteEntry(true);
    }

    /// <summary>
    /// Deletes the current hosted entry for the user.
    /// </summary>
    /// <returns></returns>
    private async Task DeleteEntry(bool reload)
    {
        if (string.IsNullOrWhiteSpace(this._blishHudApiService.AccessToken))
        {
            throw new InvalidOperationException("Not logged in to Estreya BlishHUD.");
        }

        var request = this._flurlClient.Request(this._apiBaseUrl, $"/self-hosting")
            .WithOAuthBearerToken(this._blishHudApiService.AccessToken);

        var response = await request.DeleteAsync();

        if (reload)
        {
            await this.Reload();
        }
    }

    public async Task AddEntry(string categoryKey, string zoneKey, string eventKey, DateTimeOffset startTime, int duration)
    {
        if (string.IsNullOrWhiteSpace(categoryKey)) throw new ArgumentNullException(nameof(categoryKey));
        if (string.IsNullOrWhiteSpace(zoneKey)) throw new ArgumentNullException(nameof(zoneKey));
        if (string.IsNullOrWhiteSpace(eventKey)) throw new ArgumentNullException(nameof(eventKey));

        var accountServiceTimeoutSec = 5;
        var accountServiceFinished = await this._accountService.WaitForCompletion(TimeSpan.FromSeconds(accountServiceTimeoutSec));
        if (!accountServiceFinished) throw new InvalidOperationException($"Account Service did not respond within {accountServiceTimeoutSec} seconds.");

        var accountName = this._accountService.Account?.Name;

        if (string.IsNullOrWhiteSpace(accountName)) throw new InvalidOperationException("Account Service did not return a valid account.");

        if (string.IsNullOrWhiteSpace(this._blishHudApiService.AccessToken))
        {
            throw new InvalidOperationException("Not logged in to Estreya BlishHUD.");
        }

        var instanceIP = GameService.Gw2Mumble.Info.ServerAddress;

        if (string.IsNullOrWhiteSpace(instanceIP)) throw new InvalidOperationException("Could not find valid server ip address.");

        var request = this._flurlClient.Request(this._apiBaseUrl, $"/self-hosting/categories/{categoryKey}/zones/{zoneKey}/events/{eventKey}")
            .WithOAuthBearerToken(this._blishHudApiService.AccessToken);

        var response = await request.PostJsonAsync(new SelfHostingEventEntry()
        {
            AccountName = accountName,
            InstanceIP = instanceIP,
            // Don't need category key
            Duration = duration,
            StartTime = startTime.ToUniversalTime(),
        });

        await this.Reload();
    }

    public async Task ReportHost(string accountName, SelfHostingReportType type, string reason)
    {
        if (string.IsNullOrWhiteSpace(this._blishHudApiService.AccessToken))
        {
            throw new InvalidOperationException("Not logged in to Estreya BlishHUD.");
        }

        var request = this._flurlClient.Request(this._apiBaseUrl, $"/self-hosting/report")
            .WithOAuthBearerToken(this._blishHudApiService.AccessToken);

        var response = await request.PostJsonAsync(new 
        {
            accountName = accountName,
            type = type.ToString(),
            reason = reason
        });
    }

    protected override void DoUnload()
    {
        base.DoUnload();

        try
        {
            AsyncHelper.RunSync(() => this.DeleteEntry(false));
        }
        catch (Exception)
        {
            // Do not fail
        }
    }
}
