namespace ClientAPI.HealthChecks;

/// <summary>
/// Outcome of a failed amount check: either the returned amount exceeded its
/// threshold (<see cref="ThresholdBreach"/>) or the request could not be completed
/// (<see cref="TechnicalIssue"/>).
/// </summary>
public abstract record CheckFailure(string Name)
{
    public sealed record ThresholdBreach(string Name, decimal Amount, decimal Threshold) : CheckFailure(Name);

    public sealed record TechnicalIssue(string Name, string Reason) : CheckFailure(Name);
}
