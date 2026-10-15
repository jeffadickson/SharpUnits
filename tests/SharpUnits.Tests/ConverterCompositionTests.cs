using SharpUnits;
using Xunit;
namespace SharpUnits.Tests;
/// <summary>Java-inspired converter composition and inversion behavioral tests.</summary>
public sealed class ConverterCompositionTests
{
 [Theory]
 [InlineData(-40)]
 [InlineData(0)]
 [InlineData(32)]
 [InlineData(212)]
 public void FahrenheitInverseRoundTrip(double value)
 {
  var converter=UnitConversion.Fahrenheit;
  Assert.Equal(value,converter.Inverse().ToSi(converter.ToSi(value)),9);
 }
 [Fact] public void CompositionPreservesAffineOrder()
 {
  var first=new UnitConversion(2,3);
  var second=new UnitConversion(5,7);
  var combined=first.Then(second);
  Assert.Equal(second.ToSi(first.ToSi(11)),combined.ToSi(11),10);
 }
 [Fact] public void InverseCompositionIsIdentity()
 {
  var converter=UnitConversion.Celsius;
  var identity=converter.Then(converter.Inverse());
  Assert.Equal(0,identity.Offset,10);
  Assert.Equal(1,identity.Scale,10);
 }
 [Fact] public void RejectNonfiniteScale()
 {
  Assert.Throws<ArgumentOutOfRangeException>(()=>new UnitConversion(double.NaN,0));
  Assert.Throws<ArgumentOutOfRangeException>(()=>new UnitConversion(double.PositiveInfinity,0));
 }
}
