using System.Net.Http.Json;
using Common.Models;

namespace WeatherCli.Services.WeatherService;

public class WeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;

    public WeatherService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CurrentWeather?> GetCurrentWeatherForZipCode(string zipCode, TemperatureUnit temperatureUnit)
    {
        var urlToCall = $"v1/Weather/Current/{zipCode}?units={temperatureUnit}";
        try
        {
            return await _httpClient.GetFromJsonAsync<CurrentWeather>(urlToCall);
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Error fetching current weather: {ex.Message}");
            return null;
        }
    }

    public async Task<AverageWeather?> GetAverageWeather(string zipCode, TemperatureUnit temperatureUnit, int timePeriodDays)
    {
        var urlToCall = $"v1/Weather/Average/{zipCode}?units={temperatureUnit}&timePeriod={timePeriodDays}";
        try
        {
            return await _httpClient.GetFromJsonAsync<AverageWeather>(urlToCall);
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Error fetching average weather: {ex.Message}");
            return null;
        }
    }
}