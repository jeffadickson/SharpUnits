using System.Numerics;
namespace SharpUnits;
/// <summary>Finite angular frequency in radians per second.</summary>
public readonly record struct AngularFrequency
{
 /// <summary>Radians per second.</summary>
 public double RadiansPerSecond {get;}
 /// <summary>Constructs a validated frequency.</summary>
 public AngularFrequency(double value){if(!double.IsFinite(value)||value<0)throw new ArgumentOutOfRangeException(nameof(value));RadiansPerSecond=value;}
 /// <summary>Converts hertz to angular frequency.</summary>
 public static AngularFrequency FromHertz(double hertz)=>new(checkedFinite(2*Math.PI*hertz));
 private static double checkedFinite(double value)=>double.IsFinite(value)?value:throw new ArgumentOutOfRangeException(nameof(value));
}
/// <summary>Complex impedance in ohms.</summary>
public readonly record struct Impedance
{
 /// <summary>Complex ohms.</summary>
 public Complex Ohms {get;}
 /// <summary>Constructs a finite impedance.</summary>
 public Impedance(Complex ohms){if(!double.IsFinite(ohms.Real)||!double.IsFinite(ohms.Imaginary))throw new ArgumentOutOfRangeException(nameof(ohms));Ohms=ohms;}
 /// <summary>Series combination.</summary>
 public static Impedance operator +(Impedance a,Impedance b)=>new(a.Ohms+b.Ohms);
 /// <summary>Parallel combination.</summary>
 public static Impedance Parallel(Impedance a,Impedance b)=>a.Ohms==Complex.Zero||b.Ohms==Complex.Zero?new(Complex.Zero):new(a.Ohms*b.Ohms/(a.Ohms+b.Ohms));
}
/// <summary>Complex RMS voltage.</summary>
public readonly record struct VoltagePhasor(Complex Volts)
{
 /// <summary>AC Ohm's law.</summary>
 public static CurrentPhasor operator /(VoltagePhasor v,Impedance z)=>z.Ohms==Complex.Zero?throw new DivideByZeroException():new(v.Volts/z.Ohms);
}
/// <summary>Complex RMS current.</summary>
public readonly record struct CurrentPhasor(Complex Amperes);
/// <summary>Ideal lumped-element AC impedance factories.</summary>
public static class AcElectrical
{
 /// <summary>Resistor impedance.</summary>
 public static Impedance Resistor(Resistance r)=>new(new Complex(r.Ohms,0));
 /// <summary>Inductor impedance jωL.</summary>
 public static Impedance Inductor(Inductance l,AngularFrequency f)=>new(new Complex(0,f.RadiansPerSecond*l.Henries));
 /// <summary>Capacitor impedance 1/(jωC).</summary>
 public static Impedance Capacitor(Capacitance c,AngularFrequency f)
 {
  if(c.Farads<=0||f.RadiansPerSecond<=0)throw new ArgumentOutOfRangeException(nameof(c));
  return new(new Complex(0,-1/(f.RadiansPerSecond*c.Farads)));
 }
}
