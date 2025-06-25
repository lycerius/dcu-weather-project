namespace WeatherService.Services.WeatherProviders.OpenWeather.Models;

/// <summary>
/// Represents a result from OpenWeather's weather api
/// </summary>
public record OpenWeatherResult
{
    /// <summary>
    /// The Latitude of the weather result
    /// </summary>
    public required double Lat { get; set; }
    /// <summary>
    /// The Longitude of the weather result
    /// </summary>
    public required double Lon { get; set; }
    /// <summary>
    /// The current weather for the area
    /// </summary>
    public required CurrentWeatherReport Current { get; set; }
    /// <summary>
    /// A List of Weather reports by day
    /// </summary>
    public required List<FutureWeatherReport> Daily { get; set; }
}

public record CurrentWeatherReport
{
    /// <summary>
    /// The current temperature of the location
    /// </summary>
    public required double Temp { get; set; }
    /// <summary>
    /// The current date time of the weather report
    /// </summary>
    public required long Dt { get; set; }
    public DateTime UtcDateTime
    {
        get
        {
            return DateTimeOffset.FromUnixTimeSeconds(Dt).UtcDateTime;
        }
    }
}

public record FutureWeatherReport
{
    /// <summary>
    /// The temperature statistics for the given day.
    /// </summary>
    public required Temperature Temp { get; set; }
    /// <summary>
    /// If there is rain predicted for the area, the amount of rain in inches, else null
    /// </summary>
    public double? Rain { get; set; }
    /// <summary>
    /// The current date time of the weather report
    /// </summary>
    public required long Dt { get; set; }
    public DateTime UtcDateTime
    {
        get
        {
            return DateTimeOffset.FromUnixTimeSeconds(Dt).UtcDateTime;
        }
    }
}

public record Temperature
{
    /// <summary>
    /// The day time average temperature
    /// </summary>
    public required double Day { get; set; }
    /// <summary>
    /// The morning average temperature
    /// </summary>
    public required double Morn { get; set; }
    /// <summary>
    /// The evening average temperature
    /// </summary>
    public required double Eve { get; set; }
    /// <summary>
    /// The night time average temperature
    /// </summary>
    public required double Night { get; set; }
}
