# DCU Weather Project

A .NET 9.0 solution for retrieving current and average weather data by ZIP code, using the OpenWeather API. The project consists of a Web API (`WeatherService`) and a command-line interface (`WeatherCli`).

---

## Table of Contents

- [Project Structure](#project-structure)
- [Prerequisites](#prerequisites)
- [Running the Web API Locally](#running-the-web-api-locally)
- [Running the Web API via Docker](#running-the-web-api-via-docker)
- [API Endpoints](#api-endpoints)
- [Authentication](#authentication)
  - [Registering and Logging In](#registering-and-logging-in)
  - [How Authentication Works](#how-authentication-works)
- [Using the CLI](#using-the-cli)
  - [CLI Usage Examples](#cli-usage-examples)
  - [Example CLI Error Messages](#example-cli-error-messages)
- [Configuration](#configuration)
- [Testing](#testing)
- [Notes](#notes)

---

## Project Structure

```
dcu-weather-project/
├── Common/                # Shared models and enums
├── WeatherService/        # ASP.NET Core Web API
├── WeatherCli/            # .NET CLI for weather queries
├── WeatherService.Tests/  # Unit tests for API and providers
├── WeatherCli.Tests/      # Unit tests for CLI and auth
├── Dockerfile             # Dockerfile for building the API container
```

---

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) (optional, for containerized runs)
- An [OpenWeather API key](https://openweathermap.org/api) (for real data)

---

## Running the Web API Locally

1. **Configure API Key**

   Edit `WeatherService/appsettings.json` and set your OpenWeather API key:

   ```json
   "DcuWeatherApp": {
     "OpenWeatherApiKey": "YOUR_OPENWEATHER_API_KEY"
   }
   ```

2. **Build and Run**

   ```sh
   cd WeatherService
   dotnet build
   dotnet run
   ```

   The API will be available at `http://localhost:5000` (or as specified in `launchSettings.json`).

---

## Running the Web API via Docker

A root-level `Dockerfile` is provided for building and running the API container.

1. **Build the Docker Image**

   ```sh
   docker build -t dcu-weather-service .
   ```

2. **Run the Container**

   ```sh
   docker run -e "DcuWeatherApp__OpenWeatherApiKey=YOUR_OPENWEATHER_API_KEY" -p 5000:5000 dcu-weather-service
   ```

   The API will be available at [http://localhost:5000](http://localhost:5000).

---

## API Endpoints

All endpoints require authentication (see [Authentication](#authentication)).

- **GET /v1/Weather/Current/{zipCode}?units={C|F}**
  - Returns current weather for the given ZIP code.
  - Returns HTTP 400 for invalid input or not found, HTTP 500 for general errors.

  **Example Response:**
  ```json
  {
    "currentTemperature": 60,
    "unit": "F",
    "lat": 45.67,
    "lon": 54.36,
    "rainPossibleToday": true
  }
  ```

- **GET /v1/Weather/Average/{zipCode}?units={C|F}&timePeriod={2-5}**
  - Returns average weather for the given ZIP code and period (2-5 days).
  - Returns HTTP 400 for invalid input, not found, or out-of-range timePeriod; HTTP 500 for general errors.

  **Example Response:**
  ```json
  {
    "averageTemperature": 60,
    "unit": "F",
    "lat": 45.67,
    "lon": 54.36,
    "rainPossibleInPeriod": true
  }
  ```

---

## Authentication

The DCU Weather API requires authentication for all weather endpoints. The CLI provides commands to register and log in, storing your credentials for future requests.

### Registering and Logging In

Before using weather commands, you must register and log in:

#### Register a New User

```sh
dotnet run --project WeatherCli -- register-user --email your@email.com --password yourpassword
```

#### Log In

```sh
dotnet run --project WeatherCli -- login-user --email your@email.com --password yourpassword
```

If login is successful, your authentication token will be saved locally for future CLI requests.

### How Authentication Works

- **Credential Storage:**  
  After a successful login, your authentication tokens are stored in a file named `.credentials.txt` in the `WeatherCli` directory.
- **File Format:**  
  The file contains a JSON object with your access and refresh tokens.
- **Usage:**  
  The CLI automatically reads this file and attaches your bearer token to all API requests.
- **Token Refresh:**  
  If your access token expires, the CLI will attempt to refresh it using the refresh token stored in `.credentials.txt`.

**Note:**  
Keep your `.credentials.txt` file secure. If you wish to log out, simply delete this file.

---

## Using the CLI

The CLI allows you to fetch weather data from the API and output it in text, JSON, or YAML.

### Build the CLI

```sh
cd WeatherCli
dotnet build
```

### CLI Usage

```sh
dotnet run --project WeatherCli -- [command] [options]
```

#### CLI Commands

- `register-user`  
  Register a new user account.
- `login-user`  
  Log in and store your authentication token.
- `get-current-weather`  
  Fetches current weather for a ZIP code (requires authentication).
- `get-average-weather`  
  Fetches average weather for a ZIP code over a period (2-5 days, requires authentication).

#### Common Options

- `--host` (default: `localhost`)  
  The API host.
- `--protocol` (default: `http`)  
  The protocol (`http` or `https`).
- `--port` (default: `5000`)  
  The API port.
- `--zipcode` (required for weather commands)  
  The ZIP code to query.
- `--units` (required for weather commands)  
  Temperature units: `celsius` or `fahrenheit`.
- `--output` (default: `text`)  
  Output format: `text`, `json`, or `yaml`.

#### `get-average-weather` Additional Option

- `--timeperiod` (required)  
  Number of days to average over (2-5).

---

### CLI Usage Examples

**Register and log in:**

```sh
dotnet run --project WeatherCli -- register-user --email alice@example.com --password secret
dotnet run --project WeatherCli -- login-user --email alice@example.com --password secret
```

**Get current weather in Celsius for ZIP 90210:**

```sh
dotnet run --project WeatherCli -- get-current-weather --zipcode 90210 --units celsius
```

**Get average weather in Fahrenheit for ZIP 90210 over 3 days, output as JSON:**

```sh
dotnet run --project WeatherCli -- get-average-weather --zipcode 90210 --units fahrenheit --timeperiod 3 --output json
```

**Specify a custom API host and port:**

```sh
dotnet run --project WeatherCli -- get-current-weather --zipcode 90210 --units celsius --host localhost --port 5000
```

---

### Example CLI Error Messages

**Invalid ZIP code:**
```
Zip code must be a 5-digit number.
```

**Invalid time period:**
```
Invalid time period specified. Please use a value between 2 and 5.
```

**Invalid units:**
```
Invalid temperature unit specified. Please use 'fahrenheit' or 'celsius'.
```

**Authentication required:**
```
User is not authenticated. Please log in first.
```

**API returns not found:**
```
No weather data found for the specified ZIP code.
```

**General error:**
```
Error fetching current weather: [error details]
```

---

## Configuration

- **API Key:**  
  Set in `WeatherService/appsettings.json` or via environment variable `DcuWeatherApp__OpenWeatherApiKey` (for Docker).

- **API Base Address:**  
  The CLI defaults to `http://localhost:5000`. Use `--host`, `--port`, and `--protocol` to override.

---

## Testing

To run all tests:

```sh
dotnet test
```

---

## Notes

- The CLI supports output in text, JSON, and YAML for easy integration.
- For development, you can use the included `launchSettings.json` or Docker for isolated runs.
- Authentication is required for weather queries; register and log in before using weather commands.
- `.credentials.txt` is not encrypted by default, but the code is structured for future secure storage.
- All endpoints return HTTP 400 for invalid input or not found, and HTTP 500 for general errors.

---
