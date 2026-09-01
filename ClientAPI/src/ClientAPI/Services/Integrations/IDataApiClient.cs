namespace ClientAPI.Services.Integrations;

public interface IDataApiClient
{
    Task<string> GetResourceAsync(string resourceId, CancellationToken cancellationToken = default);
}
