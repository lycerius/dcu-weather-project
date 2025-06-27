using Common.Models;
using Microsoft.Extensions.Caching.Memory;
using WeatherService.Exceptions;
using WeatherService.Services.WeatherProviders.OpenWeather.Models;

namespace WeatherService.Services.WeatherProviders.OpenWeather;

public class OpenWeatherWeatherProvider : IWeatherProvider
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly TemperatureUnitsConverter _temperatureUnitsConverter;
    private readonly IMemoryCache _memoryCache;

    public OpenWeatherWeatherProvider(
        IConfiguration configuration,
        TemperatureUnitsConverter temperatureUnitsConverter,
        IHttpClientFactory httpClientFactory,
        IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
        _configuration = configuration;
        _httpClient = httpClientFactory.CreateClient("openWeatherClient");
        _temperatureUnitsConverter = temperatureUnitsConverter;
    }

    public async Task<CurrentWeather?> GetCurrentWeatherForZipCode(string zip, TemperatureUnit tempUnit)
    {
        var weatherResult = await GetOpenWeatherResultForZipCode(zip);

        return weatherResult == null ? null : new CurrentWeather
        {
            CurrentTemperature = _temperatureUnitsConverter.ConvertKelvinToUnits(weatherResult.Current.Temp, tempUnit),
            Lat = weatherResult.Lat,
            Long = weatherResult.Lon,
            Unit = tempUnit,
            RainPossibleToday = GetRainPossibleToday(weatherResult)
        };
    }

    public async Task<AverageWeather?> GetAverageWeatherForZipCode(string zip, int timePeriod, TemperatureUnit tempUnit)
    {
        var weatherResult = await GetOpenWeatherResultForZipCode(zip);

        return weatherResult == null ? null : new AverageWeather
        {
            Lat = weatherResult.Lat,
            Lon = weatherResult.Lon,
            Unit = tempUnit,
            AverageTemperature = _temperatureUnitsConverter.ConvertKelvinToUnits(GetAverageTemperatureForPeriod(weatherResult, timePeriod), tempUnit),
            RainPossibleInPeriod = GetRainPossibleInPeriod(weatherResult, timePeriod)
        };
    }

    /// <summary>
    /// Calls the OpenWeatherApi for the given zipcode and returns the result if found
    /// </summary>
    private async Task<OpenWeatherResult?> GetOpenWeatherResultForZipCode(string zip)
    {
        var zipToGeocode = await TryGetLatAndLongForZip(zip);
        if (zipToGeocode == null)
            return null;

        return await TryGetWeatherResult(zipToGeocode.Lat, zipToGeocode.Lon);
    }

    private async Task<GeocodeResult?> TryGetLatAndLongForZip(string zip)
    {
        try
        {
            return await GetLatAndLongForZip(zip);
        }
        catch (HttpRequestException e)
        {
            if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            throw new WeatherProviderException("Error retrieving geocode for zip code", e);
        }
    }

    private async Task<OpenWeatherResult?> TryGetWeatherResult(double lat, double lon)
    {
        try
        {
            var uriToGet = WithAppIdInUri($"data/3.0/onecall?lat={lat}&lon={lon}");
            return await _httpClient.GetFromJsonAsync<OpenWeatherResult>(uriToGet);
        }
        catch (HttpRequestException e)
        {
            if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            throw new WeatherProviderException("Error retrieving weather for zip code", e);
        }
    }

    private bool GetRainPossibleToday(OpenWeatherResult rainCheck)
    {
        var today = rainCheck.Current.UtcDateTime.Date;
        var todaysWeatherReport = rainCheck.Daily.FirstOrDefault(fw => fw.UtcDateTime.Date == today);
        return todaysWeatherReport?.Rain is > 0;
    }

    private double GetAverageTemperatureForPeriod(OpenWeatherResult openWeatherResult, int timePeriodDays)
    {
        return openWeatherResult.Daily
            .OrderBy(w => w.Dt)
            .Take(timePeriodDays)
            .Select(w => (w.Temp.Morn + w.Temp.Day + w.Temp.Eve + w.Temp.Night) / 4)
            .Average();
    }

    private bool GetRainPossibleInPeriod(OpenWeatherResult rainCheck, int timePeriodDays)
    {
        return rainCheck.Daily
            .OrderBy(w => w.Dt)
            .Take(timePeriodDays)
            .Any(w => w.Rain is > 0);
    }

    private async Task<GeocodeResult?> GetLatAndLongForZip(string zip)
    {
        if (!_memoryCache.TryGetValue(zip, out GeocodeResult? geocodeResult))
        {
            var uriToGet = WithAppIdInUri($"geo/1.0/zip?zip={zip},US");
            geocodeResult = await _httpClient.GetFromJsonAsync<GeocodeResult>(uriToGet);

            if (geocodeResult != null)
            {
                // Cache the result for 1 day
                _memoryCache.Set(zip, geocodeResult, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1),
                });
            }
        }
        return geocodeResult;
    }

    private string WithAppIdInUri(string baseUri)
    {
        return $"{baseUri}&appid={_configuration["DcuWeatherApp:OpenWeatherApiKey"]}";
    }
}