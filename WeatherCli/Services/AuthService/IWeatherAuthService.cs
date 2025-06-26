using WeatherCli.Models;

namespace WeatherCli.Services.WeatherAuthService;

/// <summary>
/// Defines methods for authenticating users and managing authentication tokens
/// for the Weather API. Implementations handle user registration, login,
/// token retrieval, and token refresh.
/// </summary>
public interface IWeatherAuthService
{
    /// <summary>
    /// Retrieves the current bearer token for the authenticated user, if available.
    /// </summary>
    /// <returns>The <see cref="AuthToken"/> if available; otherwise, null.</returns>
    Task<AuthToken?> GetBearerToken();

    /// <summary>
    /// Logs in a user with the specified email and password.
    /// </summary>
    /// <param name="email">The user's email address.</param>
    /// <param name="password">The user's password.</param>
    /// <returns>True if login is successful; otherwise, false.</returns>
    Task<bool> LoginUser(string email, string password);

    /// <summary>
    /// Refreshes the authentication token using the provided refresh token.
    /// </summary>
    /// <param name="authToken">The current <see cref="AuthToken"/> containing the refresh token.</param>
    /// <returns>The new <see cref="AuthToken"/> if refresh is successful; otherwise, null.</returns>
    Task<AuthToken?> RefreshToken(AuthToken authToken);

    /// <summary>
    /// Registers a new user with the specified email and password.
    /// </summary>
    /// <param name="email">The user's email address.</param>
    /// <param name="password">The user's password.</param>
    /// <returns>True if registration is successful; otherwise, false.</returns>
    Task<bool> RegisterUser(string email, string password);
}