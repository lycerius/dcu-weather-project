using Lycerius.DCUWeather.Common;

namespace Lycerius.DCUWeather.Services;

public interface IWeatherProvider
{
    /// <summary>
    /// Retrieves the current weather for the given zipcode, and in the given temperature units.
    /// </summary>
    /// <param name="zip">The zipcode to fetch weather for</param>
    /// <param name="tempUnit">The temperature unit to give temperatures in</param>
    /// <returns></returns>
    Task<CurrentWeather?> GetCurrentWeatherForZipCode(string zip, string tempUnit);
}