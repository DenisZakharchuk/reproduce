namespace APINexus.Configuration;

/// <summary>
/// Proxy settings applied to the Scalar API reference. The proxy is OPT-IN: leave
/// <see cref="ProxyUrl"/> unset/empty to disable it (test requests against external
/// APIs will then be subject to the browser's CORS policy).
/// </summary>
public class ScalarProxyOptions
{
    public string? ProxyUrl { get; set; }
}
