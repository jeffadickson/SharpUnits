using SharpUnits;
using Xunit;
namespace SharpUnits.Tests;
/// <summary>Independent analytical checks for AC frequency-response APIs.</summary>
public sealed class BodeFrequencyResponseTests
{
 [Fact] public void RcLowPassCorner()
 {
  var r=new Resistance(1000);var c=new Capacitance(1e-6);
  var f=1/(2*Math.PI*r.Ohms*c.Farads);
  var p=BodeFrequencyResponse.RcLowPass(r,c,f);
  Assert.Equal(-10*Math.Log10(2),p.MagnitudeDecibels,8);
  Assert.Equal(-45,p.PhaseDegrees,8);
 }
 [Fact] public void RcHighPassCorner()
 {
  var r=new Resistance(1000);var c=new Capacitance(1e-6);
  var f=1/(2*Math.PI*r.Ohms*c.Farads);
  Assert.Equal(45,BodeFrequencyResponse.RcHighPass(r,c,f).PhaseDegrees,8);
 }
 [Fact] public void RlLowPassCorner()
 {
  var r=new Resistance(100);var l=new Inductance(.01);
  var f=r.Ohms/(2*Math.PI*l.Henries);
  Assert.Equal(-45,BodeFrequencyResponse.RlLowPass(r,l,f).PhaseDegrees,8);
 }
 [Fact] public void RlcResonance()
 {
  var circuit=new SeriesRlcCircuit(new Resistance(10),new Inductance(.01),new Capacitance(1e-6));
  Assert.Equal(1591.5494309189535,circuit.ResonantHertz,7);
  Assert.Equal(0,BodeFrequencyResponse.RlcBandPass(circuit,circuit.ResonantHertz).MagnitudeDecibels,8);
 }
 [Fact] public void LogarithmicSweep()
 {
  var values=BodeFrequencyResponse.LogarithmicFrequencies(10,10000,501);
  Assert.Equal(501,values.Count);
  Assert.Equal(10,values[0]);
  Assert.Equal(10000,values[^1]);
 }
 [Fact] public void InvalidFrequencyRejected()=>Assert.Throws<ArgumentOutOfRangeException>(()=>BodeFrequencyResponse.RcLowPass(new Resistance(1),new Capacitance(1),0));
}
