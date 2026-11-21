using SharpUnits;
using Xunit;

namespace SharpUnits.Tests;

/// <summary>Tests for Structure-of-Arrays QuantitySeries&lt;T&gt; buffers and unit-aware PlotAxisScaler tick generation.</summary>
public sealed class QuantitySeriesAndPlotTicksTests
{
    [Fact]
    public void SeriesAutoScalesAndConvertsBuffersCleanly()
    {
        double[] rawVolts = [-0.05, 0.0, 0.125, 0.25];
        var trace = QuantitySeries<double>.FromValues(rawVolts, Units.Volt);

        var scaled = trace.AutoScale();
        Assert.Equal("mV", scaled.Unit.Symbol);
        Assert.Equal([-50d, 0d, 125d, 250d], scaled.Values.ToArray());

        // Single-pass float conversion for GPU plotting
        var gpuTrace = scaled.As<float>();
        Assert.Equal("mV", gpuTrace.Unit.Symbol);
        Assert.Equal(250f, gpuTrace.Values[3]);
        Assert.Equal(250f, gpuTrace[3].Value);
    }

    [Fact]
    public void SeriesSupportsDimensionAndIsqCheckedAlgebraAndStatistics()
    {
        var v = QuantitySeries<double>.FromValues([10d, 20d, 30d], Units.Volt);
        var i = QuantitySeries<double>.FromValues([2d, 4d, 5d], Units.Ampere);

        var p = v * i;
        Assert.Equal("V·A", p.Unit.Symbol);
        Assert.Equal(RationalDimension.Power, p.Dimension);
        Assert.Equal([20d, 80d, 150d], p.Values.ToArray());

        var r = (v / i).In(Units.Ohm);
        Assert.Equal([5d, 5d, 6d], r.Values.ToArray());

        // Mixed-unit series subtraction aligns automatically
        var offsetMv = QuantitySeries<double>.FromValues([1000d, 2000d, 3000d], Units.Millivolt);
        var diff = v - offsetMv;
        Assert.Equal("mV", diff.Unit.Symbol);
        Assert.Equal([9000d, 18000d, 27000d], diff.Values.ToArray());

        // ISQ kind protection across series
        var energySeries = QuantitySeries<double>.FromValues([1d, 2d], Units.Joule);
        var torqueSeries = QuantitySeries<double>.FromValues([1d, 2d], Units.NewtonMeter);
        Assert.Throws<InvalidOperationException>(() => _ = energySeries + torqueSeries);

        // Statistics including exact RMS of a sine wave
        double[] sine = Enumerable.Range(0, 1000).Select(k => Math.Sin(2 * Math.PI * k / 1000d)).ToArray();
        var ac = QuantitySeries<double>.FromValues(sine, Units.Volt);
        Assert.Equal(1d / Math.Sqrt(2d), ac.Rms().Value, 6);
        Assert.Equal(2d, ac.PeakToPeak().Value, 6);
    }

    [Fact]
    public void AffineSeriesConvertsGaugeToAbsoluteAndComputesTemperatureDeltas()
    {
        var gauge = QuantitySeries<double>.FromGaugePressure([0d, 1d, 2d], Units.Bar);
        var abs = gauge.RelativeTo(PointOrigins.Vacuum);
        Assert.Equal(1.01325, abs.Values[0], 9);
        Assert.Equal(3.01325, abs.Values[2], 9);

        var chA = QuantitySeries<double>.FromCelsius([20d, 25d, 30d]);
        var chB = QuantitySeries<double>.FromCelsius([18d, 20d, 22d]);
        var deltaT = chA - chB;
        Assert.Null(deltaT.Origin); // point series - point series = displacement series
        Assert.Equal([2d, 5d, 8d], deltaT.Values.ToArray());
        Assert.Throws<InvalidOperationException>(() => _ = chA + chB);
    }

