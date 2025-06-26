using WeatherCli.Models;

namespace WeatherCli.Services.CredentialStorage;

/// <summary>
/// Defines methods for storing, retrieving, and clearing authentication tokens
/// for the Weather CLI. Implementations may use files, memory, or other storage mechanisms.
/// </summary>
public interface ICredentialStorage
{
    /// <summary>
    /// Saves the authentication token to the storage.
    /// </summary>
    /// <param name="token">The authentication token to save.</param>
    void SaveToken(AuthToken token);

    /// <summary>
    /// Retrieves the authentication token from the storage.
    /// </summary>
    /// <returns>The saved authentication token, or null if not found.</returns>
    AuthToken? GetToken();

    /// <summary>
    /// Clears the stored authentication token.
    /// </summary>
    void ClearToken();
}