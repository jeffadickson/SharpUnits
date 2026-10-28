using System.Numerics;
using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace SharpUnits.Tests;

/// <summary>Adversarial core checks against algebraic invariants and explicit Java source contracts.</summary>
[Trait("WorkPackage","1")]
public sealed class CoreRobustnessTests
{
    private static JavaUnit A=>JavaUnit.FromCatalog("AMPERE");
    private static JavaUnit K=>JavaUnit.FromCatalog("KELVIN");
    private static JavaNumber I(int value)=>JavaNumber.Integer(value);

    [Fact]
    public void PrimitiveDoubleUnitOverloadsAvoidNumberLongNarrowing()
    {
        const double boundary=9223372036854775808d;
        var exact=JavaNumber.Integer(BigInteger.Parse("9223372036854776000"));
        Assert.Equal(exact,JavaNumber.FromDoubleAsRational(boundary));
        Assert.Equal(exact.Negate(),JavaNumber.FromDoubleAsRational(-boundary));
        Assert.Equal(JavaNumber.Integer(long.MaxValue),JavaNumber.FromDouble(boundary));
        Assert.Equal(exact,new JavaUnitAdapter(A).Multiply(boundary).ToSystemConverter().Convert(I(1)));
        Assert.Equal(exact,new JavaUnitAdapter(A).Shift(boundary).ToSystemConverter().Convert(I(0)));
        Assert.Throws<ArgumentOutOfRangeException>(()=>JavaNumber.FromDoubleAsRational(double.NaN));
    }

    [Fact]
    public void MalformedDefaultDimensionsAndConvertersCannotEnterJavaGraphs()
    {
        Assert.Throws<ArgumentException>(()=>JavaUnit.Base("bad",default));
        Assert.Throws<ArgumentException>(()=>JavaUnit.Coherent(default));
        Assert.Throws<ArgumentOutOfRangeException>(()=>JavaUnit.FromDefinition(new UnitDefinition("bad",A.Dimension,default)));
    }

    [Fact]
    public void PrefixPowerIdentityAndReductionMatchPinnedJava()
    {
        var kilo=JavaUnitConverterAdapter.ForPrefix(10,3);
        var rational=JavaUnitConverterAdapter.Multiply(I(1000));
        Assert.NotEqual(kilo,rational);Assert.Equal(kilo.Convert(I(7)),rational.Convert(I(7)));
        Assert.Equal(JavaUnitConverterAdapter.Multiply(I(10)),JavaUnitConverterAdapter.ForPrefix(10,1));
        Assert.Equal(JavaUnitConverterAdapter.ForPrefix(10,-3),kilo.Inverse());
        Assert.True(kilo.Concatenate(kilo.Inverse()).IsIdentity);
        Assert.True(kilo.Concatenate(JavaUnitConverterAdapter.Multiply(JavaNumber.Ratio(1,1000))).IsIdentity);
        Assert.True(JavaUnitConverterAdapter.Multiply(JavaNumber.Ratio(1,1000)).Concatenate(kilo).IsIdentity);
        Assert.False(JavaUnitConverterAdapter.ForPrefix(2,10).Concatenate(JavaUnitConverterAdapter.ForPrefix(4,-5)).IsIdentity);
        Assert.Equal(I(1024),new JavaPrefix("Ki",1024,1).ExactValue);
        Assert.Equal("KIBI",new JavaPrefix("Ki",1024,1).GetName());
        Assert.Equal("custom",new JavaPrefix("custom",1024,1).GetName());
        Assert.Throws<ArgumentOutOfRangeException>(()=>JavaUnitConverterAdapter.ForPrefix(1,3));
        Assert.Throws<ArgumentOutOfRangeException>(()=>JavaUnitConverterAdapter.ForPrefix(10,1025));
        Assert.Throws<ArgumentOutOfRangeException>(()=>JavaUnitConverterAdapter.ForPrefix(10,-1025));
        Assert.Equal(JavaNumber.Integer(BigInteger.One<<1024),JavaUnitConverterAdapter.ForPrefix(2,1024).Convert(I(1)));
        Assert.Equal(JavaNumber.Ratio(1,BigInteger.One<<1024),JavaUnitConverterAdapter.ForPrefix(2,-1024).Convert(I(1)));
        Assert.True(JavaUnitConverterAdapter.ForPrefix(10,0).IsIdentity);
        Assert.Throws<ArgumentNullException>(()=>new JavaUnitAdapter(A).Prefix((JavaPrefix)null!));
        Assert.Throws<ArgumentNullException>(()=>new JavaUnitAdapter(A).Prefix((SiPrefix)null!));
    }

