namespace APINexus.Configuration;

public class ApiSourcesOptions
{
    public List<ApiSourceEntry> Sources { get; set; } = [];
}

public class ApiSourceEntry
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string SpecUrl { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = [];

    /// <summary>
    /// Optional named sets of reusable request values (à la Postman environments),
    /// rendered into the OpenAPI document as the "x-scalar-environments" extension.
    /// When non-empty, the spec is served through a local rewriting endpoint instead
    /// of being loaded directly from <see cref="SpecUrl"/>.
    /// </summary>
    public Dictionary<string, ApiEnvironmentEntry> Environments { get; set; } = [];

    /// <summary>Name of the environment selected by default (must be a key in <see cref="Environments"/>).</summary>
    public string? ActiveEnvironment { get; set; }
}

public class ApiEnvironmentEntry
{
    public string? Description { get; set; }
    public string? Color { get; set; }
    public Dictionary<string, string> Variables { get; set; } = [];
}
