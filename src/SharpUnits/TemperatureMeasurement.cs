namespace SharpUnits;
/// <summary>Distinguishes absolute temperatures from differences.</summary>
public enum TemperatureKind
{
 /// <summary>Thermodynamic absolute temperature.</summary>
 Absolute,
 /// <summary>Signed temperature interval.</summary>
 Difference
}
/// <summary>Temperature value preserving its physical interpretation.</summary>
public readonly record struct TemperatureMeasurement
{
 /// <summary>Magnitude in kelvin.</summary>
 public double Kelvin {get;}
 /// <summary>Absolute or difference kind.</summary>
 public TemperatureKind Kind {get;}
 /// <summary>Constructs a validated measurement.</summary>
 public TemperatureMeasurement(double kelvin,TemperatureKind kind)
 {
  if(!Enum.IsDefined(kind))throw new ArgumentOutOfRangeException(nameof(kind));
  if(!double.IsFinite(kelvin)||kind==TemperatureKind.Absolute&&kelvin<0)throw new ArgumentOutOfRangeException(nameof(kelvin));
  Kelvin=kelvin;Kind=kind;
 }
 /// <summary>Wraps an absolute temperature.</summary>
 public static TemperatureMeasurement From(AbsoluteTemperature value)=>new(value.Kelvin,TemperatureKind.Absolute);
 /// <summary>Wraps a temperature difference.</summary>
 public static TemperatureMeasurement From(TemperatureDifference value)=>new(value.Kelvin,TemperatureKind.Difference);
 /// <summary>Converts to absolute temperature, rejecting differences.</summary>
 public AbsoluteTemperature AsAbsolute()=>Kind==TemperatureKind.Absolute?new(Kelvin):throw new InvalidOperationException("Not absolute.");
 /// <summary>Converts to temperature difference, rejecting absolutes.</summary>
 public TemperatureDifference AsDifference()=>Kind==TemperatureKind.Difference?new(Kelvin):throw new InvalidOperationException("Not a difference.");
 /// <summary>Converts to dimension-only quantity, discarding semantic kind.</summary>
 public Quantity ToDimensionOnlyQuantity()=>new(Kelvin,new Dimension(0,0,0,0,1,0,0));
 /// <summary>Adds compatible absolute/difference operands.</summary>
 public static TemperatureMeasurement operator +(TemperatureMeasurement a,TemperatureMeasurement b)
 {
  if(a.Kind==TemperatureKind.Absolute&&b.Kind==TemperatureKind.Difference)return From(a.AsAbsolute()+b.AsDifference());
  if(a.Kind==TemperatureKind.Difference&&b.Kind==TemperatureKind.Absolute)return From(b.AsAbsolute()+a.AsDifference());
  if(a.Kind==TemperatureKind.Difference&&b.Kind==TemperatureKind.Difference)return From(a.AsDifference()+b.AsDifference());
  throw new InvalidOperationException("Two absolute temperatures cannot be added.");
 }
 /// <summary>Subtracts temperatures using physically meaningful result kinds.</summary>
 public static TemperatureMeasurement operator -(TemperatureMeasurement a,TemperatureMeasurement b)
 {
  if(a.Kind==TemperatureKind.Absolute&&b.Kind==TemperatureKind.Absolute)return From(a.AsAbsolute()-b.AsAbsolute());
  if(a.Kind==TemperatureKind.Absolute&&b.Kind==TemperatureKind.Difference)return From(a.AsAbsolute()-b.AsDifference());
  if(a.Kind==TemperatureKind.Difference&&b.Kind==TemperatureKind.Difference)return From(a.AsDifference()-b.AsDifference());
  throw new InvalidOperationException("Difference minus absolute is undefined.");
 }
}
