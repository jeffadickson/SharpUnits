using System.Text.Json;
using SharpUnits;
using SharpUnits.Electrical;
using Xunit;

namespace SharpUnits.Electrical.Tests;

/// <summary>Tests for NativeAOT-safe System.Text.Json converters in SharpUnits.Electrical.</summary>
public sealed class ElectricalJsonTests
{
    private sealed record RfChannelSpec(
        Voltage Bias,
        Current Limit,
        Resistance Load,
        Capacitance Coupling,
        PowerLevelDbm TxPower,
        VoltageLevelDbv NoiseFloor,
        Decibels Gain);

    [Fact]
    public void RoundTripsElectricalAndRfTypesInStructuredAndCompactModes()
    {
        var opts = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
            .AddSharpUnits()
            .AddSharpUnitsElectrical();

        var spec = new RfChannelSpec(
            Bias: 3.3.Volts(),
            Limit: 250.0.Milliamperes(),
            Load: 50.0.Ohms(),
            Coupling: 4.7.Microfarads(),
            TxPower: 0.0.Dbm(),
            NoiseFloor: (-80.0).Dbv(),
            Gain: 12.5.Decibels());

        string json = JsonSerializer.Serialize(spec, opts);
        var restored = JsonSerializer.Deserialize<RfChannelSpec>(json, opts)!;

        Assert.Equal(3.3, restored.Bias.Volts, 12);
        Assert.Equal(0.25, restored.Limit.Amperes, 12);
        Assert.Equal(50.0, restored.Load.Ohms, 12);
        Assert.Equal(4.7e-6, restored.Coupling.Farads, 12);
        Assert.Equal(0.0, restored.TxPower.Value, 12);
        Assert.Equal(-80.0, restored.NoiseFloor.Value, 12);
        Assert.Equal(12.5, restored.Gain.Value, 12);

        // Compact human-authored string deserialization
        Assert.Equal(250.0, JsonSerializer.Deserialize<Current>("\"250 mA\"", opts).Milliamperes, 9);
        Assert.Equal(4.7, JsonSerializer.Deserialize<Capacitance>("\"4.7 uF\"", opts).Microfarads, 9);
        Assert.Equal(-20.0, JsonSerializer.Deserialize<VoltageLevelDbv>("\"-20 dBV\"", opts).Value, 12);
        Assert.Equal(10.0, JsonSerializer.Deserialize<PowerLevelDbm>("\"10 dBm\"", opts).Value, 12);
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<PowerLevelDbm>("\"10 dBV\"", opts));
    }
}
