using System.Globalization;
using SharpUnits;
using Xunit;

namespace SharpUnits.Tests;

/// <summary>Property-based invariants, extended engineering structs, ISpanFormattable, and parser security fuzz tests.</summary>
public sealed class PropertyAndFuzzInvariantTests
{
    [Fact]
    public void EveryStandardUnitRoundTripsCleanly()
    {
        double[] samples = [0d, 1d, -1d, 42.125, 1e-6, 1e6];
        foreach (var unit in Units.AllStandard)
        {
            foreach (double x in samples)
            {
                double si = unit.Converter.ToSi(x);
                double back = unit.Converter.FromSi(si);
                double tol = Math.Max(1e-9, Math.Abs(x) * 1e-12);
                Assert.True(Math.Abs(back - x) <= tol, $"Round-trip failed for {unit.Symbol} at {x}: got {back}");
            }
        }
    }

    [Fact]
    public void EveryPrefixableUnitComposesWithAll24SiPrefixes()
    {
        var reg = UnitRegistry.Standard;
        foreach (var unit in Units.AllStandard.Where(u => u.AllowPrefixes))
        {
            foreach (var prefix in SiPrefixCatalog.All)
            {
                string symbol = prefix.Symbol + unit.Symbol;
                var resolved = reg.Resolve(symbol);
                Assert.Equal(unit.RationalDimension, resolved.RationalDimension);
                double expectedScale = unit.Converter.Scale * prefix.Value;
                Assert.Equal(1.0, resolved.Converter.Scale / expectedScale, 9);
            }
        }
    }

    [Fact]
    public void AffineSpaceAxiomsHoldAcrossRandomizedTemperaturesAndPressures()
    {
        var rng = new Random(20261121);
        for (int i = 0; i < 100; i++)
        {
            double c1 = rng.NextDouble() * 200.0 - 50.0;
            double c2 = rng.NextDouble() * 200.0 - 50.0;
            double d1 = rng.NextDouble() * 40.0 - 20.0;
            double d2 = rng.NextDouble() * 40.0 - 20.0;

            var p1 = QuantityPoint<double>.FromCelsius(c1);
            var p2 = QuantityPoint<double>.FromCelsius(c2);
            var q1 = Quantity<double>.Of(d1, Units.Kelvin);
            var q2 = Quantity<double>.Of(d2, Units.Kelvin);

            // Associativity: (p1 + q1) + q2 == p1 + (q1 + q2)
            Assert.Equal(((p1 + q1) + q2).Value, (p1 + (q1 + q2)).Value, 9);
            // Antisymmetry: (p2 - p1) == -(p1 - p2)
            Assert.Equal((p2 - p1).Value, (-(p1 - p2)).Value, 9);
            // Recovery: p1 + (p2 - p1) == p2
            Assert.Equal((p1 + (p2 - p1)).Value, p2.Value, 9);
        }
    }

    [Fact]
    public void ExtendedQuantitiesAndDimensionalCrossOperatorsWork()
    {
        Area area = Length.FromMeters(3) * Length.FromMeters(4);
        Assert.Equal(12.0, area.SquareMeters, 12);

        Volume vol = area * Length.FromMeters(2);
        Assert.Equal(24.0, vol.CubicMeters, 12);
        Assert.Equal(24_000.0, vol.Liters, 9);

        Velocity speed = Length.FromKilometers(100) / Duration.FromHours(1);
        Assert.Equal(100.0, speed.KilometersPerHour, 9);

        Acceleration accel = speed / Duration.FromSeconds(10);
        Force f = Mass.FromKilograms(1000) * accel;
        Pressure p = f / area;
        Assert.Equal(f.Newtons, (p * area).Newtons, 9);

        Density rho = Mass.FromKilograms(24_000) / vol;
        Assert.Equal(1.0, rho.GramsPerCubicCentimeter, 9);

        Angle right = Angle.FromDegrees(90);
        Assert.Equal(1.0, right.Sin(), 12);
        Assert.Equal(0.0, right.Cos(), 12);

        Torque t = Torque.FromNewtonMeters(50);
        Assert.Equal(Isq.Torque, t.ToQuantity().Spec);

        MagneticFluxDensity b = MagneticFluxDensity.FromTesla(1);
        Assert.Equal(10_000.0, b.Gauss, 9);

        BitRate link = DataSize.FromMebibytes(100) / Duration.FromSeconds(8);
        Assert.Equal(100.0, (link * Duration.FromSeconds(8)).Mebibytes, 9);
    }

    [Fact]
    public void SpanFormattableFormatsIntoStackBufferWithoutTruncation()
    {
        var q = Quantity.Of(0.0025, Units.Volt);
        Span<char> buf = stackalloc char[32];
        Assert.True(q.TryFormat(buf, out int written, "eng", CultureInfo.InvariantCulture));
        Assert.Equal("2.5 mV", buf[..written].ToString());

        Span<char> tiny = stackalloc char[2];
        Assert.False(q.TryFormat(tiny, out _, "eng", CultureInfo.InvariantCulture));
    }

    [Fact]
    public void ParsersRejectDeepNestingOversizedStringsAndRandomFuzzWithoutCrashing()
    {
        var unitParser = new UnitExpressionParser(UnitRegistry.Standard);

        string deepUnits = new string('(', 200) + "V" + new string(')', 200);
        Assert.Throws<FormatException>(() => unitParser.ParseSymbolic(deepUnits));

        string hugeUnits = new string('V', UnitExpressionParser.MaxInputLength + 10);
        Assert.Throws<FormatException>(() => unitParser.ParseSymbolic(hugeUnits));

        // Deterministic fuzzing of random strings must never throw unexpected exceptions or hang
        var rng = new Random(42);
        const string alphabet = "VmAΩkHz/^*()·1234567890-+.°CF µ²⁻¹\t ";
        for (int i = 0; i < 300; i++)
        {
            int len = rng.Next(0, 40);
            var chars = new char[len];
            for (int j = 0; j < len; j++) chars[j] = alphabet[rng.Next(alphabet.Length)];
            string candidate = new(chars);
            _ = UnitEnvironment.Standard.TryResolveUnit(candidate, out _);
            _ = UnitEnvironment.Standard.TryParseQuantity(candidate, out _);
        }
    }
}
