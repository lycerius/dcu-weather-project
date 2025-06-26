using System.Text.Json;
using CommandLine;
using Common.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
        [Option(Default = LogLevel.Error, HelpText = "The minimum log level to use (Trace|Debug|Information|Warning|Error|Critical)")]
        public required LogLevel LogLevel { get; set; }
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
        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddSingleton<ICredentialStorage, FileCredentialStorage>();
        builder.Services.AddSingleton<IWeatherAuthService, WeatherAuthService>();
        builder.Services.AddSingleton<IWeatherService, WeatherService>();

        await Parser.Default.ParseArguments<RegisterUserOptions, LoginUserOptions, GetCurrentWeatherOptions, GetAverageWeatherOptions>(args)
            .WithParsed<BaseOptions>(opts => BaseOptionsSetup(opts, builder))
            .WithParsedAsync<RegisterUserOptions>(async opts =>
            {
                await RegisterUser(opts, builder);
            }).Result
            .WithParsedAsync<LoginUserOptions>(async opts =>
            {
                await LoginUser(opts, builder);
            }).Result
            .WithParsedAsync<GetCurrentWeatherOptions>(async opts =>
            {
                await GetCurrentWeather(opts, builder);
            }).Result
            .WithParsedAsync<GetAverageWeatherOptions>(async opts =>
            {
                await GetAverageWeather(opts, builder);
            });
    }

    private static void BaseOptionsSetup(BaseOptions options, HostApplicationBuilder builder)
    {
        builder.Services.AddHttpClient("weatherClient", client =>
        {
            client.BaseAddress = new Uri($"{options.Protocol}://{options.Host}:{options.Port}");
        });

        builder.Services.AddLogging(opt =>
        {
            opt.SetMinimumLevel(options.LogLevel);
        });
    }

    private static async Task RegisterUser(RegisterUserOptions options, HostApplicationBuilder builder)
    {
        var host = builder.Build();
        var authService = host.Services.GetRequiredService<IWeatherAuthService>();
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

    private static async Task LoginUser(LoginUserOptions options, HostApplicationBuilder builder)
    {
        var host = builder.Build();
        var authService = host.Services.GetRequiredService<IWeatherAuthService>();
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

    private static async Task GetCurrentWeather(GetCurrentWeatherOptions options, HostApplicationBuilder builder)
    {
        var host = builder.Build();
        var dcuWeatherService = host.Services.GetRequiredService<IWeatherService>();
        var results = await dcuWeatherService.GetCurrentWeatherForZipCode(options.ZipCode, options.TemperatureUnit);
        PrintResultsInSpecifiedOutput(results, options);
    }

    private static async Task GetAverageWeather(GetAverageWeatherOptions options, HostApplicationBuilder builder)
    {
        var host = builder.Build();
        var dcuWeatherService = host.Services.GetRequiredService<IWeatherService>();
        var results = await dcuWeatherService.GetAverageWeather(options.ZipCode, options.TemperatureUnit, options.TimePeriod);
        PrintResultsInSpecifiedOutput(results, options);
    }

    private static void PrintResultsInSpecifiedOutput(object? toPrint, BaseWeatherOptions options)
    {
        switch (options.Output)
        {
            case OutputFormat.JSON:
                Console.WriteLine(JsonSerializer.Serialize(toPrint, new JsonSerializerOptions { WriteIndented = true }));
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
