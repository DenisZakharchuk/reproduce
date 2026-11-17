using System.ComponentModel.DataAnnotations;

namespace DataProvider.Models;

public class AcquireSyncLockRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Tenant { get; set; } = string.Empty;

    public DateTime SyncDate { get; set; }
}
