using Common.Models;

namespace WeatherService.Models;

public record GetCurrentWeatherQuery
{
    public required string ZipCode { get; set; }
    public required TemperatureUnit Units { get; set; }
}
