using System.Numerics.Tensors;
using System.Text.Json;
using SharpUnits;
using SharpUnits.Tensors;
using Xunit;

namespace SharpUnits.Tensors.Tests;

/// <summary>Tests N-dimensional QuantityTensor&lt;T&gt;, zero-copy Tensor&lt;T&gt;/ReadOnlyTensorSpan&lt;T&gt; interop, and TensorPrimitives SIMD math.</summary>
public sealed class QuantityTensorTests
{
    [Fact]
    public void OscilloscopeMultiChannelTensorSlicesConvertsAndDropsZeroCopy()
    {
        // [2 channels, 4 samples] in Volts
        double[] raw = [0.001, 0.002, 0.003, 0.004, -0.010, 0.010, -0.010, 0.010];
        var scope = QuantityTensor<double>.FromValues(raw, [2, 4], Units.Volt);

        Assert.Equal(2, scope.Rank);
        Assert.Equal(8, scope.FlattenedLength);
        Assert.Equal(0.003, scope.GetQuantity(0, 2).Value, 12);

        // Zero-copy ReadOnlyTensorSpan view
        ReadOnlyTensorSpan<double> span = scope.AsReadOnlyTensorSpan();
        Assert.Equal(2, span.Rank);
        Assert.Equal(0.003, span[0, 2], 12);

        // Slice channel 0 and convert to mV
        var ch0Mv = scope.SliceLeadingAxis(0).In(Units.Millivolt);
        Assert.Equal(1, ch0Mv.Rank);
        Assert.Equal([1.0, 2.0, 3.0, 4.0], ch0Mv.FlatValues.ToArray());

        // AutoScale picks mV across the entire 2x4 capture
        var scaled = scope.AutoScale();
        Assert.Equal("mV", scaled.Unit.Symbol);
        Assert.Equal(-10.0, scaled.Min().Value, 9);
        Assert.Equal(10.0, scaled.Max().Value, 9);

        // Drop directly to raw Tensor<double> in mV and wrap right back
        Tensor<double> rawMv = scope.ToTensorIn(Units.Millivolt);
        Assert.Equal(4.0, rawMv[0, 3], 9);
        var wrapped = QuantityTensor<double>.FromTensor(rawMv, Units.Millivolt);
        Assert.Equal(4.0, wrapped.GetQuantity(0, 3).Value, 9);

        // Reshape preserves unit
        var reshaped = wrapped.Reshape([4, 2]);
        Assert.Equal(4, reshaped.Shape[0]);
        Assert.Equal(2, reshaped.Shape[1]);
        Assert.Equal(Units.Millivolt, reshaped.Unit);
    }

    [Fact]
    public void ThermalCameraAffineTensorEnforcesAffineAlgebraAndDtoRoundTrip()
    {
        float[] frameA = [20f, 22f, 24f, 26f];
        float[] frameB = [25f, 27f, 29f, 36f];
        var tA = QuantityTensor<float>.FromCelsius(frameA, [2, 2]);
        var tB = QuantityTensor<float>.FromCelsius(frameB, [2, 2]);

        // Adding two affine Celsius frames is physically invalid
        Assert.Throws<InvalidOperationException>(() => tA + tB);

        // Subtracting two affine frames yields a linear Delta-T tensor!
        var deltaT = tB - tA;
        Assert.Null(deltaT.Origin);
        Assert.Equal(5f, deltaT.Min().Value, 4);
        Assert.Equal(10f, deltaT.Max().Value, 4);

        // Re-anchoring to AbsoluteZero converts 20 °C -> 293.15 K
        var kelvinFrame = tA.RelativeTo(PointOrigins.AbsoluteZero);
        Assert.Equal(293.15f, kelvinFrame.GetPoint(0, 0).Value, 2);

        // DTO round-trip with default serializer
        string json = JsonSerializer.Serialize(tA.ToData());
        var back = QuantityTensor<float>.FromData(JsonSerializer.Deserialize<QuantityTensorData<float>>(json));
        Assert.Equal(PointOrigins.IcePoint, back.Origin);
        Assert.Equal(26f, back.GetPoint(1, 1).Value, 4);
    }

