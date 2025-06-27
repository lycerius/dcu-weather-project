using Common.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WeatherService.Controllers;
using WeatherService.Services.WeatherProviders;
using FluentValidation;
using FluentValidation.TestHelper;

namespace WeatherService.Tests;

public class WeatherControllerTests
{
    private readonly Mock<IWeatherProvider> _weatherProvider;
    private readonly Mock<ILogger<WeatherControllerV1>> _logger;
    private readonly IValidator<GetCurrentWeatherQuery> _currentWeatherValidator;
    private readonly IValidator<GetAverageWeatherQuery> _averageWeatherValidator;

    public WeatherControllerTests()
    {
        _weatherProvider = new Mock<IWeatherProvider>();
        _logger = new Mock<ILogger<WeatherControllerV1>>();
        _currentWeatherValidator = new GetCurrentWeatherQueryValidator();
        _averageWeatherValidator = new GetAverageWeatherQueryValidator();
    }

    private WeatherControllerV1 CreateController()
        => new WeatherControllerV1(
            _logger.Object,
            _weatherProvider.Object,
            _currentWeatherValidator,
            _averageWeatherValidator
        );

    private static void AssertBadRequestWithMessage(IActionResult? result, string expectedMessage)
    {
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains(expectedMessage, (IEnumerable<string>)badRequest.Value);
    }

