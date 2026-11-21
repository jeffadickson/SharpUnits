using System.Globalization;
using SharpUnits;
using Xunit;

namespace SharpUnits.Tests;

/// <summary>Tests for idiomatic C# IFormattable/IParsable, fluent extensions, and deterministic scripting host interop (StarLark-cs / Teal).</summary>
public sealed class CSharpStyleAndScriptingHostTests
{
    [Fact]
    public void IFormattableAndIParsableWorkWithCSharpInterpolation()
    {
        var v = Quantity.Of(0.0025, Units.Volt);
        Assert.Equal("2.5 mV", FormattableString.Invariant($"{v:eng}"));

        var r = Quantity.Of(1250, Units.Ohm);
        Assert.Equal("1.25 kOhm", FormattableString.Invariant($"{r:eng:ascii}"));

        Assert.True(Quantity.TryParse("12.5 kΩ", CultureInfo.InvariantCulture, out var parsed));
        Assert.Equal(12500d, parsed.SiValue, 9);
        Assert.False(Quantity.TryParse("not a unit", CultureInfo.InvariantCulture, out _));
    }

    [Fact]
    public void FluentCSharpExtensionsComposeCleanly()
    {
        var d = 0.1m.WithUnit(Units.Millivolt) + 0.2m.WithUnit(Units.Millivolt);
        Assert.Equal(0.3m, d.Value);

        var f = 2.0.Kilohertz();
        Assert.Equal(0.5, f.Period.Milliseconds, 12);

        var t = 100.0.DegreesCelsius() - 32.0.DegreesFahrenheit();
        Assert.Equal(100.0, t.In(Units.Kelvin).Value, 9);

        var absBar = 2.0.BarGauge().RelativeTo(PointOrigins.Vacuum);
        Assert.Equal(3.01325, absBar.Value, 9);
    }

    [Fact]
    public void UnitEnvironmentAndScriptOpsSupportDeterministicScriptingHosts()
    {
        var env = UnitEnvironment.Standard;

        // Simulate StarLark-cs / Teal expressions:
        // i = 250 * unit("mA"); r = 4 * unit("kOhm"); v = (i * r).in("kV")
        var mA = env.ResolveUnit("mA");
        var kOhm = env.ResolveUnit("kOhm");
        var current = UnitScriptOps.Multiply(250.0, mA);
        var resistance = UnitScriptOps.Multiply(4.0, kOhm);
        var voltage = (Quantity<double>)UnitScriptOps.Multiply(current, resistance);
        var inKv = env.Convert(voltage, "kV");
        Assert.Equal(1.0, inKv.Value, 12);
        Assert.Equal("kV", inKv.Unit.Symbol);

        // Affine points in script host: point - point => displacement; point + point rejected
        var p1 = env.CreatePoint(25.0, "°C");
        var p2 = env.CreatePoint(212.0, "degF");
        var span = (Quantity<double>)UnitScriptOps.Subtract(p2, p1);
        Assert.Equal(75.0, span.In(Units.Kelvin).Value, 9);
        Assert.Throws<InvalidOperationException>(() => UnitScriptOps.Add(p1, p2));

        // Gauge pressure to vacuum absolute
        var gauge = env.CreatePoint(2.0, "bar", "gauge");
        var absolute = env.ConvertPoint(gauge, "bar", "vacuum");
        Assert.Equal(3.01325, absolute.Value, 9);
    }
}
