namespace Estreya.BlishHUD.EventTable.Utils
{
    using Blish_HUD;
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

        public static Task UpdateAsync(Func<GameTime, Task> call, GameTime gameTime, double interval, ref double lastCheck)
        {
            lastCheck += gameTime.ElapsedGameTime.TotalMilliseconds;

            if (lastCheck >= interval)
            {
                lock (_asyncStateMonitor)
                {
                    if (_asyncStateMonitor.Contains(call.Method.MethodHandle.Value))
                    {
                        Logger.Debug($"Async {call.Method.Name} has skipped its cadence because it has not completed running.");
                        return Task.CompletedTask;
                    }

                    _asyncStateMonitor.Add(call.Method.MethodHandle.Value);
                }

                Task task = call(gameTime).ContinueWith(_ =>
                {
                    lock (_asyncStateMonitor)
                    {
                        _asyncStateMonitor.Remove(call.Method.MethodHandle.Value);
                    }
                });
                lastCheck = 0;

                return task;
            }

            return Task.CompletedTask;
        }
    }
}
