using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using WeatherService.Controllers.Validators;
using WeatherService.Models;
using WeatherService.Services;
using WeatherService.Services.WeatherProviders;
using WeatherService.Services.WeatherProviders.OpenWeather;

var builder = WebApplication.CreateBuilder(args);
//Logging
builder.Logging.AddConsole();

//Auth Config
builder.Services.AddAuthorization();
builder.Services.AddIdentityApiEndpoints<IdentityUser>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

//Swagger config
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{

    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Weather Service API",
        Version = "v1",
        Description = "API for fetching weather data"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            []
        }
    });
}
);

//Database
//Auth Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseInMemoryDatabase("AppDb")
);

builder.Services.AddMemoryCache();

//HttpClients
builder.Services.AddHttpClient("openWeatherClient", client =>
{
    client.BaseAddress = new Uri("https://api.openweathermap.org/");
});

//Services
builder.Services.AddScoped<IWeatherProvider, OpenWeatherWeatherProvider>();
builder.Services.AddScoped<TemperatureUnitsConverter>();

// Validators
builder.Services.AddScoped<IValidator<GetCurrentWeatherQuery>, GetCurrentWeatherQueryValidator>();
builder.Services.AddScoped<IValidator<GetAverageWeatherQuery>, GetAverageWeatherQueryValidator>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapIdentityApi<IdentityUser>();
app.Run();