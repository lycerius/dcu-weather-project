using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Moq;
using Moq.Protected;
using WeatherCli.Models;
using WeatherCli.Services;

namespace WeatherCli.Tests;

public class DCUAuthServiceTests
{
    private readonly Mock<HttpMessageHandler> _httpClientHandler;
    private readonly HttpClient _httpClient;

    public DCUAuthServiceTests()
    {
        _httpClientHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpClientHandler.Object)
        {
            BaseAddress = new Uri("https://localhost:1234")
        };
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

        var service = new DCUAuthService(_httpClient);

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

        var service = new DCUAuthService(_httpClient);

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
    public async Task LoginUser_ShouldReturnTrue_AndWriteCredentials_OnSuccess()
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

        var service = new DCUAuthService(_httpClient);

        // Clean up before test
        if (File.Exists(".credentials.txt"))
            File.Delete(".credentials.txt");

        // Act
        var result = await service.LoginUser("test@example.com", "password");

        // Assert
        Assert.True(result);
        Assert.True(File.Exists(".credentials.txt"));
        var fileContent = await File.ReadAllTextAsync(".credentials.txt");
        var tokenFromFile = JsonSerializer.Deserialize<AuthToken>(fileContent);
        Assert.Equal(authToken.AccessToken, tokenFromFile!.AccessToken);
        Assert.Equal(authToken.RefreshToken, tokenFromFile.RefreshToken);

        // Verify
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

        // Clean up after test
        File.Delete(".credentials.txt");
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

        var service = new DCUAuthService(_httpClient);

        // Clean up before test
        if (File.Exists(".credentials.txt"))
            File.Delete(".credentials.txt");

        // Act
        var result = await service.LoginUser("fail@example.com", "password");

        // Assert
        Assert.False(result);
        Assert.False(File.Exists(".credentials.txt"));

        // Verify
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
        await File.WriteAllTextAsync(".credentials.txt", JsonSerializer.Serialize(authToken));

        var service = new DCUAuthService(_httpClient);

        // Act
        var result = await service.GetBearerToken();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(authToken.AccessToken, result!.AccessToken);
        Assert.Equal(authToken.RefreshToken, result.RefreshToken);

        // No HTTP call to verify

        // Clean up
        File.Delete(".credentials.txt");
    }

    [Fact]
    public async Task GetBearerToken_ShouldReturnNull_WhenNoCredentialsFile()
    {
        // Arrange
        if (File.Exists(".credentials.txt"))
            File.Delete(".credentials.txt");

        var service = new DCUAuthService(_httpClient);

        // Act
        var result = await service.GetBearerToken();

        // Assert
        Assert.Null(result);

        // No HTTP call to verify
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

        var service = new DCUAuthService(_httpClient);

        // Write old token to file
        await File.WriteAllTextAsync(".credentials.txt", JsonSerializer.Serialize(oldToken));

        // Act
        var result = await service.RefreshToken(oldToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(newToken.AccessToken, result!.AccessToken);
        Assert.Equal(newToken.RefreshToken, result.RefreshToken);

        // File should be updated
        var fileContent = await File.ReadAllTextAsync(".credentials.txt");
        var tokenFromFile = JsonSerializer.Deserialize<AuthToken>(fileContent);
        Assert.Equal(newToken.AccessToken, tokenFromFile!.AccessToken);

        // Verify
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

        // Clean up
        File.Delete(".credentials.txt");
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

        var service = new DCUAuthService(_httpClient);

        // Write old token to file
        await File.WriteAllTextAsync(".credentials.txt", JsonSerializer.Serialize(oldToken));

        // Act
        var result = await service.RefreshToken(oldToken);

        // Assert
        Assert.Null(result);

        // File should remain unchanged
        var fileContent = await File.ReadAllTextAsync(".credentials.txt");
        var tokenFromFile = JsonSerializer.Deserialize<AuthToken>(fileContent);
        Assert.Equal(oldToken.AccessToken, tokenFromFile!.AccessToken);

        // Verify
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

        // Clean up
        File.Delete(".credentials.txt");
    }
}