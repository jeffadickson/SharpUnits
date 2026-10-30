using System.Numerics;
using Xunit;

namespace SharpUnits.Tests;

/// <summary>Public boundary witnesses for the retained core mutation debt.</summary>
/// <remarks>Expected values follow exact arithmetic, public metadata and .NET argument contracts.
/// Stryker independently challenges each assertion against altered production code.</remarks>
[Trait("WorkPackage","1")]
public sealed class MutationDebtBoundaryTests
{
    private static JavaUnit A=>JavaUnit.FromCatalog("AMPERE");
    private static JavaNumber I(int value)=>JavaNumber.Integer(value);

    [Fact]
    public void DimensionValidationRejectsAnIndividuallyMalformedExponent()
    {
        var valid=A.Dimension;
        Assert.Throws<ArgumentException>(()=>new JavaDimensionAdapter(valid with {Length=default}));
        Assert.Throws<ArgumentException>(()=>new JavaDimensionAdapter(valid with {Luminosity=default}));
        Assert.Throws<ArgumentNullException>(()=>new JavaDimensionAdapter(valid).Divide(null!));
    }

    [Fact]
    public void DimensionMapRejectsNumeratorsOutsideItsIntegerApi()
    {
        var dimension=A.Dimension with {Length=new Rational((BigInteger)int.MaxValue+1,1)};
        Assert.Throws<OverflowException>(()=>new JavaDimensionAdapter(dimension).GetBaseDimensions());
    }

    [Theory]
    [InlineData("1 ")]
    [InlineData(" 1")]
    [InlineData("+ 1")]
    [InlineData("1\t")]
    public void DecimalTokensRejectWhitespaceInsideTheToken(string text)=>
        Assert.Throws<FormatException>(()=>JavaNumber.Decimal(text));

    [Theory]
    [InlineData("1E4097")]
    [InlineData("1E-4097")]
    public void DecimalInputBudgetIsEnforcedBeforeTheLargerNumericBudget(string text)
    {
        var error=Assert.Throws<ArgumentOutOfRangeException>(()=>JavaNumber.Decimal(text));
        Assert.Equal("text",error.ParamName);
    }

    [Theory]
    [InlineData(4096)]
    [InlineData(-4096)]
    public void DecimalInputScaleBoundaryIsInclusive(int exponent)
    {
        var value=JavaNumber.Decimal("1E"+exponent);
        var power=BigInteger.Pow(10,Math.Abs(exponent));
        Assert.Equal(exponent>0?new Rational(power,1):new Rational(1,power),value.Fraction);
    }

    [Fact]
    public void NullOperandsFailThroughThePublicArgumentContracts()
    {
        var unit=A;var adapter=new JavaUnitAdapter(unit);var number=I(2);
        var quantity=new JavaNumberQuantity(number,unit);
        Action[] operations={
            ()=>JavaUnit.FromCatalog(null!),()=>unit.Multiply((JavaNumber)null!),()=>unit.Divide((JavaNumber)null!),
            ()=>unit.Shift(null!),()=>unit.GetConverterTo(null!),()=>unit.GetConverterToAny(null!),
            ()=>number.SubtractPinnedIndriya(null!),()=>quantity.To(null!),()=>quantity.Add(null!),
            ()=>quantity.Multiply((JavaNumber)null!),()=>quantity.Multiply((JavaNumberQuantity)null!),
            ()=>quantity.AsType(new QuantityKind("Current"),null!),
            ()=>adapter.GetConverterTo(null!),()=>adapter.GetConverterToAny(null!),
            ()=>adapter.Multiply((JavaUnitAdapter)null!),()=>adapter.Divide((JavaUnitAdapter)null!),
            ()=>adapter.MultiplyUnit(null!),()=>adapter.DivideUnit(null!),
            ()=>JavaUnitConverterAdapter.Add(null!),()=>JavaUnitConverterAdapter.Multiply(null!),
            ()=>JavaUnitConverterAdapter.Identity.Convert((JavaNumber)null!),
            ()=>SiPrefixes.Apply(null!,unit.ToDefinition()),()=>SiPrefixes.Apply("k",null!),
            ()=>new SiPrefix("test","t",1).Apply(null!)
        };
        foreach(var operation in operations)Assert.Throws<ArgumentNullException>(operation);
    }

