using System.Numerics;
using FsCheck.Xunit;
using Xunit;
namespace SharpUnits.Tests;

/// <summary>Tests the actual production scale-budget primitive without fabricated object state.</summary>
[Trait("WorkPackage","1")]
public sealed class DecimalScaleBudgetTests
{
    [Theory]
    [InlineData(0,0,0)]
    [InlineData(int.MaxValue,0,int.MaxValue)]
    [InlineData(int.MinValue,0,int.MinValue)]
    [InlineData(int.MaxValue,-1,int.MaxValue-1)]
    [InlineData(int.MinValue,1,int.MinValue+1)]
    public void InclusiveScaleBoundsAreAccepted(int left,int right,int expected)=>
        Assert.Equal(expected,JavaNumber.CombineDecimalScales(left,right));
    [Theory]
    [InlineData(int.MaxValue,1)]
    [InlineData(int.MinValue,-1)]
    [InlineData(int.MaxValue,int.MaxValue)]
    [InlineData(int.MinValue,int.MinValue)]
    public void OverflowDoesNotWrapRetainedScale(int left,int right)=>
        Assert.Throws<OverflowException>(()=>JavaNumber.CombineDecimalScales(left,right));
    [Property(MaxTest=500,Replay="385,907")]
    public bool ScaleAdditionAgreesWithAnUnboundedIntegerOracle(int left,int right)
    {
        var expected=new BigInteger(left)+right;
        try{return JavaNumber.CombineDecimalScales(left,right)==expected&&expected>=int.MinValue&&expected<=int.MaxValue;}
        catch(OverflowException){return expected<int.MinValue||expected>int.MaxValue;}
    }
}
