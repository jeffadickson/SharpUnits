using System.Numerics;
using System.Text.Json;
using FsCheck.Xunit;
using Xunit;
namespace SharpUnits.Tests;

/// <summary>Independent pinned Java chains normalized at each public operation boundary.</summary>
[Trait("WorkPackage","1")]
public sealed class MutationDebtChainTests
{
    public static IEnumerable<object[]> Cases()
    {
        using var document=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"mutation-debt-chain-cases.json")));
        foreach(var item in document.RootElement.GetProperty("cases").EnumerateArray())
            yield return new object[]{item.GetProperty("id").GetString()!,item.GetProperty("expected").GetString()!};
    }
    [Theory]
    [MemberData(nameof(Cases))]
    public void NormalizedChainMatchesPinnedJava(string id,string expected)
    {
        var big=JavaNumber.Integer(BigInteger.One<<62);var primitive=JavaNumber.FromNumeric(1L<<62);
        var binary=JavaNumber.FromDouble(Math.ScaleB(1d,62));var minusOne=JavaNumber.Integer(-1);
        var intermediate=id switch
        {
            "negate-big-boundary"=>big.Negate(),
            "negate-minimum-twice"=>JavaNumber.FromNumeric(long.MinValue).Negate().Negate(),
            "add-big"=>big.Add(minusOne),"add-long"=>primitive.Add(minusOne),"add-double"=>binary.Add(minusOne),
            "multiply"=>JavaNumber.Integer(BigInteger.One<<61).Multiply(JavaNumber.Integer(2)),
            "divide"=>JavaNumber.Integer(BigInteger.One<<63).Divide(JavaNumber.Integer(2)),
            "reciprocal-twice"=>big.Reciprocal().Reciprocal(),
            "negate-big-twice"=>big.Negate().Negate(),"negate-long-boundary"=>primitive.Negate(),
            "negate-long-twice"=>primitive.Negate().Negate(),"zero-add-long"=>primitive.Add(JavaNumber.Zero),
            "zero-add-big"=>big.Add(JavaNumber.Zero),"multiply-one-long"=>primitive.Multiply(JavaNumber.One),
            "divide-one-long"=>primitive.Divide(JavaNumber.One),"zero-add-double"=>binary.Add(JavaNumber.Zero),
            _=>throw new ArgumentException("Unknown retained chain",nameof(id))
        };
        Assert.Equal(expected,CoreFixtures.ObserveNumber(intermediate.Multiply(JavaNumber.Decimal("0.1234567890123456789012345678901234"))));
    }
    [Property(MaxTest=500,Replay="385,901")]
    public bool PrimitiveLongMultiplicationRetainsExactDecimalProducts(long input)
    {
        var integer=input>=0?long.MaxValue-(input&1023):long.MinValue+(input&1023);
        var a=JavaNumber.FromNumeric(integer);var b=JavaNumber.Decimal("0.1234567890123456789012345678901234");
        var expected=new Rational(new BigInteger(integer)*BigInteger.Parse("1234567890123456789012345678901234"),BigInteger.Pow(10,34));
        return a.Multiply(b).Fraction==expected&&b.Multiply(a).Fraction==expected;
    }
    [Property(MaxTest=500,Replay="385,903")]
    public bool RationalComparisonSeparatesExactAndDecimal128Projection(int input)
    {
        var denominator=BigInteger.Pow(10,50);var delta=input%1000;if(delta==0)delta=1;
        var rational=JavaNumber.Ratio(denominator/2+delta,denominator);var binary=JavaNumber.FromDouble(0.5);
        return rational.CompareTo(binary)==0&&binary.CompareTo(rational)==0
            &&Math.Sign(rational.CompareTo(JavaNumber.Ratio(1,2)))==Math.Sign(delta);
    }
}
