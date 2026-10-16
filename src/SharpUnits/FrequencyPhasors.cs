using System.Numerics;
namespace SharpUnits;
/// <summary>RMS voltage phasor tagged with its positive frequency in hertz.</summary>
public readonly record struct FrequencyVoltage
{
 /// <summary>RMS phasor.</summary>
 public VoltagePhasor Voltage {get;}
 /// <summary>Frequency in hertz.</summary>
 public double Hertz {get;}
 /// <summary>Creates a frequency-tagged voltage.</summary>
 public FrequencyVoltage(VoltagePhasor voltage,double hertz)
 {
  if(!double.IsFinite(hertz)||hertz<=0)throw new ArgumentOutOfRangeException(nameof(hertz));
  if(!double.IsFinite(voltage.Volts.Real)||!double.IsFinite(voltage.Volts.Imaginary))throw new ArgumentOutOfRangeException(nameof(voltage));
  Voltage=voltage;Hertz=hertz;
 }
 /// <summary>Computes current through a same-frequency impedance.</summary>
 public FrequencyCurrent Divide(FrequencyImpedance impedance)
 {
  if(Hertz!=impedance.Hertz)throw new InvalidOperationException("Phasor frequency mismatch.");
  return new FrequencyCurrent(Voltage/impedance.Impedance,Hertz);
 }
}
/// <summary>RMS current phasor tagged with frequency.</summary>
public readonly record struct FrequencyCurrent
{
 /// <summary>RMS phasor.</summary>
 public CurrentPhasor Current {get;}
 /// <summary>Frequency in hertz.</summary>
 public double Hertz {get;}
 /// <summary>Creates a frequency-tagged current.</summary>
 public FrequencyCurrent(CurrentPhasor current,double hertz)
 {
  if(!double.IsFinite(hertz)||hertz<=0)throw new ArgumentOutOfRangeException(nameof(hertz));
  if(!double.IsFinite(current.Amperes.Real)||!double.IsFinite(current.Amperes.Imaginary))throw new ArgumentOutOfRangeException(nameof(current));
  Current=current;Hertz=hertz;
 }
}
/// <summary>Complex impedance tagged with frequency.</summary>
public readonly record struct FrequencyImpedance
{
 /// <summary>Complex impedance.</summary>
 public Impedance Impedance {get;}
 /// <summary>Frequency in hertz.</summary>
 public double Hertz {get;}
 /// <summary>Creates a frequency-tagged impedance.</summary>
 public FrequencyImpedance(Impedance impedance,double hertz)
 {
  if(!double.IsFinite(hertz)||hertz<=0)throw new ArgumentOutOfRangeException(nameof(hertz));
  Impedance=impedance;Hertz=hertz;
 }
 /// <summary>Ideal inductor impedance at the specified frequency.</summary>
 public static FrequencyImpedance Inductor(Inductance inductance,double hertz)=>
  new(AcElectrical.Inductor(inductance,AngularFrequency.FromHertz(hertz)),hertz);
}
/// <summary>RMS complex power S=V times conjugate(I).</summary>
public readonly record struct ComplexPower
{
 /// <summary>Complex apparent power in volt-amperes.</summary>
 public Complex VoltAmperes {get;}
 /// <summary>Real power in watts.</summary>
 public double Watts=>VoltAmperes.Real;
 /// <summary>Reactive power in vars.</summary>
 public double Vars=>VoltAmperes.Imaginary;
 /// <summary>Apparent power magnitude in volt-amperes.</summary>
 public double ApparentVoltAmperes=>VoltAmperes.Magnitude;
 /// <summary>Constructs finite complex power.</summary>
 public ComplexPower(Complex value)
 {
  if(!double.IsFinite(value.Real)||!double.IsFinite(value.Imaginary))throw new ArgumentOutOfRangeException(nameof(value));
  VoltAmperes=value;
 }
 /// <summary>Computes complex power from same-frequency RMS phasors.</summary>
 public static ComplexPower FromRms(FrequencyVoltage voltage,FrequencyCurrent current)
 {
  if(voltage.Hertz!=current.Hertz)throw new InvalidOperationException("Phasor frequency mismatch.");
  return new(voltage.Voltage.Volts*Complex.Conjugate(current.Current.Amperes));
 }
}
