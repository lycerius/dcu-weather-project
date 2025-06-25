namespace Lycerius.DCUWeather.Common;

/// <summary>
/// Reperesents current weather conditions for the given area
/// </summary>
public record CurrentWeather
{
    /// <summary>
    /// The current temperature
    /// </summary>
    public required double CurrentTemperature { get; set; }

    /// <summary>
    /// The units temperatures are in
    /// </summary>
    public required string Unit { get; set; }
    /// <summary>
    /// The latitude of the given location
    /// </summary>
    public required double Lat { get; set; }
    /// <summary>
    /// The Longitude of the given location
    /// </summary>
    public required double Long { get; set; }
    /// <summary>
    /// Set to true if rain is possible in the given timeframe
    /// </summary>
    public required bool RainPossibleToday { get; set; }
}