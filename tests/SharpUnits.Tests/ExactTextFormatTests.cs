using System.Numerics;
using FsCheck.Xunit;
using Xunit;

namespace SharpUnits.Tests;

/// <summary>Independent graph, exact-value, and failure witnesses for native exact text.</summary>
public sealed class ExactTextFormatTests
{
    private static T Value<T>(TextResult<T> result) where T:notnull => result.Match(value=>value,error=>throw new Xunit.Sdk.XunitException($"{error.Code} at {error.Position}"));
    private static TextError Error<T>(TextResult<T> result) where T:notnull => result.Match(_=>throw new Xunit.Sdk.XunitException("Expected a typed failure"),error=>error);

    [Theory]
    [InlineData("m*2","scale(1)",2,0)]
    [InlineData("K+3","offset(0)",1,3)]
    [InlineData("°C","scale(1)",1,273.15)]
    [InlineData("transform(m*2;scale(1))","offset(0)",2,0)]
    public void IdentityTextTransformPreservesParentMapping(string parent,string operation,double scale,double offset)
    {
        // An identity local map must not erase the parent's scale or origin.
        var format=new ExactTextFormat();
        var original=Value(format.ParseUnit(parent));
        var parsed=Value(format.ParseUnit($"transform({parent};{operation})"));
        Assert.Equal(original,parsed);
        foreach(var value in new[]{-3,0,1,7})
            Assert.Equal(value*scale+offset,parsed.SystemConverter.Convert(value),10);
        Assert.Equal(parsed,Value(format.ParseUnit(format.FormatUnit(parsed))));
    }

    [Fact]
    public void LegacyDecimalFactorUsesItsExactHalfScale()
    {
        var format=new JavaUnitTextFormat(UnitRegistry.CreateStandard());
        var unit=format.Parse("m*0.5");
        Assert.Equal(new Rational(1,2),JavaUnit.FromDefinition(unit).SystemConverter.AffineCoefficients().Scale.Fraction);
    }

    [Fact]
    public void LegacyEmittedFahrenheitExpressionCanBeParsed()
    {
        var registry=UnitRegistry.CreateStandard();var format=new JavaUnitTextFormat(registry);
        var unit=format.Parse(format.Format(registry.Resolve("°F")));
        Assert.InRange(unit.Converter.ToSi(32),273.15-1e-12,273.15+1e-12);
    }

    [Theory]
    [InlineData("m*0.5",1,2,0,1)]
    [InlineData("m*(1÷3)",1,3,0,1)]
    [InlineData("m*2147483648",2147483648L,1,0,1)]
    [InlineData("(K*2)+3",2,1,6,1)]
    [InlineData("(K+3)*2",2,1,3,1)]
    [InlineData("K+273.15",1,1,27315,100)]
    public void ParsesIndependentExactAffineCoefficients(string text,long sn,long sd,long on,long od)
    {
        var unit=Value(new ExactTextFormat().ParseUnit(text));var c=unit.SystemConverter.AffineCoefficients();
        Assert.Equal(new Rational(sn,sd),c.Scale.Fraction);Assert.Equal(new Rational(on,od),c.Offset.Fraction);
    }

    public static IEnumerable<object[]> Graphs()
    {
        var m=JavaUnit.FromCatalog("METRE");var k=JavaUnit.FromCatalog("KELVIN");var s=JavaUnit.FromCatalog("SECOND");
        yield return new object[]{m.Multiply(JavaNumber.Decimal("0.5"))};
        yield return new object[]{k.Multiply(JavaNumber.Ratio(5,9)).Shift(JavaNumber.Ratio(45967,100))};
        yield return new object[]{k.Multiply(JavaNumber.Integer(2)).Shift(JavaNumber.Decimal("3.50"))};
        yield return new object[]{k.Shift(JavaNumber.Integer(3)).Multiply(JavaNumber.Integer(2))};
        yield return new object[]{m.Transform(JavaUnitConverterAdapter.ForPrefix(10,-3))};
        yield return new object[]{m.Transform(JavaUnitConverterAdapter.Multiply(JavaNumber.Integer(2)).Then(JavaUnitConverterAdapter.Add(JavaNumber.Decimal("3.50"))))};
        yield return new object[]{m.Transform(JavaUnitConverterAdapter.Logarithm(10))};
        yield return new object[]{m.Transform(JavaUnitConverterAdapter.Logarithm(10).Inverse())};
        yield return new object[]{m.Multiply(s.Pow(-1)).Root(2)};
        yield return new object[]{JavaUnit.FromCatalog("NEWTON").Multiply(m)};
        yield return new object[]{JavaUnit.One};
        yield return new object[]{JavaUnit.One.Shift(JavaNumber.Integer(2))};
        yield return new object[]{JavaUnit.One.Multiply(JavaNumber.Integer(2)).Shift(JavaNumber.Integer(3))};
    }

