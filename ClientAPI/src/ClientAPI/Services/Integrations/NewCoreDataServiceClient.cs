using ClientAPI.Configuration;
using ClientAPI.Models;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace ClientAPI.Services.Integrations;

public interface INewCoreDataServiceClient
{
    Task<NewCoreDataResponse> GetDataAsync(string identifier, CancellationToken cancellationToken = default);
}

public class NewCoreDataServiceClient : INewCoreDataServiceClient
{
    private readonly HttpClient _httpClient;

    public NewCoreDataServiceClient(IHttpClientFactory httpClientFactory, IOptionsMonitor<CoreDataServiceOptions> options)
    {
        _httpClient = httpClientFactory.CreateClient("NewCoreDataService");
        _httpClient.BaseAddress = new Uri(options.CurrentValue.NewBaseUrl);
    }

    public async Task<NewCoreDataResponse> GetDataAsync(string identifier, CancellationToken cancellationToken = default)
    {
        // TODO: implement actual call
        var response = await _httpClient.GetAsync($"api/data/{Uri.EscapeDataString(identifier)}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<NewCoreDataResponse>(cancellationToken)
               ?? throw new InvalidOperationException("Empty response from New Core Data Service.");
    }
}
