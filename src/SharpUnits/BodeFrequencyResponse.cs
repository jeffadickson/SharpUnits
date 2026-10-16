using System.Numerics;
namespace SharpUnits;
/// <summary>Complex voltage transfer sample and Bode representation.</summary>
public readonly record struct BodePoint
{
 /// <summary>Frequency in hertz.</summary>
 public double Hertz {get;}
 /// <summary>Dimensionless complex voltage gain.</summary>
 public Complex Gain {get;}
 /// <summary>Magnitude in decibels.</summary>
 public double MagnitudeDecibels=>Gain==Complex.Zero?double.NegativeInfinity:20*Math.Log10(Gain.Magnitude);
 /// <summary>Wrapped phase in degrees.</summary>
 public double PhaseDegrees=>Math.Atan2(Gain.Imaginary,Gain.Real)*180/Math.PI;
 /// <summary>Constructs a finite frequency-response point.</summary>
 public BodePoint(double hertz,Complex gain)
 {
  if(!double.IsFinite(hertz)||hertz<=0)throw new ArgumentOutOfRangeException(nameof(hertz));
  if(!double.IsFinite(gain.Real)||!double.IsFinite(gain.Imaginary))throw new ArgumentOutOfRangeException(nameof(gain));
  Hertz=hertz;Gain=gain;
 }
}
/// <summary>Ideal passive filter voltage transfer functions.</summary>
public static class BodeFrequencyResponse
{
 /// <summary>RC capacitor-output low pass.</summary>
 public static BodePoint RcLowPass(Resistance r,Capacitance c,double hz)
 {
  Validate(r.Ohms,c.Farads,hz);
  return new(hz,Complex.One/new Complex(1,2*Math.PI*hz*r.Ohms*c.Farads));
 }
 /// <summary>RC resistor-output high pass.</summary>
 public static BodePoint RcHighPass(Resistance r,Capacitance c,double hz)
 {
  Validate(r.Ohms,c.Farads,hz);
  var jw=new Complex(0,2*Math.PI*hz*r.Ohms*c.Farads);
  return new(hz,jw/(Complex.One+jw));
 }
 /// <summary>RL resistor-output low pass.</summary>
 public static BodePoint RlLowPass(Resistance r,Inductance l,double hz)
 {
  Validate(r.Ohms,l.Henries,hz);
  return new(hz,new Complex(r.Ohms,0)/new Complex(r.Ohms,2*Math.PI*hz*l.Henries));
 }
 /// <summary>RL inductor-output high pass.</summary>
 public static BodePoint RlHighPass(Resistance r,Inductance l,double hz)
 {
  Validate(r.Ohms,l.Henries,hz);
  var jw=new Complex(0,2*Math.PI*hz*l.Henries);
  return new(hz,jw/(new Complex(r.Ohms,0)+jw));
 }
 /// <summary>Series RLC resistor-output band pass.</summary>
 public static BodePoint RlcBandPass(SeriesRlcCircuit circuit,double hz)
 {
  ArgumentNullException.ThrowIfNull(circuit);
  var z=circuit.ImpedanceAt(hz).Ohms;
  if(z==Complex.Zero)throw new DivideByZeroException();
  return new(hz,new Complex(circuit.Resistance.Ohms,0)/z);
 }
 /// <summary>Samples frequencies in the supplied order.</summary>
 public static IReadOnlyList<BodePoint> Sweep(IEnumerable<double> frequencies,Func<double,BodePoint> response)
 {
  ArgumentNullException.ThrowIfNull(frequencies);
  ArgumentNullException.ThrowIfNull(response);
  return Array.AsReadOnly(frequencies.Select(response).ToArray());
 }
 /// <summary>Logarithmically spaced positive frequencies, including endpoints.</summary>
 public static IReadOnlyList<double> LogarithmicFrequencies(double start,double stop,int count)
 {
  if(!double.IsFinite(start)||start<=0)throw new ArgumentOutOfRangeException(nameof(start));
  if(!double.IsFinite(stop)||stop<=start)throw new ArgumentOutOfRangeException(nameof(stop));
  if(count<2||count>100000)throw new ArgumentOutOfRangeException(nameof(count));
  var values=new double[count];
  double a=Math.Log(start),b=Math.Log(stop);
  for(int i=0;i<count;i++)values[i]=Math.Exp(a+(b-a)*i/(count-1d));
  values[0]=start;values[^1]=stop;
  return Array.AsReadOnly(values);
 }
 private static void Validate(double r,double component,double hz)
 {
  if(!double.IsFinite(r)||r<=0)throw new ArgumentOutOfRangeException(nameof(r));
  if(!double.IsFinite(component)||component<=0)throw new ArgumentOutOfRangeException(nameof(component));
  if(!double.IsFinite(hz)||hz<=0)throw new ArgumentOutOfRangeException(nameof(hz));
 }
}
