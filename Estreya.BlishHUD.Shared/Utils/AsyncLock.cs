namespace Estreya.BlishHUD.Shared.Utils;

using System;
using System.Threading;
using System.Threading.Tasks;

public class AsyncLock
{
    private readonly IDisposable _releaser;
    private readonly Task<IDisposable> _releaserTask;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    public AsyncLock()
    {
        this._releaser = new Releaser(this._semaphore);
        this._releaserTask = Task.FromResult(this._releaser);
    }

    public IDisposable Lock()
    {
        this._semaphore.Wait();
        return this._releaser;
    }

    public bool IsFree()
    {
        return this._semaphore.CurrentCount > 0;
    }

    public void ThrowIfBusy(string message = null)
    {
        if (!this.IsFree())
        {
            throw new LockBusyException(message);
        }
    }

    public Task<IDisposable> LockAsync()
    {
        Task waitTask = this._semaphore.WaitAsync();
        return waitTask.IsCompleted
            ? this._releaserTask
            : waitTask.ContinueWith(
                (_, releaser) => (IDisposable)releaser, this._releaser,
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
    }

    private class Releaser : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;

        public Releaser(SemaphoreSlim semaphore)
        {
            this._semaphore = semaphore;
        }

        public void Dispose()
        {
            this._semaphore.Release();
        }
    }

    public class LockBusyException : Exception
    {
        public LockBusyException() : this(null) { }

        public LockBusyException(string message) : base(message ?? "The lock is currently busy and can't be entered.") { }
    }
}