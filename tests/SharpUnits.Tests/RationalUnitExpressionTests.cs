using SharpUnits;
using Xunit;
namespace SharpUnits.Tests;
/// <summary>Rational product-unit compatibility regression cases.</summary>
public sealed class RationalUnitExpressionTests
{
 [Fact] public void SquareRootAndSquarePreserveVoltageDimension()
 {
  var volts=RationalUnitExpression.From(UnitRegistry.CreateStandard().Resolve("V"));
  Assert.Equal(volts.Dimension,volts.Root(2).Pow(2).Dimension);
 }
 [Fact] public void MilliampTimesKiloohmHasVoltageDimensionAndUnitScale()
 {
  var units=UnitRegistry.CreateStandard();
  var product=RationalUnitExpression.From(units.Resolve("mA"))*RationalUnitExpression.From(units.Resolve("kΩ"));
  Assert.Equal(RationalDimension.From(Dimension.Voltage),product.Dimension);
  Assert.Equal(1,product.Scale,12);
 }
 [Fact] public void RejectAffineTemperatureInProduct()
 {
  Assert.Throws<InvalidOperationException>(()=>RationalUnitExpression.From(UnitRegistry.CreateStandard().Resolve("°F")));
 }
 [Fact] public void RejectNonpositiveRoot()
 {
  var volts=RationalUnitExpression.From(UnitRegistry.CreateStandard().Resolve("V"));
  Assert.Throws<ArgumentOutOfRangeException>(()=>volts.Root(0));
 }
}
