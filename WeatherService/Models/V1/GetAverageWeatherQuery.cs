using Common.Models;

namespace WeatherService.Models;

public record GetAverageWeatherQuery
{
    public required string ZipCode { get; set; }
    public required string TimePeriod { get; set; }
    public required TemperatureUnit Units { get; set; }
}
