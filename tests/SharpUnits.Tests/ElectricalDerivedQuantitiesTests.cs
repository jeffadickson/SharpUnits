using SharpUnits;
using Xunit;
namespace SharpUnits.Tests;
/// <summary>Typed electrical identities and dimensional regression tests.</summary>
public sealed class ElectricalDerivedQuantitiesTests
{
 [Fact] public void CurrentTimesTimeProducesCharge()
 {
  var charge=ElectricalRelations.Charge(new Current(.25),new Duration(4));
  Assert.Equal(1,charge.Coulombs,12);
  Assert.Equal(.25,(charge/new Duration(4)).Amperes,12);
 }
 [Fact] public void CapacitanceTimesVoltageProducesCharge()
 {
  var charge=new Capacitance(.000001)*new Voltage(5);
  Assert.Equal(.000005,charge.Coulombs,12);
  Assert.Equal(.000001,(charge/new Voltage(5)).Farads,12);
 }
 [Fact] public void ConductanceTimesVoltageProducesCurrent()
 {
  var conductance=ElectricalRelations.Conductance(new Resistance(100));
  Assert.Equal(.05,(conductance*new Voltage(5)).Amperes,12);
 }
 [Fact] public void InductanceTimesCurrentProducesFluxLinkage()
 {
  Assert.Equal(.4,(new Inductance(2)*new Current(.2)).Webers,12);
 }
 [Fact] public void VoltageOverCurrentIsResistance()
 {
  Assert.Equal(100,(new Voltage(5)/new Current(.05)).Ohms,12);
 }
}
