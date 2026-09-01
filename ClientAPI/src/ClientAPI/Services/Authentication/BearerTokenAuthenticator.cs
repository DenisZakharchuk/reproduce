using System.Net.Http.Headers;

namespace ClientAPI.Services.Authentication;

public class BearerTokenAuthenticator : IRequestAuthenticator
{
    private readonly IKeyProvider _keyProvider;

    public BearerTokenAuthenticator(IKeyProvider keyProvider) => _keyProvider = keyProvider;

    public async Task AuthenticateAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var auth = await _keyProvider.GetApiKeyAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
    }
}
