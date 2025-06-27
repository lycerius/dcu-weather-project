using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using WeatherCli.Models;
using WeatherCli.Services.CredentialStorage;

namespace WeatherCli.Services.WeatherAuthService;

public class WeatherAuthService : IWeatherAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ICredentialStorage _credentialStorage;
    private readonly ILogger<WeatherAuthService> _logger;

    public WeatherAuthService(IHttpClientFactory httpClientFactory, ICredentialStorage credentialStorage, ILogger<WeatherAuthService> logger)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("weatherClient");
        _credentialStorage = credentialStorage;
    }

    public async Task<bool> RegisterUser(string email, string password)
    {
        return await PostAndLogResult("register", new { Email = email, Password = password }, "registering user");
    }

    public async Task<bool> LoginUser(string email, string password)
    {
        _credentialStorage.ClearToken(); // Invalidate any existing credentials before login
        var response = await _httpClient.PostAsJsonAsync("login", new { Email = email, Password = password });
        if (response.IsSuccessStatusCode)
        {
            var authToken = await response.Content.ReadFromJsonAsync<AuthToken>();
            if (authToken != null)
                _credentialStorage.SaveToken(authToken);
            return true;
        }
        await LogErrorResponse(response, "logging in user");
        return false;
    }

    public async Task<AuthToken?> GetBearerToken()
    {
        var token = _credentialStorage.GetToken();
        if (token == null)
        {
            _logger.LogError("No credentials found. Please log in first.");
            return null;
        }
        return await RefreshToken(token);
    }

    public async Task<AuthToken?> RefreshToken(AuthToken authToken)
    {
        var response = await _httpClient.PostAsJsonAsync("refresh", new { authToken.RefreshToken });
        if (response.IsSuccessStatusCode)
        {
            var newAuthToken = await response.Content.ReadFromJsonAsync<AuthToken>();
            if (newAuthToken != null)
                _credentialStorage.SaveToken(newAuthToken);
            return newAuthToken;
        }

        await LogErrorResponse(response, "refreshing token");
        return null;
    }

    private async Task<bool> PostAndLogResult(string endpoint, object payload, string action)
    {
        var response = await _httpClient.PostAsJsonAsync(endpoint, payload);
        if (response.IsSuccessStatusCode)
            return true;

        await LogErrorResponse(response, action);
        return false;
    }

    private async Task LogErrorResponse(HttpResponseMessage response, string action)
    {
        var content = await response.Content.ReadAsStringAsync();
        _logger.LogError($"Error {action}: {response.StatusCode}.\n{content}");
    }
}