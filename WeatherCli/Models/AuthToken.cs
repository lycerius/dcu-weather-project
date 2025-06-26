namespace WeatherCli.Models;

/// <summary>
/// Represents an authentication token used for accessing the DCU Weather API.
/// </summary>
public record AuthToken
{
    /// <summary>
    /// The type of the token, typically "Bearer".
    /// </summary>
    public string? TokenType { get; set; }
    /// <summary>
    /// The access token string used for authentication.
    /// </summary>
    public string? AccessToken { get; set; }
    /// <summary>
    /// The number of seconds until the token expires.
    /// </summary>
    public int ExpiresIn { get; set; }
    /// <summary>
    /// The refresh token string used to obtain a new access token when the current one expires.
    /// </summary>
    public string? RefreshToken { get; set; }
}