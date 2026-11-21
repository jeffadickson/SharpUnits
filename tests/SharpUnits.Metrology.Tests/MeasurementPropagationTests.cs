using SharpUnits;
using SharpUnits.Metrology;
using Xunit;

namespace SharpUnits.Metrology.Tests;

/// <summary>Tests for first-order uncorrelated uncertainty propagation across +, -, *, /, and scalar scaling.</summary>
public sealed class MeasurementPropagationTests
{
    [Fact]
    public void AdditionAndSubtractionPropagateInQuadrature()
    {
        var a = new Measurement(Quantity.Of(10, Units.Volt), 0.3);
        var b = new Measurement(Quantity.Of(4, Units.Volt), 0.4);

        var sum = a + b;
        Assert.Equal(14.0, sum.Value.SiValue, 12);
        Assert.Equal(0.5, sum.StandardUncertainty, 12);

        var diff = a - b;
        Assert.Equal(6.0, diff.Value.SiValue, 12);
        Assert.Equal(0.5, diff.StandardUncertainty, 12);
        Assert.Equal(1.0, diff.ExpandedUncertainty(2), 12);
    }

    [Fact]
    public void MultiplicationDivisionAndScalarScaling()
    {
        var v = new Measurement(Quantity.Of(10, Units.Volt), 0.1);   // 1%
        var i = new Measurement(Quantity.Of(2, Units.Ampere), 0.04); // 2%

        var r = v / i; // 5 Ω, relative u = sqrt(1%^2 + 2%^2) = sqrt(5)% -> 5 * 0.02236067977 = 0.1118033989
        Assert.Equal(RationalDimension.Resistance, r.Value.RationalDimension);
        Assert.Equal(5.0, r.Value.SiValue, 12);
        Assert.Equal(5.0 * Math.Sqrt(0.01 * 0.01 + 0.02 * 0.02), r.StandardUncertainty, 12);

        var scaled = r * 2;
        Assert.Equal(10.0, scaled.Value.SiValue, 12);
        Assert.Equal(2 * r.StandardUncertainty, scaled.StandardUncertainty, 12);
    }
}
