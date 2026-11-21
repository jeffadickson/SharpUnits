using SharpUnits;
using Xunit;

namespace SharpUnits.Tests;

/// <summary>Tests for the unified Quantity value type (display unit, affine safety, semantic kinds, parsing, and rational safety).</summary>
public sealed class UnifiedQuantityTests
{
    [Fact]
    public void DefaultRationalIsCanonicalZero()
    {
        Rational r = default;
        Assert.Equal(0, r.Numerator);
        Assert.Equal(1, r.Denominator);
        Assert.True(r.IsZero);
        Assert.Equal(Rational.Zero, r);
        Assert.Equal(Rational.One, r + Rational.One);

        RationalDimension d = default;
        Assert.True(d.IsDimensionless);
        Assert.Equal(RationalDimension.None, d);
    }

    [Fact]
    public void DisplayUnitPreservationAndConversion()
    {
        var v = Quantity.Of(250, Units.Millivolt);
        Assert.Equal(250, v.Value, 12);
        Assert.Equal(0.25, v.SiValue, 12);
        Assert.Equal("mV", v.UnitSymbol);

        var inVolts = v.In(Units.Volt);
        Assert.Equal(0.25, inVolts.Value, 12);
        Assert.Equal("V", inVolts.UnitSymbol);
        Assert.True(v.IsEquivalentTo(inVolts));
        Assert.Equal(250_000, v.AsValueIn("µV"), 6);
    }

    [Fact]
    public void AffineTemperaturePointAndDeltaRules()
    {
        var t1 = Quantity.Of(20, Units.Celsius);
        var t2 = Quantity.Of(100, Units.Celsius);

        // Cannot add two absolute temperatures
        Assert.Throws<InvalidOperationException>(() => _ = t1 + t2);

        // Subtracting two absolute temperatures yields a relative difference
        var delta = t2 - t1;
        Assert.Equal(QuantityScale.Relative, delta.Scale);
        Assert.Equal(80, delta.Value, 9);
        Assert.Equal(80, delta.SiValue, 9);
        Assert.Equal("Δ°C", delta.UnitSymbol);

        // Adding a relative difference to an absolute temperature shifts the point
        var warmed = t1 + Quantity.Delta(15, Units.Celsius);
        Assert.Equal(QuantityScale.Absolute, warmed.Scale);
        Assert.Equal(35, warmed.Value, 9);

        // Cannot scale or multiply absolute affine temperatures
        Assert.Throws<InvalidOperationException>(() => _ = t1 * 2);
        Assert.Throws<InvalidOperationException>(() => _ = -t1);

        // Can scale relative temperature intervals
        var doubled = delta * 2;
        Assert.Equal(160, doubled.Value, 9);
    }

    [Fact]
    public void SemanticKindsPreventMixingEnergyAndTorqueWithoutRegistryCeremony()
    {
        var energy = Quantity.Of(10, Units.Joule);
        var torque = Quantity.Of(10, Units.NewtonMeter);

        Assert.Equal(energy.RationalDimension, torque.RationalDimension);
        Assert.Equal(QuantityKind.Energy, energy.Kind);
        Assert.Equal(QuantityKind.Torque, torque.Kind);

        Assert.Throws<InvalidOperationException>(() => _ = energy + torque);
        Assert.Throws<InvalidOperationException>(() => _ = energy - torque);
        Assert.False(energy.IsEquivalentTo(torque));

        var sumEnergy = energy + Quantity.Of(5, Units.Joule);
        Assert.Equal(15, sumEnergy.Value, 12);
    }

    [Fact]
    public void ArithmeticOperatorsAndComparisons()
    {
        var i = Quantity.Parse("250 mA");
        var r = Quantity.Parse("4 kΩ");
        var v = i * r;

        Assert.Equal(RationalDimension.Voltage, v.RationalDimension);
        Assert.Equal(1000, v.SiValue, 9);
        Assert.Equal("V", v.UnitSymbol);
        Assert.True(v > Quantity.Parse("500 V"));
        Assert.True(Quantity.Parse("1 kV").IsEquivalentTo(v, 1e-9));

        var half = v / 2;
        Assert.Equal(500, half.Value, 9);

        var root = Quantity.Of(16, Units.Volt).Sqrt();
        Assert.Equal(4, root.SiValue, 12);
        Assert.Equal(new Rational(1, 2), root.RationalDimension.Mass);
    }

    [Theory]
    [InlineData("100mV", 0.1, "mV")]
    [InlineData("4.7 uF", 4.7e-6, "µF")]
    [InlineData("10 kOhm", 10000.0, "kΩ")]
    [InlineData("25 degC", 298.15, "°C")]
    [InlineData("10 Δ°C", 10.0, "Δ°C")]
    [InlineData("10 nV/Hz^(1/2)", 1e-8, "nV/Hz^(1/2)")]
    public void QuantityParseSupportsFlexibleFormats(string text, double expectedSi, string expectedSymbol)
    {
        var q = Quantity.Parse(text);
        Assert.Equal(expectedSi, q.SiValue, 12);
        Assert.Equal(expectedSymbol, q.UnitSymbol);
    }
}
