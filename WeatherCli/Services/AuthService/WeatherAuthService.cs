using System.Net.Http.Json;
using WeatherCli.Models;
using WeatherCli.Services.CredentialStorage;

namespace WeatherCli.Services.WeatherAuthService;

public class WeatherAuthService : IWeatherAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ICredentialStorage _credentialStorage;

    public WeatherAuthService(HttpClient httpClient, ICredentialStorage credentialStorage)
    {
        _httpClient = httpClient;
        _credentialStorage = credentialStorage;
    }

    public async Task<bool> RegisterUser(string email, string password)
    {
        var response = await _httpClient.PostAsJsonAsync("register", new { Email = email, Password = password });
        if (response.IsSuccessStatusCode)
        {
            return true;
        }
        Console.WriteLine($"Error registering user: {response.StatusCode}.\n{await response.Content.ReadAsStringAsync()}");
        return false;
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
        Console.WriteLine($"Error logging in user: {response.StatusCode}.\n{await response.Content.ReadAsStringAsync()}");
        return false;
    }

    public Task<AuthToken?> GetBearerToken()
    {
        var token = _credentialStorage.GetToken();
        if (token == null)
        {
            Console.WriteLine("No credentials found. Please log in first.");
        }
        return Task.FromResult(token);
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

        Console.WriteLine($"Error refreshing token: {response.StatusCode}.\n{await response.Content.ReadAsStringAsync()}");
        return null;
    }
}