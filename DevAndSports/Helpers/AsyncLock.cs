using System;
using System.Threading;
using System.Threading.Tasks;

namespace DevAndSports.Helpers
{
    public sealed class AsyncLock
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly Task<IDisposable> _releaser;

        public AsyncLock()
        {
            _releaser = Task.FromResult((IDisposable)new Releaser(this));
        }

        public Task<IDisposable> LockAsync()
        {
            return LockAsync(CancellationToken.None);
        }

        public Task<IDisposable> LockAsync(CancellationToken ct)
        {
            var wait = _semaphore.WaitAsync(ct);
            ct.ThrowIfCancellationRequested();
            return wait.IsCompleted
                ? _releaser
                : wait.ContinueWith(
                    (_, state) => (IDisposable)state,
                    _releaser.Result,
                    ct,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
        }

        private sealed class Releaser : IDisposable
        {
            private readonly AsyncLock m_toRelease;
            internal Releaser(AsyncLock toRelease) { m_toRelease = toRelease; }
            public void Dispose() { m_toRelease._semaphore.Release(); }
        }
    }
}