    [Fact]
    public void ConcatenationAndNativeThenHaveExplicitOppositeOrders()
    {
        var a=new JavaUnitConverterAdapter(new UnitConversion(2,3));var b=new JavaUnitConverterAdapter(new UnitConversion(5,7));
        Assert.Equal(new Rational(67,1),a.Concatenate(b).Convert(I(5)).Fraction);
        Assert.Equal(new Rational(72,1),a.Then(b).Convert(I(5)).Fraction);
        Assert.Equal(4,a.Concatenate(b).ConversionSteps.Count);
        Assert.Single(a.Concatenate(a.Inverse()).ConversionSteps);
        Assert.True(a.Concatenate(a.Inverse()).IsIdentity);
        Assert.Equal(a,a.Concatenate(JavaUnitConverterAdapter.Identity));
        Assert.Equal(a.GetHashCode(),a.Concatenate(JavaUnitConverterAdapter.Identity).GetHashCode());
        Assert.Equal(JavaNumberKind.Rational,new JavaUnitConverterAdapter(UnitConversion.Linear(.5)).Convert(I(1)).Kind);
        Assert.Equal(JavaNumberKind.Binary64,new JavaUnitConverterAdapter(UnitConversion.Celsius).Convert(I(0)).Kind);
    }

    [Fact]
    public void ConverterStepsAreImmutableAndInCompositionOrder()
    {
        var converter=JavaUnitConverterAdapter.Add(I(3)).Concatenate(JavaUnitConverterAdapter.Multiply(I(2)));
        Assert.Equal(new Rational(4,1),converter.ConversionSteps[0].Convert(I(1)).Fraction);
        Assert.Equal(new Rational(2,1),converter.ConversionSteps[1].Convert(I(1)).Fraction);
        Assert.Throws<NotSupportedException>(()=>((IList<JavaUnitConverterAdapter>)converter.ConversionSteps)[0]=JavaUnitConverterAdapter.Identity);
        Assert.False(converter.Equals(null));Assert.False(converter.Equals((object)"converter"));
        Assert.Throws<DivideByZeroException>(()=>JavaUnitConverterAdapter.Multiply(I(0)).Inverse());
    }

    [Fact]
    public void ExactInputsRetainPrecisionBeyondBinary64AndClrDecimal()
    {
        var integer=BigInteger.Parse("1234567890123456789012345678901234567890");
        var number=JavaNumber.Integer(integer);Assert.Equal(integer+1,number.Add(I(1)).Fraction.Numerator);
        var decimalValue=JavaNumber.Decimal("0.123456789012345678901234567890123456789");
        Assert.Equal(new Rational(BigInteger.Parse("123456789012345678901234567890123456789"),BigInteger.Pow(10,39)),decimalValue.Fraction);
        Assert.Equal(new Rational(1,3),I(1).Divide(I(3)).Fraction);
        Assert.NotEqual(JavaNumber.Decimal("0.10"),JavaNumber.Decimal("0.1"));
        Assert.Equal(0,JavaNumber.Decimal("0.10").CompareTo(JavaNumber.Decimal("0.1")));
        Assert.Equal(JavaNumber.Decimal("1.25").GetHashCode(),JavaNumber.Decimal("1.25").GetHashCode());
    }

    [Fact]
    public void PinnedDecimalBigIntegerNarrowingIsConfinedToCompatibilityArithmetic()
    {
        var n=JavaNumber.Integer(BigInteger.One<<64);var tenth=JavaNumber.Decimal("0.1");
        Assert.Equal(tenth,n.Add(tenth)); // Java's longValue() wraps 2^64 to zero.
        Assert.Equal(new Rational((BigInteger.One<<64)*10+1,10),n.AddExact(tenth).Fraction);
        Assert.Equal(n.AddExact(tenth),tenth.AddExact(n));
    }

