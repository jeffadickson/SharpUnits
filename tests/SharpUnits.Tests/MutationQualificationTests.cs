using System.Numerics;
using Xunit;
namespace SharpUnits.Tests;

/// <summary>Independent boundary witnesses prompted by the broad mutation review.</summary>
[Trait("WorkPackage","1")]
public sealed class MutationQualificationTests
{
    [Fact]
    public void DefaultLargeIntegerArithmeticPreservesBothOperandOrders()
    {
        var integer=JavaNumber.Integer(BigInteger.One<<64);var tenth=JavaNumber.Decimal("0.1");
        var sum=new Rational((BigInteger.One<<64)*10+1,10);
        Assert.Equal(sum,integer.Add(tenth).Fraction);Assert.Equal(sum,tenth.Add(integer).Fraction);
        Assert.Equal(new Rational(1-(BigInteger.One<<64)*10,10),tenth.Subtract(integer).Fraction);
        Assert.Equal(new Rational((BigInteger.One<<64)*10-1,10),integer.Subtract(tenth).Fraction);
        Assert.Equal(tenth,tenth.SubtractPinnedIndriya(integer));
        Assert.Equal(new Rational(BigInteger.One<<64,10),integer.Multiply(tenth).Fraction);
        Assert.Equal(new Rational(BigInteger.One<<64,10),tenth.Multiply(integer).Fraction);
        Assert.Equal(new Rational(0,1),integer.Multiply(JavaNumber.Zero).Fraction);
        Assert.Equal(new Rational(0,1),JavaNumber.Zero.Multiply(integer).Fraction);
        Assert.Equal(JavaNumber.Zero,integer.Multiply(JavaNumber.Decimal("0.0")));
    }
    [Fact]
    public void AffineNativeBridgeConvertsWithoutConsultingRetainedGraphMetadata()
    {
        var kelvin=JavaUnit.FromCatalog("KELVIN");
        var native=new UnitDefinition("custom",kelvin.Dimension,new UnitConversion(3,7));
        var graph=JavaUnit.FromDefinition(native);
        Assert.Equal(new Rational(7,1),graph.SystemConverter.Convert(JavaNumber.Zero).Fraction);
        Assert.Equal(new Rational(22,1),graph.SystemConverter.Convert(JavaNumber.Integer(5)).Fraction);
        var exported=kelvin.Multiply(JavaNumber.Integer(3)).Shift(JavaNumber.Ratio(7,3)).ToDefinition("exported");
        // Call the native converter directly: JavaModel round trips could conceal a bad projection.
        Assert.Equal(7,exported.Converter.ToSi(0),12);Assert.Equal(22,exported.Converter.ToSi(5),12);
        Assert.Equal(5,exported.Converter.FromSi(22),12);Assert.Equal(3,exported.Converter.Scale,12);
        var third=kelvin.Multiply(JavaNumber.Ratio(1,3)).ToDefinition();
        var exact=typeof(UnitDefinition).GetProperty("ExactLinearScale",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
        Assert.Equal(new Rational(1,3),Assert.IsType<Rational>(exact.GetValue(third)));
    }
    [Fact]
    public void ProductDepthUsesTheDeepestFactorInEitherOrder()
    {
        var deep=JavaUnit.FromCatalog("AMPERE");var second=JavaUnit.FromCatalog("SECOND");
        // Base depth one +62 transformations =63; the product adds exactly one level.
        for(var i=0;i<62;i++)deep=deep.Shift(JavaNumber.One);
        var left=deep.Multiply(second);var right=second.Multiply(deep);
        Assert.Equal(left,right);
        Assert.Throws<ArgumentOutOfRangeException>(()=>left.Shift(JavaNumber.One));
        Assert.Throws<ArgumentOutOfRangeException>(()=>right.Shift(JavaNumber.One));
        deep=deep.Shift(JavaNumber.One); // Depth64 is accepted; another product level is rejected.
        Assert.Throws<ArgumentOutOfRangeException>(()=>deep.Multiply(second));
        Assert.Throws<ArgumentOutOfRangeException>(()=>second.Multiply(deep));
    }
    [Fact]
    public void RelativeQuantitiesRejectNonconstantDerivativeConversion()
    {
        var ampere=JavaUnit.FromCatalog("AMPERE");
        var logarithmic=ampere.Transform(JavaUnitConverterAdapter.Logarithm(10));
        var relative=new JavaNumberQuantity(JavaNumber.Integer(10),logarithmic,JavaQuantityScale.Relative);
        Assert.Throws<NotSupportedException>(()=>relative.To(ampere));
        var affine=new JavaNumberQuantity(JavaNumber.Integer(3),ampere.Multiply(JavaNumber.Integer(2)),JavaQuantityScale.Relative);
        Assert.Equal(new Rational(6,1),affine.To(ampere).Value.Fraction);
    }
    [Fact]
    public void UnitEquivalenceRejectsNullDifferentDimensionsAndDifferentScales()
    {
        var ampere=JavaUnit.FromCatalog("AMPERE");var adapter=new JavaUnitAdapter(ampere);
        Assert.False(ampere.IsEquivalentTo(null));Assert.False(adapter.IsEquivalentTo(null));
        Assert.False(ampere.IsEquivalentTo(JavaUnit.FromCatalog("SECOND")));
        Assert.False(adapter.IsEquivalentTo(new JavaUnitAdapter(ampere.Multiply(JavaNumber.Integer(2)))));
    }
    [Fact]
    public void NativePrefixBridgeRetainsExactJavaOperationIdentity()
    {
        var ampere=JavaUnit.FromCatalog("AMPERE");
        var prefixed=SiPrefixes.Apply("k",ampere.ToDefinition());
        var expected=ampere.Transform(JavaUnitConverterAdapter.ForPrefix(10,3));
        Assert.Equal(expected,new JavaUnitAdapter(prefixed).Model);
        Assert.Equal(expected.SystemConverter,new JavaUnitAdapter(prefixed).Model.SystemConverter);
        Assert.Equal("kA",prefixed.Symbol);
        // A custom alternate cannot be reconstructed from a prefixed symbol alone.
        var alternate=ampere.Alternate("custom_current");
        var custom=SiPrefixes.Apply("k",alternate.ToDefinition());
        Assert.Equal(alternate.Transform(JavaUnitConverterAdapter.ForPrefix(10,3)),new JavaUnitAdapter(custom).Model);
    }
    [Fact]
    public void UnknownPrefixAndNonfinitePrimitiveInputsFailThroughDeclaredContracts()
    {
        Assert.Throws<KeyNotFoundException>(()=>SiPrefixCatalog.Resolve("not-a-prefix"));
        Assert.Throws<ArgumentException>(()=>SiPrefixes.Apply("not-a-prefix",JavaUnit.FromCatalog("AMPERE").ToDefinition()));
        foreach(var value in new[]{double.NaN,double.PositiveInfinity,double.NegativeInfinity})
        {
            Assert.Throws<ArgumentOutOfRangeException>(()=>JavaNumber.FromDouble(value));
            Assert.Throws<ArgumentOutOfRangeException>(()=>JavaNumber.FromDoubleAsRational(value));
        }
    }
}

