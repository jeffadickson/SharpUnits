using SharpUnits;
using Xunit;
namespace SharpUnits.Tests;
/// <summary>Regression tests for Java-style product units and canonical dimensions.</summary>
public sealed class UnitExpressionTests
{
 [Fact] public void CurrentTimesResistanceIsVoltage()
 {
  var r=UnitRegistry.CreateStandard();
  var result=UnitExpression.From(r.Resolve("mA"))*UnitExpression.From(r.Resolve("kΩ"));
  Assert.Equal(Dimension.Voltage,result.Dimension);
  Assert.Equal(1,result.Scale,12);
 }
 [Fact] public void VoltageDividedByResistanceIsCurrent()
 {
  var r=UnitRegistry.CreateStandard();
  var result=UnitExpression.From(r.Resolve("V"))/UnitExpression.From(r.Resolve("Ω"));
  Assert.Equal(Dimension.ElectricCurrent,result.Dimension);
 }
 [Fact] public void MultiplicationWithAffineUnitsRejected()
 {
  Assert.Throws<InvalidOperationException>(()=>UnitExpression.From(UnitRegistry.CreateStandard().Resolve("°C")));
 }
 [Fact] public void ExponentZeroIsDimensionless()
 {
  var r=UnitRegistry.CreateStandard();
  var result=UnitExpression.From(r.Resolve("kΩ")).Pow(0);
  Assert.Equal(Dimension.None,result.Dimension);
  Assert.Equal(1,result.Scale,12);
 }
 [Fact] public void ReciprocalCancels()
 {
  var r=UnitRegistry.CreateStandard();
  var unit=UnitExpression.From(r.Resolve("mA"));
  var result=unit/unit;
  Assert.Equal(Dimension.None,result.Dimension);
  Assert.Equal(1,result.Scale,12);
 }
}