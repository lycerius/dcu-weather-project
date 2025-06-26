using Common.Models;

namespace WeatherCli.Services.WeatherService;

/// <summary>
/// Defines methods for retrieving weather data from the Weather API.
/// Implementations provide access to current and average weather information by ZIP code.
/// </summary>
public interface IWeatherService
{
    /// <summary>
    /// Retrieves the average weather for a given ZIP code, temperature unit, and time period.
    /// </summary>
    /// <param name="zipCode">The ZIP code to query.</param>
    /// <param name="temperatureUnit">The temperature unit (Celsius or Fahrenheit).</param>
    /// <param name="timePeriodDays">The number of days to average over.</param>
    /// <returns>An <see cref="AverageWeather"/> object if found; otherwise, null.</returns>
    Task<AverageWeather?> GetAverageWeather(string zipCode, TemperatureUnit temperatureUnit, int timePeriodDays);

    /// <summary>
    /// Retrieves the current weather for a given ZIP code and temperature unit.
    /// </summary>
    /// <param name="zipCode">The ZIP code to query.</param>
    /// <param name="temperatureUnit">The temperature unit (Celsius or Fahrenheit).</param>
    /// <returns>A <see cref="CurrentWeather"/> object if found; otherwise, null.</returns>
    Task<CurrentWeather?> GetCurrentWeatherForZipCode(string zipCode, TemperatureUnit temperatureUnit);
}