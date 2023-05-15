namespace Estreya.BlishHUD.Shared.Services;

using Blish_HUD;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.Shared.Extensions;
using Estreya.BlishHUD.Shared.Helpers;
using Estreya.BlishHUD.Shared.Threading;
using Estreya.BlishHUD.Shared.Utils;
using Gw2Sharp.WebApi.Exceptions;
using Gw2Sharp.WebApi.V2.Models;
using Humanizer;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public abstract class APIService : ManagedService
{
    protected readonly Gw2ApiManager _apiManager;

    private readonly EventWaitHandle _eventWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

    protected new APIServiceConfiguration Configuration { get; }

    private AsyncRef<double> _timeSinceUpdate = 0;

    private readonly AsyncLock _loadingLock = new AsyncLock();
    public bool Loading { get; protected set; }

    public string ProgressText { get; private set; } = string.Empty;

    /// <summary>
    /// Fired when the new api service has been fetched.
    /// </summary>
    public event EventHandler Updated;

    public APIService(Gw2ApiManager apiManager, APIServiceConfiguration configuration) : base(configuration)
    {
        this._apiManager = apiManager;
        this.Configuration = configuration;
    }

    protected sealed override Task Initialize()
    {
        this._apiManager.SubtokenUpdated += this.ApiManager_SubtokenUpdated;
        return Task.CompletedTask;
    }

    protected virtual Task DoInitialize() => Task.CompletedTask;

    private void ApiManager_SubtokenUpdated(object sender, ValueEventArgs<IEnumerable<Gw2Sharp.WebApi.V2.Models.TokenPermission>> e)
    {
        Logger.Info("Received new subtoken with permissions: {0}", e.Value.Humanize());

        // Load already called. Don't refresh if no permissions needed anyway.
        if (this.Configuration.NeededPermissions.Count > 0)
        {
            AsyncHelper.RunSync(this.Clear);
            this._timeSinceUpdate.Value = this.Configuration.UpdateInterval.TotalMilliseconds;
        }
    }

    protected sealed override void InternalUnload()
    {
        this._apiManager.SubtokenUpdated -= this.ApiManager_SubtokenUpdated;

        this.DoUnload();
    }

    protected virtual void DoUnload() { }

    protected sealed override void InternalUpdate(GameTime gameTime)
    {
        if (this.Configuration.UpdateInterval != Timeout.InfiniteTimeSpan)
        {
            _ = UpdateUtil.UpdateAsync(this.Load, gameTime, this.Configuration.UpdateInterval.TotalMilliseconds, this._timeSinceUpdate);
        }

        this.DoUpdate(gameTime);
    }

    protected virtual void DoUpdate(GameTime gameTime) { }


    /// <summary>
    /// Loads the api data of the state.
    /// <para>If this function is overridden, the function <see cref="SignalCompletion"/> has to be called at the end when <see cref="Load"/> is not called.</para>
    /// </summary>
    /// <returns></returns>
    protected override async Task Load()
    {
        await this.LoadFromAPI(true);
    }

    protected async Task LoadFromAPI(bool resetCompletion = true)
    {
        if (!this._loadingLock.IsFree())
        {
            Logger.Warn("Tried to load again while already loading.");
            return;
        }

        using (await this._loadingLock.LockAsync())
        {
            if (resetCompletion)
            {
                _ = this._eventWaitHandle.Reset();
            }

            this.Loading = true;

            try
            {
                IProgress<string> progress = new Progress<string>(this.ReportProgress);
                progress.Report($"Loading {this.GetType().Name}");
                await this.FetchFromAPI(this._apiManager, progress);
                this.SignalUpdated();
            }
            finally
            {
                this.Loading = false;
                this.SignalCompletion();
            }
        }
    }

    protected void ReportProgress(string status)
    {
        this.ProgressText = status;
    }

    protected abstract Task FetchFromAPI(Gw2ApiManager apiManager, IProgress<string> progress);

    protected void SignalUpdated()
    {
        this.Updated?.Invoke(this, EventArgs.Empty);
    }

    protected void SignalCompletion()
    {
        this.ReportProgress(null);
        _ = this._eventWaitHandle.Set();
    }

    public Task<bool> WaitForCompletion()
    {
        return this.WaitForCompletion(Timeout.InfiniteTimeSpan);
    }

    public async Task<bool> WaitForCompletion(TimeSpan timeout)
    {
        return await this._eventWaitHandle.WaitOneAsync(timeout, this.CancellationToken);
    }
}
