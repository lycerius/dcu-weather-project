using Lycerius.DCUWeather.Common;
using Lycerius.DCUWeather.Services.OpenWeather.Models;

namespace Lycerius.DCUWeather.Services.OpenWeather;

public class OpenWeatherWeatherProvider : IWeatherProvider
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly TemperatureUnitsConverter _temperatureUnitsConverter;

    public OpenWeatherWeatherProvider(IConfiguration configuration, TemperatureUnitsConverter temperatureUnitsConverter, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _httpClient = httpClientFactory.CreateClient("openWeatherClient");
        _temperatureUnitsConverter = temperatureUnitsConverter;
    }

    public async Task<CurrentWeather?> GetCurrentWeatherForZipCode(string zip, string tempUnit)
    {
        GeocodeResult? zipToGeocode;
        try
        {
            zipToGeocode = await GetLatAndLongForZip(zip);
        }
        catch (HttpRequestException e)
        {
            if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            else
            {
                throw;
            }
        }

        if (zipToGeocode == null)
        {
            return null;
        }

        OpenWeatherResult? weatherResult;
        try
        {
            var uriToGet = WithAppIdInUri($"data/3.0/onecall?lat={zipToGeocode.Lat}&lon={zipToGeocode.Lon}");
            weatherResult = await _httpClient.GetFromJsonAsync<OpenWeatherResult>(uriToGet);
        }
        catch (HttpRequestException e)
        {
            if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            else
            {
                throw;
            }
        }

        return weatherResult == null ? null : new CurrentWeather
        {
            CurrentTemperature = _temperatureUnitsConverter.ConvertKelvinToUnits(weatherResult.Current.Temp, tempUnit),
            Lat = zipToGeocode.Lat,
            Long = zipToGeocode.Lon,
            Unit = tempUnit,
            RainPossibleToday = GetRainPossibleToday(weatherResult)
        };
    }

    /// <summary>
    /// Returns if rain is possible for the current day of the given weather result
    /// </summary>
    /// <param name="rainCheck">The weather result to check rain for</param>
    /// <returns>true if rain is predicted for the current day, else false</returns>
    private bool GetRainPossibleToday(OpenWeatherResult rainCheck)
    {
        var today = rainCheck.Current.UtcDateTime.Date;
        var todaysWeatherReport = rainCheck.Daily.Where(fw => fw.UtcDateTime.Date == today).First();
        if (todaysWeatherReport == null)
        {
            return false;
        }

        return todaysWeatherReport.Rain != null && todaysWeatherReport.Rain > 0;
    }

    /// <summary>
    /// Gets the latitude and longtitude for the given input zip
    /// </summary>
    /// <param name="zip">The zipcode to get lat and long for</param>
    /// <returns>The latitude and longitude for the given zip</returns>
    private async Task<GeocodeResult?> GetLatAndLongForZip(string zip)
    {
        //TODO: This should be cached
        var uriToGet = WithAppIdInUri($"geo/1.0/zip?zip={zip},US");
        return await _httpClient.GetFromJsonAsync<GeocodeResult>(uriToGet);
    }

    private string WithAppIdInUri(string baseUri)
    {
        return $"{baseUri}&appid={_configuration["DcuWeatherApp:OpenWeatherApiKey"]}";
    }
}