namespace ClientAPI.Services.Concurrency;

/// <summary>
/// Single-flight coordination: coalesces concurrent callers so a guarded action runs at most once.
/// The check-lock-check (double-checked locking) is encapsulated here, over any
/// <see cref="ISingleInflightGuard"/> critical section.
/// </summary>
public interface ISingleFlight
{
    /// <summary>
    /// Checks <paramref name="isSatisfied"/> lock-free first and returns immediately if already
    /// satisfied. Otherwise enters the guard, RE-checks the condition — so work already completed by
    /// a concurrent caller is not repeated — and runs <paramref name="action"/> only when still needed.
    /// </summary>
    Task RunIfNeededAsync(
        Func<bool> isSatisfied,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default);
}
