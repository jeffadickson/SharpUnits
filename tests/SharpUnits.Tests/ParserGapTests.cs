using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace SharpUnits.Tests;

/// <summary>Regression, boundary and generated checks for the three recorded parser gaps.</summary>
public sealed class ParserGapTests
{
    // @relation(JP-014-COMPOUND, JP-014-RATIONAL-SPACE, JP-014-ALIAS, scope=file)
    [Property(MaxTest=300,Replay="385,389")]
    public bool RationalDenominatorWhitespacePreservesValue(int numerator,PositiveInt denominator)
    {
        string[] separators=[" ","\t","\n","\r\n","\u2003"];
        var separator=separators[(uint)numerator%(uint)separators.Length];
        var parsed=new QuantityTextFormat(UnitRegistry.CreateStandard()).ParseJavaQuantity(
            FormattableString.Invariant($"{numerator}÷{separator}{denominator.Get} A"));
        return parsed.Value==(double)numerator/denominator.Get;
    }

    [Property(MaxTest=300,Replay="385,391")]
    public bool CompoundPrefixCancellationPreservesCurrent(short magnitude)
    {
        var registry=UnitRegistry.CreateStandard();
        var parsed=new QuantityTextFormat(registry).ParseJavaQuantity(
            FormattableString.Invariant($"{magnitude} (mA*kΩ)/(V/A)"));
        return parsed.To(registry.Resolve("A")).Value==magnitude;
    }

    [Fact]
    public void AliasResolutionIsLocalAndExactRegistrationsWin()
    {
        var registry=UnitRegistry.CreateStandard();var format=new QuantityTextFormat(registry);
        Assert.Same(registry.Resolve("°C"),format.ParseJavaQuantity("20 ℃").Unit);
        Assert.Equal(293.15,format.ParseJavaQuantity("20 ℃").SiQuantity.SiValue);
        Assert.Throws<KeyNotFoundException>(()=>registry.Resolve("℃"));
        var custom=new UnitDefinition("℃",Dimension.ElectricCurrent,UnitConversion.Linear(2));
        Assert.True(registry.Register(custom));
        Assert.Same(custom,format.ParseJavaQuantity("20 ℃").Unit);
        Assert.Equal(40,format.ParseJavaQuantity("20 ℃").SiQuantity.SiValue);
    }

    [Fact]
    public void JavaDivisionAssociationDoesNotChangeLegacyGrammar()
    {
        var registry=UnitRegistry.CreateStandard();
        var java=new QuantityTextFormat(registry).ParseJavaQuantity("1 A/A/A");
        Assert.Equal(RationalDimension.From(Dimension.ElectricCurrent),java.Unit.RationalDimension);
        Assert.Equal(RationalDimension.From(Dimension.ElectricCurrent).Pow(-1),new UnitExpressionParser(registry).Parse("A/A/A").Dimension);
    }

    [Fact]
    public void CompoundResourceAndAffineGuardsRemainExplicit()
    {
        var format=new QuantityTextFormat(UnitRegistry.CreateStandard());
        Assert.Equal(1,format.ParseJavaQuantity("1 "+new string('(',64)+"A"+new string(')',64)).Value);
        Assert.Throws<FormatException>(()=>format.ParseJavaQuantity("1 "+new string('(',65)+"A"+new string(')',65)));
        Assert.Throws<FormatException>(()=>format.ParseJavaQuantity("1 A^1025"));
        Assert.Throws<FormatException>(()=>format.ParseJavaQuantity("1 A^-1025"));
        Assert.Throws<FormatException>(()=>format.ParseJavaQuantity("1 mA^1024")); // Scale underflows to zero.
        Assert.Throws<FormatException>(()=>format.ParseJavaQuantity("1 °C*A"));
        Assert.Throws<FormatException>(()=>format.ParseJavaQuantity("1 ℃/A"));
    }

    [Fact]
    public void RationalSizeBudgetSpansBothLiteralsAndDimensionlessFactorsWork()
    {
        var registry=UnitRegistry.CreateStandard();var format=new QuantityTextFormat(registry);
        var small=new string('1',511);var large=new string('1',512);
        Assert.Equal(1,format.ParseJavaQuantity(small+"÷ "+small+" A").Value);
        // 511 + division sign + 512 digits exactly fills the numeric budget.
        Assert.Equal(.1,format.ParseJavaQuantity(small+"÷ "+large+" A").Value);
        Assert.Throws<FormatException>(()=>format.ParseJavaQuantity(large+"÷ "+large+" A"));
        Assert.Equal(1,format.ParseJavaQuantity("1 A/1").To(registry.Resolve("A")).Value);
    }

    [Fact]
    public void SeededMalformedCompoundFuzzingRejectsMissingGroups()
    {
        const int seed=385015;var random=new Random(seed);
        var format=new QuantityTextFormat(UnitRegistry.CreateStandard());
        for(var i=0;i<1000;i++)
        {
            var depth=random.Next(1,20);
            var text="1 "+new string('(',depth)+"mA*kΩ"+new string(')',depth-1);
            Assert.True(Record.Exception(()=>format.ParseJavaQuantity(text)) is FormatException,$"seed={seed}, case={i}, depth={depth}");
        }
    }
}
