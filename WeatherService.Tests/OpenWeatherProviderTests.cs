namespace WeatherService.Tests;

using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Common.Models;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using WeatherService.Exceptions;
using WeatherService.Services;
using WeatherService.Services.WeatherProviders.OpenWeather;
using WeatherService.Services.WeatherProviders.OpenWeather.Models;

public class OpenWeatherWeatherProviderTests
{
    private readonly Mock<IConfiguration> _configuration;
    private readonly Mock<IHttpClientFactory> _httpClientFactory;
    private readonly Mock<TemperatureUnitsConverter> _temperatureUnitsConverter;
    private readonly Mock<HttpMessageHandler> _httpClientHandler;
    private readonly Mock<IMemoryCache> _memoryCache;

    public OpenWeatherWeatherProviderTests()
    {
        _configuration = new Mock<IConfiguration>();
        _httpClientFactory = new Mock<IHttpClientFactory>();
        _temperatureUnitsConverter = new Mock<TemperatureUnitsConverter>();
        _httpClientHandler = new Mock<HttpMessageHandler>();
        _memoryCache = new Mock<IMemoryCache>();

        // Mock IMemoryCache.CreateEntry to return a mock ICacheEntry
        var mockCacheEntry = new Mock<ICacheEntry>();
        _memoryCache.Setup(m => m.CreateEntry(It.IsAny<object>()))
            .Returns(mockCacheEntry.Object);

        _configuration.Setup(c => c["DcuWeatherApp:OpenWeatherApiKey"]).Returns("test-api-key");
        _httpClientFactory.Setup(f => f.CreateClient("openWeatherClient")).Returns(new HttpClient(_httpClientHandler.Object, false)
        {
            BaseAddress = new Uri("https://api.openweathermap.org/")
        });


    }

