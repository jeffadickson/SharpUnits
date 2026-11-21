using SharpUnits;
using Xunit;

namespace SharpUnits.Tests;

/// <summary>Tests for engineering auto-scaling, plot axis unit selection, and core strongly-typed structs.</summary>
public sealed class UnitAutoScalerAndCoreQuantitiesTests
{
    [Theory]
    [InlineData(0.0025, "V", "2.5 mV")]
    [InlineData(15000, "Hz", "15 kHz")]
    [InlineData(4.7e-6, "F", "4.7 µF")]
    [InlineData(2.2e6, "Ω", "2.2 MΩ")]
    [InlineData(0.0, "V", "0 V")]
    [InlineData(-50e-9, "s", "-50 ns")]
    public void EngineeringFormatAutoScalesSingleValues(double siValue, string unitSymbol, string expected)
    {
        var q = Quantity.Of(siValue, unitSymbol);
        Assert.Equal(expected, q.FormatEngineering("G4"));
    }

    [Fact]
    public void AsciiModeProducesInstrumentSafeStrings()
    {
        var cap = Quantity.Of(4.7e-6, Units.Farad);
        Assert.Equal("4.7 uF", cap.FormatEngineering("G4", asciiOnly: true));

        var res = Quantity.Of(1250, Units.Ohm);
        Assert.Equal("1.25 kOhm", res.FormatEngineering("G4", asciiOnly: true));
    }

    [Fact]
    public void AxisAutoScalerSelectsSharedUnitAndConvertsSweepFast()
    {
        var axis = UnitAutoScaler.SelectForAxis(-0.05, 0.25, Units.Volt);
        Assert.Equal("mV", axis.Unit.Symbol);
        Assert.Equal(1000d, axis.ScaleFromSi, 9);

        double[] siSamples = [-0.05, 0.0, 0.125, 0.25];
        double[] display = new double[siSamples.Length];
        axis.ConvertFromSi(siSamples, display);

        Assert.Equal([-50d, 0d, 125d, 250d], display);
    }

    [Fact]
    public void SeriesAutoScalerHandlesKilogramsAndFrequencies()
    {
        double[] sweepHz = [100d, 1000d, 25_000d, 500_000d];
        var freqAxis = UnitAutoScaler.SelectForSeries(sweepHz, Units.Hertz);
        Assert.Equal("kHz", freqAxis.Unit.Symbol);
        Assert.Equal(500d, freqAxis.FromSi(500_000d), 9);

        var smallMass = UnitAutoScaler.SelectForSiMagnitude(0.005, Units.Kilogram);
        Assert.Equal("g", smallMass.Unit.Symbol);
        Assert.Equal(5d, smallMass.FromSi(0.005), 9);

        var normalMass = UnitAutoScaler.SelectForSiMagnitude(5.0, Units.Kilogram);
        Assert.Equal("kg", normalMass.Unit.Symbol);
    }

    [Fact]
    public void CoreStronglyTypedStructsRoundTripAndCompute()
    {
        var f = Frequency.FromKilohertz(2);
        Assert.Equal(2000d, f.Hertz, 12);
        Assert.Equal(0.5, f.Period.Milliseconds, 12);
        Assert.Equal(f, Frequency.FromQuantity(f.ToQuantity()));

        var p = Power.FromKilowatts(1.5);
        var e = p * Duration.FromHours(2);
        Assert.Equal(3.0, e.KilowattHours, 12);
        Assert.Equal(p, e / Duration.FromHours(2));

        var pressure = Pressure.FromBar(2);
        Assert.Equal(200d, pressure.Kilopascals, 9);
        Assert.Equal(pressure, Pressure.FromQuantity(pressure.ToQuantity()));

        var len = Length.FromInches(10) + Length.FromMillimeters(46);
        Assert.Equal(300d, len.Millimeters, 9);
    }
}
