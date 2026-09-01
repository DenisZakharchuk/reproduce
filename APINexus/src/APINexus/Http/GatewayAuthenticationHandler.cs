
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

internal class GatewayAuthenticationHandler(GatewayAuthService authService) : DelegatingHandler
{
    private readonly GatewayAuthService authService = authService;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await authService.GetAuthenticationTokenAsync();
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                response.Dispose(); // Dispose the previous response before retrying
                // Token might be expired, try to refresh it
                token = await authService.GetAuthenticationTokenAsync(forceRefresh: true);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                response = await base.SendAsync(request, cancellationToken);
            }
            return response;
        }
        catch (Exception ex)
        {
            // Handle exceptions if needed
            throw;
        }
    }
}

internal class GatewayAuthService
{
    // null when idle; set to an in-flight TCS while a refresh is running
    private TaskCompletionSource<string>? _pendingRefresh;
    private volatile AuthData? _authData;

    public async Task<string> GetAuthenticationTokenAsync(bool forceRefresh = false)
    {
        if (!forceRefresh && _authData is not null)
            return _authData.Token;

        var staleData = _authData;
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var existing = Interlocked.CompareExchange(ref _pendingRefresh, tcs, null);

        if (existing is not null)
            return await existing.Task; // coalesce: wait for the in-flight refresh

        // We won the CAS — we are responsible for completing tcs
        try
        {
            // Another refresh finished between our fast-path check and winning the CAS
            if (forceRefresh && !ReferenceEquals(_authData, staleData))
            {
                tcs.SetResult(_authData!.Token);
                return _authData.Token;
            }

            if (!forceRefresh && _authData is not null)
            {
                tcs.SetResult(_authData.Token);
                return _authData.Token;
            }

            var newData = await RefreshTokenAsync(_authData?.RefreshToken)
                ?? await InitializeTokenAsync();
            _authData = newData;
            tcs.SetResult(newData.Token);
            return newData.Token;
        }
        catch (Exception ex)
        {
            tcs.SetException(ex);
            throw;
        }
        finally
        {
            Interlocked.Exchange(ref _pendingRefresh, null);
        }
    }

    private async Task<AuthData> InitializeTokenAsync()
    {
        // Logic to initialize the token
        // This is a placeholder implementation. Replace with actual logic.
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://example.com/api/authenticate");
        request.Content = new StringContent("{ \"username\": \"user\", \"password\": \"pass\" }", System.Text.Encoding.UTF8, "application/json");
        using var httpClient = new HttpClient();
        var response = await httpClient.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<AuthData>();
        }

        throw new InvalidOperationException("Failed to initialize authentication token.");
    }

    private async Task<AuthData?> RefreshTokenAsync(string? refreshToken)
    {
        // Logic to refresh the token using the refresh token
        // This is a placeholder implementation. Replace with actual logic.
        if (string.IsNullOrEmpty(refreshToken))
        {
            throw new InvalidOperationException("Refresh token is not available.");
        }

        // Simulate an asynchronous operation to refresh the token
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://example.com/api/refresh-token");
        request.Content = new StringContent($"{{ \"refreshToken\": \"{refreshToken}\" }}", System.Text.Encoding.UTF8, "application/json");
        using var httpClient = new HttpClient();
        var response = await httpClient.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<AuthData>();
        }

        return null;
    }

    private class AuthData
    {
        public string Token { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
    }
}