    [Fact]
    public void JavaRationalEqualityCanDifferFromExactComparisonWithoutBreakingClrHashing()
    {
        var a=JavaNumber.Ratio(1,3);var b=JavaNumber.Ratio(BigInteger.Pow(10,40)+1,3*BigInteger.Pow(10,40));
        Assert.NotEqual(a.Fraction,b.Fraction);Assert.Equal(a,b);Assert.True(a.CompareTo(b)<0);
        Assert.Equal(a.GetHashCode(),b.GetHashCode());Assert.Single(new HashSet<JavaNumber>{a,b});
    }
    [Fact]
    public void Decimal128TiesRoundToEvenAtTheDocumentedBoundary()
    {
        var low=JavaNumber.Decimal("1234567890123456789012345678901234.4").Add(JavaNumber.Decimal("0.1"));
        var high=JavaNumber.Decimal("1234567890123456789012345678901235.4").Add(JavaNumber.Decimal("0.1"));
        Assert.Equal(BigInteger.Parse("1234567890123456789012345678901234"),low.Fraction.Numerator);
        Assert.Equal(BigInteger.Parse("1234567890123456789012345678901236"),high.Fraction.Numerator);
    }

    [Fact]
    public void NumericDomainAndResourceLimitsAreExplicit()
    {
        foreach(var text in new[]{"", " ","-",".","1_0","1E","1..2","NaN","Infinity","١"})Assert.Throws<FormatException>(()=>JavaNumber.Decimal(text));
        Assert.Throws<FormatException>(()=>JavaNumber.Decimal(new string('1',4097)));
        Assert.Throws<ArgumentOutOfRangeException>(()=>JavaNumber.Decimal("1E999999"));
        Assert.Throws<ArgumentOutOfRangeException>(()=>JavaNumber.Integer(BigInteger.One<<16384));
        Assert.Throws<DivideByZeroException>(()=>JavaNumber.Ratio(1,0));
        foreach(var value in new object[]{true,'1',"1",DayOfWeek.Monday,new object()})Assert.Throws<ArgumentException>(()=>JavaNumber.FromNumeric(value));
        foreach(var value in new[]{double.NaN,double.PositiveInfinity,double.NegativeInfinity})Assert.Throws<ArgumentOutOfRangeException>(()=>JavaNumber.FromDouble(value));
        Assert.Equal(JavaNumber.Zero,JavaNumber.FromDouble(-0d));
        Assert.Equal(I(1),JavaNumber.FromNumeric((byte)1));Assert.Equal(I(1),JavaNumber.FromNumeric((sbyte)1));
        Assert.Equal(I(1),JavaNumber.FromNumeric((short)1));Assert.Equal(I(1),JavaNumber.FromNumeric((ushort)1));
        Assert.Equal(I(1),JavaNumber.FromNumeric(1u));Assert.Equal(I(1),JavaNumber.FromNumeric(1L));Assert.Equal(I(1),JavaNumber.FromNumeric(1UL));
        Assert.Equal(JavaNumber.FromDouble(.25),JavaNumber.FromNumeric(.25f));Assert.Equal(JavaNumber.Decimal("0.25"),JavaNumber.FromNumeric(.25m));
        Assert.Equal(1,I(1).CompareTo(null));Assert.False(I(1).Equals((object)"1"));
    }

    [Fact]
    public void CatalogGraphIdentityDoesNotCollapseAlternateUnitsByDimension()
    {
        var hz=JavaUnit.FromCatalog("HERTZ");var bq=JavaUnit.FromCatalog("BECQUEREL");
        Assert.NotEqual(hz,bq);Assert.True(hz.IsEquivalentTo(bq));
        var volt=JavaUnit.FromCatalog("VOLT");
        Assert.Equal(volt,A.Multiply(volt).Divide(A));
        Assert.Equal(volt.GetHashCode(),A.Multiply(volt).Divide(A).GetHashCode());
        Assert.Equal("Volt",new JavaUnitAdapter(volt).Name);
        Assert.Null(A.GetBaseUnits());Assert.Empty(JavaUnit.One.GetBaseUnits()!);
        Assert.NotEqual(A,A.Alternate("customA"));Assert.True(A.IsEquivalentTo(A.Alternate("customA")));
        Assert.Equal(A.Alternate("customA"),A.Alternate("first").Alternate("customA"));
        Assert.Throws<InvalidOperationException>(()=>A.Multiply(I(2)).Alternate("scaled"));
    }

