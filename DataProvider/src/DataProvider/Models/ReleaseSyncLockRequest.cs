using System.ComponentModel.DataAnnotations;
using DataProvider.Models.Enums;

namespace DataProvider.Models;

public class ReleaseSyncLockRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Tenant { get; set; } = string.Empty;

    public DateTime SyncDate { get; set; }

    public SyncLockStatus Status { get; set; }
}
