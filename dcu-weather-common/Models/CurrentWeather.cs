using System.Text.Json.Serialization;

namespace Lycerius.DCUWeather.Common.Models;

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
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required TemperatureUnit Unit { get; set; }
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

    public override string ToString()
    {
        return $"Location: {Lat},{Long}\n"
        + $"Current Temperature: {CurrentTemperature}\n"
        + $"Temperature Unit: {Unit}";
    }
}