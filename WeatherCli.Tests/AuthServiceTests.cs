using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Moq;
using Moq.Protected;
using WeatherCli.Models;
using WeatherCli.Services.CredentialStorage;
using WeatherCli.Services.WeatherAuthService;

namespace WeatherCli.Tests;

public class AuthServiceTests
{
    private readonly Mock<HttpMessageHandler> _httpClientHandler;
    private readonly HttpClient _httpClient;
    private readonly Mock<ICredentialStorage> _credentialStorage;

    public AuthServiceTests()
    {
        _httpClientHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpClientHandler.Object)
        {
            BaseAddress = new Uri("https://localhost:1234")
        };
        _credentialStorage = new Mock<ICredentialStorage>();
    }

    [Fact]
    public async Task RegisterUser_ShouldReturnTrue_OnSuccess()
    {
        // Arrange
        _httpClientHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri != null &&
                    req.RequestUri.ToString().EndsWith("/register")
                ),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var service = new WeatherAuthService(_httpClient, _credentialStorage.Object);

        // Act
        var result = await service.RegisterUser("test@example.com", "password");

        // Assert
        Assert.True(result);

        // Verify
        _httpClientHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post &&
                req.RequestUri != null &&
                req.RequestUri.ToString().EndsWith("/register")
            ),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task RegisterUser_ShouldReturnFalse_OnFailure()
    {
        // Arrange
        _httpClientHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("fail", Encoding.UTF8, MediaTypeNames.Text.Plain)
            });

        var service = new WeatherAuthService(_httpClient, _credentialStorage.Object);

        // Act
        var result = await service.RegisterUser("fail@example.com", "password");

        // Assert
        Assert.False(result);

        // Verify
        _httpClientHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post &&
                req.RequestUri != null &&
                req.RequestUri.ToString().EndsWith("/register")
            ),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task LoginUser_ShouldReturnTrue_AndSaveCredentials_OnSuccess()
    {
        // Arrange
        var authToken = new AuthToken { AccessToken = "abc", RefreshToken = "def" };
        var json = JsonSerializer.Serialize(authToken);

        _httpClientHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri != null &&
                    req.RequestUri.ToString().EndsWith("/login")
                ),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json)
            });

        var service = new WeatherAuthService(_httpClient, _credentialStorage.Object);

        _credentialStorage.Setup(s => s.ClearToken());
        _credentialStorage.Setup(s => s.SaveToken(It.IsAny<AuthToken>()));

        // Act
        var result = await service.LoginUser("test@example.com", "password");

        // Assert
        Assert.True(result);
        _credentialStorage.Verify(s => s.ClearToken(), Times.Once());
        _credentialStorage.Verify(s => s.SaveToken(It.Is<AuthToken>(t => t.AccessToken == "abc" && t.RefreshToken == "def")), Times.Once());

        // Verify HTTP
        _httpClientHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post &&
                req.RequestUri != null &&
                req.RequestUri.ToString().EndsWith("/login")
            ),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task LoginUser_ShouldReturnFalse_OnFailure()
    {
        // Arrange
        _httpClientHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("fail", Encoding.UTF8, MediaTypeNames.Text.Plain)
            });

        var service = new WeatherAuthService(_httpClient, _credentialStorage.Object);

        _credentialStorage.Setup(s => s.ClearToken());

        // Act
        var result = await service.LoginUser("fail@example.com", "password");

        // Assert
        Assert.False(result);
        _credentialStorage.Verify(s => s.ClearToken(), Times.Once());
        _credentialStorage.Verify(s => s.SaveToken(It.IsAny<AuthToken>()), Times.Never());

        // Verify HTTP
        _httpClientHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post &&
                req.RequestUri != null &&
                req.RequestUri.ToString().EndsWith("/login")
            ),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task GetBearerToken_ShouldReturnToken_WhenCredentialsExist()
    {
        // Arrange
        var authToken = new AuthToken { AccessToken = "abc", RefreshToken = "def" };
        _credentialStorage.Setup(s => s.GetToken()).Returns(authToken);

        var service = new WeatherAuthService(_httpClient, _credentialStorage.Object);

        // Act
        var result = await service.GetBearerToken();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(authToken.AccessToken, result!.AccessToken);
        Assert.Equal(authToken.RefreshToken, result.RefreshToken);
        _credentialStorage.Verify(s => s.GetToken(), Times.Once());
    }

    [Fact]
    public async Task GetBearerToken_ShouldReturnNull_WhenNoCredentials()
    {
        // Arrange
        _credentialStorage.Setup(s => s.GetToken()).Returns((AuthToken?)null);

        var service = new WeatherAuthService(_httpClient, _credentialStorage.Object);

        // Act
        var result = await service.GetBearerToken();

        // Assert
        Assert.Null(result);
        _credentialStorage.Verify(s => s.GetToken(), Times.Once());
    }

    [Fact]
    public async Task RefreshToken_ShouldReturnNewToken_OnSuccess()
    {
        // Arrange
        var oldToken = new AuthToken { AccessToken = "old", RefreshToken = "refresh" };
        var newToken = new AuthToken { AccessToken = "new", RefreshToken = "refresh2" };
        var json = JsonSerializer.Serialize(newToken);

        _httpClientHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri != null &&
                    req.RequestUri.ToString().EndsWith("/refresh")
                ),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json)
            });

        _credentialStorage.Setup(s => s.SaveToken(It.IsAny<AuthToken>()));

        var service = new WeatherAuthService(_httpClient, _credentialStorage.Object);

        // Act
        var result = await service.RefreshToken(oldToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(newToken.AccessToken, result!.AccessToken);
        Assert.Equal(newToken.RefreshToken, result.RefreshToken);
        _credentialStorage.Verify(s => s.SaveToken(It.Is<AuthToken>(t => t.AccessToken == "new" && t.RefreshToken == "refresh2")), Times.Once());

        // Verify HTTP
        _httpClientHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post &&
                req.RequestUri != null &&
                req.RequestUri.ToString().EndsWith("/refresh")
            ),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task RefreshToken_ShouldReturnNull_OnFailure()
    {
        // Arrange
        var oldToken = new AuthToken { AccessToken = "old", RefreshToken = "refresh" };

        _httpClientHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("fail", Encoding.UTF8, MediaTypeNames.Text.Plain)
            });

        var service = new WeatherAuthService(_httpClient, _credentialStorage.Object);

        // Act
        var result = await service.RefreshToken(oldToken);

        // Assert
        Assert.Null(result);
        _credentialStorage.Verify(s => s.SaveToken(It.IsAny<AuthToken>()), Times.Never());

        // Verify HTTP
        _httpClientHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post &&
                req.RequestUri != null &&
                req.RequestUri.ToString().EndsWith("/refresh")
            ),
            ItExpr.IsAny<CancellationToken>()
        );
    }
}