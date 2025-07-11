using Common.Models;

namespace WeatherService.Services.WeatherProviders;

public interface IWeatherProvider
{
    /// <summary>
    /// Retrieves the current weather for the given zipcode, and in the given temperature units.
    /// </summary>
    /// <param name="zip">The zipcode to fetch weather for</param>
    /// <param name="tempUnit">The temperature unit to give temperatures in</param>
    /// <returns>The current weather for the given zipcode</returns>
    Task<CurrentWeather?> GetCurrentWeatherForZipCode(string zip, TemperatureUnit tempUnit);

    /// <summary>
    /// Gets the average weather (and if rain is possible) for the given zipcode and timePeriod, and in the given temperature units.
    /// </summary>
    /// <param name="zip">The zipcode to fetch weather for</param>
    /// <param name="timePeriod">The time period to calculate over</param>
    /// <param name="tempUnit">The temperature unit to give temperatures in</param>
    /// <returns>The average weather for the given zipcode</returns>
    Task<AverageWeather?> GetAverageWeatherForZipCode(string zip, int timePeriod, TemperatureUnit tempUnit);
}