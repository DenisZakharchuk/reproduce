namespace ClientAPI.Services.QueueManagement;

/// <summary>
/// Supplies the queue key (e.g. tenant/partition) for the current scope. Populate it per request
/// (middleware/claims) on the producing side; consumers set it per message to their queue's key.
/// </summary>
public interface ITenantContext
{
    string QueueKey { get; }
}
