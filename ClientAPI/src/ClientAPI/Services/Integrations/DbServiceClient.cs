using ClientAPI.Configuration;
using Microsoft.Extensions.Options;

namespace ClientAPI.Services.Integrations;

public class DbServiceClient : IDbServiceClient
{
    private readonly HttpClient _httpClient;

    public DbServiceClient(IHttpClientFactory httpClientFactory, IOptionsMonitor<DbServiceOptions> options)
    {
        _httpClient = httpClientFactory.CreateClient("DbService");
        _httpClient.BaseAddress = new Uri(options.CurrentValue.BaseUrl);
    }

    public async Task<string> GetDataAsync(int entityId, CancellationToken cancellationToken = default)
    {
        // TODO: implement actual call
        var response = await _httpClient.GetAsync($"api/data/{entityId}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}