    [Fact]
    public async Task GetCurrentWeather_ShouldReturnOk_WhenWeatherExists()
    {
        // Arrange
        var expected = new CurrentWeather
        {
            Lat = 1.23,
            Long = 4.56,
            CurrentTemperature = 22.5,
            Unit = TemperatureUnit.C,
            RainPossibleToday = true
        };
        _weatherProvider.Setup(w => w.GetCurrentWeatherForZipCode("90210", TemperatureUnit.C))
            .ReturnsAsync(expected);

        var controller = CreateController();

        // Act
        var result = await controller.GetCurrentWeather("90210", TemperatureUnit.C);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expected, okResult.Value);
        _weatherProvider.Verify(w => w.GetCurrentWeatherForZipCode("90210", TemperatureUnit.C), Times.Once());
    }

    [Fact]
    public async Task GetCurrentWeather_ShouldReturnBadRequest_WhenWeatherIsNull()
    {
        // Arrange
        _weatherProvider.Setup(w => w.GetCurrentWeatherForZipCode("00000", TemperatureUnit.F))
            .ReturnsAsync((CurrentWeather?)null);

        var controller = CreateController();

        // Act
        var result = await controller.GetCurrentWeather("00000", TemperatureUnit.F);

        // Assert
        Assert.IsType<BadRequestResult>(result.Result);
        _weatherProvider.Verify(w => w.GetCurrentWeatherForZipCode("00000", TemperatureUnit.F), Times.Once());
    }

    [Fact]
    public async Task GetCurrentWeather_ShouldReturnBadRequest_WhenValidationFails()
    {
        // Arrange
        var controller = CreateController();

        // Invalid zip code (not 5 digits)
        var result = await controller.GetCurrentWeather("12", TemperatureUnit.C);

        // Assert
        AssertBadRequestWithMessage(result.Result, "Zip code must be a 5-digit number.");
        _weatherProvider.Verify(w => w.GetCurrentWeatherForZipCode(It.IsAny<string>(), It.IsAny<TemperatureUnit>()), Times.Never());
    }

    [Fact]
    public async Task GetCurrentWeather_ShouldReturn500_WhenExceptionThrown()
    {
        // Arrange
        _weatherProvider.Setup(w => w.GetCurrentWeatherForZipCode(It.IsAny<string>(), It.IsAny<TemperatureUnit>()))
            .ThrowsAsync(new System.Exception("fail"));

        var controller = CreateController();

        // Act
        var result = await controller.GetCurrentWeather("90210", TemperatureUnit.C);

        // Assert
        var statusResult = Assert.IsType<StatusCodeResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
        _weatherProvider.Verify(w => w.GetCurrentWeatherForZipCode("90210", TemperatureUnit.C), Times.Once());
    }

    [Fact]
    public async Task GetAverageWeather_ShouldReturnOk_WhenWeatherExists()
    {
        // Arrange
        var expected = new AverageWeather
        {
            Lat = 1.23,
            Lon = 4.56,
            AverageTemperature = 15.5,
            Unit = TemperatureUnit.F,
            RainPossibleInPeriod = false
        };
        _weatherProvider.Setup(w => w.GetAverageWeatherForZipCode("90210", 3, TemperatureUnit.F))
            .ReturnsAsync(expected);

        var controller = CreateController();

        // Act
        var result = await controller.GetAverageWeather("90210", "3", TemperatureUnit.F);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expected, okResult.Value);
        _weatherProvider.Verify(w => w.GetAverageWeatherForZipCode("90210", 3, TemperatureUnit.F), Times.Once());
    }

    [Fact]
    public async Task GetAverageWeather_ShouldReturnBadRequest_WhenWeatherIsNull()
    {
        // Arrange
        _weatherProvider.Setup(w => w.GetAverageWeatherForZipCode("00000", 2, TemperatureUnit.C))
            .ReturnsAsync((AverageWeather?)null);

        var controller = CreateController();

        // Act
        var result = await controller.GetAverageWeather("00000", "2", TemperatureUnit.C);

        // Assert
        Assert.IsType<BadRequestResult>(result.Result);
        _weatherProvider.Verify(w => w.GetAverageWeatherForZipCode("00000", 2, TemperatureUnit.C), Times.Once());
    }

    [Theory]
    [InlineData("notanumber")]
    [InlineData("1")]
    [InlineData("6")]
    public async Task GetAverageWeather_ShouldReturnBadRequest_WhenTimePeriodInvalid(string timePeriod)
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = await controller.GetAverageWeather("90210", timePeriod, TemperatureUnit.C);

        // Assert
        AssertBadRequestWithMessage(result.Result, "Time period must be an integer between 2 and 5.");
        _weatherProvider.Verify(w => w.GetAverageWeatherForZipCode(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TemperatureUnit>()), Times.Never());
    }

    [Fact]
    public async Task GetAverageWeather_ShouldReturnBadRequest_WhenValidationFails()
    {
        // Arrange
        var controller = CreateController();

        // Invalid zip code (not 5 digits)
        var result = await controller.GetAverageWeather("12", "3", TemperatureUnit.C);

        // Assert
        AssertBadRequestWithMessage(result.Result, "Zip code must be a 5-digit number.");
        _weatherProvider.Verify(w => w.GetAverageWeatherForZipCode(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TemperatureUnit>()), Times.Never());
    }

    [Fact]
    public async Task GetAverageWeather_ShouldReturn500_WhenExceptionThrown()
    {
        // Arrange
        _weatherProvider.Setup(w => w.GetAverageWeatherForZipCode(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TemperatureUnit>()))
            .ThrowsAsync(new System.Exception("fail"));

        var controller = CreateController();

        // Act
        var result = await controller.GetAverageWeather("90210", "3", TemperatureUnit.C);

        // Assert
        var statusResult = Assert.IsType<StatusCodeResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
        _weatherProvider.Verify(w => w.GetAverageWeatherForZipCode("90210", 3, TemperatureUnit.C), Times.Once());
    }

    // --- Validator Tests ---

    private static GetCurrentWeatherQueryValidator CreateCurrentWeatherValidator() => new GetCurrentWeatherQueryValidator();
    private static GetAverageWeatherQueryValidator CreateAverageWeatherValidator() => new GetAverageWeatherQueryValidator();

    [Fact]
    public void GetCurrentWeatherQueryValidator_Should_HaveError_When_ZipCodeIsEmpty()
    {
        var validator = CreateCurrentWeatherValidator();
        var model = new GetCurrentWeatherQuery { ZipCode = "", Units = TemperatureUnit.C };
        var result = validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.ZipCode)
            .WithErrorMessage("Zip code is required.");
    }

    [Fact]
    public void GetCurrentWeatherQueryValidator_Should_HaveError_When_ZipCodeIsInvalid()
    {
        var validator = CreateCurrentWeatherValidator();
        var model = new GetCurrentWeatherQuery { ZipCode = "12", Units = TemperatureUnit.C };
        var result = validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.ZipCode)
            .WithErrorMessage("Zip code must be a 5-digit number.");
    }

    [Fact]
    public void GetCurrentWeatherQueryValidator_Should_NotHaveError_When_Valid()
    {
        var validator = CreateCurrentWeatherValidator();
        var model = new GetCurrentWeatherQuery { ZipCode = "12345", Units = TemperatureUnit.F };
        var result = validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void GetAverageWeatherQueryValidator_Should_HaveError_When_ZipCodeIsEmpty()
    {
        var validator = CreateAverageWeatherValidator();
        var model = new GetAverageWeatherQuery { ZipCode = "", TimePeriod = "3", Units = TemperatureUnit.C };
        var result = validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.ZipCode)
            .WithErrorMessage("Zip code is required.");
    }

    [Fact]
    public void GetAverageWeatherQueryValidator_Should_HaveError_When_ZipCodeIsInvalid()
    {
        var validator = CreateAverageWeatherValidator();
        var model = new GetAverageWeatherQuery { ZipCode = "abc", TimePeriod = "3", Units = TemperatureUnit.C };
        var result = validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.ZipCode)
            .WithErrorMessage("Zip code must be a 5-digit number.");
    }

    [Fact]
    public void GetAverageWeatherQueryValidator_Should_HaveError_When_TimePeriodIsEmpty()
    {
        var validator = CreateAverageWeatherValidator();
        var model = new GetAverageWeatherQuery { ZipCode = "12345", TimePeriod = "", Units = TemperatureUnit.C };
        var result = validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.TimePeriod)
            .WithErrorMessage("Time period is required.");
    }

    [Theory]
    [InlineData("notanumber")]
    [InlineData("1")]
    [InlineData("6")]
    public void GetAverageWeatherQueryValidator_Should_HaveError_When_TimePeriodInvalid(string timePeriod)
    {
        var validator = CreateAverageWeatherValidator();
        var model = new GetAverageWeatherQuery { ZipCode = "12345", TimePeriod = timePeriod, Units = TemperatureUnit.C };
        var result = validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.TimePeriod)
            .WithErrorMessage("Time period must be an integer between 2 and 5.");
    }

    [Fact]
    public void GetAverageWeatherQueryValidator_Should_NotHaveError_When_Valid()
    {
        var validator = CreateAverageWeatherValidator();
        var model = new GetAverageWeatherQuery { ZipCode = "12345", TimePeriod = "3", Units = TemperatureUnit.F };
        var result = validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }
}