using System.Numerics;
using SharpUnits;
using Xunit;
namespace SharpUnits.Tests;
/// <summary>Frequency-tagged AC phasor and complex-power tests.</summary>
public sealed class FrequencyPhasorTests
{
 [Fact] public void ResistiveLoadHasRealPower()
 {
  var v=new FrequencyVoltage(new VoltagePhasor(new Complex(10,0)),50);
  var i=new FrequencyCurrent(new CurrentPhasor(new Complex(2,0)),50);
  var p=ComplexPower.FromRms(v,i);
  Assert.Equal(20,p.Watts,12);
  Assert.Equal(0,p.Vars,12);
 }
 [Fact] public void IdealInductorHasPositiveReactivePower()
 {
  var v=new FrequencyVoltage(new VoltagePhasor(new Complex(10,0)),50);
  var z=FrequencyImpedance.Inductor(new Inductance(.1),50);
  var p=ComplexPower.FromRms(v,v.Divide(z));
  Assert.Equal(0,p.Watts,10);
  Assert.True(p.Vars>0);
 }
 [Fact] public void MismatchedFrequencyRejected()
 {
  var v=new FrequencyVoltage(new VoltagePhasor(new Complex(10,0)),50);
  var i=new FrequencyCurrent(new CurrentPhasor(new Complex(2,0)),60);
  Assert.Throws<InvalidOperationException>(()=>ComplexPower.FromRms(v,i));
  Assert.Throws<InvalidOperationException>(()=>v.Divide(new FrequencyImpedance(new Impedance(new Complex(10,0)),60)));
 }
 [Fact] public void RejectNonpositiveFrequency()=>Assert.Throws<ArgumentOutOfRangeException>(()=>new FrequencyVoltage(new VoltagePhasor(Complex.One),0));
}
