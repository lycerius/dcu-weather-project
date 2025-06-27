using Common.Models;
using FluentValidation.TestHelper;
using WeatherService.Controllers.Validators;
using WeatherService.Models;

namespace WeatherService.Tests.Validators;

public class GetCurrentWeatherQueryValidatorTests
{
    private readonly GetCurrentWeatherQueryValidator _validator = new();

    [Theory]
    [InlineData("", TemperatureUnit.C, "Zip code is required.")]
    [InlineData("12", TemperatureUnit.C, "Zip code must be a 5-digit number.")]
    [InlineData("abcde", TemperatureUnit.F, "Zip code must be a 5-digit number.")]
    public void Should_HaveValidationError_For_InvalidZip(string zip, TemperatureUnit units, string expectedMessage)
    {
        var model = new GetCurrentWeatherQuery { ZipCode = zip, Units = units };
        var result = _validator.TestValidate(model);
        Assert.Contains(expectedMessage, result.Errors.Select(e => e.ErrorMessage));
    }

    [Fact]
    public void Should_HaveValidationError_For_InvalidUnits()
    {
        var model = new GetCurrentWeatherQuery { ZipCode = "12345", Units = (TemperatureUnit)999 };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Units)
            .WithErrorMessage("Units must be a valid temperature unit.");
    }

    [Fact]
    public void Should_NotHaveValidationError_For_ValidInput()
    {
        var model = new GetCurrentWeatherQuery { ZipCode = "12345", Units = TemperatureUnit.F };
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }
}