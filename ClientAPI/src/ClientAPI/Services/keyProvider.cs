public class KeyProvider
{
    private class LoginRequest
    {
        public string username { get; set; }
        public string password { get; set; }
    }

    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public KeyProvider(IConfiguration configuration)
    {
        _configuration = configuration;
        _httpClient = new HttpClient();
    }

    public async Task<AuthData> GetApiKey()
    {
        if (string.IsNullOrEmpty(_configuration["useLogin"]) || _configuration["useLogin"] != "true")
        {
            var response = await _httpClient.PostAsJsonAsync(_configuration["loginUrl"], new LoginRequest { username = _configuration["username"], password = _configuration["password"] });
            response.EnsureSuccessStatusCode();
            var authData = await response.Content.ReadFromJsonAsync<AuthData>();
            return authData;
        }
        else
        {
            var response = await _httpClient.PostAsJsonAsync(_configuration["tokenUrl"], new TokenRequest { token = _configuration["apiKey"] });
            response.EnsureSuccessStatusCode();
            var authData = await response.Content.ReadFromJsonAsync<AuthData>();
            return authData;
        }
    }

    public class AuthData
    {
        public string token { get; set; }
    }

    private class TokenRequest
    {
        public string token { get; set; }
    }
}