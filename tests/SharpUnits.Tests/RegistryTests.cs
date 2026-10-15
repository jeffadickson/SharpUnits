using SharpUnits;
using Xunit;
namespace SharpUnits.Tests;
public class RegistryTests
{
 [Fact] public void MilliampsToAmps()=>Assert.Equal(.25,UnitRegistry.CreateStandard().Convert(250,"mA","A"),10);
 [Fact] public void FahrenheitToCelsius()=>Assert.Equal(100,UnitRegistry.CreateStandard().Convert(212,"°F","°C"),9);
 [Fact] public void RejectIncompatible()=>Assert.Throws<InvalidOperationException>(()=>UnitRegistry.CreateStandard().Convert(1,"V","A"));
 [Fact] public void RejectDuplicateSymbol()
 {
  var r=UnitRegistry.CreateStandard();
  Assert.False(r.Register(new UnitDefinition("V",Dimension.Voltage,UnitConversion.Linear(1))));
 }
 [Fact] public void PropagateUncertainty()
 {
  var v=new Measurement(Electrical.Volts(10),.2);
  var i=new Measurement(Electrical.Amperes(2),.1);
  var p=v*i;
  Assert.Equal(20,p.Value.SiValue,10);
  Assert.Equal(Math.Sqrt(1.16),p.StandardUncertainty,10);
 }
}