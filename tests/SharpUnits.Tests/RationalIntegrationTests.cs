using SharpUnits;
using Xunit;
namespace SharpUnits.Tests;
/// <summary>Regression coverage for canonical rational dimensions across public unit APIs.</summary>
public sealed class RationalIntegrationTests
{
 [Fact] public void LegacyUnitExpressionSupportsFractionalRoot()
 {
  var unit=UnitExpression.From(UnitRegistry.CreateStandard().Resolve("V"));
  var root=unit.Root(2);
  Assert.Equal(new Rational(1,2),root.RationalDimension.Mass);
  Assert.Throws<InvalidOperationException>(()=>_ = root.Dimension);
  Assert.Equal(Dimension.Voltage,root.Pow(2).Dimension);
 }
 [Fact] public void FractionalUnitDefinitionRoundTrip()
 {
  var dimension=RationalDimension.From(Dimension.Voltage).Root(2);
  var unit=new UnitDefinition("sqrtV",dimension,UnitConversion.Linear(2));
  var q=unit.ToQuantity(3);
  Assert.Equal(dimension,q.RationalDimension);
  Assert.Equal(3,unit.FromQuantity(q),10);
 }
 [Fact] public void FractionalUnitRegistryConversion()
 {
  var d=RationalDimension.From(Dimension.Voltage).Root(2);
  var registry=new UnitRegistry();
  Assert.True(registry.Register(new UnitDefinition("sqrtV",d,UnitConversion.Linear(1))));
  Assert.True(registry.Register(new UnitDefinition("msqrtV",d,UnitConversion.Linear(.001))));
  Assert.Equal(1000,registry.Convert(1,"sqrtV","msqrtV"),10);
 }
 [Fact] public void LegacyIntegerUnitFactoryUnchanged()
 {
  var q=UnitRegistry.CreateStandard().Resolve("mA").ToQuantity(250);
  Assert.Equal(Dimension.ElectricCurrent,q.Dimension);
  Assert.Equal(.25,q.SiValue,10);
 }
}
