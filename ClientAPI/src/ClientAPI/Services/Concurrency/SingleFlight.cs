namespace ClientAPI.Services.Concurrency;

/// <summary>
/// Double-checked-locking implementation of <see cref="ISingleFlight"/>, layered over an
/// <see cref="ISingleInflightGuard"/>. The guard supplies mutual exclusion; this type owns the
/// check-lock-check policy, so swapping the guard (semaphore, distributed lock, queue lease)
/// requires no change here.
/// </summary>
public sealed class SingleFlight : ISingleFlight
{
    private readonly ISingleInflightGuard _guard;

    public SingleFlight(ISingleInflightGuard guard) => _guard = guard;

    public async Task RunIfNeededAsync(
        Func<bool> isSatisfied,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default)
    {
        // Lock-free fast path: the common case never enters the guard.
        if (isSatisfied())
            return;

        using (await _guard.EnterAsync(cancellationToken))
        {
            // Re-check under the guard: a caller that queued behind an in-flight action may find
            // the condition already satisfied, coalescing concurrent callers onto one execution.
            if (isSatisfied())
                return;

            await action(cancellationToken);
        }
    }
}
