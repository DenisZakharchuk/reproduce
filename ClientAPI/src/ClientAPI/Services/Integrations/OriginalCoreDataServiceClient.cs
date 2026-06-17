using ClientAPI.Configuration;
using ClientAPI.Models;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace ClientAPI.Services.Integrations;

public interface IOriginalCoreDataServiceClient
{
    Task<OriginalCoreDataResponse> GetDataAsync(int entityId, CancellationToken cancellationToken = default);
}

public class OriginalCoreDataServiceClient : IOriginalCoreDataServiceClient
{
    private readonly HttpClient _httpClient;

    public OriginalCoreDataServiceClient(IHttpClientFactory httpClientFactory, IOptionsMonitor<CoreDataServiceOptions> options)
    {
        _httpClient = httpClientFactory.CreateClient("OriginalCoreDataService");
        _httpClient.BaseAddress = new Uri(options.CurrentValue.OriginalBaseUrl);
    }

    public async Task<OriginalCoreDataResponse> GetDataAsync(int entityId, CancellationToken cancellationToken = default)
    {
        // TODO: implement actual call
        var response = await _httpClient.GetAsync($"api/data/{entityId}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OriginalCoreDataResponse>(cancellationToken)
               ?? throw new InvalidOperationException("Empty response from Original Core Data Service.");
    }
}
