namespace ClientAPI.Services.Concurrency;

/// <summary>
/// <see cref="SemaphoreSlim"/>-backed single-slot guard: at most one caller holds the lock at a time.
/// </summary>
public sealed class SingleInflightGuard : ISingleInflightGuard, IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<IDisposable> EnterAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        return new Releaser(_semaphore);
    }

    public void Dispose() => _semaphore.Dispose();

    private sealed class Releaser : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;
        private int _released;

        public Releaser(SemaphoreSlim semaphore) => _semaphore = semaphore;

        // Guard against double-dispose over-releasing the slot (which would throw SemaphoreFullException).
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0)
                _semaphore.Release();
        }
    }
}
