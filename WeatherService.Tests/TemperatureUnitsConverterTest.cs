using System;
using Common.Models;
using Moq;
using WeatherService.Services;
using Xunit;

namespace WeatherService.Tests;

public class TemperatureUnitsConverterTest
{
    [Theory]
    [InlineData(273.15, TemperatureUnit.C, 0.0)]
    [InlineData(300.15, TemperatureUnit.C, 27.0)]
    [InlineData(273.15, TemperatureUnit.F, 32.0)]
    [InlineData(310.15, TemperatureUnit.F, 98.6)]
    public void ConvertKelvinToUnits_ShouldConvertCorrectly(double kelvin, TemperatureUnit unit, double expected)
    {
        // Arrange
        var converter = new TemperatureUnitsConverter();

        // Act
        var result = converter.ConvertKelvinToUnits(kelvin, unit);

        // Assert
        Assert.Equal(expected, result, 1);
    }

    [Fact]
    public void ConvertKelvinToUnits_ShouldThrowException_OnUnknownUnit()
    {
        // Arrange
        var converter = new TemperatureUnitsConverter();

        // Act & Assert
        var ex = Assert.Throws<NotSupportedException>(() => converter.ConvertKelvinToUnits(273.15, (TemperatureUnit)999));
        Assert.Contains("Unknown output units", ex.Message);
    }
}