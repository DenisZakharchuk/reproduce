using APINexus.Models;

namespace APINexus.Services;

/// <summary>
/// Fetches an API source's OpenAPI document and, when the source declares
/// "Environments" in configuration, injects the "x-scalar-environments" /
/// "x-scalar-active-environment" extensions so Scalar can offer predefined,
/// reusable sets of request values.
/// </summary>
public interface IApiSourceSpecService
{
    Task<ApiSourceSpecResult> GetRewrittenSpecAsync(
        string sourceId,
        IReadOnlyCollection<string> userRoles,
        CancellationToken cancellationToken);
}
