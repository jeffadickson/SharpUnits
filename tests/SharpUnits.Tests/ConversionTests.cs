using SharpUnits;
using Xunit;

namespace SharpUnits.Tests;

/// <summary>Core unit conversion and dimensional mismatch tests.</summary>
public sealed class ConversionTests
{
    [Theory]
    [InlineData(0, 273.15)]
    [InlineData(100, 373.15)]
    public void CelsiusToKelvin(double c, double k) => Assert.Equal(k, UnitConversion.Celsius.ToSi(c), 9);

    [Theory]
    [InlineData(32, 273.15)]
    [InlineData(212, 373.15)]
    public void FahrenheitToKelvin(double f, double k) => Assert.Equal(k, UnitConversion.Fahrenheit.ToSi(f), 9);

    [Fact] public void RoundTrip() => Assert.Equal(77, UnitConversion.Fahrenheit.FromSi(UnitConversion.Fahrenheit.ToSi(77)), 9);
    [Fact] public void DimensionMismatch() => Assert.Throws<InvalidOperationException>(() => SiQuantities.Volts(1) + SiQuantities.Ohms(1));
    [Fact] public void RejectZeroScale() => Assert.Throws<ArgumentOutOfRangeException>(() => new UnitConversion(0, 0));

    [Fact]
    public void UnitDefinitionRejectsMismatch()
    {
        var kelvin = new UnitDefinition("K", new Dimension(0, 0, 0, 0, 1, 0, 0), UnitConversion.Linear(1));
        Assert.Throws<InvalidOperationException>(() => kelvin.FromQuantity(SiQuantities.Volts(5)));
    }
}