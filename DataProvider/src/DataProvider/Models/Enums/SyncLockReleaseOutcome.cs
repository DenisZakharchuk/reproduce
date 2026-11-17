namespace DataProvider.Models.Enums;

public enum SyncLockReleaseOutcome
{
    Released,
    AlreadyReleased,
    NotFound,
    AlreadyReleasedWithDifferentStatus
}
