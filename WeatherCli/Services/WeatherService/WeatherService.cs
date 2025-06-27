using System.Net.Http.Headers;
using System.Net.Http.Json;
using Common.Models;
using Microsoft.Extensions.Logging;
using WeatherCli.Services.WeatherAuthService;

namespace WeatherCli.Services.WeatherService;

public class WeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly IWeatherAuthService _weatherAuthService;
    private readonly ILogger<WeatherService> _logger;

    public WeatherService(IHttpClientFactory httpClientFactory, IWeatherAuthService weatherAuthService, ILogger<WeatherService> logger)
    {
        _logger = logger;
        _weatherAuthService = weatherAuthService;
        _httpClient = httpClientFactory.CreateClient("weatherClient");
    }

    public async Task<CurrentWeather?> GetCurrentWeatherForZipCode(string zipCode, TemperatureUnit temperatureUnit)
    {
        var urlToCall = $"v1/Weather/Current/{zipCode}?units={temperatureUnit}";
        return await GetFromWeatherApi<CurrentWeather>(urlToCall, "Error fetching current weather");
    }

    public async Task<AverageWeather?> GetAverageWeather(string zipCode, TemperatureUnit temperatureUnit, int timePeriodDays)
    {
        var urlToCall = $"v1/Weather/Average/{zipCode}?units={temperatureUnit}&timePeriod={timePeriodDays}";
        return await GetFromWeatherApi<AverageWeather>(urlToCall, "Error fetching average weather");
    }

    private async Task<T?> GetFromWeatherApi<T>(string url, string errorMessage) where T : class
    {
        await EnsureAuthenticated();
        try
        {
            return await _httpClient.GetFromJsonAsync<T>(url);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, $"{errorMessage}: {ex.Message}");
            return null;
        }
    }

    private async Task EnsureAuthenticated()
    {
        var token = await _weatherAuthService.GetBearerToken() ?? throw new UnauthorizedAccessException("User is not authenticated. Please log in first.");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
    }
}