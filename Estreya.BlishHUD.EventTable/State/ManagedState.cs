namespace Estreya.BlishHUD.EventTable.State
{
    using Blish_HUD;
    using Estreya.BlishHUD.EventTable.Helpers;
    using Microsoft.Xna.Framework;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public abstract class ManagedState : IDisposable
    {
        private static readonly Logger Logger = Logger.GetLogger<ManagedState>();

        private SemaphoreSlim _saveSemaphore = new SemaphoreSlim(1, 1);

        private int SaveInternal { get; set; }

        private TimeSpan TimeSinceSave { get; set; } = TimeSpan.Zero;

        public bool Running { get; private set; } = false;
        public bool AwaitLoad { get; }

        protected ManagedState(bool awaitLoad = true, int saveInterval = 60000)
        {
            this.AwaitLoad = awaitLoad;
            this.SaveInternal = saveInterval;
        }

        public async Task Start()
        {
            if (this.Running)
            {
                Logger.Warn("Trying to start state \"{0}\" which is already running.", this.GetType().Name);
                return;
            }

            Logger.Debug("Starting managed state: {0}", this.GetType().Name);

            await this.Initialize();
            await this.Load();

            this.Running = true;
        }

        public void Stop()
        {
            if (!this.Running)
            {
                Logger.Warn("Trying to stop state \"{0}\" which is not running.", this.GetType().Name);
                return;
            }

            Logger.Debug("Stopping managed state: {0}", this.GetType().Name);

            this.Running = false;
        }

        public void Update(GameTime gameTime)
        {
            if (!this.Running)
            {
                return;
            }

            this.TimeSinceSave += gameTime.ElapsedGameTime;

            if (this.SaveInternal != -1 && this.TimeSinceSave.TotalMilliseconds >= this.SaveInternal)
            {
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
                }
            }

            this.InternalUpdate(gameTime);
        }

        public async Task Reload()
        {
            if (!this.Running)
            {
                Logger.Warn("Trying to reload state \"{0}\" which is not running.", this.GetType().Name);
                return;
            }

            Logger.Debug("Reloading state: {0}", this.GetType().Name);

            await this.InternalReload();
        }

        protected abstract Task InternalReload();

        private void Unload()
        {
            if (!this.Running)
            {
                Logger.Warn("Trying to unload state \"{0}\" which is not running.", this.GetType().Name);
                return;
            }

            Logger.Debug("Unloading state: {0}", this.GetType().Name);

            this.InternalUnload();
        }

        public abstract Task Clear();

        protected abstract void InternalUnload();

        protected abstract Task Initialize();

        protected abstract void InternalUpdate(GameTime gameTime);

        protected abstract Task Save();
        protected abstract Task Load();

        public void Dispose()
        {
            this.Unload();
            this.Stop();
        }
    }
}