    [Theory]
    [InlineData(-1024)]
    [InlineData(1024)]
    public void UnitExponentAndRootBoundariesAreInclusive(int power)
    {
        Assert.Equal(A.Dimension.Pow(power),A.Pow(power).Dimension);
        Assert.Equal(A,A.Root(power).Pow(power));
    }

    [Theory]
    [InlineData(-1025)]
    [InlineData(1025)]
    public void UnitBoundsRejectInputsBeforeProductNormalization(int power)
    {
        // Dimensionless one would otherwise normalize away the invalid exponent.
        Assert.Throws<ArgumentOutOfRangeException>(()=>JavaUnit.One.Pow(power));
        Assert.Throws<ArgumentOutOfRangeException>(()=>JavaUnit.One.Root(power));
    }

    [Fact]
    public void ExportedSymbolsRespectExplicitOverrideIntrinsicAndFallback()
    {
        var named=JavaUnit.Base("intrinsic",A.Dimension);
        var unnamed=named.Multiply(I(2));
        Assert.Equal("override",named.ToDefinition("override").Symbol);
        Assert.Equal("intrinsic",named.ToDefinition().Symbol);
        Assert.Equal("Java-derived",unnamed.ToDefinition().Symbol);
        Assert.Equal("override",unnamed.ToDefinition("override").Symbol);
        var affine=named.Shift(I(3));
        Assert.Equal("affine-override",affine.ToDefinition("affine-override").Symbol);
        Assert.Equal("Java-derived",affine.ToDefinition().Symbol);
        var namedAffine=JavaUnit.FromDefinition(new UnitDefinition("named-affine",A.Dimension,new UnitConversion(2,3)));
        Assert.Equal("named-affine",namedAffine.ToDefinition().Symbol);
    }

    [Fact]
    public void NativeAdaptersRetainTheOriginalRecordAndItsSymbol()
    {
        var native=A.ToDefinition("native-alias");var adapter=new JavaUnitAdapter(native);
        Assert.Equal("native-alias",adapter.Symbol);Assert.Same(native,adapter.Definition);
        var inverse=adapter.Inverse();
        Assert.Equal(A.Dimension.Pow(-1),inverse.Dimension);Assert.Equal(1,inverse.Scale);
        Assert.Equal("Java-transformed",new JavaUnitAdapter(A.Multiply(I(2))).ToString());
    }

    [Fact]
    public void CatalogBaseNamesArePublicMetadata()
    {
        foreach(var (field,name) in new[]{("METRE","Metre"),("KILOGRAM","Kilogram"),("SECOND","Second"),
                    ("AMPERE","Ampere"),("KELVIN","Kelvin"),("MOLE","Mole"),("CANDELA","Candela")})
        {
            Assert.Equal(name,JavaUnit.FromCatalog(field).Name);
            Assert.Equal(name,JavaUnit.Coherent(JavaUnit.FromCatalog(field).Dimension).Name);
        }
    }

    [Fact]
    public void NativeIdentityImportsRespectDimensionAndSymbol()
    {
        var none=JavaUnit.One.Dimension;
        Assert.Equal(JavaUnit.One,JavaUnit.FromDefinition(new UnitDefinition("one",none,UnitConversion.Linear(1))));
        Assert.Equal(JavaUnit.One,JavaUnit.FromDefinition(new UnitDefinition("1",none,UnitConversion.Linear(1))));
        Assert.NotEqual(JavaUnit.One,JavaUnit.FromDefinition(new UnitDefinition("one",A.Dimension,UnitConversion.Linear(1))));
        Assert.NotEqual(JavaUnit.One,JavaUnit.FromDefinition(new UnitDefinition("1",none,UnitConversion.Linear(2))));
        var coherent=JavaUnit.FromDefinition(new UnitDefinition("A",A.Dimension,UnitConversion.Linear(1)));
        Assert.Equal(A,coherent);
        var custom=JavaUnit.FromDefinition(new UnitDefinition("custom",A.Dimension,UnitConversion.Linear(1)));
        Assert.Equal(JavaUnitKind.Base,custom.Kind);Assert.Equal("custom",custom.Symbol);
    }

