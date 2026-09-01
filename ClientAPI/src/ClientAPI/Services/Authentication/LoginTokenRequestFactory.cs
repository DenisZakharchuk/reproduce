using ClientAPI.Configuration;
using Microsoft.Extensions.Options;

namespace ClientAPI.Services.Authentication;

public class LoginTokenRequestFactory : ITokenRequestFactory
{
    private readonly IOptionsMonitor<KeyProviderOptions> _options;

    public LoginTokenRequestFactory(IOptionsMonitor<KeyProviderOptions> options) => _options = options;

    public TokenRequestDescriptor Create()
    {
        var options = _options.CurrentValue;
        return new TokenRequestDescriptor(options.LoginUrl, new LoginRequest(options.Username, options.Password));
    }

    private sealed record LoginRequest(string Username, string Password);
}
