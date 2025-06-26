using Common.Models;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeatherService.Services.WeatherProviders;

namespace WeatherService.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class WeatherController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly IWeatherProvider _weatherProvider;
    private readonly IValidator<GetCurrentWeatherQuery> _currentWeatherValidator;
    private readonly IValidator<GetAverageWeatherQuery> _averageWeatherValidator;

    public WeatherController(
        ILogger logger,
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

// Query DTOs and Validators (place in appropriate files in production)
public class GetCurrentWeatherQuery
{
    public string ZipCode { get; set; } = default!;
    public TemperatureUnit Units { get; set; }
}

public class GetAverageWeatherQuery
{
    public string ZipCode { get; set; } = default!;
    public string TimePeriod { get; set; } = default!;
    public TemperatureUnit Units { get; set; }
}

public class GetCurrentWeatherQueryValidator : AbstractValidator<GetCurrentWeatherQuery>
{
    public GetCurrentWeatherQueryValidator()
    {
        RuleFor(x => x.ZipCode)
            .NotEmpty().WithMessage("Zip code is required.")
            .Matches(@"^\d{5}$").WithMessage("Zip code must be a 5-digit number.");
        RuleFor(x => x.Units)
            .IsInEnum().WithMessage("Units must be a valid temperature unit.");
    }
}

public class GetAverageWeatherQueryValidator : AbstractValidator<GetAverageWeatherQuery>
{
    public GetAverageWeatherQueryValidator()
    {
        RuleFor(x => x.ZipCode)
            .NotEmpty().WithMessage("Zip code is required.")
            .Matches(@"^\d{5}$").WithMessage("Zip code must be a 5-digit number.");
        RuleFor(x => x.TimePeriod)
            .NotEmpty().WithMessage("Time period is required.")
            .Must(tp => int.TryParse(tp, out var n) && n >= 2 && n <= 5)
            .WithMessage("Time period must be an integer between 2 and 5.");
        RuleFor(x => x.Units)
            .IsInEnum().WithMessage("Units must be a valid temperature unit.");
    }
}