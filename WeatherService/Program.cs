using System.Text.Json.Serialization;
using WeatherService.Services;
using WeatherService.Services.WeatherProviders.OpenWeather;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMemoryCache();

//HttpClients
builder.Services.AddHttpClient("openWeatherClient", client =>
{
    client.BaseAddress = new Uri("https://api.openweathermap.org/");
});

//Services
builder.Services.AddScoped<IWeatherProvider, OpenWeatherWeatherProvider>();
builder.Services.AddScoped<TemperatureUnitsConverter>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.MapControllers();
app.Run();