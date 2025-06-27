using Common.Models;
using FluentValidation.TestHelper;
using WeatherService.Controllers.Validators;
using WeatherService.Models;

namespace WeatherService.Tests.Validators;

public class GetAverageWeatherQueryValidatorTests
{
    private readonly GetAverageWeatherQueryValidator _validator = new();

    [Theory]
    [InlineData("", "3", TemperatureUnit.C, "Zip code is required.")]
    [InlineData("abc", "3", TemperatureUnit.C, "Zip code must be a 5-digit number.")]
    [InlineData("12345", "", TemperatureUnit.C, "Time period is required.")]
    [InlineData("12345", "notanumber", TemperatureUnit.C, "Time period must be an integer between 2 and 5.")]
    [InlineData("12345", "1", TemperatureUnit.C, "Time period must be an integer between 2 and 5.")]
    [InlineData("12345", "6", TemperatureUnit.C, "Time period must be an integer between 2 and 5.")]
    public void Should_HaveValidationError_For_InvalidInput(string zip, string timePeriod, TemperatureUnit units, string expectedMessage)
    {
        var model = new GetAverageWeatherQuery { ZipCode = zip, TimePeriod = timePeriod, Units = units };
        var result = _validator.TestValidate(model);
        Assert.Contains(expectedMessage, result.Errors.Select(e => e.ErrorMessage));
    }

    [Fact]
    public void Should_HaveValidationError_For_InvalidUnits()
    {
        var model = new GetAverageWeatherQuery { ZipCode = "12345", TimePeriod = "3", Units = (TemperatureUnit)999 };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Units)
            .WithErrorMessage("Units must be a valid temperature unit.");
    }

    [Fact]
    public void Should_NotHaveValidationError_For_ValidInput()
    {
        var model = new GetAverageWeatherQuery { ZipCode = "12345", TimePeriod = "3", Units = TemperatureUnit.F };
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }
}