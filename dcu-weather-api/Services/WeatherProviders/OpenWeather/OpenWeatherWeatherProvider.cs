using System.Threading.Tasks;
using Lycerius.DCUWeather.Common;
using Lycerius.DCUWeather.Services.OpenWeather.Models;

namespace Lycerius.DCUWeather.Services.OpenWeather;

public class OpenWeatherWeatherProvider : IWeatherProvider
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;


    public OpenWeatherWeatherProvider(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _httpClient = httpClientFactory.CreateClient("openWeatherClient");
    }

    public async Task<CurrentWeather> GetCurrentWeatherForZipCode(string zip, string tempUnit)
    {
        var zipToGeocode = await GetLatAndLongForZip(zip);
        var uriToGet = WithAppIdInUri($"data/3.0/onecall?lat={zipToGeocode.Lat}&lon={zipToGeocode.Lon}");
        OpenWeatherResult weatherResult;

        try
        {
            weatherResult = await _httpClient.GetFromJsonAsync<OpenWeatherResult>(uriToGet);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw e;
        }

        return new CurrentWeather
        {
            CurrentTemperature = weatherResult.Current.Temp,
            Lat = zipToGeocode.Lat,
            Long = zipToGeocode.Lon,
            Unit = tempUnit
        };
    }

    private async Task<GeocodeResult> GetLatAndLongForZip(string zip)
    {
        try
        {
            var uriToGet = WithAppIdInUri($"geo/1.0/zip?zip={zip},US");
            return await _httpClient.GetFromJsonAsync<GeocodeResult>(uriToGet);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return null;
    }

    private string WithAppIdInUri(string baseUri)
    {
        return $"{baseUri}&appid={_configuration["DcuWeatherApp:OpenWeatherApiKey"]}";
    }
}