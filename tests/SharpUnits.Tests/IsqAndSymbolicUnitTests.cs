using SharpUnits;
using Xunit;

namespace SharpUnits.Tests;

/// <summary>Tests for ISO 80000 ISQ quantity specifications and algebraic symbolic unit expressions.</summary>
public sealed class IsqAndSymbolicUnitTests
{
    [Fact]
    public void IsqSubSpecsCombineToCommonAncestor()
    {
        var width = Quantity<double>.Of(3, Units.Meter, Isq.Width);
        var height = Quantity<double>.Of(4, Units.Meter, Isq.Height);

        var perimeterHalf = width + height;
        Assert.Equal(7, perimeterHalf.Value, 12);
        Assert.Equal(Isq.Length, perimeterHalf.Spec);

        // Explicit down-cast within same kind tree succeeds
        var asDistance = perimeterHalf.AsSpec(Isq.Distance);
        Assert.Equal(Isq.Distance, asDistance.Spec);

        // Non-generic Quantity also enforces ISQ hierarchy
        var qW = Quantity.Of(3, Units.Meter, spec: Isq.Width);
        var qH = Quantity.Of(4, Units.Meter, spec: Isq.Height);
        Assert.Equal(Isq.Length, (qW + qH).Spec);
    }

    [Fact]
    public void DistinctIsqKindsSharingDimensionCannotBeMixed()
    {
        var energy = Quantity<double>.Of(10, Units.Joule, Isq.Work);
        var torque = Quantity<double>.Of(10, Units.NewtonMeter, Isq.Torque);

        Assert.Equal(energy.Dimension, torque.Dimension);
        Assert.Throws<InvalidOperationException>(() => _ = energy + torque);
        Assert.Throws<InvalidOperationException>(() => _ = energy.AsSpec(Isq.Torque));

        var freq = Quantity<double>.Of(60, Units.Hertz, Isq.Frequency);
        var omega = Quantity<double>.Of(377, Units.Radian / Units.Second, Isq.AngularFrequency);
        Assert.Equal(freq.Dimension, omega.Dimension);
        Assert.Throws<InvalidOperationException>(() => _ = freq + omega);
    }

    [Fact]
    public void SymbolicUnitAlgebraPreservesStructureAndCancelsIdenticalFactors()
    {
        var velocity = Units.Meter / Units.Second;
        Assert.Equal("m/s", velocity.Symbol);
        Assert.Equal(RationalDimension.Velocity, velocity.Dimension);

        var accel = Units.Meter / Units.Second.Pow(2);
        Assert.Equal("m/s²", accel.Symbol);
        Assert.Equal("m/s^2", accel.AsciiSymbol);
        Assert.Equal(RationalDimension.Acceleration, accel.Dimension);

        var cancelled = velocity * Units.Second;
        Assert.Equal(SymbolicUnit.From(Units.Meter), cancelled);
        Assert.Equal("m", cancelled.Symbol);

        var nanoVolt = UnitRegistry.Standard.Resolve("nV");
        var noiseDensity = nanoVolt / Units.Hertz.Root(2);
        Assert.Equal("nV/Hz^(1/2)", noiseDensity.Symbol);
        Assert.Equal(RationalDimension.Voltage - RationalDimension.Frequency.Root(2), noiseDensity.Dimension);

        var ohmProduct = Units.Milliampere * Units.Kiloohm;
        Assert.Equal("mA·kΩ", ohmProduct.Symbol);
        Assert.Equal("mA*kOhm", ohmProduct.AsciiSymbol);
        Assert.Equal(1d, ohmProduct.ScaleToSi, 12);
        Assert.Equal(SymbolicUnit.From(Units.Volt), ohmProduct.ToCoherentSi());
    }

    [Fact]
    public void ExpressionParserProducesStructuredSymbolicUnits()
    {
        var parser = new UnitExpressionParser(UnitRegistry.Standard);
        var parsed = parser.ParseSymbolic("mA*kΩ");
        Assert.Equal(RationalDimension.Voltage, parsed.Dimension);
        Assert.Equal(1d, parsed.ScaleToSi, 12);
        Assert.Equal(2, parsed.Factors.Count);
    }
}
