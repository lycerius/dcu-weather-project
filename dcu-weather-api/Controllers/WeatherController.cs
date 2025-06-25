using Lycerius.DCUWeather.Common;
using Lycerius.DCUWeather.Services;
using Microsoft.AspNetCore.Mvc;

namespace Lycerius.DCUWeather.Controllers;

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
    public async Task<ActionResult<CurrentWeather>> GetCurrentWeather(string zipCode, string units)
    {
        try
        {
            var currentWeatherForZipcode = await _weatherProvider.GetCurrentWeatherForZipCode(zipCode, units);
            return currentWeatherForZipcode != null ? Ok(currentWeatherForZipcode) : NotFound();
        }
        catch (Exception)
        {
            //TODO: Is there a default 500 page we can throw here instead?
            return StatusCode(StatusCodes.Status500InternalServerError);
        }

    }
}