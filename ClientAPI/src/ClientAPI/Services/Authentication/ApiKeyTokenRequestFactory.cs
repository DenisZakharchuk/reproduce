using ClientAPI.Configuration;
using Microsoft.Extensions.Options;

namespace ClientAPI.Services.Authentication;

public class ApiKeyTokenRequestFactory : ITokenRequestFactory
{
    private readonly IOptionsMonitor<KeyProviderOptions> _options;

    public ApiKeyTokenRequestFactory(IOptionsMonitor<KeyProviderOptions> options) => _options = options;

    public TokenRequestDescriptor Create()
    {
        var options = _options.CurrentValue;
        return new TokenRequestDescriptor(options.TokenUrl, new TokenRequest(options.ApiKey));
    }

    private sealed record TokenRequest(string Token);
}
