using SharpUnits;
using Xunit;

namespace SharpUnits.Tests;

/// <summary>Tests for the standard unit catalog, dynamic SI prefixes, aliases, and thread-safe expression parsing.</summary>
public sealed class UnitCatalogAndPrefixTests
{
    [Theory]
    [InlineData(1, "in", "mm", 25.4, 9)]
    [InlineData(1, "ft", "m", 0.3048, 9)]
    [InlineData(1, "mi", "km", 1.609344, 9)]
    [InlineData(1000, "mil", "in", 1.0, 9)]
    [InlineData(1, "lb", "g", 453.59237, 6)]
    [InlineData(16, "oz", "lb", 1.0, 9)]
    [InlineData(1, "kg", "g", 1000.0, 9)]
    [InlineData(2500, "mg", "g", 2.5, 9)]
    [InlineData(1, "h", "min", 60.0, 9)]
    [InlineData(1, "ms", "us", 1000.0, 9)]
    [InlineData(1, "MHz", "kHz", 1000.0, 9)]
    [InlineData(1, "atm", "Pa", 101325.0, 6)]
    [InlineData(1, "bar", "kPa", 100.0, 9)]
    [InlineData(14.6959487755, "psi", "atm", 1.0, 6)]
    [InlineData(760, "Torr", "atm", 1.0, 9)]
    [InlineData(180, "deg", "rad", Math.PI, 9)]
    [InlineData(1, "rev", "°", 360.0, 9)]
    [InlineData(1, "kWh", "MJ", 3.6, 9)]
    [InlineData(50, "%", "1", 0.5, 9)]
    [InlineData(500, "ppm", "%", 0.05, 9)]
    [InlineData(100, "°C", "°F", 212.0, 9)]
    [InlineData(0, "°C", "K", 273.15, 9)]
    [InlineData(491.67, "°R", "K", 273.15, 6)]
    [InlineData(4.7, "uF", "nF", 4700.0, 9)]
    [InlineData(10, "kOhm", "Ω", 10000.0, 9)]
    [InlineData(25, "degC", "K", 298.15, 9)]
    public void StandardRegistryConvertsCommonUnits(double value, string from, string to, double expected, int precision)
    {
        double actual = UnitRegistry.Standard.Convert(value, from, to);
        Assert.Equal(expected, actual, precision);
    }

    [Fact]
    public void DynamicPrefixesComposeWithPrefixableBaseUnits()
    {
        var reg = UnitRegistry.Standard;
        Assert.Equal(1.0, reg.Resolve("pF").Converter.Scale * 1e12, 12);
        Assert.Equal(1.0, reg.Resolve("uH").Converter.Scale * 1e6, 12);
        Assert.Equal(1.0, reg.Resolve("µA").Converter.Scale * 1e6, 12);
        Assert.Equal(1e9, reg.Resolve("GHz").Converter.Scale, 6);
        Assert.Equal(1e6, reg.Resolve("MΩ").Converter.Scale, 6);
        Assert.Equal(10, reg.Resolve("daL").Converter.Scale * 1000, 9); // 1 daL = 10 L = 0.01 m^3
    }

    [Theory]
    [InlineData("m°C")]
    [InlineData("k°F")]
    [InlineData("mkg")]
    [InlineData("kin")]
    public void NonPrefixableUnitsRejectPrefixes(string symbol)
    {
        Assert.Throws<KeyNotFoundException>(() => UnitRegistry.Standard.Resolve(symbol));
    }

    [Fact]
    public void ExpressionParserSupportsSuperscriptsAndRationalExponents()
    {
        var parser = new UnitExpressionParser(UnitRegistry.Standard);

        var accel = parser.Parse("m·s⁻²");
        Assert.Equal(RationalDimension.Acceleration, accel.Dimension);
        Assert.Equal(1d, accel.Scale, 12);

        var area = parser.Parse("cm²");
        Assert.Equal(RationalDimension.Area, area.Dimension);
        Assert.Equal(1.0, area.Scale * 1e4, 12);

        // Voltage noise spectral density: nV / sqrt(Hz)
        var noiseDensity = parser.Parse("nV/Hz^(1/2)");
        Assert.Equal(RationalDimension.Voltage - RationalDimension.Frequency.Root(2), noiseDensity.Dimension);
        Assert.Equal(1.0, noiseDensity.Scale * 1e9, 12);
    }

    [Fact]
    public void ExpressionParserIsThreadSafeUnderConcurrentCalls()
    {
        var parser = new UnitExpressionParser(UnitRegistry.Standard);
        string[] inputs = ["mA*kΩ", "m/s^2", "V/Hz^(1/2)", "cm²", "kW*h", "N/m²"];
        Parallel.For(0, 200, i =>
        {
            string expr = inputs[i % inputs.Length];
            var parsed = parser.Parse(expr);
            Assert.True(double.IsFinite(parsed.Scale));
        });
    }
}
