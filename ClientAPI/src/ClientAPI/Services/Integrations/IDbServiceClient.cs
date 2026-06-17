namespace ClientAPI.Services.Integrations;

public interface IDbServiceClient
{
    Task<string> GetDataAsync(int entityId, CancellationToken cancellationToken = default);
}
