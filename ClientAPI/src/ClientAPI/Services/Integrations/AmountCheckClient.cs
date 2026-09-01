using ClientAPI.Configuration;
using ClientAPI.Models;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace ClientAPI.Services.Integrations;

public interface IAmountCheckClient
{
    Task<decimal> GetAmountAsync(string name, CancellationToken cancellationToken = default);
}

public class AmountCheckClient : IAmountCheckClient
{
    private readonly HttpClient _httpClient;

    public AmountCheckClient(IHttpClientFactory httpClientFactory, IOptionsMonitor<AmountThresholdHealthCheckOptions> options)
    {
        _httpClient = httpClientFactory.CreateClient("AmountThresholdHealthCheck");
        _httpClient.BaseAddress = new Uri(options.CurrentValue.Url);
    }

    public async Task<decimal> GetAmountAsync(string name, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            string.Empty, new AmountCheckRequest { Name = name }, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AmountCheckResponse>(cancellationToken)
                     ?? throw new InvalidOperationException($"Empty response from amount check '{name}'.");
        return result.Amount;
    }
}
