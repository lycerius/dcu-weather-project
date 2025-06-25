using WeatherService.Services;
using WeatherService.Services.WeatherProviders.OpenWeather;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();

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
    app.MapOpenApi();
}


app.MapControllers();
app.Run();