    [Theory,MemberData(nameof(Graphs))]
    public void RoundTripRetainsGraphRatherThanOnlyDimensions(JavaUnit graph)
    {
        var format=new ExactTextFormat();var text=format.FormatUnit(graph);var parsed=Value(format.ParseUnit(text));
        Assert.Equal(graph,parsed);AssertGraph(graph,parsed);
    }

    private static void AssertGraph(JavaUnit expected,JavaUnit actual)
    {
        Assert.Equal(expected.Kind,actual.Kind);Assert.Equal(expected.Dimension,actual.Dimension);
        if(expected.ParentUnit is not null)AssertGraph(expected.ParentUnit,actual.ParentUnit!);
        if(expected.TransformConverter is not null)
        {
            var a=expected.TransformConverter.Operations;var b=actual.TransformConverter!.Operations;Assert.Equal(a.Count,b.Count);
            for(var i=0;i<a.Count;i++)
            {
                Assert.Equal(a[i].Kind,b[i].Kind);Assert.Equal(a[i].Radix,b[i].Radix);Assert.Equal(a[i].Exponent,b[i].Exponent);
                Assert.Equal(a[i].Parameter.Fraction,b[i].Parameter.Fraction);Assert.Equal(a[i].Parameter.Kind,b[i].Parameter.Kind);
                Assert.Equal(a[i].Parameter.DecimalScale,b[i].Parameter.DecimalScale);
            }
        }
        foreach(var pair in expected.Factors){var found=actual.Factors.Single(item=>item.Key.Equals(pair.Key));Assert.Equal(pair.Value,found.Value);AssertGraph(pair.Key,found.Key);}
    }

    [Theory]
    [InlineData("9007199254740993",JavaQuantityScale.Absolute)]
    [InlineData("1.2300",JavaQuantityScale.Relative)]
    [InlineData("1e-40",JavaQuantityScale.Absolute)]
    public void QuantityRoundTripRetainsMagnitudeFamilyScaleAndDisplayUnit(string input,JavaQuantityScale scale)
    {
        var unit=JavaUnit.FromCatalog("KELVIN").Multiply(JavaNumber.Ratio(5,9)).Shift(JavaNumber.Decimal("459.67"));
        var original=new JavaNumberQuantity(JavaNumber.Decimal(input),unit,scale);var format=new ExactTextFormat();
        var result=Value(format.ParseQuantity(format.FormatQuantity(original)));
        Assert.Equal(original.Value.Fraction,result.Value.Fraction);Assert.Equal(original.Value.Kind,result.Value.Kind);
        Assert.Equal(original.Value.DecimalScale,result.Value.DecimalScale);Assert.Equal(scale,result.Scale);AssertGraph(unit,result.Unit);
    }

    [Fact]
    public void BinaryAndRationalValuesKeepTheirFamilies()
    {
        var format=new ExactTextFormat();
        foreach(var value in new[]{JavaNumber.Ratio(2,3),JavaNumber.FromDouble(0.1)})
        {
            var q=new JavaNumberQuantity(value,JavaUnit.FromCatalog("AMPERE"));var result=Value(format.ParseQuantity(format.FormatQuantity(q)));
            Assert.Equal(value.Kind,result.Value.Kind);Assert.Equal(value.Fraction,result.Value.Fraction);
        }
    }

