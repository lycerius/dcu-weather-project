using Lycerius.DCUWeather.Common;
using Lycerius.DCUWeather.Common.Models;
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
    /// <param name="zip">The zipcode to get weather for</param>
    /// <returns>The weather result if weather was found, else null</returns>
    private async Task<OpenWeatherResult?> GetOpenWeatherResultForZipCode(string zip)
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

        return weatherResult;
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
    /// Calculates the average temperature for the given weather report, over the specified time period
    /// </summary>
    /// <param name="openWeatherResult">The weather report to calculate on</param>
    /// <param name="timePeriodDays">The time period in days to calculate over</param>
    /// <returns>The average temperature for the given time period</returns>
    private double GetAverageTemperatureForPeriod(OpenWeatherResult openWeatherResult, int timePeriodDays)
    {

        return openWeatherResult.Daily
        .OrderBy(w => w.Dt)
        .Take(timePeriodDays)
        .Select(w => (w.Temp.Morn + w.Temp.Day + w.Temp.Eve + w.Temp.Night) / 4)
        .Average();
    }

    /// <summary>
    /// Calculates if rain is possible for the given weather report, over the specified time period
    /// </summary>
    /// <param name="rainCheck">The weather report to calculate a rain check on</param>
    /// <param name="timePeriodDays">The time period in days to calculate over</param>
    /// <returns>If rain is possible over the given time period</returns>
    private bool GetRainPossibleInPeriod(OpenWeatherResult rainCheck, int timePeriodDays)
    {
        return rainCheck.Daily
        .OrderBy(w => w.Dt)
        .Take(timePeriodDays)
        .Any(w => w.Rain != null && w.Rain > 0);
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