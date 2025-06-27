using Common.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WeatherService.Controllers;
using WeatherService.Services.WeatherProviders;
using FluentValidation;
using WeatherService.Models;

namespace WeatherService.Tests;

public class WeatherControllerTests
{
    private readonly Mock<IWeatherProvider> _weatherProvider;
    private readonly Mock<ILogger<WeatherControllerV1>> _logger;
    private readonly Mock<IValidator<GetCurrentWeatherQuery>> _currentWeatherValidator;
    private readonly Mock<IValidator<GetAverageWeatherQuery>> _averageWeatherValidator;

    public WeatherControllerTests()
    {
        _weatherProvider = new Mock<IWeatherProvider>();
        _logger = new Mock<ILogger<WeatherControllerV1>>();
        _currentWeatherValidator = new Mock<IValidator<GetCurrentWeatherQuery>>();
        _averageWeatherValidator = new Mock<IValidator<GetAverageWeatherQuery>>();

        // Default: always valid
        _currentWeatherValidator
            .Setup(v => v.ValidateAsync(It.IsAny<GetCurrentWeatherQuery>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _averageWeatherValidator
            .Setup(v => v.ValidateAsync(It.IsAny<GetAverageWeatherQuery>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
    }

    private WeatherControllerV1 CreateController()
        => new WeatherControllerV1(
            _logger.Object,
            _weatherProvider.Object,
            _currentWeatherValidator.Object,
            _averageWeatherValidator.Object
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
        _currentWeatherValidator
            .Setup(v => v.ValidateAsync(It.IsAny<GetCurrentWeatherQuery>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(
                new[] { new FluentValidation.Results.ValidationFailure("ZipCode", "Zip code must be a 5-digit number.") }
            ));

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
        _averageWeatherValidator
            .Setup(v => v.ValidateAsync(It.IsAny<GetAverageWeatherQuery>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(
                new[] { new FluentValidation.Results.ValidationFailure("TimePeriod", "Time period must be an integer between 2 and 5.") }
            ));

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
        _averageWeatherValidator
            .Setup(v => v.ValidateAsync(It.IsAny<GetAverageWeatherQuery>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(
                new[] { new FluentValidation.Results.ValidationFailure("ZipCode", "Zip code must be a 5-digit number.") }
            ));

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

    [Fact]
    public async Task GetCurrentWeather_ShouldReturnBadRequest_WhenValidationReturnsMultipleErrors()
    {
        // Arrange
        _currentWeatherValidator
            .Setup(v => v.ValidateAsync(It.IsAny<GetCurrentWeatherQuery>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(
                new[]
                {
                    new FluentValidation.Results.ValidationFailure("ZipCode", "Zip code is required."),
                    new FluentValidation.Results.ValidationFailure("Units", "Units must be a valid temperature unit.")
                }
            ));

        var controller = CreateController();

        // Act
        var result = await controller.GetCurrentWeather("", (TemperatureUnit)999);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var errors = ((IEnumerable<string>)badRequest.Value).ToList();
        Assert.Contains("Zip code is required.", errors);
        Assert.Contains("Units must be a valid temperature unit.", errors);
        _weatherProvider.Verify(w => w.GetCurrentWeatherForZipCode(It.IsAny<string>(), It.IsAny<TemperatureUnit>()), Times.Never());
    }

    [Fact]
    public async Task GetAverageWeather_ShouldReturnBadRequest_WhenValidationReturnsMultipleErrors()
    {
        // Arrange
        _averageWeatherValidator
            .Setup(v => v.ValidateAsync(It.IsAny<GetAverageWeatherQuery>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(
                new[]
                {
                    new FluentValidation.Results.ValidationFailure("ZipCode", "Zip code is required."),
                    new FluentValidation.Results.ValidationFailure("TimePeriod", "Time period must be an integer between 2 and 5."),
                    new FluentValidation.Results.ValidationFailure("Units", "Units must be a valid temperature unit.")
                }
            ));

        var controller = CreateController();

        // Act
        var result = await controller.GetAverageWeather("", "1", (TemperatureUnit)999);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var errors = ((IEnumerable<string>)badRequest.Value).ToList();
        Assert.Contains("Zip code is required.", errors);
        Assert.Contains("Time period must be an integer between 2 and 5.", errors);
        Assert.Contains("Units must be a valid temperature unit.", errors);
        _weatherProvider.Verify(w => w.GetAverageWeatherForZipCode(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TemperatureUnit>()), Times.Never());
    }
}