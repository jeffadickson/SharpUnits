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

    [Theory]
    [InlineData(1, "KiB", "B", 1024.0, 9)]
    [InlineData(1, "MiB", "KiB", 1024.0, 9)]
    [InlineData(1, "GiB", "MiB", 1024.0, 9)]
    [InlineData(1, "TiB", "GiB", 1024.0, 9)]
    [InlineData(1, "B", "bit", 8.0, 9)]
    [InlineData(1, "kB", "B", 1000.0, 9)]
    [InlineData(100, "Mbps", "MBps", 12.5, 9)]
    [InlineData(1, "Mibps", "Kibps", 1024.0, 9)]
    [InlineData(60, "rpm", "Hz", 1.0, 9)]
    [InlineData(1, "g0", "Gal", 980.665, 6)]
    [InlineData(1, "gn", "g0", 1.0, 9)]
    [InlineData(1, "nmi", "m", 1852.0, 9)]
    [InlineData(1, "kn", "kph", 1.852, 9)]
    [InlineData(1, "mph", "kph", 1.609344, 9)]
    [InlineData(1, "ha", "are", 100.0, 9)]
    [InlineData(1, "gal", "qt", 4.0, 9)]
    [InlineData(1, "gal", "floz", 128.0, 9)]
    [InlineData(1, "cc", "mL", 1.0, 9)]
    [InlineData(1, "bbl", "gal", 42.0, 9)]
    [InlineData(1, "slug", "kg", 14.59390293720636, 9)]
    [InlineData(1, "ton_us", "lb", 2000.0, 9)]
    [InlineData(1, "ton_uk", "lb", 2240.0, 9)]
    [InlineData(1, "kgf", "N", 9.80665, 9)]
    [InlineData(1, "kip", "lbf", 1000.0, 9)]
    [InlineData(1, "ksi", "psi", 1000.0, 9)]
    [InlineData(1, "inHg", "Pa", 3386.388666666667, 6)]
    [InlineData(1, "inH2O", "Pa", 249.08891, 6)]
    [InlineData(60, "arcmin", "°", 1.0, 9)]
    [InlineData(3600, "arcsec", "°", 1.0, 9)]
    [InlineData(100, "grad", "°", 90.0, 9)]
    [InlineData(1, "cal", "J", 4.184, 9)]
    [InlineData(1, "kcal", "Cal", 1.0, 9)]
    [InlineData(1, "BTU", "J", 1055.05585262, 6)]
    [InlineData(10000, "G", "T", 1.0, 9)]
    [InlineData(1000, "mG", "G", 1.0, 9)]
    [InlineData(1, "P", "cP", 100.0, 9)]
    [InlineData(1, "St", "cSt", 100.0, 9)]
    [InlineData(1, "M", "mM", 1000.0, 9)]
    [InlineData(100, "rem", "Sv", 1.0, 9)]
    [InlineData(1, "Ci", "GBq", 37.0, 9)]
    [InlineData(1, "thou", "mil", 1.0, 9)]
    [InlineData(1, "micron", "µm", 1.0, 9)]
    [InlineData(1000, "ppt", "ppb", 1.0, 9)]
    [InlineData(10, "‰", "%", 1.0, 9)]
    public void ExpandedEngineeringCatalogAndIecBinaryPrefixesConvertAccurately(
        double value, string from, string to, double expected, int precision)
    {
        double actual = UnitRegistry.Standard.Convert(value, from, to);
        Assert.Equal(expected, actual, precision);
    }

    [Fact]
    public void Iso80000DistinctKindsPreventCrossDomainMixing()
    {
        // Radionuclide activity (Bq) vs periodic frequency (Hz)
        Assert.Throws<InvalidOperationException>(() =>
            Quantity.Of(10, Units.Becquerel) + Quantity.Of(10, Units.Hertz));

        // Absorbed dose (Gy) vs dose equivalent (Sv / rem)
        Assert.Throws<InvalidOperationException>(() =>
            Quantity.Of(1, Units.Gray) + Quantity.Of(1, Units.Sievert));

        // Luminous flux (lm) vs luminous intensity (cd)
        Assert.Throws<InvalidOperationException>(() =>
            Quantity.Of(100, Units.Lumen) + Quantity.Of(100, Units.Candela));

        // Information (bit / B) vs plane angle (rad)
        Assert.Throws<InvalidOperationException>(() =>
            Quantity.Of(8, Units.Bit) + Quantity.Of(1, Units.Radian));

        // Information rate (bps) vs periodic frequency (Hz)
        Assert.Throws<InvalidOperationException>(() =>
            Quantity.Of(100, Units.BitsPerSecond) + Quantity.Of(100, Units.Hertz));

        // Binary prefixes rejected on non-information units (e.g. Kim, KiV)
        Assert.Throws<KeyNotFoundException>(() => UnitRegistry.Standard.Resolve("Kim"));
        Assert.Throws<KeyNotFoundException>(() => UnitRegistry.Standard.Resolve("KiV"));
    }
}
