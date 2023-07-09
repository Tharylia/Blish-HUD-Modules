namespace Estreya.BlishHUD.Shared.Extensions;

using System;
using System.Threading;
using System.Threading.Tasks;

public static class WaitHandleExtensions
{
    public static Task<bool> WaitOneAsync(this WaitHandle waitHandle, TimeSpan timeout, CancellationToken cancellationToken)
    {
        if (waitHandle == null)
        {
            throw new ArgumentNullException(nameof(waitHandle));
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromResult(true);
        }

        TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

        RegisteredWaitHandle registeredWaitHandle = ThreadPool.RegisterWaitForSingleObject(
            waitHandle,
            (state, timedOut) => { tcs.TrySetResult(!timedOut); },
            null,
            timeout,
            true);

        cancellationToken.Register(() =>
        {
            if (registeredWaitHandle.Unregister(null))
            {
                tcs.SetCanceled();
            }
        });

        return tcs.Task.ContinueWith(continuationTask =>
        {
            registeredWaitHandle.Unregister(null);
            try
            {
                return continuationTask.Result;
            }
            catch
            {
                return false;
            }
        });
    }
}