    [Fact]
    public void TensorPrimitivesEngineeringMathComputesDotNormFmaAndDecibels()
    {
        var v = QuantitySeries<double>.FromValues([1.0, 2.0, 3.0], Units.Volt);
        var i = QuantitySeries<double>.FromValues([4.0, 5.0, 6.0], Units.Ampere);

        // Zero-copy bridge from QuantitySeries<T>
        ReadOnlyTensorSpan<double> vSpan = v.AsReadOnlyTensorSpan();
        Assert.Equal(3, vSpan.FlattenedLength);
        Assert.Equal(2000.0, v.ToTensorIn(Units.Millivolt)[1], 9);

        // Dot product multiplies units: V * A -> W (1*4 + 2*5 + 3*6 = 32 W)
        var dotPower = QuantityTensorMath.Dot(v, i);
        Assert.Equal(32.0, dotPower.Value, 12);
        Assert.Equal(RationalDimension.Power, dotPower.Dimension);

        // Norm and SumOfSquares
        var norm = QuantityTensorMath.Norm(v);
        Assert.Equal(Math.Sqrt(14.0), norm.Value, 12);
        Assert.Equal(Units.Volt, norm.Unit);

        var sumSq = QuantityTensorMath.SumOfSquares(v);
        Assert.Equal(14.0, sumSq.Value, 12);
        Assert.Equal(Units.Volt.Pow(2), sumSq.Unit);

        // Distance across different scales (V vs mV)
        var v2 = QuantitySeries<double>.FromValues([1000.0, 2000.0, 6000.0], Units.Millivolt);
        var dist = QuantityTensorMath.Distance(v, v2);
        Assert.Equal(3000.0, dist.Value, 9);
        Assert.Equal("mV", dist.Unit.Symbol);

        // CosineSimilarity & Normalize
        Assert.Equal(1.0, QuantityTensorMath.CosineSimilarity(v, v), 12);
        Tensor<double> unitVec = QuantityTensorMath.Normalize(v);
        Span<double> normBuf = stackalloc double[3];
        unitVec.FlattenTo(normBuf);
        Assert.Equal(1.0, TensorPrimitives.Norm<double>(normBuf), 12);

        // Dimensionally validated FMA: (v * i) + pBias
        var pBias = QuantitySeries<double>.FromValues([1000.0, 2000.0, 3000.0], Units.Milliwatt);
        var fma = QuantityTensorMath.MultiplyAdd(v, i, pBias);
        Assert.Equal([5.0, 12.0, 21.0], fma.Values.ToArray());

        // Incompatible FMA addend is rejected
        Assert.Throws<InvalidOperationException>(() => QuantityTensorMath.MultiplyAdd(v, i, v));

        // Clamp with unit conversion (500 mV .. 2500 mV)
        var clamped = QuantityTensorMath.Clamp(v, Quantity<double>.Of(500, Units.Millivolt), Quantity<double>.Of(2500, Units.Millivolt));
        Assert.Equal([1.0, 2.0, 2.5], clamped.Values.ToArray());

        // Vectorized Log10 -> dBV and dBm
        var volts = QuantitySeries<double>.FromValues([0.1, 1.0, 10.0], Units.Volt);
        var dbv = QuantityTensorMath.ConvertToDbv(volts);
        Assert.Equal(-20.0, dbv.Values[0], 10);
        Assert.Equal(0.0, dbv.Values[1], 10);
        Assert.Equal(20.0, dbv.Values[2], 10);

        var watts = QuantitySeries<double>.FromValues([0.001, 0.01, 1.0], Units.Watt);
        var dbm = QuantityTensorMath.ConvertToDbm(watts);
        Assert.Equal(0.0, dbm.Values[0], 10);
        Assert.Equal(10.0, dbm.Values[1], 10);
        Assert.Equal(30.0, dbm.Values[2], 10);
    }
}