    [Fact]
    public async Task GetCurrentWeatherForZipCode_ShouldReturnWeather_WhenZipCodeIsValid()
    {
        //Arrange
        MockWeatherResponse(new OpenWeatherResult()
        {
            Lat = 40.7128,
            Lon = -74.0060,
            Current = new CurrentWeatherReport()
            {
                Dt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Temp = 295.15,
            },
            Daily = [
                new (){
                 Dt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                 Rain = 0.0,
                 Temp = new Temperature() { Day = 295.15, Eve = 293.15, Morn = 290.15, Night = 292.15 }
            }]
        });

        _temperatureUnitsConverter.Setup(c => c.ConvertKelvinToUnits(295.15, TemperatureUnit.C)).Returns(22.0);

        MockGeoCodeResponse("12345", new GeocodeResult
        {
            Country = "US",
            Name = "Test City",
            Lat = 40.7128,
            Lon = -74.0060
        });

        var openWeatherWeatherProvider = CreateOpenWeatherWeatherProvider();
        //Act
        var result = await openWeatherWeatherProvider.GetCurrentWeatherForZipCode("12345", TemperatureUnit.C);
        //Assert
        Assert.NotNull(result);
        Assert.Equal(22.0, result.CurrentTemperature);
        Assert.Equal(40.7128, result.Lat);
        Assert.Equal(-74.0060, result.Long);
        Assert.Equal(TemperatureUnit.C, result.Unit);
        Assert.False(result.RainPossibleToday);

        // Verify
        _httpClientHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.RequestUri != null &&
                req.RequestUri.ToString().StartsWith("https://api.openweathermap.org/geo/1.0/zip?zip=12345,US&appid=test-api-key")
            ),
            ItExpr.IsAny<CancellationToken>()
        );
        _httpClientHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.RequestUri != null &&
                req.RequestUri.ToString().StartsWith("https://api.openweathermap.org/data/3.0/onecall?lat=40.7128&lon=-74.006&appid=test-api-key")
            ),
            ItExpr.IsAny<CancellationToken>()
        );
        _temperatureUnitsConverter.Verify(c => c.ConvertKelvinToUnits(295.15, TemperatureUnit.C), Times.AtLeastOnce());
    }

    [Fact]
    public async Task GetCurrentWeatherForZipCode_ShouldReturnNull_WhenZipCodeIsInvalid()
    {
        //Arrange
        MockGeoCodeResponse("invalid-zip", null);
        var openWeatherWeatherProvider = CreateOpenWeatherWeatherProvider();
        //Act
        var result = await openWeatherWeatherProvider.GetCurrentWeatherForZipCode("invalid-zip", TemperatureUnit.C);
        //Assert
        Assert.Null(result);

        // Verify
        _httpClientHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.RequestUri != null &&
                req.RequestUri.ToString().StartsWith("https://api.openweathermap.org/geo/1.0/zip?zip=invalid-zip,US&appid=test-api-key")
            ),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task GetCurrentWeatherForZipCode_ShouldReturnNull_WhenNoWeatherFound()
    {
        MockGeoCodeResponse("12345", new GeocodeResult
        {
            Country = "US",
            Name = "Test City",
            Lat = 40.7128,
            Lon = -74.0060
        });

        MockWeatherResponse(null);
        var openWeatherWeatherProvider = CreateOpenWeatherWeatherProvider();
        //Act
        var result = await openWeatherWeatherProvider.GetCurrentWeatherForZipCode("12345", TemperatureUnit.C);
        //Assert
        Assert.Null(result);

        // Verify
        _httpClientHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.RequestUri != null &&
                req.RequestUri.ToString().StartsWith("https://api.openweathermap.org/geo/1.0/zip?zip=12345,US&appid=test-api-key")
            ),
            ItExpr.IsAny<CancellationToken>()
        );
        _httpClientHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.RequestUri != null &&
                req.RequestUri.ToString().StartsWith("https://api.openweathermap.org/data/3.0/onecall?lat=40.7128&lon=-74.006&appid=test-api-key")
            ),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task GetCurrentWeatherForZipCode_ShouldReturnNull_WhenGeocodeResultIsNull()
    {
        // Arrange: MockGeoCodeResponse returns null for this zip
        MockGeoCodeResponse("99999", null);

        var openWeatherWeatherProvider = CreateOpenWeatherWeatherProvider();

        // Act
        var result = await openWeatherWeatherProvider.GetCurrentWeatherForZipCode("99999", TemperatureUnit.C);

        // Assert
        Assert.Null(result);

        // Verify
        _httpClientHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.RequestUri != null &&
                req.RequestUri.ToString().StartsWith("https://api.openweathermap.org/geo/1.0/zip?zip=99999,US&appid=test-api-key")
            ),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task GetCurrentWeatherForZipCode_ShouldReturnWeather_WithRainPossibleTodayFalse_WhenNoTodaysWeatherReport()
    {
        // Arrange: Daily does not contain today's date
        var today = DateTimeOffset.UtcNow.Date;
        var yesterday = today.AddDays(-1);

        var geocodeResult = new GeocodeResult
        {
            Country = "US",
            Name = "Test City",
            Lat = 40.7128,
            Lon = -74.0060
        };
        MockGeoCodeResponse("12345", geocodeResult);

        var weatherResult = new OpenWeatherResult
        {
            Lat = 40.7128,
            Lon = -74.0060,
            Current = new CurrentWeatherReport
            {
                Dt = ((DateTimeOffset)today).ToUnixTimeSeconds(),
                Temp = 295.15
            },
            Daily = [
                new ()
            {
                Dt = ((DateTimeOffset)yesterday).ToUnixTimeSeconds(),
                Rain = 1.0,
                Temp = new Temperature { Day = 295.15, Eve = 293.15, Morn = 290.15, Night = 292.15 }
            }
            ]
        };
        MockWeatherResponse(weatherResult);

        _temperatureUnitsConverter.Setup(c => c.ConvertKelvinToUnits(295.15, TemperatureUnit.C)).Returns(22.0);

        var openWeatherWeatherProvider = CreateOpenWeatherWeatherProvider();

        // Act
        var result = await openWeatherWeatherProvider.GetCurrentWeatherForZipCode("12345", TemperatureUnit.C);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.RainPossibleToday); // Edge case: no daily entry for today

        // Verify
        _httpClientHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.RequestUri != null &&
                req.RequestUri.ToString().StartsWith("https://api.openweathermap.org/geo/1.0/zip?zip=12345,US&appid=test-api-key")
            ),
            ItExpr.IsAny<CancellationToken>()
        );
        _httpClientHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.RequestUri != null &&
                req.RequestUri.ToString().StartsWith("https://api.openweathermap.org/data/3.0/onecall?lat=40.7128&lon=-74.006&appid=test-api-key")
            ),
            ItExpr.IsAny<CancellationToken>()
        );
        _temperatureUnitsConverter.Verify(c => c.ConvertKelvinToUnits(295.15, TemperatureUnit.C), Times.AtLeastOnce());
    }


    [Fact]
    public async Task GetAverageWeatherForZipCode_ShouldReturnAverageWeather_WhenZipCodeIsValid()
    {
        // Arrange
        var weatherResult = new OpenWeatherResult()
        {
            Lat = 40.7128,
            Lon = -74.0060,
            Current = new CurrentWeatherReport()
            {
                Dt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Temp = 295.15,
            },
            Daily = [
                new ()
                    {
                        Dt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                        Rain = 1.0,
                        Temp = new Temperature() { Day = 295.15, Eve = 293.15, Morn = 290.15, Night = 292.15 }
                    },
                    new ()
                    {
                        Dt = DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds(),
                        Rain = 0.0,
                        Temp = new Temperature() { Day = 296.15, Eve = 294.15, Morn = 291.15, Night = 293.15 }
                    }
            ]
        };

        MockWeatherResponse(weatherResult);

        _temperatureUnitsConverter.Setup(c => c.ConvertKelvinToUnits(It.IsAny<double>(), TemperatureUnit.C)).Returns(22.0);

        MockGeoCodeResponse("12345", new GeocodeResult
        {
            Country = "US",
            Name = "Test City",
            Lat = 40.7128,
            Lon = -74.0060
        });

        var openWeatherWeatherProvider = CreateOpenWeatherWeatherProvider();

        // Act
        var result = await openWeatherWeatherProvider.GetAverageWeatherForZipCode("12345", 2, TemperatureUnit.C);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(40.7128, result.Lat);
        Assert.Equal(-74.0060, result.Lon);
        Assert.Equal(22.0, result.AverageTemperature);
        Assert.True(result.RainPossibleInPeriod);
        Assert.Equal(TemperatureUnit.C, result.Unit);

        // Verify
        _httpClientHandler.Protected().Verify(
            "SendAsync",
            Times.AtLeast(2),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        );
        _temperatureUnitsConverter.Verify(c => c.ConvertKelvinToUnits(It.IsAny<double>(), TemperatureUnit.C), Times.AtLeastOnce());
    }

    [Fact]
    public async Task GetAverageWeatherForZipCode_ShouldReturnNull_WhenZipCodeIsInvalid()
    {
        // Arrange
        MockGeoCodeResponse("invalid-zip", null);
        var openWeatherWeatherProvider = CreateOpenWeatherWeatherProvider();

        // Act
        var result = await openWeatherWeatherProvider.GetAverageWeatherForZipCode("invalid-zip", 2, TemperatureUnit.C);

        // Assert
        Assert.Null(result);

        // Verify
        _httpClientHandler.Protected().Verify(
            "SendAsync",
            Times.AtLeastOnce(),
            ItExpr.Is<HttpRequestMessage>(req => req.RequestUri != null && req.RequestUri.ToString().Contains("geo/1.0/zip?zip=invalid-zip,US")),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task GetCurrentWeatherForZipCode_ShouldCacheZipcode()
    {
        // Arrange
        MockGeoCodeResponse("12345", new GeocodeResult
        {
            Country = "US",
            Name = "Test City",
            Lat = 40.7128,
            Lon = -74.0060
        });

        MockWeatherResponse(new OpenWeatherResult
        {
            Lat = 40.7128,
            Lon = -74.0060,
            Current = new CurrentWeatherReport
            {
                Dt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Temp = 295.15,
            },
            Daily = [
                new ()
                {
                    Dt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Rain = 0.0,
                    Temp = new Temperature() { Day = 295.15, Eve = 293.15, Morn = 290.15, Night = 292.15 }
                }
            ]
        });

        _temperatureUnitsConverter.Setup(c => c.ConvertKelvinToUnits(295.15, TemperatureUnit.C)).Returns(22.0);

        var openWeatherWeatherProvider = CreateOpenWeatherWeatherProvider();

        // Act
        var result = await openWeatherWeatherProvider.GetCurrentWeatherForZipCode("12345", TemperatureUnit.C);
        // Assert
        Assert.NotNull(result);
        Assert.Equal(22.0, result.CurrentTemperature);
        Assert.Equal(40.7128, result.Lat);
        Assert.Equal(-74.0060, result.Long);
        Assert.Equal(TemperatureUnit.C, result.Unit);
        Assert.False(result.RainPossibleToday);

        // Verify that the geocode request was made
        _httpClientHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.RequestUri != null &&
                req.RequestUri.ToString().StartsWith("https://api.openweathermap.org/geo/1.0/zip?zip=12345,US&appid=test-api-key")
            ),
            ItExpr.IsAny<CancellationToken>()
        );

        // Verify that the zipcode was cached
        _memoryCache.Verify(m => m.CreateEntry(
            It.Is<object>(key => key.ToString() == "12345")
        ), Times.Once());
    }

    [Fact]
    public async Task GetAverageWeatherForZipCode_ShouldCacheZipcode()
    {
        // Arrange
        MockGeoCodeResponse("12345", new GeocodeResult
        {
            Country = "US",
            Name = "Test City",
            Lat = 40.7128,
            Lon = -74.0060
        });

        MockWeatherResponse(new OpenWeatherResult
        {
            Lat = 40.7128,
            Lon = -74.0060,
            Current = new CurrentWeatherReport
            {
                Dt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Temp = 295.15,
            },
            Daily = [
                new ()
                {
                    Dt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Rain = 0.0,
                    Temp = new Temperature() { Day = 295.15, Eve = 293.15, Morn = 290.15, Night = 292.15 }
                }
            ]
        });

        _temperatureUnitsConverter.Setup(c => c.ConvertKelvinToUnits(295.15, TemperatureUnit.C)).Returns(22.0);

        var openWeatherWeatherProvider = CreateOpenWeatherWeatherProvider();

        // Act
        var result = await openWeatherWeatherProvider.GetAverageWeatherForZipCode("12345", 2, TemperatureUnit.C);
        // Verify that the geocode request was made
        _httpClientHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.RequestUri != null &&
                req.RequestUri.ToString().StartsWith("https://api.openweathermap.org/geo/1.0/zip?zip=12345,US&appid=test-api-key")
            ),
            ItExpr.IsAny<CancellationToken>()
        );

        // Verify that the zipcode was cached
        _memoryCache.Verify(m => m.CreateEntry(
            "12345"
        ), Times.Once());
    }

    [Fact]
    public async Task GetCurrentWeatherForZipCode_ShouldUseCachedEntry_IfExists()
    {
        object something;
        // Arrange
        GeocodeResult cachedGeocodeResult = new GeocodeResult
        {
            Country = "US",
            Name = "Cached City",
            Lat = 40.7128,
            Lon = -74.0060
        };

        _memoryCache.Setup(m => m.TryGetValue("12345", out It.Ref<object>.IsAny))
        .Callback((object key, out object value) =>
        {
            value = cachedGeocodeResult;
        })
        .Returns(true);

        MockWeatherResponse(new OpenWeatherResult
        {
            Lat = 40.7128,
            Lon = -74.0060,
            Current = new CurrentWeatherReport
            {
                Dt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Temp = 295.15,
            },
            Daily = [
                new ()
                {
                    Dt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Rain = 0.0,
                    Temp = new Temperature() { Day = 295.15, Eve = 293.15, Morn = 290.15, Night = 292.15 }
                }
            ]
        });

        _temperatureUnitsConverter.Setup(c => c.ConvertKelvinToUnits(295.15, TemperatureUnit.C)).Returns(22.0);

        var openWeatherWeatherProvider = CreateOpenWeatherWeatherProvider();

        // Act
        var result = await openWeatherWeatherProvider.GetCurrentWeatherForZipCode("12345", TemperatureUnit.C);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(22.0, result.CurrentTemperature);
        Assert.Equal(40.7128, result.Lat);
        Assert.Equal(-74.0060, result.Long);
        Assert.Equal(TemperatureUnit.C, result.Unit);
        Assert.False(result.RainPossibleToday);

        // Verify that the geocode request was not made since it was cached
        _httpClientHandler.Protected().Verify(
            "SendAsync",
            Times.Never(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.RequestUri != null &&
                req.RequestUri.ToString().StartsWith("https://api.openweathermap.org/geo/1.0/zip?zip=12345,US&appid=test-api-key")
            ),
            ItExpr.IsAny<CancellationToken>()
        );

        // Verify that the weather request was made
        _httpClientHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.RequestUri != null &&
                req.RequestUri.ToString().StartsWith("https://api.openweathermap.org/data/3.0/onecall?lat=40.7128&lon=-74.006&appid=test-api-key")
            ),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task GetAverageWeatherForZipCode_ShouldReturnNull_WhenNoWeatherFound()
    {
        // Arrange
        MockGeoCodeResponse("12345", new GeocodeResult
        {
            Country = "US",
            Name = "Test City",
            Lat = 40.7128,
            Lon = -74.0060
        });

        MockWeatherResponse(null);

        var openWeatherWeatherProvider = CreateOpenWeatherWeatherProvider();

        // Act
        var result = await openWeatherWeatherProvider.GetAverageWeatherForZipCode("12345", 2, TemperatureUnit.C);

        // Assert
        Assert.Null(result);

        // Verify
        _httpClientHandler.Protected().Verify(
            "SendAsync",
            Times.AtLeast(2),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task GetCurrentWeatherForZipCode_ShouldThrow_WhenHttpRequestExceptionIsNotNotFound()
    {
        // Arrange
        _httpClientHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException("Some error", null, System.Net.HttpStatusCode.InternalServerError));

        var openWeatherWeatherProvider = CreateOpenWeatherWeatherProvider();

        // Act & Assert
        await Assert.ThrowsAsync<WeatherProviderException>(() => openWeatherWeatherProvider.GetCurrentWeatherForZipCode("12345", TemperatureUnit.C));

        // Verify
        _httpClientHandler.Protected().Verify(
            "SendAsync",
            Times.AtLeastOnce(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task GetAverageWeatherForZipCode_ShouldThrow_WhenHttpRequestExceptionIsNotNotFound()
    {
        // Arrange
        _httpClientHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException("Some error", null, System.Net.HttpStatusCode.InternalServerError));

        var openWeatherWeatherProvider = CreateOpenWeatherWeatherProvider();

        // Act & Assert
        await Assert.ThrowsAsync<WeatherProviderException>(() => openWeatherWeatherProvider.GetAverageWeatherForZipCode("12345", 2, TemperatureUnit.C));

        // Verify
        _httpClientHandler.Protected().Verify(
            "SendAsync",
            Times.AtLeastOnce(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    private OpenWeatherWeatherProvider CreateOpenWeatherWeatherProvider()
    {
        return new OpenWeatherWeatherProvider(
            _configuration.Object,
            _temperatureUnitsConverter.Object,
            _httpClientFactory.Object,
            _memoryCache.Object
        );
    }

    private void MockGeoCodeResponse(string zip, GeocodeResult? geocodeResult)
    {
        var geocodeResponse = geocodeResult == null ? null : JsonSerializer.Serialize(geocodeResult);

        _httpClientHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri != null && req.RequestUri.ToString().Contains($"geo/1.0/zip?zip={zip},US")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(() => geocodeResponse == null ? new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.NotFound
            } : new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(geocodeResponse, Encoding.UTF8, MediaTypeNames.Application.Json)
            });
    }

    private void MockWeatherResponse(OpenWeatherResult? weatherResult)
    {
        var weatherResponse = weatherResult == null ? null : JsonSerializer.Serialize(weatherResult);

        _httpClientHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri != null && req.RequestUri.ToString().Contains("data/3.0/onecall")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(() => weatherResponse == null ? new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.NotFound
            } : new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(weatherResponse, Encoding.UTF8, MediaTypeNames.Application.Json)
            });
    }
}