    [Fact]
    public void AffineProductsAndRootsCanExistBeforeAConverterIsRequested()
    {
        var c=JavaUnit.FromCatalog("CELSIUS");var inverse=c.Pow(-1);
        Assert.Equal(c.Dimension.Pow(-1),inverse.Dimension);
        Assert.Throws<NotSupportedException>(()=>inverse.SystemConverter);
        var root=A.Root(2);Assert.Equal(A,root.Pow(2));Assert.Equal(A.Pow(-1),A.Root(-1));
        Assert.Throws<NotSupportedException>(()=>root.SystemConverter);
        Assert.Throws<DivideByZeroException>(()=>A.Root(0));
        Assert.Throws<ArgumentOutOfRangeException>(()=>A.Pow(1025));
        Assert.Throws<ArgumentOutOfRangeException>(()=>A.Root(int.MinValue));
        Assert.Throws<ArgumentOutOfRangeException>(()=>A.Pow(1024).Multiply(A));
        Assert.Throws<NotSupportedException>(()=>((IDictionary<JavaUnit,Rational>)root.Factors).Clear());
        Assert.False(A.Equals((object)"A"));Assert.False(A.Equals(null));
    }

    [Fact]
    public void TransformAndPrefixApplyBeforeTheParentConverter()
    {
        var shifted=K.Shift(I(3));var scaled=shifted.Multiply(I(2));
        Assert.Equal(new Rational(13,1),scaled.SystemConverter.Convert(I(5)).Fraction);
        Assert.Equal(new Rational(13,1),new JavaUnitAdapter(shifted).Transform(JavaUnitConverterAdapter.Multiply(I(2))).ToSystemConverter().Convert(I(5)).Fraction);
        Assert.Equal(shifted,shifted.Multiply(JavaNumber.One));Assert.Equal(shifted,shifted.Shift(JavaNumber.Zero));
        Assert.Equal(K,shifted.Transform(JavaUnitConverterAdapter.Identity)); // Pinned transform(identity) returns the system unit.
        Assert.Equal(new Rational(273151,1000),new JavaUnitAdapter(JavaUnit.FromCatalog("CELSIUS")).Prefix(SiPrefixCatalog.Resolve("m")).ToSystemConverter().Convert(I(1)).Fraction);
        Assert.Equal(JavaNumber.Integer(10),SiPrefixCatalog.Resolve("m").GetValue());
        Assert.Equal(new Rational(1,1000),SiPrefixCatalog.Resolve("m").ExactValue.Fraction);
    }

    [Fact]
    public void ExactGraphRoundTripsThroughNativeAffineProjectionWithoutLosingItsIdentity()
    {
        var unit=A.Divide(I(3));var definition=unit.ToDefinition("thirdA");
        Assert.Equal(unit,JavaUnit.FromDefinition(definition));Assert.Equal(new Rational(1,3),new JavaUnitAdapter(definition).ToSystemConverter().Convert(I(1)).Fraction);
        Assert.Equal(unit,JavaUnit.FromDefinition(new JavaUnitAdapter(unit).Definition));
    }

