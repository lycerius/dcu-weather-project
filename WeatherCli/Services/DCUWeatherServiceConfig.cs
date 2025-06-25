namespace WeatherCli.Services;

public record DCUWeatherServiceConfig
{
    public required string BaseUrl { get; set; }
}