namespace Lycerius.DCUWeather.Services.OpenWeather.Models;

/// <summary>
/// Reperesents a Geocode query result from Open Weather's Geocode api
/// </summary>
public record GeocodeResult
{
    /// <summary>
    /// The Name of the Location
    /// </summary>
    public required string Name { get; set; }
    /// <summary>
    /// The country the location resides in
    /// </summary>
    public required string Country { get; set; }
    /// <summary>
    /// The Latitude of the location
    /// </summary>
    public required double Lat { get; set; }
    /// <summary>
    /// The Longitude of the location
    /// </summary>
    public required double Lon { get; set; }
}