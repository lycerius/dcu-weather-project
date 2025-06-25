using System.Text.Json;
using CommandLine;
using Lycerius.DCUWeather.Common;
using Lycerius.DCUWeather.Common.Services;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Lycerius.DCUWeather.CommandLine;

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
        [Option(Required = true, HelpText = "The zipcode to fetch weather from")]
        public required string ZipCode { get; set; }
        [Option(Default = OutputFormat.TEXT)]
        public required OutputFormat Output { get; set; }
        [Option(Required = true, HelpText = "The temperature units the weather should be in")]
        public required TemperatureUnit Units { get; set; }
    }

    [Verb("get-current-weather", HelpText = "Gets the current weather")]
    public class GetCurrentWeatherOptions : BaseOptions
    { }

    [Verb("get-average-weather", HelpText = "Gets the average weather")]
    public class GetAverageWeatherOptions : BaseOptions
    {
        [Option(Required = true, HelpText = "Number of days to calculate the average over (must be a value from 2-5)")]
        public required int TimePeriod { get; set; }
    }

    public static async Task Main(string[] args)
    {
        await Parser.Default.ParseArguments<GetCurrentWeatherOptions, GetAverageWeatherOptions>(args)
        .WithParsedAsync<GetCurrentWeatherOptions>(GetCurrentWeather).Result
        .WithParsedAsync<GetAverageWeatherOptions>(GetAverageWeather);
    }

    private static async Task GetCurrentWeather(GetCurrentWeatherOptions options)
    {
        var dcuWeatherService = GenerateService(options);
        var results = await dcuWeatherService.GetCurrentWeatherForZipCode(options.ZipCode, options.Units);
        PrintResultsInSpecifiedOutput(results, options);
    }

    private static async Task GetAverageWeather(GetAverageWeatherOptions options)
    {
        var dcuWeatherService = GenerateService(options);
        var results = await dcuWeatherService.GetAverageWeather(options.ZipCode, options.Units, options.TimePeriod);
        PrintResultsInSpecifiedOutput(results, options);
    }

    private static DCUWeatherService GenerateService(BaseOptions options)
    {
        return new DCUWeatherService(GenerateConfig(options));
    }

    private static DCUWeatherServiceConfig GenerateConfig(BaseOptions options)
    {
        return new DCUWeatherServiceConfig
        {
            BaseUrl = $"{options.Protocol}://{options.Host}:{options.Port}"
        };
    }

    private static void PrintResultsInSpecifiedOutput(object? toPrint, BaseOptions options)
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
}
