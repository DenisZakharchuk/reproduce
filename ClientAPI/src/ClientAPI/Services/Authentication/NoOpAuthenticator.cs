namespace ClientAPI.Services.Authentication;

/// <summary>Null-object authenticator: leaves the request unauthenticated.</summary>
public class NoOpAuthenticator : IRequestAuthenticator
{
    public Task AuthenticateAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
