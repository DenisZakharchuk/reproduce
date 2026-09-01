namespace ClientAPI.Configuration;

/// <summary>
/// Configuration for acquiring the downstream API token.
/// <see cref="UseLogin"/> selects which credential strategy is registered.
/// </summary>
public class KeyProviderOptions
{
    public bool UseLogin { get; set; }
    public string LoginUrl { get; set; } = string.Empty;
    public string TokenUrl { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}
