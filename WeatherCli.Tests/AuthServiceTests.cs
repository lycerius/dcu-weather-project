using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Moq;
using Moq.Protected;
using WeatherCli.Models;
using WeatherCli.Services.CredentialStorage;
using WeatherCli.Services.WeatherAuthService;
using Microsoft.Extensions.Logging;

namespace WeatherCli.Tests;

public class AuthServiceTests
{
    private readonly Mock<HttpMessageHandler> _httpClientHandler;
    private readonly HttpClient _httpClient;
    private readonly Mock<ICredentialStorage> _credentialStorage;
    private readonly Mock<IHttpClientFactory> _httpClientFactory;
    private readonly Mock<ILogger<WeatherAuthService>> _logger;

    public AuthServiceTests()
    {
        _httpClientHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpClientHandler.Object)
        {
            BaseAddress = new Uri("https://localhost:1234")
        };
        _credentialStorage = new Mock<ICredentialStorage>();
        _httpClientFactory = new Mock<IHttpClientFactory>();
        _httpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(_httpClient);
        _logger = new Mock<ILogger<WeatherAuthService>>();
    }

    private WeatherAuthService CreateService()
        => new WeatherAuthService(_httpClientFactory.Object, _credentialStorage.Object, _logger.Object);

    private void SetupHttpPost(string endpoint, HttpStatusCode statusCode, object? content = null)
    {
        _httpClientHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri != null &&
                    req.RequestUri.ToString().EndsWith(endpoint)
                ),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage(statusCode)
            {
                Content = content != null
                    ? new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, MediaTypeNames.Application.Json)
                    : null
            });
    }

    private void VerifyHttpPost(string endpoint)
    {
        _httpClientHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post &&
                req.RequestUri != null &&
                req.RequestUri.ToString().EndsWith(endpoint)
            ),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task RegisterUser_ShouldReturnTrue_OnSuccess()
    {
        // Arrange
        SetupHttpPost("/register", HttpStatusCode.OK);

        var service = CreateService();

        // Act
        var result = await service.RegisterUser("test@example.com", "password");

        // Assert
        Assert.True(result);
        VerifyHttpPost("/register");
    }

    [Fact]
    public async Task RegisterUser_ShouldReturnFalse_OnFailure()
    {
        // Arrange
        SetupHttpPost("/register", HttpStatusCode.BadRequest, "fail");

        var service = CreateService();

        // Act
        var result = await service.RegisterUser("fail@example.com", "password");

        // Assert
        Assert.False(result);
        VerifyHttpPost("/register");
    }

    [Fact]
    public async Task LoginUser_ShouldReturnTrue_AndSaveCredentials_OnSuccess()
    {
        // Arrange
        var authToken = new AuthToken { AccessToken = "abc", RefreshToken = "def" };
        SetupHttpPost("/login", HttpStatusCode.OK, authToken);

        var service = CreateService();

        _credentialStorage.Setup(s => s.ClearToken());
        _credentialStorage.Setup(s => s.SaveToken(It.IsAny<AuthToken>()));

        // Act
        var result = await service.LoginUser("test@example.com", "password");

        // Assert
        Assert.True(result);
        _credentialStorage.Verify(s => s.ClearToken(), Times.Once());
        _credentialStorage.Verify(s => s.SaveToken(It.Is<AuthToken>(t => t.AccessToken == "abc" && t.RefreshToken == "def")), Times.Once());
        VerifyHttpPost("/login");
    }

    [Fact]
    public async Task LoginUser_ShouldReturnFalse_OnFailure()
    {
        // Arrange
        SetupHttpPost("/login", HttpStatusCode.Unauthorized, "fail");

        var service = CreateService();

        _credentialStorage.Setup(s => s.ClearToken());

        // Act
        var result = await service.LoginUser("fail@example.com", "password");

        // Assert
        Assert.False(result);
        _credentialStorage.Verify(s => s.ClearToken(), Times.Once());
        _credentialStorage.Verify(s => s.SaveToken(It.IsAny<AuthToken>()), Times.Never());
        VerifyHttpPost("/login");
    }

    [Fact]
    public async Task GetBearerToken_ShouldReturnToken_WhenCredentialsExist()
    {
        // Arrange
        var authToken = new AuthToken { AccessToken = "abc", RefreshToken = "def" };
        _credentialStorage.Setup(s => s.GetToken()).Returns(authToken);
        SetupHttpPost("/refresh", HttpStatusCode.OK, authToken);

        var service = CreateService();

        // Act
        var result = await service.GetBearerToken();

        // Assert
        Assert.NotNull(result);
        Assert.True(authToken.Equals(result));
        _credentialStorage.Verify(s => s.GetToken(), Times.Once());
        VerifyHttpPost("/refresh");
    }

    [Fact]
    public async Task GetBearerToken_ShouldReturnNull_WhenNoCredentials()
    {
        // Arrange
        _credentialStorage.Setup(s => s.GetToken()).Returns((AuthToken?)null);

        var service = CreateService();

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
        SetupHttpPost("/refresh", HttpStatusCode.OK, newToken);

        _credentialStorage.Setup(s => s.SaveToken(It.IsAny<AuthToken>()));

        var service = CreateService();

        // Act
        var result = await service.RefreshToken(oldToken);

        // Assert
        Assert.NotNull(result);
        Assert.True(newToken.Equals(result));
        _credentialStorage.Verify(s => s.SaveToken(It.Is<AuthToken>(t => t.AccessToken == "new" && t.RefreshToken == "refresh2")), Times.Once());
        VerifyHttpPost("/refresh");
    }

    [Fact]
    public async Task RefreshToken_ShouldReturnNull_OnFailure()
    {
        // Arrange
        var oldToken = new AuthToken { AccessToken = "old", RefreshToken = "refresh" };
        SetupHttpPost("/refresh", HttpStatusCode.BadRequest, "fail");

        var service = CreateService();

        // Act
        var result = await service.RefreshToken(oldToken);

        // Assert
        Assert.Null(result);
        _credentialStorage.Verify(s => s.SaveToken(It.IsAny<AuthToken>()), Times.Never());
        VerifyHttpPost("/refresh");
    }
}