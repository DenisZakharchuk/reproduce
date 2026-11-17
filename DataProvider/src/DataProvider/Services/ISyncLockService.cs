using DataProvider.Models;

namespace DataProvider.Services;

public interface ISyncLockService
{
    Task<SyncLockAcquireResult> AcquireAsync(
        AcquireSyncLockRequest request,
        CancellationToken cancellationToken = default);

    Task<SyncLockReleaseResult> ReleaseAsync(
        ReleaseSyncLockRequest request,
        CancellationToken cancellationToken = default);
}
