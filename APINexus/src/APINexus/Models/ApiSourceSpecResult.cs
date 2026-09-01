namespace APINexus.Models;

public enum ApiSourceSpecStatus
{
    Ok,
    NotFound,
    Forbidden,
    UpstreamError
}

public record ApiSourceSpecResult(ApiSourceSpecStatus Status, string? Json = null);
