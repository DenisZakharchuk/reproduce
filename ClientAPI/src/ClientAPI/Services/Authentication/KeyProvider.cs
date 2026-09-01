using System.Net.Http.Json;
using ClientAPI.Models;

namespace ClientAPI.Services.Authentication;

public class KeyProvider : IKeyProvider
{
    private readonly HttpClient _httpClient;
    private readonly ITokenRequestFactory _requestFactory;

    public KeyProvider(IHttpClientFactory httpClientFactory, ITokenRequestFactory requestFactory)
    {
        _httpClient = httpClientFactory.CreateClient("KeyProvider");
        _requestFactory = requestFactory;
    }

    public async Task<AuthData> GetApiKeyAsync(CancellationToken cancellationToken = default)
    {
        var request = _requestFactory.Create();

        var response = await _httpClient.PostAsJsonAsync(request.Url, request.Payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<AuthData>(cancellationToken)
            ?? throw new InvalidOperationException("Authentication response did not contain a token.");
    }
}
