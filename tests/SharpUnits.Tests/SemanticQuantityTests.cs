using SharpUnits;
using Xunit;
namespace SharpUnits.Tests;
/// <summary>Semantic quantity identity regression tests.</summary>
public sealed class SemanticQuantityTests
{
 [Fact] public void EnergyAndTorqueCannotBeAdded()
 {
  var kinds=QuantityKindRegistry.CreateStandard();
  var d=new Dimension(2,1,-2,0,0,0,0);
  var energy=new SemanticQuantity(new Quantity(2,d),QuantityKind.Energy,kinds);
  var torque=new SemanticQuantity(new Quantity(3,d),QuantityKind.Torque,kinds);
  Assert.Throws<InvalidOperationException>(()=>energy.Add(torque,kinds));
 }
 [Fact] public void CurrentTimesResistanceProducesVoltage()
 {
  var k=QuantityKindRegistry.CreateStandard();
  var i=new SemanticQuantity(Electrical.Amperes(.25),QuantityKind.Current,k);
  var r=new SemanticQuantity(Electrical.Ohms(4000),QuantityKind.Resistance,k);
  Assert.Equal(1000,i.MultiplyAs(r,QuantityKind.Voltage,k).Value.SiValue,10);
 }
 [Fact] public void IncorrectKindDimensionRejected()
 {
  var k=QuantityKindRegistry.CreateStandard();
  Assert.Throws<InvalidOperationException>(()=>new SemanticQuantity(Electrical.Volts(1),QuantityKind.Current,k));
 }
}
