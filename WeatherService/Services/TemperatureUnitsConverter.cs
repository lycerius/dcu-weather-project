using Common.Models;

namespace WeatherService.Services;

/// <summary>
/// Converts various temperature units
/// </summary>
public class TemperatureUnitsConverter
{
    /// <summary>
    /// Converts the given kelvin units to the specified output units
    /// </summary>
    /// <param name="kelvin">The temperature in Kelvin</param>
    /// <param name="outputUnits">The unit to convert the temperature to</param>
    /// <returns>The temperature in the specified units</returns>
    /// <exception cref="Exception">If the specified outputUnits are not supported</exception>
    public virtual double ConvertKelvinToUnits(double kelvin, TemperatureUnit outputUnits)
    {
        switch (outputUnits)
        {
            case TemperatureUnit.F:
                return (kelvin - 273.15) * 1.8 + 32;
            case TemperatureUnit.C:
                return kelvin - 273.15;
            default:
                throw new Exception($"Unknown output units provided {outputUnits}");
        }
    }
}