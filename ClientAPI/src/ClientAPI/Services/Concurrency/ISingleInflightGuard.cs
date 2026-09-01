namespace ClientAPI.Services.Concurrency;

/// <summary>
/// Async mutual-exclusion primitive: at most one holder at a time. The backing is an
/// infrastructure detail (in-process semaphore, distributed lock, queue lease, etc.).
/// </summary>
public interface ISingleInflightGuard
{
    /// <summary>
    /// Awaits until the single slot is free, then returns a handle whose disposal releases it:
    /// <c>using var _ = await guard.EnterAsync(ct);</c>
    /// </summary>
    Task<IDisposable> EnterAsync(CancellationToken cancellationToken = default);
}
