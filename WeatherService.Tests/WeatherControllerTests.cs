using Common.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WeatherService.Controllers;
using WeatherService.Services;

namespace WeatherService.Tests;

public class WeatherControllerTests
{
    private readonly Mock<IWeatherProvider> _weatherProvider;

    public WeatherControllerTests()
    {
        _weatherProvider = new Mock<IWeatherProvider>();
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

        var controller = new WeatherController(_weatherProvider.Object);

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

        var controller = new WeatherController(_weatherProvider.Object);

        // Act
        var result = await controller.GetCurrentWeather("00000", TemperatureUnit.F);

        // Assert
        Assert.IsType<BadRequestResult>(result.Result);
        _weatherProvider.Verify(w => w.GetCurrentWeatherForZipCode("00000", TemperatureUnit.F), Times.Once());
    }

    [Fact]
    public async Task GetCurrentWeather_ShouldReturn500_WhenExceptionThrown()
    {
        // Arrange
        _weatherProvider.Setup(w => w.GetCurrentWeatherForZipCode(It.IsAny<string>(), It.IsAny<TemperatureUnit>()))
            .ThrowsAsync(new System.Exception("fail"));

        var controller = new WeatherController(_weatherProvider.Object);

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

        var controller = new WeatherController(_weatherProvider.Object);

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

        var controller = new WeatherController(_weatherProvider.Object);

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
        var controller = new WeatherController(_weatherProvider.Object);

        // Act
        var result = await controller.GetAverageWeather("90210", timePeriod, TemperatureUnit.C);

        // Assert
        Assert.IsType<BadRequestResult>(result.Result);
        _weatherProvider.Verify(w => w.GetAverageWeatherForZipCode(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TemperatureUnit>()), Times.Never());
    }

    [Fact]
    public async Task GetAverageWeather_ShouldReturn500_WhenExceptionThrown()
    {
        // Arrange
        _weatherProvider.Setup(w => w.GetAverageWeatherForZipCode(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TemperatureUnit>()))
            .ThrowsAsync(new System.Exception("fail"));

        var controller = new WeatherController(_weatherProvider.Object);

        // Act
        var result = await controller.GetAverageWeather("90210", "3", TemperatureUnit.C);

        // Assert
        var statusResult = Assert.IsType<StatusCodeResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);
        _weatherProvider.Verify(w => w.GetAverageWeatherForZipCode("90210", 3, TemperatureUnit.C), Times.Once());
    }
}