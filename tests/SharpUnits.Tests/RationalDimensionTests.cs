using SharpUnits;
using Xunit;
namespace SharpUnits.Tests;
/// <summary>Exact fractional dimension exponent tests for Java parity JP-003.</summary>
public sealed class RationalDimensionTests
{
 [Fact] public void RootOfVoltageHasFractionalExponents()
 {
  var voltage=RationalDimension.From(Dimension.Voltage);
  var root=voltage.Root(2);
  Assert.Equal(new Rational(1,2),root.Mass);
  Assert.Equal(new Rational(-3,2),root.Time);
  Assert.Equal(voltage,root.Pow(2));
 }
 [Fact] public void RootOfCurrentSquaredReturnsCurrent()
 {
  var current=RationalDimension.From(Dimension.ElectricCurrent);
  Assert.Equal(current,current.Pow(2).Root(2));
 }
 [Fact] public void RationalCanonicalization()
 {
  Assert.Equal(new Rational(1,2),new Rational(2,4));
  Assert.Equal(new Rational(-1,2),new Rational(1,-2));
 }
 [Fact] public void RejectZeroDenominator()=>Assert.Throws<DivideByZeroException>(()=>new Rational(1,0));
 [Fact] public void RejectInvalidRoot()=>Assert.Throws<ArgumentOutOfRangeException>(()=>RationalDimension.From(Dimension.Voltage).Root(0));
}
