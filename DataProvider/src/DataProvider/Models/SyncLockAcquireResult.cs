using DataProvider.Models.Enums;

namespace DataProvider.Models;

public class SyncLockAcquireResult
{
    public bool Acquired { get; set; }

    public SyncLockAcquireOutcome Outcome { get; set; }

    public string Tenant { get; set; } = string.Empty;

    public DateTime SyncDate { get; set; }

    public DateTime ExpiredOn { get; set; }

    public SyncLockStatus Status { get; set; }
}
