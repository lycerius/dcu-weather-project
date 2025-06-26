using System.Text.Json;
using System.Text;
using Common.Models;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Mime;
using WeatherCli.Services.WeatherService;
using WeatherCli.Services.WeatherAuthService;
using Microsoft.Extensions.Logging;

namespace WeatherCli.Tests;

public class WeatherServiceTests
{
    private readonly Mock<HttpMessageHandler> _httpClientHandler;
    private readonly HttpClient _httpClient;
    private readonly Mock<ILogger<WeatherService>> _logger;

    public WeatherServiceTests()
    {
        _httpClientHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpClientHandler.Object)
        {
            BaseAddress = new Uri("https://localhost:1234")
        };
        _logger = new Mock<ILogger<WeatherService>>();
    }

    [Fact]
    public async Task GetCurrentWeatherForZipCode_ShouldReturnWeather_WhenApiReturnsSuccess()
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
        var json = JsonSerializer.Serialize(expected);

        _httpClientHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri != null &&
                    req.RequestUri.ToString().Equals("https://localhost:1234/Weather/Current/90210?units=C")
                ),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json)
            });

        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(_httpClient);
        var weatherAuthServiceMock = new Mock<IWeatherAuthService>();
        var service = new WeatherService(httpClientFactoryMock.Object, weatherAuthServiceMock.Object, _logger.Object);

        // Act
        var result = await service.GetCurrentWeatherForZipCode("90210", TemperatureUnit.C);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expected.Lat, result.Lat);
        Assert.Equal(expected.Long, result.Long);
        Assert.Equal(expected.CurrentTemperature, result.CurrentTemperature);
        Assert.Equal(expected.Unit, result.Unit);
        Assert.Equal(expected.RainPossibleToday, result.RainPossibleToday);

        // Verify
        _httpClientHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Get &&
                req.RequestUri != null &&
                req.RequestUri.ToString().Equals("https://localhost:1234/Weather/Current/90210?units=C")
            ),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task GetCurrentWeatherForZipCode_ShouldReturnNull_WhenApiReturnsNotFound()
    {
        // Arrange
        _httpClientHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri != null &&
                    req.RequestUri.ToString().Equals("https://localhost:1234/Weather/Current/00000?units=F")
                ),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            });

        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(_httpClient);
        var weatherAuthServiceMock = new Mock<IWeatherAuthService>();
        var service = new WeatherService(httpClientFactoryMock.Object, weatherAuthServiceMock.Object, _logger.Object);

        // Act
        var result = await service.GetCurrentWeatherForZipCode("00000", TemperatureUnit.F);

        // Assert
        Assert.Null(result);

        // Verify
        _httpClientHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Get &&
                req.RequestUri != null &&
                req.RequestUri.ToString().Equals("https://localhost:1234/Weather/Current/00000?units=F")
            ),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task GetAverageWeather_ShouldReturnWeather_WhenApiReturnsSuccess()
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
        var json = JsonSerializer.Serialize(expected);

        _httpClientHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri != null &&
                    req.RequestUri.ToString().Equals("https://localhost:1234/Weather/Average/90210?units=F&timePeriod=7")
                ),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json)
            });

        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(_httpClient);
        var weatherAuthServiceMock = new Mock<IWeatherAuthService>();
        var service = new WeatherService(httpClientFactoryMock.Object, weatherAuthServiceMock.Object, _logger.Object);

        // Act
        var result = await service.GetAverageWeather("90210", TemperatureUnit.F, 7);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expected.Lat, result.Lat);
        Assert.Equal(expected.Lon, result.Lon);
        Assert.Equal(expected.AverageTemperature, result.AverageTemperature);
        Assert.Equal(expected.Unit, result.Unit);
        Assert.Equal(expected.RainPossibleInPeriod, result.RainPossibleInPeriod);

        // Verify
        _httpClientHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Get &&
                req.RequestUri != null &&
                req.RequestUri.ToString().Equals("https://localhost:1234/Weather/Average/90210?units=F&timePeriod=7")
            ),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task GetAverageWeather_ShouldReturnNull_WhenApiReturnsNotFound()
    {
        // Arrange
        _httpClientHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri != null &&
                    req.RequestUri.ToString().Equals("https://localhost:1234/Weather/Average/00000?units=C&timePeriod=3")
                ),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            });

        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(_httpClient);
        var weatherAuthServiceMock = new Mock<IWeatherAuthService>();
        var service = new WeatherService(httpClientFactoryMock.Object, weatherAuthServiceMock.Object, _logger.Object);

        // Act
        var result = await service.GetAverageWeather("00000", TemperatureUnit.C, 3);

        // Assert
        Assert.Null(result);

        // Verify
        _httpClientHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Get &&
                req.RequestUri != null &&
                req.RequestUri.ToString().Equals("https://localhost:1234/Weather/Average/00000?units=C&timePeriod=3")
            ),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task GetCurrentWeatherForZipCode_ShouldReturnNull_OnHttpError()
    {
        // Arrange
        _httpClientHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException("Network error"));

        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(_httpClient);
        var weatherAuthServiceMock = new Mock<IWeatherAuthService>();
        var service = new WeatherService(httpClientFactoryMock.Object, weatherAuthServiceMock.Object, _logger.Object);

        // Act
        var result = await service.GetCurrentWeatherForZipCode("90210", TemperatureUnit.C);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAverageWeather_ShouldReturnNull_OnHttpError()
    {
        // Arrange
        _httpClientHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException("Network error"));

        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(_httpClient);
        var weatherAuthServiceMock = new Mock<IWeatherAuthService>();
        var service = new WeatherService(httpClientFactoryMock.Object, weatherAuthServiceMock.Object, _logger.Object);

        // Act
        var result = await service.GetAverageWeather("90210", TemperatureUnit.C, 3);

        // Assert
        Assert.Null(result);
    }
}