    [Theory]
    [InlineData("m/0",TextErrorCode.ZeroDivisor,2)]
    [InlineData("(m",TextErrorCode.UnexpectedToken,2)]
    [InlineData("m junk",TextErrorCode.UnexpectedToken,2)]
    [InlineData("unknown",TextErrorCode.UnknownUnit,0)]
    [InlineData("m^1025",TextErrorCode.InvalidExponent,2)]
    [InlineData("m^",TextErrorCode.InvalidExponent,2)]
    [InlineData("m+",TextErrorCode.UnexpectedToken,2)]
    public void ExpectedFailuresAreTypedAndLocated(string text,TextErrorCode code,int position)
    {
        var format=new ExactTextFormat();var error=Error(format.ParseUnit(text));
        Assert.Equal(code,error.Code);Assert.Equal(position,error.Position);Assert.Equal("text",error.Parameter);
        Assert.Equal(JavaUnit.FromCatalog("METRE"),Value(format.ParseUnit("m")));
    }

    [Fact]
    public void NullBudgetMalformedUtf16AndNestedInputFailWithoutMutatingBindings()
    {
        var format=new ExactTextFormat();Assert.Equal(TextErrorCode.NullInput,Error(format.ParseUnit(null)).Code);
        Assert.Equal(TextErrorCode.InputLimit,Error(format.ParseUnit(new string('m',8193))).Code);
        Assert.Equal(TextErrorCode.DepthLimit,Error(format.ParseUnit(new string('(',65)+"m"+new string(')',65))).Code);
        Assert.False(format.ParseUnit("m\ud800").IsSuccess);
        Assert.Equal(JavaUnit.FromCatalog("METRE"),Value(format.ParseUnit("m")));
    }

    [Fact]
    public void TypedRailwayPreservesFailureAndPropagatesCancellation()
    {
        var error=Error(new ExactTextFormat().ParseUnit("missing"));var called=false;
        var failure=TextResult<JavaUnit>.Failure(error).Bind(unit=>{called=true;return TextResult<string>.Success(unit.Kind.ToString());});
        Assert.False(called);Assert.Same(error,Error(failure));
        Assert.Throws<OperationCanceledException>(()=>TextResult<int>.Success(1).Bind<string>(_=>throw new OperationCanceledException()));
    }

    [Fact]
    public void ExplicitBindingsAreSnapshottedAndUnknownGraphsHaveTypedErrors()
    {
        var custom=JavaUnit.Base("widget",new RationalDimension(1,0,0,0,0,0,0));var bindings=new Dictionary<string,JavaUnit>{{"widget",custom}};
        var format=new ExactTextFormat(bindings);bindings.Clear();Assert.Equal(custom,Value(format.ParseUnit(format.FormatUnit(custom))));
        Assert.Equal(TextErrorCode.UnknownUnit,Error(new ExactTextFormat().TryFormatUnit(custom)).Code);
    }

    [Fact]
    public void AliasesCannotInterceptDimensionlessOrGeneratedExpressions()
    {
        var m=JavaUnit.FromCatalog("METRE");var s=JavaUnit.FromCatalog("SECOND");
        var format=new ExactTextFormat(new Dictionary<string,JavaUnit>{{"one",m},{"(m*2)",s}});
        Assert.Equal(JavaUnit.One,Value(format.ParseUnit(format.FormatUnit(JavaUnit.One))));
        var doubled=m.Multiply(JavaNumber.Integer(2));AssertGraph(doubled,Value(format.ParseUnit(format.FormatUnit(doubled))));
    }

    [Fact]
    public void PowerDoesNotConsumeFollowingUnitDivision()
    {
        var graph=Value(new ExactTextFormat().ParseUnit("m^2/s"));
        Assert.Equal(new RationalDimension(2,0,-1,0,0,0,0),graph.Dimension);
    }

