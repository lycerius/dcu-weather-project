using System.Net.Http.Headers;
using System.Text.Json;
using CommandLine;
using Common.Models;
using WeatherCli.Services.CredentialStorage;
using WeatherCli.Services.WeatherAuthService;
using WeatherCli.Services.WeatherService;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace WeatherCli;

public class Program
{
    public enum OutputFormat
    {
        JSON, YAML, TEXT
    }
    public abstract class BaseOptions
    {
        [Option(Default = "localhost", HelpText = "The hostname providing the weather api")]
        public required string Host { get; set; }
        [Option(Default = "http", HelpText = "The http protocol to use (http|https)")]
        public required string Protocol { get; set; }
        [Option(Default = 5000, HelpText = "The port to use when calling the weather api")]
        public required int Port { get; set; }
    }

    public abstract class BaseWeatherOptions : BaseOptions
    {
        [Option(Required = true, HelpText = "The zipcode to fetch weather from")]
        public required string ZipCode { get; set; }
        [Option(Default = OutputFormat.TEXT, HelpText = "Specifies the output format (text|json|yaml)")]
        public required OutputFormat Output { get; set; }
        [Option(Required = true, HelpText = "The temperature units the weather should be in (fahrenheit|celsius)")]
        public required string Units { get; set; }

        public TemperatureUnit TemperatureUnit
        {
            get => ConvertUnitInputOptionToTemperatureUnit(Units);
        }
    }

    [Verb("register-user", HelpText = "Registers a new user")]
    public class RegisterUserOptions : BaseOptions
    {
        [Option(Required = true, HelpText = "The email of the user to register")]
        public required string Email { get; set; }
        [Option(Required = true, HelpText = "The password of the user to register")]
        public required string Password { get; set; }
    }

    [Verb("login-user", HelpText = "Logs in a user and saves the JWT token to a file")]
    public class LoginUserOptions : BaseOptions
    {
        [Option(Required = true, HelpText = "The email of the user to log in")]
        public required string Email { get; set; }
        [Option(Required = true, HelpText = "The password of the user to log in")]
        public required string Password { get; set; }
    }

    [Verb("get-current-weather", HelpText = "Gets the current weather")]
    public class GetCurrentWeatherOptions : BaseWeatherOptions
    { }

    [Verb("get-average-weather", HelpText = "Gets the average weather")]
    public class GetAverageWeatherOptions : BaseWeatherOptions
    {
        [Option(Required = true, HelpText = "Number of days to calculate the average over (must be a value from 2-5)")]
        public required int TimePeriod { get; set; }
    }

    public static async Task Main(string[] args)
    {
        await Parser.Default.ParseArguments<RegisterUserOptions, LoginUserOptions, GetCurrentWeatherOptions, GetAverageWeatherOptions>(args)
        .WithParsedAsync<RegisterUserOptions>(RegisterUser).Result
        .WithParsedAsync<LoginUserOptions>(LoginUser).Result
        .WithParsedAsync<GetCurrentWeatherOptions>(GetCurrentWeather).Result
        .WithParsedAsync<GetAverageWeatherOptions>(GetAverageWeather);
    }

    private static async Task RegisterUser(RegisterUserOptions options)
    {
        var authService = GenerateAuthService(options);
        var success = await authService.RegisterUser(options.Email, options.Password);
        if (success)
        {
            Console.WriteLine("User registered successfully.");
        }
        else
        {
            Console.WriteLine("Failed to register user.");
        }
    }

    private static async Task LoginUser(LoginUserOptions options)
    {
        var authService = GenerateAuthService(options);
        var success = await authService.LoginUser(options.Email, options.Password);
        if (success)
        {
            Console.WriteLine("User logged in successfully.");
        }
        else
        {
            Console.WriteLine("Failed to log in user.");
        }
    }

    private static async Task GetCurrentWeather(GetCurrentWeatherOptions options)
    {
        var dcuWeatherService = await GenerateWeatherService(options);
        var results = await dcuWeatherService.GetCurrentWeatherForZipCode(options.ZipCode, options.TemperatureUnit);
        PrintResultsInSpecifiedOutput(results, options);
    }

    private static async Task GetAverageWeather(GetAverageWeatherOptions options)
    {
        var dcuWeatherService = await GenerateWeatherService(options);
        var results = await dcuWeatherService.GetAverageWeather(options.ZipCode, options.TemperatureUnit, options.TimePeriod);
        PrintResultsInSpecifiedOutput(results, options);
    }

    private static async Task<IWeatherService> GenerateWeatherService(BaseOptions options)
    {
        var authService = GenerateAuthService(options);
        var authToken = await authService.GetBearerToken();

        if (authToken == null)
        {
            Console.WriteLine("Failed to retrieve authentication token. Either credentials are invalid or you need to log in first.");
            Environment.Exit(-1);
        }

        authToken = await authService.RefreshToken(authToken);
        if (authToken == null)
        {
            Console.WriteLine("Failed to refresh authentication token. Please log in again.");
            Environment.Exit(-1);
        }


        var httpClient = new HttpClient
        {
            BaseAddress = new Uri($"{options.Protocol}://{options.Host}:{options.Port}")
        };
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken.AccessToken);

        return new WeatherService(httpClient);
    }

    private static WeatherAuthService GenerateAuthService(BaseOptions options)
    {
        return new WeatherAuthService(new HttpClient
        {
            BaseAddress = new Uri($"{options.Protocol}://{options.Host}:{options.Port}")
        }, new FileCredentialStorage());
    }

    private static void PrintResultsInSpecifiedOutput(object? toPrint, BaseWeatherOptions options)
    {
        switch (options.Output)
        {
            case OutputFormat.JSON:
                Console.WriteLine(JsonSerializer.Serialize(toPrint));
                break;
            case OutputFormat.YAML:
                Console.WriteLine(new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build().Serialize(toPrint));
                break;
            case OutputFormat.TEXT:
                Console.WriteLine(toPrint?.ToString());
                break;
        }
    }

    private static TemperatureUnit ConvertUnitInputOptionToTemperatureUnit(string inputOption)
    {
        return inputOption?.ToLower() switch
        {
            "fahrenheit" => TemperatureUnit.F,
            "celsius" => TemperatureUnit.C,
            _ => throw new NotSupportedException($"The given input temperature unit is not supported: {inputOption}")
        };
    }
}
