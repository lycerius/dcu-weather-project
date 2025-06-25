using Common.Models;
using Microsoft.AspNetCore.Mvc;
using WeatherService.Services;

namespace WeatherService.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherController : ControllerBase
{
    private readonly IWeatherProvider _weatherProvider;

    public WeatherController(IWeatherProvider weatherProvider)
    {
        _weatherProvider = weatherProvider;
    }

    /// <summary>
    /// Gets the current weather for the given zipcode, with temperatures in the given units
    /// </summary>
    /// <param name="zipCode">The zipcode to fetch weather for</param>
    /// <param name="units">The units the temperatures should be in</param>
    /// <returns></returns>
    [HttpGet("Current/{zipCode}")]
    public async Task<ActionResult<CurrentWeather>> GetCurrentWeather(string zipCode, TemperatureUnit units)
    {
        try
        {
            var currentWeatherForZipcode = await _weatherProvider.GetCurrentWeatherForZipCode(zipCode, units);
            return currentWeatherForZipcode != null ? Ok(currentWeatherForZipcode) : BadRequest();
        }
        catch (Exception e)
        {
            //TODO: Is there a default 500 page we can throw here instead?
            Console.WriteLine(e);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }

    }

    [HttpGet("Average/{zipCode}")]
    public async Task<ActionResult<AverageWeather>> GetAverageWeather(string zipCode, string timePeriod, TemperatureUnit units)
    {
        try
        {
            bool success = int.TryParse(timePeriod, out int timePeriodInt);
            if (!success || timePeriodInt < 2 || timePeriodInt > 5)
            {
                return BadRequest();
            }
            var result = await _weatherProvider.GetAverageWeatherForZipCode(zipCode, timePeriodInt, units);
            return result != null ? Ok(result) : BadRequest();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}