    [Fact]
    public void LogarithmicConvertersHaveInversesButNoNativeAffineProjection()
    {
        var log=JavaUnitConverterAdapter.Logarithm(10);var exp=log.Inverse();
        Assert.False(log.IsAffine);Assert.False(log.IsLinear);Assert.True(log.Concatenate(exp).IsIdentity);
        Assert.Equal(1000,exp.Convert(3),8);Assert.Equal(3,log.Convert(1000),10);
        Assert.Throws<NotSupportedException>(()=>log.ToAffine());Assert.Throws<NotSupportedException>(()=>log.GetConversionSteps());
        Assert.Throws<ArgumentOutOfRangeException>(()=>log.Convert(0));
        foreach(var b in new[]{-1d,0d,1d,double.NaN,double.PositiveInfinity})Assert.Throws<ArgumentOutOfRangeException>(()=>JavaUnitConverterAdapter.Logarithm(b));
        var logarithmic=A.Transform(log);Assert.Equal(A,logarithmic.SystemUnit);Assert.Throws<NotSupportedException>(()=>logarithmic.ToDefinition());
        var q=new JavaNumberQuantity(I(1000),logarithmic);Assert.Equal(3,q.To(A).Value.ToDouble(),10);
        Assert.Throws<NotSupportedException>(()=>new JavaNumberQuantity(I(1),logarithmic,JavaQuantityScale.Relative).To(A));
    }

    [Fact]
    public void FactoriesAndLegacyPromotionExposeTheExactCore()
    {
        var registry=UnitRegistry.CreateStandard();IExactJavaQuantityFactory factory=new QuantityFactory(registry);
        var n=JavaNumber.Integer(BigInteger.Parse("9007199254740993"));var q=factory.CreateExactJavaQuantity(n,"A");
        Assert.Same(n,q.Value);Assert.Equal(n,JavaServiceProvider.Current.GetQuantityFactory("ElectricCurrent").Create(n,A).Value);
        Assert.Equal(new Rational(5,4),new JavaQuantityAdapter(1.25,registry.Resolve("A")).ToExact().Value.Fraction);
        var inverse=new JavaQuantityAdapter(2,registry.Resolve("°C")).InverseExact();Assert.Equal(new Rational(1,2),inverse.Value.Fraction);
        Assert.Throws<NotSupportedException>(()=>inverse.Unit.SystemConverter);
        Assert.Equal(1.25,new JavaNumberQuantity(JavaNumber.Ratio(5,4),A).ToDoubleQuantity().Value);
        Assert.Throws<ArgumentOutOfRangeException>(()=>new JavaNumberQuantity(JavaNumber.Integer(BigInteger.One<<4096),A).ToDoubleQuantity());
        Assert.Throws<KeyNotFoundException>(()=>factory.CreateExactJavaQuantity(I(1),"missing"));
    }

    [Fact]
    public void RepresentationEqualityAndDimensionCastingStayDistinctFromSemanticKinds()
    {
        var q=new JavaNumberQuantity(JavaNumber.Decimal("0.10"),A);var other=new JavaNumberQuantity(JavaNumber.Decimal("0.1"),A);
        Assert.NotEqual(q,other);Assert.True(q.IsEquivalentTo(other));
        Assert.Equal(q.GetHashCode(),new JavaNumberQuantity(JavaNumber.Decimal("0.10"),A).GetHashCode());
        Assert.Same(q,q.AsType(QuantityKind.Current,QuantityKindRegistry.CreateStandard()));
        Assert.Throws<InvalidOperationException>(()=>q.AsType(QuantityKind.Voltage,QuantityKindRegistry.CreateStandard()));
        var joules=new JavaNumberQuantity(I(1),JavaUnit.FromCatalog("JOULE"));
        Assert.Same(joules,joules.AsType(QuantityKind.Torque,QuantityKindRegistry.CreateStandard()));
        Assert.Equal(new Rational(-1,10),other.Negate().Value.Fraction);Assert.Same(q,q.To(A));
        Assert.Equal(1,q.CompareTo(null));Assert.False(q.Equals((object)"quantity"));Assert.False(q.IsEquivalentTo(null));
    }

    [Fact]
    public void DimensionFacadeRetainsRootsWhileProvidingJavaNullableMaps()
    {
        var d=new JavaDimensionAdapter(A.Dimension);Assert.Null(d.GetBaseDimensions());
        var root=d.Root(2);Assert.Equal(d,root.Pow(2));Assert.Single(root.GetBaseDimensions()!);
        Assert.Equal(d.Pow(-1),d.Root(-1));Assert.Equal(d,d.Multiply(d).Divide(d));
        Assert.Equal(d.GetHashCode(),d.Multiply(d).Divide(d).GetHashCode());
        Assert.Throws<NotSupportedException>(()=>((IDictionary<JavaDimensionAdapter,int>)root.GetBaseDimensions()!).Clear());
        Assert.Throws<DivideByZeroException>(()=>d.Root(0));Assert.Throws<ArgumentException>(()=>new JavaDimensionAdapter(default));
        Assert.False(d.Equals((object)"dimension"));Assert.False(d.Equals(null));
    }

