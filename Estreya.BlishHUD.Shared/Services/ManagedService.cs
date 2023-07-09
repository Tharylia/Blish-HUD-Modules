namespace Estreya.BlishHUD.Shared.Services;

using Blish_HUD;
using Microsoft.Xna.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;
using Threading;
using Utils;

public abstract class ManagedService : IDisposable
{
    private readonly AsyncRef<double> _lastSaved = new AsyncRef<double>(0);

    private CancellationTokenSource _cancellationTokenSource;
    protected Logger Logger;

    protected ManagedService(ServiceConfiguration configuration)
    {
        this.Logger = Logger.GetLogger(this.GetType());
        this.Configuration = configuration;
    }

    protected ServiceConfiguration Configuration { get; }

    protected CancellationToken CancellationToken => this._cancellationTokenSource.Token;

    public bool Running { get; private set; }
    public bool AwaitLoading => this.Configuration.AwaitLoading;

    public void Dispose()
    {
        this.Stop();
        this.Unload();
    }

    public async Task Start()
    {
        if (this.Running)
        {
            this.Logger.Warn("Trying to start, but already running.");
            return;
        }

        this.Logger.Debug("Starting state.");

        this._cancellationTokenSource = new CancellationTokenSource();

        await this.Initialize();

        this.Running = true;

        await this.Load();
    }

    private void Stop()
    {
        if (!this.Running)
        {
            this.Logger.Warn("Trying to stop, but not running.");
            return;
        }

        this.Logger.Debug("Stopping state.");

        this.Running = false;
    }

    public void Update(GameTime gameTime)
    {
        if (!this.Running)
        {
            return;
        }

        if (this.Configuration.SaveInterval != Timeout.InfiniteTimeSpan)
        {
            _ = UpdateUtil.UpdateAsync(this.Save, gameTime, this.Configuration.SaveInterval.TotalMilliseconds, this._lastSaved);
        }

        try
        {
            this.InternalUpdate(gameTime);
        }
        catch (Exception ex)
        {
            this.Logger.Error(ex, "Failed to update:");
        }
    }

    /// <summary>
    ///     Clears and reloads the state
    /// </summary>
    /// <returns></returns>
    public async Task Reload()
    {
        if (!this.Running)
        {
            this.Logger.Warn("Trying to reload, but not running.");
            return;
        }

        this.Logger.Debug("Reloading state.");

        this._cancellationTokenSource.Cancel();
        this._cancellationTokenSource = new CancellationTokenSource();

        await this.Clear();
        await this.Load();

        await this.InternalReload();
    }

    /// <summary>
    ///     Clears the state and requests further unload from subclasses.
    /// </summary>
    private void Unload()
    {
        if (this._cancellationTokenSource.IsCancellationRequested)
        {
            this.Logger.Warn("Already unloaded.");
            return;
        }

        this.Logger.Debug("Unloading state.");

        this._cancellationTokenSource.Cancel();

        this.InternalUnload();
    }

    protected abstract Task Initialize();
    protected abstract Task Load();

    protected virtual Task Save()
    {
        return Task.CompletedTask;
    }

    protected abstract void InternalUpdate(GameTime gameTime);

    protected virtual Task InternalReload()
    {
        return Task.CompletedTask;
    }

    protected virtual Task Clear()
    {
        return Task.CompletedTask;
    }

    protected abstract void InternalUnload();
}