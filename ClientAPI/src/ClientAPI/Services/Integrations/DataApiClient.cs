namespace ClientAPI.Services.Integrations;

public class DataApiClient : IDataApiClient
{
    private readonly HttpClient _httpClient;

    // The "dataApiHttpClient" is configured with BearerTokenHandler, so the
    // Authorization header is added automatically on every request.
    public DataApiClient(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("dataApiHttpClient");
    }

    public async Task<string> GetResourceAsync(string resourceId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/resources/{resourceId}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}
