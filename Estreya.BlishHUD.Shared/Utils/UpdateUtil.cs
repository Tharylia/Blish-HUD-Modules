namespace Estreya.BlishHUD.Shared.Utils
{
    using Blish_HUD;
    using Estreya.BlishHUD.Shared.Threading;
    using Microsoft.Xna.Framework;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public static class UpdateUtil
    {
        private static readonly Logger Logger = Logger.GetLogger(typeof(UpdateUtil));

        private static readonly HashSet<IntPtr> _asyncStateMonitor = new HashSet<IntPtr>();

        public static void Update(Action<GameTime> call, GameTime gameTime, double interval, ref double lastCheck)
        {
            lastCheck += gameTime.ElapsedGameTime.TotalMilliseconds;

            if (lastCheck >= interval)
            {
                call(gameTime);
                lastCheck = 0;
            }
        }

        public static void Update(Action call, GameTime gameTime, double interval, ref double lastCheck)
        {
            lastCheck += gameTime.ElapsedGameTime.TotalMilliseconds;

            if (lastCheck >= interval)
            {
                call();
                lastCheck = 0;
            }
        }

        public static async Task UpdateAsync(Func<GameTime, Task> call, GameTime gameTime, double interval, AsyncRef<double> lastCheck)
        {
            lastCheck.Value += gameTime.ElapsedGameTime.TotalMilliseconds;

            if (lastCheck.Value >= interval)
            {
                lock (_asyncStateMonitor)
                {
                    if (_asyncStateMonitor.Contains(call.Method.MethodHandle.Value))
                    {
                        Logger.Debug($"Async {call.Method.Name} has skipped its cadence because it has not completed running.");
                        return;
                    }

                    _ = _asyncStateMonitor.Add(call.Method.MethodHandle.Value);
                }

                Logger.Debug("Start running update function '{0}'.", call.Method.Name);

                await call(gameTime).ContinueWith(task =>
                {
                    lock (_asyncStateMonitor)
                    {
                        _ = _asyncStateMonitor.Remove(call.Method.MethodHandle.Value);
                    }
                });

                Logger.Debug("Update function '{0}' finished running.", call.Method.Name);

                lastCheck.Value = 0;
            }
        }

        public static async Task UpdateAsync(Func<Task> call, GameTime gameTime, double interval, AsyncRef<double> lastCheck)
        {
            lastCheck.Value += gameTime.ElapsedGameTime.TotalMilliseconds;

            if (lastCheck.Value >= interval)
            {
                lock (_asyncStateMonitor)
                {
                    if (_asyncStateMonitor.Contains(call.Method.MethodHandle.Value))
                    {
                        Logger.Debug($"Async {call.Method.Name} has skipped its cadence because it has not completed running.");
                        return;
                    }

                    _ = _asyncStateMonitor.Add(call.Method.MethodHandle.Value);
                }

                Logger.Debug("Start running update function '{0}'.", call.Method.Name);

                await call().ContinueWith(task =>
                {
                    lock (_asyncStateMonitor)
                    {
                        _ = _asyncStateMonitor.Remove(call.Method.MethodHandle.Value);
                    }
                });

                Logger.Debug("Update function '{0}' finished running.", call.Method.Name);

                lastCheck.Value = 0;
            }
        }
    }
}
