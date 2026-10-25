namespace DataProvider.Data;

/// <summary>
/// Provides an automatically renewed, distributed mutual-exclusion lease.
/// </summary>
/// <remarks>
/// The requested duration is the renewal interval's TTL, not the maximum time the lock can be
/// held. Callers should monitor the returned lease's loss token and cancel protected work if
/// the lease is lost.
/// </remarks>
public interface IDistributedAsyncLock
{
    /// <summary>
    /// Waits until the specified lock can be acquired or the operation is canceled. The lock is
    /// renewed until the returned lease is disposed.
    /// </summary>
    Task<IDistributedLockLease> AcquireAsync(
        string key,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Acquires the specified lock immediately if it is available; otherwise returns
    /// <see langword="null"/>. The lock is renewed until the returned lease is disposed.
    /// </summary>
    Task<IDistributedLockLease?> TryAcquireAsync(
        string key,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// A renewable distributed lock lease.
/// </summary>
public interface IDistributedLockLease : IAsyncDisposable
{
    /// <summary>
    /// Is canceled if Redis reports that ownership has been lost or a renewal fails.
    /// Protected work should observe this token and stop when exclusive ownership is no longer
    /// guaranteed.
    /// </summary>
    CancellationToken LeaseLostToken { get; }
}
