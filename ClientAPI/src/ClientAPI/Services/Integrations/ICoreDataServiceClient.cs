namespace ClientAPI.Services.Integrations;

/// <summary>
/// Common abstraction over both Original and New Core data services.
/// Implementations are resolved at runtime based on the request's OSR value.
/// </summary>
public interface ICoreDataServiceClient
{
    Task<string> GetDataAsync(int entityId, CancellationToken cancellationToken = default);
}