    [Fact]
    public void PrefixRecoveryRetainsTheMatchingParentAndActualFactor()
    {
        var volt=JavaUnit.FromCatalog("VOLT");
        foreach(var (symbol,factor) in new[]{("kV",1000),("kV",2),("daV",10),("kA",2)})
        {
            var graph=JavaUnit.FromDefinition(new UnitDefinition(symbol,volt.Dimension,UnitConversion.Linear(factor)));
            Assert.Equal(new Rational(factor,1),graph.SystemConverter.Convert(JavaNumber.One).Fraction);
            // Misleading symbols must never attach an ampere parent to a voltage unit.
            Assert.Equal(volt.Dimension,graph.Dimension);
            if(symbol!="kA")Assert.Equal(volt,graph.ParentUnit);
            else Assert.Equal(JavaUnit.Coherent(volt.Dimension),graph.ParentUnit);
        }
    }

    [Fact]
    public void RepeatedNonlinearOperationsMustNotCancel()
    {
        var log=JavaUnitConverterAdapter.Logarithm(10);var exp=log.Inverse();
        Assert.Equal(Math.Log10(9),log.Concatenate(log).Convert(I(1000000000)).ToDouble(),12);
        Assert.InRange(exp.Concatenate(exp).Convert(JavaNumber.Zero).ToDouble(),9.999999999,10.000000001);
        Assert.Equal(2,log.ConversionSteps.Count+exp.ConversionSteps.Count);
        Assert.False(log.Concatenate(log).IsIdentity);Assert.False(exp.Concatenate(exp).IsIdentity);
    }

    [Fact]
    public void IdentityDoubleConverterPreservesSpecialValueBits()
    {
        var converter=JavaUnitConverterAdapter.Identity;
        foreach(var value in new[]{-0d,double.NaN,double.PositiveInfinity,double.NegativeInfinity})
            Assert.Equal(BitConverter.DoubleToInt64Bits(value),BitConverter.DoubleToInt64Bits(converter.Convert(value)));
    }

    [Theory]
    [InlineData(null,"s",0,"name")]
    [InlineData(" ","s",0,"name")]
    [InlineData("n",null,0,"symbol")]
    [InlineData("n"," ",0,"symbol")]
    [InlineData("n","s",-31,"exponent")]
    [InlineData("n","s",31,"exponent")]
    public void PrefixConstructionRejectsInvalidMetadata(string? name,string? symbol,int exponent,string parameter)
    {
        var error=Assert.ThrowsAny<ArgumentException>(()=>new SiPrefix(name!,symbol!,exponent));
        Assert.Equal(parameter,error.ParamName);
    }

    [Fact]
    public void NullCatalogAndPrefixArgumentsRetainTheirPublicParameterNames()
    {
        Assert.Equal("field",Assert.Throws<ArgumentNullException>(()=>JavaUnit.FromCatalog(null!)).ParamName);
        Assert.Equal("prefix",Assert.Throws<ArgumentNullException>(()=>SiPrefixes.Apply(null!,A.ToDefinition())).ParamName);
        var quantity=new JavaNumberQuantity(JavaNumber.One,A);
        Assert.Equal("scalar",Assert.Throws<ArgumentNullException>(()=>quantity.Multiply((JavaNumber)null!)).ParamName);
        Assert.Equal("scalar",Assert.Throws<ArgumentNullException>(()=>quantity.Divide((JavaNumber)null!)).ParamName);
    }
}
