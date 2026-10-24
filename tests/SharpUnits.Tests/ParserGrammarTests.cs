using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace SharpUnits.Tests;

/// <summary>Generated and negative tests for the supported Java power/root grammar.</summary>
public sealed class ParserGrammarTests
{
    // @relation(JP-014-POWERS, JP-014-ROOTS, scope=file)
    [Property(MaxTest=300,Replay="385,393")]
    public bool SuperscriptDimensionsAgreeWithNativePowers(short magnitude,PositiveInt generated)
    {
        var power=1+generated.Get%3;var digit="¹²³"[power-1];
        var parsed=new QuantityTextFormat(UnitRegistry.CreateStandard()).ParseJavaQuantity(
            FormattableString.Invariant($"{magnitude} A{digit}"));
        return parsed.Value==magnitude&&parsed.Unit.RationalDimension==RationalDimension.From(Dimension.ElectricCurrent).Pow(power);
    }

    [Property(MaxTest=300,Replay="385,395")]
    public bool MatchingPowersAndRootsRestoreDimensions(PositiveInt generated)
    {
        var degree=1+generated.Get%20;
        var parsed=new QuantityTextFormat(UnitRegistry.CreateStandard()).ParseJavaQuantity(
            FormattableString.Invariant($"1 A^{degree}:{degree}"));
        return parsed.Unit.RationalDimension==RationalDimension.From(Dimension.ElectricCurrent)&&parsed.Value==1;
    }

    [Fact]
    public void JavaZeroSentinelsDoNotChangeNativePowerZero()
    {
        var registry=UnitRegistry.CreateStandard();var format=new QuantityTextFormat(registry);
        Assert.Equal(RationalDimension.From(Dimension.ElectricCurrent),format.ParseJavaQuantity("1 A^0").Unit.RationalDimension);
        Assert.Equal(RationalDimension.From(Dimension.ElectricCurrent),format.ParseJavaQuantity("1 A^1:0").Unit.RationalDimension);
        Assert.Equal(RationalDimension.From(Dimension.None),new UnitExpressionParser(registry).Parse("A^0").Dimension);
        Assert.Equal(RationalDimension.From(Dimension.None),RationalUnitExpression.From(registry.Resolve("A")).Pow(0).Dimension);
    }

    [Fact]
    public void RootBoundsAndMalformedExponentsAreRejected()
    {
        var format=new QuantityTextFormat(UnitRegistry.CreateStandard());
        Assert.Throws<FormatException>(()=>format.ParseJavaQuantity("1 A^1:1025"));
        Assert.Throws<FormatException>(()=>format.ParseJavaQuantity("1 A^1:-1025"));
        Assert.Throws<FormatException>(()=>format.ParseJavaQuantity("1 A**"));
        const int seed=385016;var random=new Random(seed);
        for(var i=0;i<1000;i++)
        {
            var text="1 A^x"+random.Next(1,100000).ToString(System.Globalization.CultureInfo.InvariantCulture);
            Assert.True(Record.Exception(()=>format.ParseJavaQuantity(text)) is FormatException,$"seed={seed}, case={i}, input={text}");
        }
    }
}
