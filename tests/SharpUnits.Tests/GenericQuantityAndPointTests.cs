using SharpUnits;
using Xunit;

namespace SharpUnits.Tests;

/// <summary>Tests for INumber&lt;T&gt; Quantity&lt;T&gt;, compile-time affine QuantityPoint&lt;T&gt;, and QuantityMath.</summary>
public sealed class GenericQuantityAndPointTests
{
    [Fact]
    public void DecimalQuantityHasZeroBinaryFloatDrift()
    {
        var a = Quantity<decimal>.Of(0.1m, Units.Millivolt);
        var b = Quantity<decimal>.Of(0.2m, Units.Millivolt);
        var sum = a + b;
        Assert.Equal(0.3m, sum.Value);
        Assert.Equal("0.3 mV", sum.ToString());

        var inVolts = sum.In(Units.Volt);
        Assert.Equal(0.0003m, inVolts.Value);
    }

    [Fact]
    public void IntegralQuantityUsesLosslessCommonUnitAndRejectsSilentTruncation()
    {
        var oneVolt = Quantity<int>.Of(1, Units.Volt);
        var halfVolt = Quantity<int>.Of(500, Units.Millivolt);

        var total = oneVolt + halfVolt;
        Assert.Equal(1500, total.Value);
        Assert.Equal("mV", total.Unit.Symbol);

        // Exact integral up-conversion succeeds
        Assert.Equal(2, Quantity<int>.Of(2000, Units.Millivolt).In(Units.Volt).Value);

        // Lossy integral conversion is rejected unless explicitly truncated or widened
        Assert.Throws<InvalidOperationException>(() => total.In(Units.Volt));
        Assert.Equal(1, total.InTruncating(Units.Volt).Value);
        Assert.Equal(1.5, total.As<double>().In(Units.Volt).Value, 12);
    }

    [Fact]
    public void GenericMultiplicationAndDivisionPreserveSymbolicUnits()
    {
        var distance = Quantity<float>.Of(100f, Units.Meter);
        var time = Quantity<float>.Of(10f, Units.Second);
        var speed = distance / time;

        Assert.Equal(10f, speed.Value);
        Assert.Equal("m/s", speed.Unit.Symbol);

        var current = Quantity<double>.Of(250, Units.Milliampere);
        var resistance = Quantity<double>.Of(4, Units.Kiloohm);
        var voltage = (current * resistance).In(Units.Volt);
        Assert.Equal(1000, voltage.Value, 12);
    }

    [Fact]
    public void AffineTemperaturePointsAvoidOffsetDriftAndWorkAcrossOrigins()
    {
        // Stored relative to IcePoint with exact decimal value
        var room = QuantityPoint<decimal>.FromCelsius(20.1m);
        Assert.Equal(20.1m, room.Value);
        Assert.Equal("20.1 °C", room.ToString());
        Assert.Equal(293.25m, room.ToKelvin().Value);

        // Cross-origin conversion: 212 °F == 100 °C == 373.15 K
        var boilingF = QuantityPoint<double>.FromFahrenheit(212.0);
        Assert.Equal(100.0, boilingF.ToCelsius().Value, 9);
        Assert.Equal(373.15, boilingF.ToKelvin().Value, 9);

        // Cross-origin subtraction produces a displacement Quantity<T> in the lossless finer unit (°R)
        var freezingF = QuantityPoint<double>.FromFahrenheit(32.0);
        var boilingC = QuantityPoint<double>.FromCelsius(100.0);
        var span = boilingC - freezingF;
        Assert.Equal("°R", span.Unit.Symbol);
        Assert.Equal(180.0, span.Value, 9);
        Assert.Equal(100.0, span.In(Units.Kelvin).Value, 9);

        // Point + displacement shifts the point; Midpoint and Lerp work naturally
        var shifted = QuantityPoint<double>.FromCelsius(20.0) + Quantity<double>.Of(5.0, Units.Kelvin);
        Assert.Equal(25.0, shifted.Value, 9);
        Assert.Equal(60.0, QuantityPoint<double>.Midpoint(QuantityPoint<double>.FromCelsius(20.0), boilingC).Value, 9);
    }

    [Fact]
    public void AffineGaugeAndAbsolutePressureOriginsConvertCleanly()
    {
        var gauge = QuantityPoint<double>.FromGaugePressure(2.0, Units.Bar);
        var absolute = gauge.RelativeTo(PointOrigins.Vacuum);

        Assert.Equal(3.01325, absolute.Value, 9);
        Assert.Equal(PointOrigins.Vacuum, absolute.Origin);

        // Custom instrument tare offset relative to standard atmosphere
        var tareOrigin = PointOrigin.RelativeTo(PointOrigins.StandardAtmosphere, 5000d, "origin.tare", "sensor tare");
        var tared = gauge.RelativeTo(tareOrigin);
        Assert.Equal(1.95, tared.Value, 9);
    }

    [Fact]
    public void QuantityMathOperatesGenericallyOverSpans()
    {
        Quantity<double>[] readings =
        [
            Quantity<double>.Of(1.0, Units.Volt),
            Quantity<double>.Of(500, Units.Millivolt),
            Quantity<double>.Of(1.5, Units.Volt)
        ];

        var (min, max) = QuantityMath.MinMax<double>(readings);
        Assert.Equal(0.5, min.In(Units.Volt).Value, 12);
        Assert.Equal(1.5, max.In(Units.Volt).Value, 12);
        Assert.Equal(1.0, QuantityMath.Mean<double>(readings).In(Units.Volt).Value, 12);
        Assert.Equal(1.0, QuantityMath.Clamp(Quantity<double>.Of(2.0, Units.Volt), min, Quantity<double>.Of(1.0, Units.Volt)).In(Units.Volt).Value, 12);
    }
}
