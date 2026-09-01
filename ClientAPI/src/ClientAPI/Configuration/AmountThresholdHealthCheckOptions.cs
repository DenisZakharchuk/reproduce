namespace ClientAPI.Configuration;

/// <summary>
/// Configuration for the amount-threshold health check.
/// Each entry in <see cref="Checks"/> maps a check name (sent as the POST payload)
/// to the maximum allowed amount; the check is unhealthy if any returned amount exceeds it.
/// </summary>
public class AmountThresholdHealthCheckOptions
{
    /// <summary>Base URL of the endpoint that returns an amount for a given check name.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Maximum number of check requests executed concurrently.</summary>
    public int MaxConcurrency { get; set; } = 4;

    /// <summary>Per-request timeout in seconds.</summary>
    public int RequestTimeoutSeconds { get; set; } = 5;

    /// <summary>Maps each check name to its amount threshold (exclusive upper bound).</summary>
    public Dictionary<string, decimal> Checks { get; set; } = new();
}
