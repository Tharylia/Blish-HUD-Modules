namespace Estreya.BlishHUD.Shared.State
{
    using Blish_HUD;
    using Estreya.BlishHUD.Shared.Threading;
    using Estreya.BlishHUD.Shared.Utils;
    using Microsoft.Xna.Framework;
    using System;
    using System.Threading.Tasks;

    public abstract class ManagedState : IDisposable
    {
        protected Logger Logger;

        private readonly int _saveInterval;

        private readonly AsyncRef<double> _lastSaved = 0;

        public bool Running { get; private set; } = false;
        public bool AwaitLoad { get; }

        protected ManagedState(bool awaitLoad = true, int saveInterval = 60000)
        {
            this.AwaitLoad = awaitLoad;
            this._saveInterval = saveInterval;

            this.Logger = Logger.GetLogger(this.GetType());
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
            await this.Load();

            this.Running = true;
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

            if (this._saveInterval != -1 /*&& this.TimeSinceSave.TotalMilliseconds >= this.SaveInternal*/)
            {
                _ = UpdateUtil.UpdateAsync(this.SaveWrapper, gameTime, this._saveInterval, this._lastSaved);

                /*
                // Prevent multiple threads running Save() at the same time.
                if (_saveSemaphore.CurrentCount > 0)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _saveSemaphore.WaitAsync();
                            await this.Save();
                            this.TimeSinceSave = TimeSpan.Zero;
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex, "{0} failed saving.", this.GetType().Name);
                        }
                        finally
                        {
                            _ = _saveSemaphore.Release();
                        }
                    });
                }
                else
                {
                    Logger.Debug("Another thread is already running Save() for {0}", this.GetType().Name);
                }*/


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
