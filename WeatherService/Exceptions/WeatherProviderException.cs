namespace WeatherService.Exceptions;

public class WeatherProviderException : Exception
{
    public WeatherProviderException(string message, Exception innerException) : base(message, innerException)
    { }
}