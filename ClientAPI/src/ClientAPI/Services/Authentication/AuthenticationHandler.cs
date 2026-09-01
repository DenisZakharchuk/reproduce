namespace ClientAPI.Services.Authentication;

/// <summary>
/// Delegates request authentication to an <see cref="IRequestAuthenticator"/>.
/// The scheme (or none) is chosen at registration; this handler never changes.
/// </summary>
public class AuthenticationHandler : DelegatingHandler
{
    private readonly IRequestAuthenticator _authenticator;

    public AuthenticationHandler(IRequestAuthenticator authenticator) => _authenticator = authenticator;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await _authenticator.AuthenticateAsync(request, cancellationToken);
        return await base.SendAsync(request, cancellationToken);
    }
}
