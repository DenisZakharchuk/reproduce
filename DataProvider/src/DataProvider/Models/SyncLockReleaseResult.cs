using DataProvider.Models.Enums;

namespace DataProvider.Models;

public class SyncLockReleaseResult
{
    public SyncLockReleaseOutcome Outcome { get; set; }

    public bool Released => Outcome is SyncLockReleaseOutcome.Released or SyncLockReleaseOutcome.AlreadyReleased;

    public string Tenant { get; set; } = string.Empty;

    public DateTime SyncDate { get; set; }

    public DateTime? ExpiredOn { get; set; }

    public SyncLockStatus? Status { get; set; }
}
