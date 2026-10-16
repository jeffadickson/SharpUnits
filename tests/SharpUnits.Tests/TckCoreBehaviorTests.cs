using SharpUnits;
using Xunit;
namespace SharpUnits.Tests;
/// <summary>Additional behavioral assertions for JSR 385 unit and quantity operations.</summary>
public sealed class TckCoreBehaviorTests
{
 [Fact] public void UnitCompatibilityIgnoresScale()
 {
  var registry=UnitRegistry.CreateStandard();
  var volts=UnitExpression.From(registry.Resolve("V"));
  var millivolts=UnitExpression.From(registry.Resolve("mV"));
  Assert.True(volts.IsCompatibleWith(millivolts));
 }
 [Fact] public void UnitInverseIsDimensionlessWithOriginal()
 {
  var registry=UnitRegistry.CreateStandard();
  var unit=UnitExpression.From(registry.Resolve("kΩ"));
  var result=unit/unit;
  Assert.Equal(RationalDimension.From(Dimension.None),result.RationalDimension);
  Assert.Equal(1,result.Scale,10);
 }
 [Fact] public void QuantityMultiplicationAndDivisionRoundTrip()
 {
  var voltage=Electrical.Amperes(.25)*Electrical.Ohms(4000);
  var current=voltage/Electrical.Ohms(4000);
  Assert.Equal(Dimension.ElectricCurrent,current.Dimension);
  Assert.Equal(.25,current.SiValue,10);
 }
 [Fact] public void QuantitySubtractionRetainsDimension()
 {
  var result=Electrical.Volts(5)-Electrical.Volts(2);
  Assert.Equal(3,result.SiValue,10);
  Assert.Equal(Dimension.Voltage,result.Dimension);
 }
 [Fact] public void QuantityValueEquality()
 {
  Assert.Equal(Electrical.Volts(5),Electrical.Volts(5));
  Assert.NotEqual(Electrical.Volts(5),Electrical.Amperes(5));
 }
}
