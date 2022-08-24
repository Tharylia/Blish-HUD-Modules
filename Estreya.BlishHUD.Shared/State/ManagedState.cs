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

        protected CancellationTokenSource CancellationTokenSource { get; } = new CancellationTokenSource();

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

            await this.Initialize();

            this.Running = true;

            await this.Load();
        }

        public void Stop()
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
                _ = UpdateUtil.UpdateAsync(this.SaveWrapper, gameTime, this.Configuration.SaveInterval.TotalMilliseconds, this._lastSaved);
            }

            try
            {
                this.InternalUpdate(gameTime);
            }catch(Exception ex)
            {
                Logger.Error(ex,"Failed to update:");
            }
        }

        public async Task Reload()
        {
            if (!this.Running)
            {
                Logger.Warn("Trying to reload, but not running.");
                return;
            }

            Logger.Debug("Reloading state.");

            await this.InternalReload();
        }

        protected abstract Task InternalReload();

        private void Unload()
        {
            if (!this.Running)
            {
                Logger.Warn("Trying to unload, but not running.");
                return;
            }

            Logger.Debug("Unloading state.");

            this.CancellationTokenSource?.Cancel();

            this.InternalUnload();
        }

        public abstract Task Clear();

        protected abstract void InternalUnload();

        protected abstract Task Initialize();

        protected abstract void InternalUpdate(GameTime gameTime);

        private async Task SaveWrapper()
        {
            Logger.Debug("Starting save.");
            await this.Save();
            Logger.Debug("Finished save.");
        }

        protected abstract Task Save();
        protected abstract Task Load();

        public void Dispose()
        {
            this.Unload();
            this.Stop();
        }
    }
}
