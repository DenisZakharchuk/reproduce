using ClientAPI.Configuration;
using ClientAPI.Services.Integrations;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace ClientAPI.HealthChecks;

/// <summary>
/// POSTs to a shared endpoint once per configured check and reports Unhealthy if any
/// returned amount exceeds its threshold. Requests run concurrently (bounded) and the
/// check short-circuits: the first breach cancels the remaining requests.
/// </summary>
public class AmountThresholdHealthCheck : IHealthCheck
{
    private readonly IAmountCheckClient _client;
    private readonly IOptionsMonitor<AmountThresholdHealthCheckOptions> _options;
    private readonly ILogger<AmountThresholdHealthCheck> _logger;

    public AmountThresholdHealthCheck(
        IAmountCheckClient client,
        IOptionsMonitor<AmountThresholdHealthCheckOptions> options,
        ILogger<AmountThresholdHealthCheck> logger)
    {
        _client = client;
        _options = options;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var options = _options.CurrentValue;
        if (options.Checks.Count == 0)
            return HealthCheckResult.Healthy("No amount checks configured.");

        // Cancelling this source stops Parallel.ForEachAsync from starting new iterations
        // and cancels in-flight requests once a breach (or an error) is found.
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Holds the first breach/error captured across threads; set exactly once via CAS.
        CheckFailure? failure = null;

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Max(1, options.MaxConcurrency),
            CancellationToken = cts.Token
        };

        try
        {
            await Parallel.ForEachAsync(options.Checks, parallelOptions, async (check, token) =>
            {
                // A prior breach/error already decided the outcome — skip before starting a request.
                if (token.IsCancellationRequested)
                    return;

                var result = await RunCheckAsync(check.Key, check.Value, options.RequestTimeoutSeconds, token);
                if (result is not null)
                    TrySetFailure(ref failure, cts, result);
            });
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            // Expected: our own short-circuit cancellation after a failure was recorded.
        }

        if (failure is not null)
        {
            var description = Describe(failure);
            _logger.LogWarning("Amount threshold health check unhealthy: {Failure}", description);
            return HealthCheckResult.Unhealthy(description);
        }

        return HealthCheckResult.Healthy("All amounts are below their thresholds.");
    }

    // Performs a single check. Returns null when the amount is within its threshold,
    // or a CheckFailure otherwise. Knows nothing about concurrency or short-circuiting.
    private async Task<CheckFailure?> RunCheckAsync(
        string name, decimal threshold, int requestTimeoutSeconds, CancellationToken token)
    {
        using var requestCts = CancellationTokenSource.CreateLinkedTokenSource(token);
        requestCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, requestTimeoutSeconds)));

        decimal amount;
        try
        {
            amount = await _client.GetAmountAsync(name, requestCts.Token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // Another check already decided the outcome — stop quietly.
            return null;
        }
        catch (Exception ex)
        {
            return new CheckFailure.TechnicalIssue(name, ex.Message);
        }

        return amount > threshold
            ? new CheckFailure.ThresholdBreach(name, amount, threshold)
            : null;
    }

    private static string Describe(CheckFailure failure) => failure switch
    {
        CheckFailure.ThresholdBreach b =>
            $"Check '{b.Name}' returned {b.Amount}, which exceeds threshold {b.Threshold}.",
        CheckFailure.TechnicalIssue t =>
            $"Check '{t.Name}' failed to execute: {t.Reason}",
        _ => $"Check '{failure.Name}' failed."
    };

    // Records the first failure atomically and cancels the remaining work.
    private static void TrySetFailure(ref CheckFailure? failure, CancellationTokenSource cts, CheckFailure value)
    {
        if (Interlocked.CompareExchange(ref failure, value, null) is null)
            cts.Cancel();
    }
}
