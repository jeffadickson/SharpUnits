using System.Text.Json;
using SharpUnits;
using Xunit;

namespace SharpUnits.Tests;

/// <summary>Tests for NativeAOT-safe System.Text.Json converters (structured, compact string, SoA series, and strict validation).</summary>
public sealed class JsonSerializationTests
{
    private sealed record TestBenchConfig(
        Quantity SupplyVoltage,
        Quantity<decimal> ExactOffset,
        QuantityPoint<double> ChamberTemp,
        QuantityPoint<double> LinePressure,
        SymbolicUnit NoiseUnit,
        Frequency SampleRate,
        Duration Timeout,
        QuantitySeries<double> CalibrationTrace);

    [Fact]
    public void StructuredJsonRoundTripsFullTestBenchDto()
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
            .AddSharpUnits();

        var original = new TestBenchConfig(
            SupplyVoltage: Quantity.Of(250, Units.Millivolt),
            ExactOffset: Quantity<decimal>.Of(0.3m, Units.Millivolt),
            ChamberTemp: QuantityPoint<double>.FromCelsius(25.5),
            LinePressure: QuantityPoint<double>.FromGaugePressure(2.0, Units.Bar),
            NoiseUnit: UnitRegistry.Standard.Resolve("nV") / Units.Hertz.Root(2),
            SampleRate: Frequency.FromKilohertz(100),
            Timeout: Duration.FromMilliseconds(250),
            CalibrationTrace: QuantitySeries<double>.FromValues([-50d, 0d, 125d, 250d], Units.Millivolt));

        string json = JsonSerializer.Serialize(original, options);
        Assert.Contains("\"unit\":\"mV\"", json);
        Assert.Contains("\"origin\":\"origin.standard_atmosphere\"", json);
        Assert.Contains("\"values\":[-50,0,125,250]", json);

        var restored = JsonSerializer.Deserialize<TestBenchConfig>(json, options)!;
        Assert.Equal(250, restored.SupplyVoltage.Value, 12);
        Assert.Equal("mV", restored.SupplyVoltage.UnitSymbol);
        Assert.Equal(0.3m, restored.ExactOffset.Value);
        Assert.Equal(25.5, restored.ChamberTemp.Value, 9);
        Assert.Equal(PointOrigins.IcePoint, restored.ChamberTemp.Origin);
        Assert.Equal(2.0, restored.LinePressure.Value, 9);
        Assert.Equal(PointOrigins.StandardAtmosphere, restored.LinePressure.Origin);
        Assert.Equal("nV/Hz^(1/2)", restored.NoiseUnit.Symbol);
        Assert.Equal(100_000, restored.SampleRate.Hertz, 9);
        Assert.Equal(0.25, restored.Timeout.Seconds, 12);
        Assert.Equal([-50d, 0d, 125d, 250d], restored.CalibrationTrace.Values.ToArray());
        Assert.Equal("mV", restored.CalibrationTrace.Unit.Symbol);
    }

    [Fact]
    public void CompactStringFormatAndCrossFormatDeserializationWorkSeamlessly()
    {
        var compactOpts = new JsonSerializerOptions()
            .AddSharpUnits(new SharpUnitsJsonOptions { Format = SharpUnitsJsonFormat.CompactString, AsciiSymbols = true });

        var q = Quantity.Of(4.7, UnitRegistry.Standard.Resolve("µF"));
        string json = JsonSerializer.Serialize(q, compactOpts);
        Assert.Equal("\"4.7 uF\"", json);

        // Structured options can also read compact strings (great for hand-edited JSON configs)
        var structuredOpts = new JsonSerializerOptions().AddSharpUnits();
        var restoredQ = JsonSerializer.Deserialize<Quantity>(json, structuredOpts);
        Assert.Equal(4.7e-6, restoredQ.SiValue, 12);

        var freq = JsonSerializer.Deserialize<Frequency>("\"2.5 MHz\"", structuredOpts);
        Assert.Equal(2.5, freq.Megahertz, 12);

        var tempPt = JsonSerializer.Deserialize<QuantityPoint<double>>("\"25 °C\"", structuredOpts);
        Assert.Equal(25.0, tempPt.Value, 9);
        Assert.Equal(PointOrigins.IcePoint, tempPt.Origin);
    }

    [Fact]
    public void StrictModeRejectsUnknownPropertiesAndIncompatibleUnits()
    {
        var opts = new JsonSerializerOptions().AddSharpUnits();

        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<Quantity>("{\"value\":10,\"unit\":\"V\",\"bogus\":123}", opts));

        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<Frequency>("\"10 V\"", opts));

        Assert.Throws<JsonException>(() =>
            JsonSerializer.Serialize(Quantity.Of(double.NaN, Units.Volt), opts));
    }
}
