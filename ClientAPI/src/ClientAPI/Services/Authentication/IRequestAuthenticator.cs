namespace ClientAPI.Services.Authentication;

/// <summary>
/// Applies an authentication scheme to an outgoing request. Add a new scheme by
/// implementing this interface — the handler is never modified (Open/Closed).
/// </summary>
public interface IRequestAuthenticator
{
    Task AuthenticateAsync(HttpRequestMessage request, CancellationToken cancellationToken);
}
