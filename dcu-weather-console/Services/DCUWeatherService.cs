using System.Net.Http.Json;
using Lycerius.DCUWeather.Common.Models;

namespace Lycerius.DCUWeather.Common.Services;

public class DCUWeatherService
{
    private readonly DCUWeatherServiceConfig _dcuWeatherServiceConfig;
    private readonly HttpClient _httpClient;

    public DCUWeatherService(DCUWeatherServiceConfig dcuWeatherServiceConfig)
    {
        _dcuWeatherServiceConfig = dcuWeatherServiceConfig;
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri(_dcuWeatherServiceConfig.BaseUrl);
    }

    public async Task<CurrentWeather?> GetCurrentWeatherForZipCode(string zipCode, TemperatureUnit temperatureUnit)
    {
        var urlToCall = $"/Weather/Current/{zipCode}?units={temperatureUnit}";
        return await _httpClient.GetFromJsonAsync<CurrentWeather>(urlToCall);
    }

    public async Task<AverageWeather?> GetAverageWeather(string zipCode, TemperatureUnit temperatureUnit, int timePeriodDays)
    {
        var urlToCall = $"/Weather/Average/{zipCode}?units={temperatureUnit}&timePeriod={timePeriodDays}";
        return await _httpClient.GetFromJsonAsync<AverageWeather>(urlToCall);
    }
}