    [Fact]
    public void PlotAxisScalerProducesCleanLinearAndLogarithmicTicks()
    {
        var linear = PlotAxisScaler.CreateLinearAxis(-0.048, 0.242, Units.Volt, targetTickCount: 6);
        Assert.Equal("mV", linear.DisplayUnit.Symbol);
        Assert.Equal(1000d, linear.ScaleFromSi, 9);
        Assert.True(linear.MinDisplay <= -48d);
        Assert.True(linear.MaxDisplay >= 242d);
        Assert.DoesNotContain(linear.MajorTicks, t => t.Label == "-0");
        Assert.Contains(linear.MajorTicks, t => t.DisplayValue == 0d && t.Label == "0");

        var bodeFreq = PlotAxisScaler.CreateLogarithmicAxis(10, 1_000_000, Units.Hertz);
        Assert.Equal(["10 Hz", "100 Hz", "1 kHz", "10 kHz", "100 kHz", "1 MHz"],
            bodeFreq.MajorTicks.Select(t => t.Label).ToArray());
        Assert.Equal(40, bodeFreq.MinorSiValues.Count); // 5 decades * 8 minor ticks (2..9)
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(1025)]
    public void SimdAcceleratedSeriesMatchesScalarAcrossUnalignedLengths(int length)
    {
        var aRaw = new double[length];
        var bRaw = new double[length];
        for (int i = 0; i < length; i++)
        {
            aRaw[i] = (i - length / 2.0) * 0.001;
            bRaw[i] = (i + 1) * 0.0005;
        }

        var a = QuantitySeries<double>.FromValues(aRaw, Units.Volt);
        var b = QuantitySeries<double>.FromValues(bRaw, Units.Volt);

        var scaled = a.In(Units.Millivolt);
        var copied = new double[length];
        a.CopyConvertedTo(Units.Millivolt, copied);
        var sum = a + b;
        var diff = a - b;
        var prod = a * b;
        var quot = a / b;
        var doubled = a * 2.0;

        for (int i = 0; i < length; i++)
        {
            Assert.Equal(aRaw[i] * 1000.0, scaled.Values[i], 9);
            Assert.Equal(scaled.Values[i], copied[i], 12);
            Assert.Equal(aRaw[i] + bRaw[i], sum.Values[i], 12);
            Assert.Equal(aRaw[i] - bRaw[i], diff.Values[i], 12);
            Assert.Equal(aRaw[i] * bRaw[i], prod.Values[i], 12);
            Assert.Equal(aRaw[i] / bRaw[i], quot.Values[i], 9);
            Assert.Equal(aRaw[i] * 2.0, doubled.Values[i], 12);
        }

        if (length > 1)
            Assert.Throws<ArgumentException>(() => a.CopyConvertedTo(Units.Millivolt, copied.AsSpan(0, length - 1)));

        var (min, max) = a.MinMax();
        Assert.Equal(aRaw.Min(), min.Value, 12);
        Assert.Equal(aRaw.Max(), max.Value, 12);
        Assert.Equal(aRaw.Average(), a.Mean().Value, 10);
        Assert.Equal(Math.Sqrt(aRaw.Average(x => x * x)), a.Rms().Value, 10);

        // Also verify SIMD float path and non-SIMD decimal path + integral SIMD path
        var fSeries = a.As<float>().In(Units.Millivolt);
        Assert.Equal(aRaw[^1] * 1000.0, fSeries.Values[^1], 3);

        var intVolts = QuantitySeries<int>.FromValues(Enumerable.Range(1, length).ToArray(), Units.Volt);
        var intMv = intVolts.In(Units.Millivolt);
        Assert.Equal(length * 1000, intMv.Values[^1]);

        var decVolts = QuantitySeries<decimal>.FromValues(Enumerable.Range(1, length).Select(x => x * 0.1m).ToArray(), Units.Volt);
        var decMv = decVolts.In(Units.Millivolt);
        Assert.Equal(length * 100m, decMv.Values[^1]);
    }
}
