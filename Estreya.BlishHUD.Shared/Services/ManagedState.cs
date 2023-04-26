namespace Estreya.BlishHUD.Shared.State
{
    using Blish_HUD;
    using Estreya.BlishHUD.Shared.Threading;
    using Estreya.BlishHUD.Shared.Utils;
    using Microsoft.Xna.Framework;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public abstract class ManagedState : IDisposable
    {
        protected Logger Logger;

        private readonly AsyncRef<double> _lastSaved = 0;

        protected StateConfiguration Configuration { get; }

        protected CancellationTokenSource _cancellationTokenSource;

        public bool Running { get; private set; } = false;
        public bool AwaitLoading => this.Configuration.AwaitLoading;

        protected ManagedState(StateConfiguration configuration)
        {
            this.Logger = Logger.GetLogger(this.GetType());
            this.Configuration = configuration;
        }

        public async Task Start()
        {
            if (this.Running)
            {
                Logger.Warn("Trying to start, but already running.");
                return;
            }

            Logger.Debug("Starting state.");

            this._cancellationTokenSource = new CancellationTokenSource();

            await this.Initialize();

            this.Running = true;

            await this.Load();
        }

        private void Stop()
        {
            if (!this.Running)
            {
                Logger.Warn("Trying to stop, but not running.");
                return;
            }

            Logger.Debug("Stopping state.");

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
            }catch(Exception ex)
            {
                Logger.Error(ex,"Failed to update:");
            }
        }

        /// <summary>
        /// Clears and reloads the state
        /// </summary>
        /// <returns></returns>
        public async Task Reload()
        {
            if (!this.Running)
            {
                Logger.Warn("Trying to reload, but not running.");
                return;
            }

            Logger.Debug("Reloading state.");

            await this.Clear();
            await this.Load();

            await this.InternalReload();
        }

        /// <summary>
        /// Clears the state and requests further unload from subclasses.
        /// </summary>
        private void Unload()
        {
            if (this._cancellationTokenSource.IsCancellationRequested)
            {
                Logger.Warn("Already unloaded.");
                return;
            }

            Logger.Debug("Unloading state.");

            this._cancellationTokenSource.Cancel();

            this.InternalUnload();
        }

        protected abstract Task Initialize();
        protected abstract Task Load();

        protected virtual Task Save()=> Task.CompletedTask;

        protected abstract void InternalUpdate(GameTime gameTime);

        protected virtual Task InternalReload() => Task.CompletedTask;

        protected virtual Task Clear() => Task.CompletedTask;

        protected abstract void InternalUnload();

        public void Dispose()
        {
            this.Stop();
            this.Unload();
        }
    }
}
