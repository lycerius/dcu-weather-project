using System.Text.Json.Serialization;

namespace Lycerius.DCUWeather.Common.Models;

/// <summary>
/// Represents average weather for the given area
/// </summary>
public record AverageWeather
{
    /// <summary>
    /// The average temperature
    /// </summary>
    public required double AverageTemperature { get; set; }

    /// <summary>
    /// The units the temperature is given in
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required TemperatureUnit Unit { get; set; }

    /// <summary>
    /// The latitude of the area
    /// </summary>
    public required double Lat { get; set; }
    /// <summary>
    /// The longitude of the area
    /// </summary>
    public required double Lon { get; set; }
    /// <summary>
    /// Whether or not rain is possible in the given period
    /// </summary>
    public required bool RainPossibleInPeriod { get; set; }
}