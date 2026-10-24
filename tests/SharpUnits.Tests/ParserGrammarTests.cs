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

    [Property(MaxTest=300,Replay="385,397")]
    public bool IntegerUnitDivisorsRoundOnce(short magnitude,PositiveInt generated)
    {
        var divisor=1+generated.Get%31;var registry=UnitRegistry.CreateStandard();
        var parsed=new QuantityTextFormat(registry).ParseJavaQuantity(FormattableString.Invariant($"{magnitude} A/{divisor}"));
        return parsed.To(registry.Resolve("A")).Value==(double)magnitude/divisor;
    }

    [Property(MaxTest=300,Replay="385,399")]
    public bool PrefixConversionMatchesIndependentDecimalParsing(short magnitude,PositiveInt generated)
    {
        (string Symbol,int Exponent)[] choices=[("d",-1),("c",-2),("n",-9),("M",6),("da",1),("Q",30),("q",-30)];
        var prefix=choices[generated.Get%choices.Length];var registry=UnitRegistry.CreateStandard();
        var parsed=new QuantityTextFormat(registry).ParseJavaQuantity(FormattableString.Invariant($"{magnitude} {prefix.Symbol}A"));
        var expected=double.Parse(FormattableString.Invariant($"{magnitude}e{prefix.Exponent}"),System.Globalization.CultureInfo.InvariantCulture);
        return parsed.To(registry.Resolve("A")).Value==expected;
    }

    [Fact]
    public void PrefixesAreLocalAndExactRegistrationsOverrideInference()
    {
        var registry=UnitRegistry.CreateStandard();var format=new QuantityTextFormat(registry);
        Assert.Equal(1e-9,format.ParseJavaQuantity("1 nA").To(registry.Resolve("A")).Value);
        Assert.Throws<KeyNotFoundException>(()=>registry.Resolve("nA"));
        Assert.True(format.ParseJavaQuantity("1 µA").Equals(format.ParseJavaQuantity("1 μA")));
        var custom=new UnitDefinition("nA",Dimension.ElectricCurrent,UnitConversion.Linear(42));
        Assert.True(registry.Register(custom));
        Assert.Same(custom,format.ParseJavaQuantity("2 nA").Unit);
        Assert.Equal(84,format.ParseJavaQuantity("2 nA").SiQuantity.SiValue);
        Assert.Throws<KeyNotFoundException>(()=>format.ParseJavaQuantity("1 kmA"));
        Assert.Equal(1,SiPrefixes.Apply("k",registry.Resolve("mA")).Converter.Scale);
    }

    [Fact]
    public void SeededUnknownPrefixSuffixFuzzingRejectsMissingUnits()
    {
        const int seed=385017;var random=new Random(seed);
        var format=new QuantityTextFormat(UnitRegistry.CreateStandard());string[] prefixes=["q","Q","da","µ","μ","n","M"];
        for(var i=0;i<1000;i++)
        {
            var text="1 "+prefixes[random.Next(prefixes.Length)]+"unknown"+i.ToString(System.Globalization.CultureInfo.InvariantCulture);
            Assert.True(Record.Exception(()=>format.ParseJavaQuantity(text)) is KeyNotFoundException,$"seed={seed}, case={i}, input={text}");
        }
    }

    [Fact]
    public void ExactUnitFactorsSurviveConversionAndArithmetic()
    {
        var registry=UnitRegistry.CreateStandard();var format=new QuantityTextFormat(registry);
        var third=format.ParseJavaQuantity("3 A/3");var ampere=registry.Resolve("A");
        Assert.Equal(1,third.SiQuantity.SiValue);
        Assert.Equal(1,third.To(ampere).Value);
        Assert.Equal(3,third.To(ampere).To(third.Unit).Value);
        Assert.Equal(2,third.Add(format.ParseJavaQuantity("1 A")).To(ampere).Value);
        Assert.Equal(1,third.Multiply(format.ParseJavaQuantity("1 A")).To(new UnitDefinition("A²",Dimension.ElectricCurrent.Pow(2),UnitConversion.Linear(1))).Value);
        Assert.Equal(1,third.Divide(format.ParseJavaQuantity("1 A")).To(new UnitDefinition("1",Dimension.None,UnitConversion.Linear(1))).Value);
        Assert.Equal(3,format.ParseJavaQuantity("1 A/3").Inverse().To(new UnitDefinition("invA",Dimension.ElectricCurrent.Pow(-1),UnitConversion.Linear(1))).Value);
        Assert.Equal(third.Unit,format.ParseJavaQuantity("2 A/3").Unit);
        Assert.NotEqual(third.Unit,new UnitDefinition(third.Unit.Symbol,third.Unit.RationalDimension,third.Unit.Converter));
    }

    [Fact]
    public void IntegerFactorAndExactArithmeticBudgetsAreExplicit()
    {
        var format=new QuantityTextFormat(UnitRegistry.CreateStandard());
        foreach(var text in new[]{"1 A*0","1 A/0","1 A*2147483648","1 A*-2147483649","1 ((A*2147483647)/(A*2147483646))^1024"})
            Assert.Throws<FormatException>(()=>format.ParseJavaQuantity(text));
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
