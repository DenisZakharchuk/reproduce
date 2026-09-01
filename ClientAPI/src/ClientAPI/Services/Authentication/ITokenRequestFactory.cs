namespace ClientAPI.Services.Authentication;

/// <summary>
/// Builds the credential-specific request used to acquire an auth token.
/// Add a new authentication mode by implementing this interface — no change
/// to <see cref="KeyProvider"/> is required (Open/Closed).
/// </summary>
public interface ITokenRequestFactory
{
    TokenRequestDescriptor Create();
}

/// <param name="Url">Endpoint to POST the credentials to.</param>
/// <param name="Payload">Body serialized as JSON.</param>
public record TokenRequestDescriptor(string Url, object Payload);