    [Fact]
    public void ConverterRadixIsNotRestrictedLikeItsExponent()
    {
        var unit=JavaUnit.FromCatalog("METRE").Transform(JavaUnitConverterAdapter.ForPrefix(2048,2));var format=new ExactTextFormat();
        AssertGraph(unit,Value(format.ParseUnit(format.FormatUnit(unit))));
    }

    [Fact]
    public void MalformedBindingOrQuotedUtf16CannotEscapeTypedBoundary()
    {
        Assert.Throws<ArgumentException>(()=>new ExactTextFormat(new Dictionary<string,JavaUnit>{{"x\ud800",JavaUnit.One}}));
        Assert.False(new ExactTextFormat().ParseUnit("unit(\"\\ud800\")").IsSuccess);
    }

    [Fact]
    public void CloseRationalAliasDoesNotEraseAdditionalExactDigits()
    {
        var m=JavaUnit.FromCatalog("METRE");var ordinary=m.Multiply(JavaNumber.Ratio(1,3));
        var different=m.Multiply(JavaNumber.Ratio(BigInteger.Pow(10,40)+1,3*BigInteger.Pow(10,40)));
        var format=new ExactTextFormat(new Dictionary<string,JavaUnit>{{"thirdMetre",ordinary}});
        AssertGraph(different,Value(format.ParseUnit(format.FormatUnit(different))));
    }

