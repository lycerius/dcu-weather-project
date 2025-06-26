# Use the official .NET 9.0 SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ./WeatherService ./WeatherService
COPY ./Common ./Common

WORKDIR /src/WeatherService
RUN dotnet restore

# Build the app
RUN dotnet publish -c Release -o /app/publish

# Use the official ASP.NET runtime image for the final container
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS="http://+:5000"

# Expose the port
EXPOSE 5000

# Entry point
ENTRYPOINT ["dotnet", "WeatherService.dll"]