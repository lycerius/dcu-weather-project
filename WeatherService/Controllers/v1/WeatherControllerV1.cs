using Common.Models;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeatherService.Models;
using WeatherService.Services.WeatherProviders;

namespace WeatherService.Controllers;

[ApiController]
[Route("v1/Weather")]
[Authorize]
public class WeatherControllerV1 : ControllerBase
{
    private readonly ILogger<WeatherControllerV1> _logger;
    private readonly IWeatherProvider _weatherProvider;
    private readonly IValidator<GetCurrentWeatherQuery> _currentWeatherValidator;
    private readonly IValidator<GetAverageWeatherQuery> _averageWeatherValidator;

    public WeatherControllerV1(
        ILogger<WeatherControllerV1> logger,
        IWeatherProvider weatherProvider,
        IValidator<GetCurrentWeatherQuery> currentWeatherValidator,
        IValidator<GetAverageWeatherQuery> averageWeatherValidator)
    {
        _logger = logger;
        _weatherProvider = weatherProvider;
        _currentWeatherValidator = currentWeatherValidator;
        _averageWeatherValidator = averageWeatherValidator;
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
        var query = new GetCurrentWeatherQuery { ZipCode = zipCode, Units = units };
        var validationResult = await _currentWeatherValidator.ValidateAsync(query);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors.Select(e => e.ErrorMessage));
        }

        return await ProtectedCallWithErrorLoggingAsync<CurrentWeather>(async () =>
        {
            var currentWeatherForZipcode = await _weatherProvider.GetCurrentWeatherForZipCode(zipCode, units);
            return currentWeatherForZipcode != null ? Ok(currentWeatherForZipcode) : BadRequest();
        }, "An error occurred while fetching current weather for zipcode {0}", zipCode);
    }

    [HttpGet("Average/{zipCode}")]
    public async Task<ActionResult<AverageWeather>> GetAverageWeather(string zipCode, string timePeriod, TemperatureUnit units)
    {
        var query = new GetAverageWeatherQuery { ZipCode = zipCode, TimePeriod = timePeriod, Units = units };
        var validationResult = await _averageWeatherValidator.ValidateAsync(query);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors.Select(e => e.ErrorMessage));
        }

        return await ProtectedCallWithErrorLoggingAsync<AverageWeather>(async () =>
        {
            _ = int.TryParse(timePeriod, out int timePeriodInt);
            var result = await _weatherProvider.GetAverageWeatherForZipCode(zipCode, timePeriodInt, units);
            return result != null ? Ok(result) : BadRequest();
        }, "An error occurred while fetching average weather for zipcode {0}", zipCode);
    }

    private async Task<ActionResult<T>> ProtectedCallWithErrorLoggingAsync<T>(Func<Task<ActionResult<T>>> action, string errorMessage, params object[] logFormatingArgs)
    {
        try
        {
            return await action();
        }
        catch (Exception e)
        {
            _logger.LogError(e, errorMessage, logFormatingArgs);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
