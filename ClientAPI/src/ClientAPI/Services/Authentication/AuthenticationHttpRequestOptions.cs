namespace ClientAPI.Services.Authentication;

/// <summary>
/// Well-known <see cref="HttpRequestMessage.Options"/> keys used by the
/// authentication pipeline.
/// </summary>
public static class AuthenticationHttpRequestOptions
{
    /// <summary>
    /// When set, the authenticator must obtain a fresh token, bypassing any cache.
    /// Set by <see cref="RetryOnUnauthorizedHandler"/> before a retry.
    /// </summary>
    public static readonly HttpRequestOptionsKey<bool> ForceReauthentication = new("ForceReauthentication");
}
