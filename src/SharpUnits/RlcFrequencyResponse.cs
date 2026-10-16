using System.Numerics;
namespace SharpUnits;
/// <summary>Ideal series RLC network for sinusoidal steady-state analysis.</summary>
public sealed class SeriesRlcCircuit
{
 /// <summary>Series resistance.</summary>
 public Resistance Resistance {get;}
 /// <summary>Series inductance.</summary>
 public Inductance Inductance {get;}
 /// <summary>Series capacitance.</summary>
 public Capacitance Capacitance {get;}
 /// <summary>Constructs a validated circuit.</summary>
 public SeriesRlcCircuit(Resistance resistance,Inductance inductance,Capacitance capacitance)
 {
  if(!double.IsFinite(resistance.Ohms)||resistance.Ohms<0)throw new ArgumentOutOfRangeException(nameof(resistance));
  if(!double.IsFinite(inductance.Henries)||inductance.Henries<=0)throw new ArgumentOutOfRangeException(nameof(inductance));
  if(!double.IsFinite(capacitance.Farads)||capacitance.Farads<=0)throw new ArgumentOutOfRangeException(nameof(capacitance));
  Resistance=resistance;Inductance=inductance;Capacitance=capacitance;
 }
 /// <summary>Resonance frequency in hertz.</summary>
 public double ResonantHertz=>1/(2*Math.PI*Math.Sqrt(Inductance.Henries*Capacitance.Farads));
 /// <summary>Quality factor at resonance.</summary>
 public double QualityFactor=>Resistance.Ohms==0?double.PositiveInfinity:2*Math.PI*ResonantHertz*Inductance.Henries/Resistance.Ohms;
 /// <summary>Complex series impedance at frequency in hertz.</summary>
 public Impedance ImpedanceAt(double hertz)
 {
  if(!double.IsFinite(hertz)||hertz<=0)throw new ArgumentOutOfRangeException(nameof(hertz));
  double w=2*Math.PI*hertz;
  return new(new Complex(Resistance.Ohms,w*Inductance.Henries-1/(w*Capacitance.Farads)));
 }
 /// <summary>Current phasor under an RMS voltage source.</summary>
 public CurrentPhasor CurrentAt(double hertz,Complex rmsVoltage)=>new VoltagePhasor(rmsVoltage)/ImpedanceAt(hertz);
}
