using ClientAPI.Models;

namespace ClientAPI.Services.Authentication;

public interface IKeyProvider
{
    Task<AuthData> GetApiKeyAsync(CancellationToken cancellationToken = default);
}