    [Fact]
    public void PublicUnitFacadesPreserveGraphsAndNativeProjectionRules()
    {
        var a=new JavaUnitAdapter(A);var v=new JavaUnitAdapter(JavaUnit.FromCatalog("VOLT"));
        Assert.Equal(a.Model,a.MultiplyUnit(v).DivideUnit(v).Model);
        Assert.Equal(a.Model,a.PowerUnit(3).RootUnit(3).Model);
        Assert.Equal(a.Model.Pow(-1),a.InverseUnit().Model);Assert.Equal(A,a.CoherentUnit.Model);
        Assert.True(a.IsEquivalentTo(a.Alternate("anotherA")));Assert.Equal(a.GetHashCode(),new JavaUnitAdapter(A).GetHashCode());
        Assert.False(a.Equals((object)"A"));Assert.False(a.Equals(null));Assert.Same(a,a.AsType(A.Dimension));
        Assert.Equal(A,a.Multiply((IConvertible)2).Divide((IConvertible)2).CoherentUnit.Model);
        Assert.Equal(new Rational(3,1),a.Shift((IConvertible)2).ToSystemConverter().Convert(I(1)).Fraction);
        Assert.Equal(new Rational(1,2),a.Divide(.5).ToSystemConverter().Inverse().Convert(I(1)).Fraction);
        Assert.Equal(a.Dimension,a.SystemUnit.Dimension);Assert.Single(a.BaseUnits);Assert.Null(a.GetBaseUnits());
        Assert.Equal("A",a.ToString());Assert.Contains("transformed",a.Multiply(2d).ToString());
        Assert.Null(a.Multiply(2d).Symbol);Assert.Null(a.Multiply(2d).Name);
        Assert.Equal(1,a.GetConverterToAny(new JavaUnitAdapter(A)).Convert(1));
        var third=A.Divide(I(3)).ToDefinition("thirdA");
        var prefixed=SiPrefixes.Apply("k",third);Assert.Equal(new Rational(1000,3),JavaUnit.FromDefinition(prefixed).SystemConverter.Convert(I(1)).Fraction);
        var custom=new SiPrefix("custom","X",3);Assert.Equal(new Rational(1000,3),JavaUnit.FromDefinition(custom.Apply(third)).SystemConverter.Convert(I(1)).Fraction);
        Assert.Throws<InvalidOperationException>(()=>SiPrefixes.Apply("m",UnitRegistry.CreateStandard().Resolve("°C")));
        var native=new UnitDefinition("custom",Dimension.ElectricCurrent,UnitConversion.Linear(2));
        Assert.Equal(new Rational(2,1),new JavaUnitAdapter(native).ToSystemConverter().Convert(I(1)).Fraction);
    }

