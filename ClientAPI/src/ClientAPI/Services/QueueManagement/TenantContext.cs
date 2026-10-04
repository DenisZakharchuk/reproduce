namespace ClientAPI.Services.QueueManagement;

/// <summary>Mutable, scope-lived implementation of <see cref="ITenantContext"/>.</summary>
public sealed class TenantContext : ITenantContext
{
    public string QueueKey { get; set; } = string.Empty;
}
