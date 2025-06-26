using System.Net.Http.Json;
using System.Text.Json;
using WeatherCli.Models;

namespace WeatherCli.Services;

/// <summary>
/// Service for handling user authentication and authorization with the DCU Weather API.
/// Provides methods for registering users, logging in, retrieving bearer tokens, and refreshing tokens.
/// Credentials are stored locally in a file named ".credentials.txt".
/// </summary>
public class DCUAuthService
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="DCUAuthService"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client used for API requests.</param>
    public DCUAuthService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Registers a new user with the given email and password.
    /// </summary>
    /// <param name="email">The user's email address.</param>
    /// <param name="password">The user's password.</param>
    /// <returns>True if registration is successful; otherwise, false.</returns>
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

    /// <summary>
    /// Logs in a user with the given email and password. Stores credentials locally if successful.
    /// </summary>
    /// <param name="email">The user's email address.</param>
    /// <param name="password">The user's password.</param>
    /// <returns>True if login is successful; otherwise, false.</returns>
    public async Task<bool> LoginUser(string email, string password)
    {
        File.Delete(".credentials.txt"); // Invalidate any existing credentials before login
        var response = await _httpClient.PostAsJsonAsync("login", new { Email = email, Password = password });
        if (response.IsSuccessStatusCode)
        {
            var credentialsFile = File.CreateText(".credentials.txt");
            var authToken = await response.Content.ReadFromJsonAsync<AuthToken>();
            await credentialsFile.WriteAsync(JsonSerializer.Serialize(authToken));
            await credentialsFile.FlushAsync();
            credentialsFile.Close();
            return true;
        }
        Console.WriteLine($"Error logging in user: {response.StatusCode}.\n{await response.Content.ReadAsStringAsync()}");
        return false;
    }

    /// <summary>
    /// Retrieves the bearer token from the local credentials file, if it exists.
    /// </summary>
    /// <returns>The <see cref="AuthToken"/> if found; otherwise, null.</returns>
    public async Task<AuthToken?> GetBearerToken()
    {
        if (!File.Exists(".credentials.txt"))
        {
            Console.WriteLine("No credentials file found. Please log in first.");
            return null;
        }

        var authToken = JsonSerializer.Deserialize<AuthToken>(await File.ReadAllTextAsync(".credentials.txt"));
        return authToken;
    }

    /// <summary>
    /// Refreshes the authentication token using the provided refresh token and updates the local credentials file.
    /// </summary>
    /// <param name="authToken">The current <see cref="AuthToken"/> containing the refresh token.</param>
    /// <returns>The new <see cref="AuthToken"/> if refresh is successful; otherwise, null.</returns>
    public async Task<AuthToken?> RefreshToken(AuthToken authToken)
    {
        var response = await _httpClient.PostAsJsonAsync("refresh", new { authToken.RefreshToken });
        if (response.IsSuccessStatusCode)
        {
            var newAuthToken = await response.Content.ReadFromJsonAsync<AuthToken>();
            await File.WriteAllTextAsync(".credentials.txt", JsonSerializer.Serialize(newAuthToken));
            return newAuthToken;
        }

        Console.WriteLine($"Error refreshing token: {response.StatusCode}.\n{await response.Content.ReadAsStringAsync()}");
        return null;
    }
}