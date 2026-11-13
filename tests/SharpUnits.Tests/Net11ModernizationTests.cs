using System.Numerics;
using FsCheck.Xunit;
using Xunit;

namespace SharpUnits.Tests;

/// <summary>Observable invariants for shared immutable prefix tables on .NET11.</summary>
public sealed class Net11ModernizationTests
{
    [Fact]
    public void PublicMetricPrefixSequencePreservesPriorCatalogOrder()
    {
        string[] expected = ["Q", "R", "Y", "Z", "E", "P", "T", "G", "M", "k", "h", "da",
            "d", "c", "m", "µ", "n", "p", "f", "a", "z", "y", "r", "q"];
        Assert.Equal(expected, new JavaSystemOfUnitsService().MetricPrefixes.Select(prefix => prefix.Symbol));
    }

    [Fact]
    public void SharedPrefixDefinitionsRejectMutation()
    {
        // Parsers share this catalog. Inspect the collection contract without
        // corrupting process-global state in the negative witness.
        var definitions = Assert.IsAssignableFrom<ICollection<KeyValuePair<string, int>>>(SiPrefixes.Definitions);
        Assert.True(definitions.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => definitions.Add(new("invalid", 0)));
    }

    [Fact]
    public void PrefixLookupRetainsOrdinalCaseAndUnicodeIdentity()
    {
        Assert.Equal(30, SiPrefixes.Exponent("Q"));
        Assert.Equal(-30, SiPrefixes.Exponent("q"));
        Assert.Equal(-6, SiPrefixes.Exponent("µ"));
        Assert.Throws<ArgumentException>(() => SiPrefixes.Exponent("K"));
    }

    [Property(MaxTest = 300, Replay = "385,1113")]
    public bool EveryPrefixRetainsItsIndependentExactFactor(byte selection)
    {
        var prefix = SiPrefixCatalog.All[selection % SiPrefixCatalog.All.Count];
        var power = BigInteger.Pow(10, Math.Abs(prefix.Exponent));
        var expected = prefix.Exponent < 0 ? new Rational(1, power) : new Rational(power, 1);
        return prefix.ExactValue.Fraction == expected && SiPrefixes.Exponent(prefix.Symbol) == prefix.Exponent;
    }
}
