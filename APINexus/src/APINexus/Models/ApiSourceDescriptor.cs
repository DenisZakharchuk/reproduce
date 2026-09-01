namespace APINexus.Models;

public record ApiSourceDescriptor(
    string Id,
    string Title,
    string SpecUrl,
    IReadOnlyList<string> RequiredRoles,
    IReadOnlyDictionary<string, ApiEnvironmentDescriptor> Environments,
    string? ActiveEnvironment)
{
    /// <summary>True when this source's spec must be served through the local
    /// rewriting endpoint (to inject "x-scalar-environments") instead of directly
    /// from <see cref="SpecUrl"/>.</summary>
    public bool RequiresSpecRewrite => Environments.Count > 0;
}

public record ApiEnvironmentDescriptor(
    string? Description,
    string? Color,
    IReadOnlyDictionary<string, string> Variables);
