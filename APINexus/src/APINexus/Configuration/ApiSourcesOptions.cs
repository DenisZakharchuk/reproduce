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
}
