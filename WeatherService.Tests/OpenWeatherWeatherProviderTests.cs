namespace WeatherService.Tests;

using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Common.Models;
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

    private OpenWeatherWeatherProvider CreateProvider()
        => new OpenWeatherWeatherProvider(
            _configuration.Object,
            _temperatureUnitsConverter.Object,
            _httpClientFactory.Object,
            _memoryCache.Object
        );

    private void SetupGeoCode(string zip, GeocodeResult? geocodeResult)
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
                StatusCode = HttpStatusCode.NotFound
            } : new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(geocodeResponse, Encoding.UTF8, MediaTypeNames.Application.Json)
            });
    }

    private void SetupWeather(OpenWeatherResult? weatherResult)
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
                StatusCode = HttpStatusCode.NotFound
            } : new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(weatherResponse, Encoding.UTF8, MediaTypeNames.Application.Json)
            });
    }

    private void VerifyGeoCodeRequest(string zip, Times times)
    {
        _httpClientHandler.Protected().Verify(
            "SendAsync",
            times,
            ItExpr.Is<HttpRequestMessage>(req =>
                req.RequestUri != null &&
                req.RequestUri.ToString().StartsWith($"https://api.openweathermap.org/geo/1.0/zip?zip={zip},US&appid=test-api-key")
            ),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    private void VerifyWeatherRequest(string lat, string lon, Times times)
    {
        _httpClientHandler.Protected().Verify(
            "SendAsync",
            times,
            ItExpr.Is<HttpRequestMessage>(req =>
                req.RequestUri != null &&
                req.RequestUri.ToString().StartsWith($"https://api.openweathermap.org/data/3.0/onecall?lat={lat}&lon={lon}&appid=test-api-key")
            ),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task GetCurrentWeatherForZipCode_ShouldReturnWeather_WhenZipCodeIsValid()
    {
        // Arrange
        var zip = "12345";
        var lat = 40.7128;
        var lon = -74.0060;
        var kelvin = 295.15;
        var expectedTemp = 22.0;

        SetupWeather(new OpenWeatherResult
        {
            Lat = lat,
            Lon = lon,
            Current = new CurrentWeatherReport { Dt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), Temp = kelvin },
            Daily = [
                new ()
                {
                    Dt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Rain = 0.0,
                    Temp = new Temperature { Day = kelvin, Eve = 293.15, Morn = 290.15, Night = 292.15 }
                }
            ]
        });

        _temperatureUnitsConverter.Setup(c => c.ConvertKelvinToUnits(kelvin, TemperatureUnit.C)).Returns(expectedTemp);

        SetupGeoCode(zip, new GeocodeResult
        {
            Country = "US",
            Name = "Test City",
            Lat = lat,
            Lon = lon
        });

        var provider = CreateProvider();

        // Act
        var result = await provider.GetCurrentWeatherForZipCode(zip, TemperatureUnit.C);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedTemp, result.CurrentTemperature);
        Assert.Equal(lat, result.Lat);
        Assert.Equal(lon, result.Long);
        Assert.Equal(TemperatureUnit.C, result.Unit);
        Assert.False(result.RainPossibleToday);

        VerifyGeoCodeRequest(zip, Times.Once());
        VerifyWeatherRequest(lat.ToString(), lon.ToString(), Times.Once());
        _temperatureUnitsConverter.Verify(c => c.ConvertKelvinToUnits(kelvin, TemperatureUnit.C), Times.AtLeastOnce());
    }

    [Fact]
    public async Task GetCurrentWeatherForZipCode_ShouldReturnNull_WhenZipCodeIsInvalid()
    {
        // Arrange
        var zip = "invalid-zip";
        SetupGeoCode(zip, null);
        var provider = CreateProvider();

        // Act
        var result = await provider.GetCurrentWeatherForZipCode(zip, TemperatureUnit.C);

        // Assert
        Assert.Null(result);
        VerifyGeoCodeRequest(zip, Times.Once());
    }

    [Fact]
    public async Task GetCurrentWeatherForZipCode_ShouldReturnNull_WhenNoWeatherFound()
    {
        var zip = "12345";
        var lat = 40.7128;
        var lon = -74.0060;

        SetupGeoCode(zip, new GeocodeResult
        {
            Country = "US",
            Name = "Test City",
            Lat = lat,
            Lon = lon
        });

        SetupWeather(null);
        var provider = CreateProvider();

        // Act
        var result = await provider.GetCurrentWeatherForZipCode(zip, TemperatureUnit.C);

        // Assert
        Assert.Null(result);
        VerifyGeoCodeRequest(zip, Times.Once());
        VerifyWeatherRequest(lat.ToString(), lon.ToString(), Times.Once());
    }

    [Fact]
    public async Task GetCurrentWeatherForZipCode_ShouldReturnNull_WhenGeocodeResultIsNull()
    {
        var zip = "99999";
        SetupGeoCode(zip, null);

        var provider = CreateProvider();

        // Act
        var result = await provider.GetCurrentWeatherForZipCode(zip, TemperatureUnit.C);

        // Assert
        Assert.Null(result);
        VerifyGeoCodeRequest(zip, Times.Once());
    }

    [Fact]
    public async Task GetCurrentWeatherForZipCode_ShouldReturnWeather_WithRainPossibleTodayFalse_WhenNoTodaysWeatherReport()
    {
        var zip = "12345";
        var lat = 40.7128;
        var lon = -74.0060;
        var kelvin = 295.15;
        var expectedTemp = 22.0;
        var today = DateTimeOffset.UtcNow.Date;
        var yesterday = today.AddDays(-1);

        SetupGeoCode(zip, new GeocodeResult
        {
            Country = "US",
            Name = "Test City",
            Lat = lat,
            Lon = lon
        });

        SetupWeather(new OpenWeatherResult
        {
            Lat = lat,
            Lon = lon,
            Current = new CurrentWeatherReport { Dt = ((DateTimeOffset)today).ToUnixTimeSeconds(), Temp = kelvin },
            Daily = [
                new ()
                {
                    Dt = ((DateTimeOffset)yesterday).ToUnixTimeSeconds(),
                    Rain = 1.0,
                    Temp = new Temperature { Day = kelvin, Eve = 293.15, Morn = 290.15, Night = 292.15 }
                }
            ]
        });

        _temperatureUnitsConverter.Setup(c => c.ConvertKelvinToUnits(kelvin, TemperatureUnit.C)).Returns(expectedTemp);

        var provider = CreateProvider();

        // Act
        var result = await provider.GetCurrentWeatherForZipCode(zip, TemperatureUnit.C);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.RainPossibleToday);

        VerifyGeoCodeRequest(zip, Times.Once());
        VerifyWeatherRequest(lat.ToString(), lon.ToString(), Times.Once());
        _temperatureUnitsConverter.Verify(c => c.ConvertKelvinToUnits(kelvin, TemperatureUnit.C), Times.AtLeastOnce());
    }

    [Fact]
    public async Task GetAverageWeatherForZipCode_ShouldReturnAverageWeather_WhenZipCodeIsValid()
    {
        var zip = "12345";
        var lat = 40.7128;
        var lon = -74.0060;

        SetupWeather(new OpenWeatherResult
        {
            Lat = lat,
            Lon = lon,
            Current = new CurrentWeatherReport { Dt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), Temp = 295.15 },
            Daily = [
                new ()
                {
                    Dt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Rain = 1.0,
                    Temp = new Temperature { Day = 295.15, Eve = 293.15, Morn = 290.15, Night = 292.15 }
                },
                new ()
                {
                    Dt = DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds(),
                    Rain = 0.0,
                    Temp = new Temperature { Day = 296.15, Eve = 294.15, Morn = 291.15, Night = 293.15 }
                }
            ]
        });

        _temperatureUnitsConverter.Setup(c => c.ConvertKelvinToUnits(It.IsAny<double>(), TemperatureUnit.C)).Returns(22.0);

        SetupGeoCode(zip, new GeocodeResult
        {
            Country = "US",
            Name = "Test City",
            Lat = lat,
            Lon = lon
        });

        var provider = CreateProvider();

        // Act
        var result = await provider.GetAverageWeatherForZipCode(zip, 2, TemperatureUnit.C);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(lat, result.Lat);
        Assert.Equal(lon, result.Lon);
        Assert.Equal(22.0, result.AverageTemperature);
        Assert.True(result.RainPossibleInPeriod);
        Assert.Equal(TemperatureUnit.C, result.Unit);

        VerifyGeoCodeRequest(zip, Times.AtLeast(1));
        _temperatureUnitsConverter.Verify(c => c.ConvertKelvinToUnits(It.IsAny<double>(), TemperatureUnit.C), Times.AtLeastOnce());
    }

    [Fact]
    public async Task GetAverageWeatherForZipCode_ShouldReturnNull_WhenZipCodeIsInvalid()
    {
        var zip = "invalid-zip";
        SetupGeoCode(zip, null);
        var provider = CreateProvider();

        // Act
        var result = await provider.GetAverageWeatherForZipCode(zip, 2, TemperatureUnit.C);

        // Assert
        Assert.Null(result);
        VerifyGeoCodeRequest(zip, Times.AtLeastOnce());
    }

    [Fact]
    public async Task GetCurrentWeatherForZipCode_ShouldCacheZipcode()
    {
        var zip = "12345";
        var lat = 40.7128;
        var lon = -74.0060;
        var kelvin = 295.15;
        var expectedTemp = 22.0;

        SetupGeoCode(zip, new GeocodeResult
        {
            Country = "US",
            Name = "Test City",
            Lat = lat,
            Lon = lon
        });

        SetupWeather(new OpenWeatherResult
        {
            Lat = lat,
            Lon = lon,
            Current = new CurrentWeatherReport { Dt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), Temp = kelvin },
            Daily = [
                new ()
                {
                    Dt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Rain = 0.0,
                    Temp = new Temperature { Day = kelvin, Eve = 293.15, Morn = 290.15, Night = 292.15 }
                }
            ]
        });

        _temperatureUnitsConverter.Setup(c => c.ConvertKelvinToUnits(kelvin, TemperatureUnit.C)).Returns(expectedTemp);

        var provider = CreateProvider();

        // Act
        var result = await provider.GetCurrentWeatherForZipCode(zip, TemperatureUnit.C);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedTemp, result.CurrentTemperature);
        Assert.Equal(lat, result.Lat);
        Assert.Equal(lon, result.Long);
        Assert.Equal(TemperatureUnit.C, result.Unit);
        Assert.False(result.RainPossibleToday);

        // Verify that the geocode request was made
        VerifyGeoCodeRequest(zip, Times.Once());

        // Verify that the zipcode was cached
        _memoryCache.Verify(m => m.CreateEntry(
            It.Is<object>(key => key.ToString() == zip)
        ), Times.Once());
    }

    [Fact]
    public async Task GetAverageWeatherForZipCode_ShouldCacheZipcode()
    {
        var zip = "12345";
        var lat = 40.7128;
        var lon = -74.0060;
        var kelvin = 295.15;
        var expectedTemp = 22.0;

        SetupGeoCode(zip, new GeocodeResult
        {
            Country = "US",
            Name = "Test City",
            Lat = lat,
            Lon = lon
        });

        SetupWeather(new OpenWeatherResult
        {
            Lat = lat,
            Lon = lon,
            Current = new CurrentWeatherReport { Dt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), Temp = kelvin },
            Daily = [
                new ()
                {
                    Dt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Rain = 0.0,
                    Temp = new Temperature { Day = kelvin, Eve = 293.15, Morn = 290.15, Night = 292.15 }
                }
            ]
        });

        _temperatureUnitsConverter.Setup(c => c.ConvertKelvinToUnits(kelvin, TemperatureUnit.C)).Returns(expectedTemp);

        var provider = CreateProvider();

        // Act
        var result = await provider.GetAverageWeatherForZipCode(zip, 2, TemperatureUnit.C);

        // Verify that the geocode request was made
        VerifyGeoCodeRequest(zip, Times.Once());

        // Verify that the zipcode was cached
        _memoryCache.Verify(m => m.CreateEntry(
            zip
        ), Times.Once());
    }

    [Fact]
    public async Task GetCurrentWeatherForZipCode_ShouldUseCachedEntry_IfExists()
    {
        var zip = "12345";
        var lat = 40.7128;
        var lon = -74.0060;
        var kelvin = 295.15;
        var expectedTemp = 22.0;

        GeocodeResult cachedGeocodeResult = new GeocodeResult
        {
            Country = "US",
            Name = "Cached City",
            Lat = lat,
            Lon = lon
        };

        _memoryCache.Setup(m => m.TryGetValue(zip, out It.Ref<object>.IsAny))
        .Callback((object key, out object value) =>
        {
            value = cachedGeocodeResult;
        })
        .Returns(true);

        SetupWeather(new OpenWeatherResult
        {
            Lat = lat,
            Lon = lon,
            Current = new CurrentWeatherReport { Dt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), Temp = kelvin },
            Daily = [
                new ()
                {
                    Dt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Rain = 0.0,
                    Temp = new Temperature { Day = kelvin, Eve = 293.15, Morn = 290.15, Night = 292.15 }
                }
            ]
        });

        _temperatureUnitsConverter.Setup(c => c.ConvertKelvinToUnits(kelvin, TemperatureUnit.C)).Returns(expectedTemp);

        var provider = CreateProvider();

        // Act
        var result = await provider.GetCurrentWeatherForZipCode(zip, TemperatureUnit.C);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedTemp, result.CurrentTemperature);
        Assert.Equal(lat, result.Lat);
        Assert.Equal(lon, result.Long);
        Assert.Equal(TemperatureUnit.C, result.Unit);
        Assert.False(result.RainPossibleToday);

        // Verify that the geocode request was not made since it was cached
        _httpClientHandler.Protected().Verify(
            "SendAsync",
            Times.Never(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.RequestUri != null &&
                req.RequestUri.ToString().StartsWith($"https://api.openweathermap.org/geo/1.0/zip?zip={zip},US&appid=test-api-key")
            ),
            ItExpr.IsAny<CancellationToken>()
        );

        // Verify that the weather request was made
        VerifyWeatherRequest(lat.ToString(), lon.ToString(), Times.Once());
    }

    [Fact]
    public async Task GetAverageWeatherForZipCode_ShouldReturnNull_WhenNoWeatherFound()
    {
        var zip = "12345";
        var lat = 40.7128;
        var lon = -74.0060;

        SetupGeoCode(zip, new GeocodeResult
        {
            Country = "US",
            Name = "Test City",
            Lat = lat,
            Lon = lon
        });

        SetupWeather(null);

        var provider = CreateProvider();

        // Act
        var result = await provider.GetAverageWeatherForZipCode(zip, 2, TemperatureUnit.C);

        // Assert
        Assert.Null(result);

        // Verify
        VerifyGeoCodeRequest(zip, Times.AtLeast(1));
        VerifyWeatherRequest(lat.ToString(), lon.ToString(), Times.AtLeast(1));
    }

    [Fact]
    public async Task GetCurrentWeatherForZipCode_ShouldThrow_WhenHttpRequestExceptionIsNotNotFound()
    {
        _httpClientHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException("Some error", null, HttpStatusCode.InternalServerError));

        var provider = CreateProvider();

        // Act & Assert
        await Assert.ThrowsAsync<WeatherProviderException>(() => provider.GetCurrentWeatherForZipCode("12345", TemperatureUnit.C));

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
        _httpClientHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException("Some error", null, HttpStatusCode.InternalServerError));

        var provider = CreateProvider();

        // Act & Assert
        await Assert.ThrowsAsync<WeatherProviderException>(() => provider.GetAverageWeatherForZipCode("12345", 2, TemperatureUnit.C));

        _httpClientHandler.Protected().Verify(
            "SendAsync",
            Times.AtLeastOnce(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        );
    }
}
