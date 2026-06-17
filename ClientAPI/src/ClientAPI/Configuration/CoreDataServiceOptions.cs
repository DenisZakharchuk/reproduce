using ClientAPI.Models.Enums;

namespace ClientAPI.Configuration;

/// <summary>
/// Configuration for the Core data services.
/// OsrMappings drives which service (Original or New) is used for each OSR value.
/// </summary>
public class CoreDataServiceOptions
{
    public string OriginalBaseUrl { get; set; } = string.Empty;
    public string NewBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Maps each <see cref="Osr"/> value to the Core data service type to use.
    /// Keys must match <see cref="Osr"/> enum member names.
    /// </summary>
    public Dictionary<Osr, CoreDataServiceType> OsrMappings { get; set; } = new();
}
