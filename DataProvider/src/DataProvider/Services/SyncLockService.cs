using DataProvider.Data;
using DataProvider.Models;
using DataProvider.Models.Enums;

namespace DataProvider.Services;

public class SyncLockService : ISyncLockService
{
    private const int LeaseDurationMinutes = 10;
    private readonly IStoredProcedureExecutor _executor;

    public SyncLockService(IStoredProcedureExecutor executor)
    {
        _executor = executor;
    }

    public async Task<SyncLockAcquireResult> AcquireAsync(
        AcquireSyncLockRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateTenant(request.Tenant);

        var result = await _executor.QuerySingleOrDefaultAsync<SyncLockAcquireResult>(
            "usp_AcquireSyncLock",
            new
            {
                Tenant = request.Tenant.Trim(),
                SyncDate = request.SyncDate.Date,
                LeaseDurationMinutes
            },
            cancellationToken);

        if (result is null)
            throw new InvalidOperationException("The acquire sync lock procedure returned no result.");

        result.Outcome = result.Acquired
            ? SyncLockAcquireOutcome.Acquired
            : result.Status switch
            {
                SyncLockStatus.Running => SyncLockAcquireOutcome.AlreadyRunning,
                SyncLockStatus.Done => SyncLockAcquireOutcome.AlreadyDone,
                _ => throw new InvalidOperationException(
                    $"The acquire sync lock procedure returned an unexpected unavailable status: {result.Status}.")
            };

        return result;
    }

    public async Task<SyncLockReleaseResult> ReleaseAsync(
        ReleaseSyncLockRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateTenant(request.Tenant);
        if (request.Status is not (SyncLockStatus.Done or SyncLockStatus.Failed))
            throw new ArgumentOutOfRangeException(nameof(request), "Release status must be Done or Failed.");

        var result = await _executor.QuerySingleOrDefaultAsync<SyncLockReleaseRecord>(
            "usp_ReleaseSyncLock",
            new
            {
                Tenant = request.Tenant.Trim(),
                SyncDate = request.SyncDate.Date,
                Status = request.Status.ToString()
            },
            cancellationToken);

        if (result is null)
            throw new InvalidOperationException("The release sync lock procedure returned no result.");

        var outcome = result.Released
            ? result.WasAlreadyReleased
                ? SyncLockReleaseOutcome.AlreadyReleased
                : SyncLockReleaseOutcome.Released
            : result.Status is null
                ? SyncLockReleaseOutcome.NotFound
                : result.Status is SyncLockStatus.Done or SyncLockStatus.Failed
                    ? SyncLockReleaseOutcome.AlreadyReleasedWithDifferentStatus
                    : throw new InvalidOperationException(
                        $"The release sync lock procedure returned an unexpected status: {result.Status}.");

        return new SyncLockReleaseResult
        {
            Outcome = outcome,
            Tenant = result.Tenant,
            SyncDate = result.SyncDate,
            ExpiredOn = result.ExpiredOn,
            Status = result.Status
        };
    }

    private static void ValidateTenant(string tenant)
    {
        if (string.IsNullOrWhiteSpace(tenant) || tenant.Trim().Length > 100)
            throw new ArgumentException("Tenant must contain between 1 and 100 characters.", nameof(tenant));
    }

    private sealed class SyncLockReleaseRecord
    {
        public bool Released { get; set; }

        public bool WasAlreadyReleased { get; set; }

        public string Tenant { get; set; } = string.Empty;

        public DateTime SyncDate { get; set; }

        public DateTime? ExpiredOn { get; set; }

        public SyncLockStatus? Status { get; set; }
    }
}