    [Fact]
    public void NullsAndInvalidGraphRequestsDoNotCreatePlaceholderUnits()
    {
        Assert.Throws<ArgumentNullException>(()=>new JavaUnitAdapter((JavaUnit)null!));
        Assert.Throws<ArgumentNullException>(()=>new JavaUnitAdapter((UnitDefinition)null!));
        Assert.Throws<ArgumentNullException>(()=>JavaUnit.FromDefinition(null!));
        Assert.Throws<KeyNotFoundException>(()=>JavaUnit.FromCatalog("MISSING"));
        Assert.Throws<ArgumentNullException>(()=>JavaUnit.FromCatalog(null!));
        Assert.Throws<ArgumentException>(()=>JavaUnit.Base("",A.Dimension));
        Assert.Throws<ArgumentException>(()=>A.Alternate(" "));
        Assert.Throws<ArgumentNullException>(()=>A.Multiply((JavaUnit)null!));
        Assert.Throws<ArgumentNullException>(()=>A.Divide((JavaUnit)null!));
        Assert.Throws<ArgumentNullException>(()=>A.Transform(null!));
        Assert.Throws<ArgumentNullException>(()=>JavaNumber.FromNumeric(null!));
        Assert.Throws<ArgumentNullException>(()=>I(1).Add(null!));Assert.Throws<ArgumentNullException>(()=>I(1).Subtract(null!));
        Assert.Throws<ArgumentNullException>(()=>I(1).Divide(null!));Assert.Throws<ArgumentNullException>(()=>I(1).AddExact(null!));
        Assert.Throws<ArgumentNullException>(()=>new JavaNumberQuantity(null!,A));
        Assert.Throws<ArgumentNullException>(()=>new JavaNumberQuantity(I(1),null!));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new JavaNumberQuantity(I(1),A,(JavaQuantityScale)99));
        Assert.Throws<ArgumentNullException>(()=>new JavaNumberQuantity(I(1),A).To(null!));
        Assert.Throws<ArgumentNullException>(()=>new JavaDimensionAdapter(A.Dimension).Multiply(null!));
        Assert.Throws<ArgumentNullException>(()=>JavaUnitConverterAdapter.Identity.Concatenate(null!));
        Assert.Throws<ArgumentNullException>(()=>JavaUnitConverterAdapter.Identity.Then(null!));
        Assert.Throws<ArgumentException>(()=>new JavaUnitConverterAdapter(default));
        var deep=A;for(var i=0;i<63;i++)deep=deep.Shift(I(1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>deep.Shift(I(1)));
    }
    [Fact]
    public void ProductAndTransformEqualityRejectPartiallyMatchingGraphs()
    {
        var v=JavaUnit.FromCatalog("VOLT");var ohm=JavaUnit.FromCatalog("OHM");
        Assert.NotEqual(JavaUnit.Base("x",A.Dimension),JavaUnit.Base("y",A.Dimension));
        Assert.NotEqual(JavaUnit.Base("x",A.Dimension),JavaUnit.Base("x",K.Dimension));
        Assert.Equal(JavaUnit.Base("x",A.Dimension,"one"),JavaUnit.Base("x",A.Dimension,"two"));
        Assert.NotEqual(A.Multiply(I(2)),A.Multiply(I(3)));Assert.NotEqual(A.Multiply(I(2)),v.Multiply(I(2)));
        Assert.NotEqual(A.Multiply(v),A.Multiply(ohm));Assert.NotEqual(A.Multiply(v),A.Multiply(v).Multiply(ohm));
        Assert.NotEqual(A.Pow(2),A.Pow(3));Assert.NotEqual(A.Pow(2).Multiply(v),A.Pow(3).Multiply(v));
        var log2=JavaUnitConverterAdapter.Logarithm(2);var log10=JavaUnitConverterAdapter.Logarithm(10);
        Assert.False(log2.Concatenate(log10.Inverse()).IsIdentity);Assert.False(log2.Inverse().Concatenate(log10).IsIdentity);
        Assert.False(log2.Concatenate(log10).IsIdentity);Assert.True(log2.Inverse().Concatenate(log2).IsIdentity);
        Assert.False(JavaNumber.One.Equals((JavaNumber?)null));
        Assert.False(new JavaNumberQuantity(I(1),A).Equals((JavaNumberQuantity?)null));
    }

