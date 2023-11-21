namespace Estreya.BlishHUD.Shared.Services;

using Blish_HUD;
using Blish_HUD.Modules.Managers;
using Extensions;
using Gw2Sharp.WebApi.V2.Models;
using Helpers;
using Humanizer;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Threading;
using Utils;

public abstract class APIService : ManagedService
{
    protected readonly Gw2ApiManager _apiManager;

    private readonly EventWaitHandle _eventWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

    private readonly AsyncLock _loadingLock = new AsyncLock();

    private readonly AsyncRef<double> _timeSinceUpdate = new AsyncRef<double>(0);

    public APIService(Gw2ApiManager apiManager, APIServiceConfiguration configuration) : base(configuration)
    {
        this._apiManager = apiManager;
        this.Configuration = configuration;
    }

    protected new APIServiceConfiguration Configuration { get; }
    public bool Loading { get; protected set; }

    public DateTimeOffset LastUpdated { get; protected set; }

    public string ProgressText { get; private set; } = string.Empty;

    /// <summary>
    ///     Fired when the new api service has been fetched.
    /// </summary>
    public event EventHandler Updated;

    protected sealed override Task Initialize()
    {
        this._apiManager.SubtokenUpdated += this.ApiManager_SubtokenUpdated;
        return Task.CompletedTask;
    }

    protected virtual Task DoInitialize()
    {
        return Task.CompletedTask;
    }

    private void ApiManager_SubtokenUpdated(object sender, ValueEventArgs<IEnumerable<TokenPermission>> e)
    {
        this.Logger.Info("Received new subtoken with permissions: {0}", e.Value.Humanize());

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

    public override Task Reload()
    {
        this._timeSinceUpdate.Value = 0;
        return base.Reload();
    }

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
    ///     Loads the api data of the state.
    ///     <para>
    ///         If this function is overridden, the function <see cref="SignalCompletion" /> has to be called at the end when
    ///         <see cref="Load" /> is not called.
    ///     </para>
    /// </summary>
    /// <returns></returns>
    protected override async Task Load()
    {
        await this.LoadFromAPI();
    }

    protected async Task<bool> LoadFromAPI(bool resetCompletion = true)
    {
        if (!this._loadingLock.IsFree())
        {
            this.Logger.Warn("Tried to load again while already loading.");
            return false;
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

                var result = await this.FetchFromAPI(this._apiManager, progress);
                this.SignalUpdated();

                return result;
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

    protected abstract Task<bool> FetchFromAPI(Gw2ApiManager apiManager, IProgress<string> progress);

    protected void SignalUpdated()
    {
        this.LastUpdated = DateTimeOffset.UtcNow;
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