    [Property(MaxTest=300,Replay="385,1613")]
    public bool MalformedTextDoesNotThrow(FsCheck.NonNull<string> input)
    {
        var text=input.Get.Length>128?input.Get[..128]:input.Get;var format=new ExactTextFormat();
        _=format.ParseUnit(text);_=format.ParseQuantity(text);return true;
    }
    [Fact]
    public void FormattingDoesNotUseAmbientNegativeSign()
    {
        var old=System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            var culture=(System.Globalization.CultureInfo)System.Globalization.CultureInfo.InvariantCulture.Clone();culture.NumberFormat.NegativeSign="~";
            System.Globalization.CultureInfo.CurrentCulture=culture;var format=new ExactTextFormat();
            var graph=JavaUnit.FromCatalog("METRE").Multiply(JavaUnit.FromCatalog("SECOND").Pow(-2)).Root(3);
            AssertGraph(graph,Value(format.ParseUnit(format.FormatUnit(graph))));
        }
        finally{System.Globalization.CultureInfo.CurrentCulture=old;}
    }

    [Fact]
    public void QuotedOperatorNamesRoundTripWithoutTakingOverGrammar()
    {
        var custom=JavaUnit.Base("widget",new RationalDimension(1,0,0,0,0,0,0));
        var format=new ExactTextFormat(new Dictionary<string,JavaUnit>{{"odd*unit @\"quoted\"",custom}});
        AssertGraph(custom,Value(format.ParseUnit(format.FormatUnit(custom))));
    }

    [Fact]
    public void OutputBudgetReturnsFailureInsteadOfUnparseableText()
    {
        var graph=JavaUnit.FromCatalog("METRE");var factor=JavaNumber.Integer(BigInteger.Parse(new string('9',200)));
        for(var i=0;i<55;i++)graph=graph.Multiply(factor);
        Assert.Equal(TextErrorCode.InputLimit,Error(new ExactTextFormat().TryFormatUnit(graph)).Code);
    }

    [Fact]
    public void SharedGraphAliasMatchingDoesNotRequireTreeExpansion()
    {
        static JavaUnit Build()
        {
            var graph=JavaUnit.FromCatalog("METRE");
            for(var i=0;i<14;i++)graph=graph.Multiply(JavaNumber.Integer(2)).Multiply(graph.Shift(JavaNumber.One));
            return graph;
        }
        var registered=Build();var copy=Build();var format=new ExactTextFormat(new Dictionary<string,JavaUnit>{{"dag",registered}});
        Assert.Equal("dag",format.FormatUnit(copy));
    }

    [Fact]
    public void Utf16EscapesAlwaysReturnTypedOutcomes()
    {
        var format=new ExactTextFormat();
        for(var code=0;code<=ushort.MaxValue;code++)_=format.ParseUnit("unit(\"\\u"+code.ToString("x4",System.Globalization.CultureInfo.InvariantCulture)+"\")");
    }
    [Fact]
    public void DecimalFactorPreservesDigitsPastBinary64AndDecimal128()
    {
        const string digits="123456789012345678901234567890123456789";
        var graph=Value(new ExactTextFormat().ParseUnit("m*0."+digits));
        Assert.Equal(new Rational(BigInteger.Parse(digits),BigInteger.Pow(10,39)),graph.SystemConverter.AffineCoefficients().Scale.Fraction);
    }

    [Fact]
    public void ResultContractsRejectNullHandlersAndInvalidErrors()
    {
        Assert.Throws<ArgumentNullException>(()=>TextResult<string>.Success(null!));
        Assert.Throws<ArgumentNullException>(()=>TextResult<int>.Failure(null!));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new TextError((TextErrorCode)999,"text"));
        Assert.Throws<ArgumentException>(()=>new TextError(TextErrorCode.UnknownUnit," "));
        Assert.Throws<ArgumentNullException>(()=>new TextError(TextErrorCode.UnknownUnit,null!));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new TextError(TextErrorCode.UnknownUnit,"text",-1));
        Assert.Throws<ArgumentNullException>(()=>TextResult<int>.Success(1).Match<string>(null!,_=>"error"));
        Assert.Throws<ArgumentNullException>(()=>TextResult<int>.Success(1).Match(value=>value.ToString(),null!));
        Assert.Throws<ArgumentNullException>(()=>TextResult<int>.Success(1).Bind<string>(null!));
        Assert.Throws<InvalidOperationException>(()=>TextResult<int>.Success(1).Bind<string>(_=>null!));
    }
    [Theory]
    [InlineData("transform(m;log(0))",TextErrorCode.InvalidNumber)]
    [InlineData("transform(m;exp(1))",TextErrorCode.InvalidNumber)]
    [InlineData("transform(m;log(2.0000000000000000000000000000000000000001))",TextErrorCode.UnsupportedGraph)]
    public void FloatingConverterBasesCannotSilentlyDiscardExactDigits(string text,TextErrorCode code)
    {
        Assert.Equal(code,Error(new ExactTextFormat().ParseUnit(text)).Code);
    }
    [Fact]
    public void SuccessfulDeepGraphFormattingIsWithinReaderNestingBudget()
    {
        var graph=JavaUnit.FromCatalog("METRE");
        for(var i=0;i<24;i++)graph=(i%2==0?graph.Pow(2):graph.Root(2)).Multiply(JavaNumber.Integer(2));
        var format=new ExactTextFormat();var output=Value(format.TryFormatUnit(graph));
        AssertGraph(graph,Value(format.ParseUnit(output)));
    }
    [Fact]
    public void ThrowingFormatWrappersKeepExpectedExceptionCategories()
    {
        var format=new ExactTextFormat();
        Assert.Equal("unit",Assert.Throws<ArgumentNullException>(()=>format.FormatUnit(null!)).ParamName);
        Assert.Equal("quantity",Assert.Throws<ArgumentNullException>(()=>format.FormatQuantity(null!)).ParamName);
        var unknown=JavaUnit.Base("unknown",new RationalDimension(1,0,0,0,0,0,0));
        Assert.Throws<KeyNotFoundException>(()=>format.FormatUnit(unknown));
        var graph=JavaUnit.FromCatalog("METRE");var factor=JavaNumber.Integer(BigInteger.Parse(new string('9',200)));
        for(var i=0;i<55;i++)graph=graph.Multiply(factor);
        Assert.Throws<FormatException>(()=>format.FormatUnit(graph));
    }
    [Property(MaxTest=200,Replay="385,1611")]
    public bool ExactRationalTransformsRoundTrip(short numerator,byte denominator)
    {
        var factor=JavaNumber.Ratio((BigInteger)numerator+32769,(int)denominator+1);
        var graph=JavaUnit.FromCatalog("METRE").Multiply(factor);var format=new ExactTextFormat();
        return Value(format.ParseUnit(format.FormatUnit(graph))).Equals(graph);
    }
}