    [Fact]
    public void SupportedBudgetEdgesAreAcceptedAndOversizedRequestsFailBeforeLargeAllocation()
    {
        Assert.Equal(JavaNumber.One,JavaNumber.Decimal(new string('0',4095)+"1"));
        Assert.Equal(new Rational(BigInteger.Pow(10,4096),1),JavaNumber.Decimal("1E4096").Fraction);
        Assert.Equal(new Rational(1,BigInteger.Pow(10,4096)),JavaNumber.Decimal("1E-4096").Fraction);
        var largest=(BigInteger.One<<16384)-1;
        Assert.Equal(largest,JavaNumber.Integer(largest).Fraction.Numerator);
        Assert.Equal(largest,JavaNumber.Ratio(1,largest).Fraction.Denominator);
        Assert.Throws<ArgumentOutOfRangeException>(()=>JavaNumber.Ratio(1,BigInteger.One<<16384));
        Assert.Equal(JavaNumber.Integer(long.MaxValue),JavaNumber.FromDouble(9223372036854775808d));
        Assert.Equal(JavaNumber.Integer(long.MinValue),JavaNumber.FromDouble(-9223372036854775808d));
        Assert.Throws<ArgumentNullException>(()=>JavaNumber.Decimal(null!));
        JavaNumber.Decimal("1E1"); // Warm ordinary parser infrastructure before allocation measurement.
        var before=GC.GetAllocatedBytesForCurrentThread();
        Assert.Throws<ArgumentOutOfRangeException>(()=>JavaNumber.Decimal("1E999999"));
        var allocated=GC.GetAllocatedBytesForCurrentThread()-before;
        Assert.InRange(allocated,0,65536); // Reject the exponent before constructing a huge BigInteger.
        var product=JavaUnit.One;
        for(var index=0;index<128;index++)product=product.Multiply(JavaUnit.Base("atom"+index,A.Dimension));
        Assert.Equal(128,product.Factors.Count);
        Assert.Throws<ArgumentOutOfRangeException>(()=>product.Multiply(JavaUnit.Base("overflowAtom",A.Dimension)));
        Assert.Equal(A,A.Root(32).Root(32).Pow(1024));
        Assert.Throws<ArgumentOutOfRangeException>(()=>A.Root(32).Root(32).Root(2));
    }

    [Fact]
    public void CatalogScopesAndCachedHashesRemainImmutableAndStructurallyEqual()
    {
        var first=JavaUnit.CreateCatalog();var second=JavaUnit.CreateCatalog();
        Assert.Equal(44,first.Count);Assert.NotSame(first["VOLT"],second["VOLT"]);Assert.Equal(first["VOLT"],second["VOLT"]);
        Assert.Throws<NotSupportedException>(()=>((IDictionary<string,JavaUnit>)first).Clear());
        var a=JavaUnit.Base("shared",A.Dimension);
        for(var index=0;index<12;index++)a=a.Multiply(a.Alternate("side"+index)).Alternate("node"+index);
        Assert.Equal(a.GetHashCode(),a.GetHashCode());Assert.True(a.Equals(a));
        Assert.Equal(new Rational(5463,20),new JavaUnitAdapter(UnitRegistry.CreateStandard().Resolve("°F")).ToSystemConverter().Convert(I(32)).Fraction);
    }
    [Property(MaxTest=300,Replay="385,701")]
    public bool NormalizedProductsCancelAndHashIndependentlyOfOrder(int input)
    {
        var power=input%9;var b=JavaUnit.FromCatalog("OHM").Pow(power);var left=A.Multiply(b);var right=b.Multiply(A);
        return left.Equals(right)&&left.GetHashCode()==right.GetHashCode()&&left.Divide(b).Equals(A);
    }

    [Property(MaxTest=300,Replay="385,703")]
    public bool ExactAffineInverseRoundTripsWithoutBinaryProjection(int input,int offset)
    {
        var scale=I(1+(input&31));var shift=I(offset%10000);var value=I(input%10000);
        var converter=JavaUnitConverterAdapter.Add(shift).Concatenate(JavaUnitConverterAdapter.Multiply(scale));
        return converter.Inverse().Convert(converter.Convert(value)).CompareTo(value)==0;
    }

    [Property(MaxTest=500,Replay="385,705")]
    public bool MalformedDecimalSuffixesCannotBeAccepted(NonNull<string> suffix)
    {
        try{JavaNumber.Decimal("1_"+suffix.Get);return false;}catch(FormatException){return true;}
    }

    [Property(MaxTest=300,Replay="385,707")]
    public bool RationalArithmeticAndReciprocalRemainExact(int input,int divisor)
    {
        var n=input%100000;var d=1+(divisor&1023);var q=JavaNumber.Ratio(n,d);
        return q.AddExact(q.Negate()).Equals(JavaNumber.Zero)&&q.Multiply(I(d)).Fraction==new Rational(n,1);
    }
}
