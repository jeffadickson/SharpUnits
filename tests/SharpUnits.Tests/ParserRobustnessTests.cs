using System.Globalization;
using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace SharpUnits.Tests;

/// <summary>Negative, seeded property and malformed-input fuzz tests for the supported parser envelope.</summary>
public sealed class ParserRobustnessTests
{
    // @relation(JP-014-RATIONAL, JP-014-INVALID, JP-014-LEGACY, scope=file)
    private static QuantityTextFormat Format()=>new(UnitRegistry.CreateStandard());

    [Property(MaxTest=300,Replay="385,385")]
    public bool ExactIntegerRationalRoundsLikeBinaryDivision(int numerator,PositiveInt denominator)
    {
        // Both int operands convert exactly to double; hardware division is an independent
        // correctly-rounded oracle for these rational inputs, not for arbitrary BigInteger ratios.
        var text=FormattableString.Invariant($"{numerator}÷{denominator.Get} A");
        return Format().ParseJavaQuantity(text).Value==(double)numerator/denominator.Get;
    }

    [Property(MaxTest=300,Replay="385,387")]
    public bool IntegerParsingPreservesDisplayAndUnit(int value)
    {
        var registry=UnitRegistry.CreateStandard();
        var parsed=new QuantityTextFormat(registry).ParseJavaQuantity(value.ToString(CultureInfo.InvariantCulture)+" mA");
        return parsed.Value==value&&ReferenceEquals(parsed.Unit,registry.Resolve("mA"))&&parsed.Scale==JavaQuantityScale.Absolute;
    }

    [Fact]
    public void SeededMalformedSuffixFuzzingRejectsNumericGarbage()
    {
        const int seed=385014;
        var random=new Random(seed);
        var formatter=Format();
        // Minimal invalid core is one ASCII letter appended to a numeric token.
        // Generated prefixes vary signs and magnitude; the separator remains outside that token.
        for(var i=0;i<1000;i++)
        {
            var prefix=random.Next(-1_000_000,1_000_000).ToString(CultureInfo.InvariantCulture);
            var suffix=(char)('g'+random.Next(20));
            var text=prefix+suffix+" A";
            Assert.True(Record.Exception(()=>formatter.ParseJavaQuantity(text)) is FormatException,$"seed={seed}, case={i}, input={text}");
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("÷ A")]
    [InlineData("1÷ A")]
    [InlineData("1.5÷2 A")]
    [InlineData("1e9999 A")]
    [InlineData("1 A trailing")]
    public void MalformedOrUnsupportedTextDoesNotReturnPartialQuantity(string text)
    {
        Assert.NotNull(Record.Exception(()=>Format().ParseJavaQuantity(text)));
    }

    [Fact]
    public void ResourceAndNullGuardsAreExplicit()
    {
        var formatter=Format();
        Assert.Throws<ArgumentNullException>(()=>formatter.ParseJavaQuantity(null!));
        Assert.Throws<FormatException>(()=>formatter.ParseJavaQuantity(new string('1',4097)));
        Assert.Throws<FormatException>(()=>formatter.ParseJavaQuantity(new string('1',1025)+" A"));
        Assert.Throws<DivideByZeroException>(()=>formatter.ParseJavaQuantity("1÷0 A"));
        Assert.Throws<KeyNotFoundException>(()=>formatter.ParseJavaQuantity("1 unknown"));
        // The documented boundary itself is admitted before underflow, rather than rejected by length.
        Assert.Equal(0,formatter.ParseJavaQuantity("0."+new string('0',1021)+"1 A").Value);
    }

    [Fact]
    public void LegacyParserStillReturnsCoherentSi()
    {
        var formatter=Format();
        Assert.Equal(.25,formatter.Parse("250 mA").SiValue);
        Assert.Equal(250,formatter.ParseJavaQuantity("250 mA").Value);
        Assert.Equal(283.15,formatter.Parse("10 °C").SiValue);
        Assert.Equal(10,formatter.ParseJavaQuantity("10 °C").Value);
        Assert.Throws<FormatException>(()=>formatter.Parse("1\tA"));
    }
}
