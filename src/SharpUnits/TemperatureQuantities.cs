namespace SharpUnits;
/// <summary>Absolute thermodynamic temperature in kelvin.</summary>
/// <remarks>Absolute temperatures cannot be added to other absolute temperatures in this model.</remarks>
public readonly record struct AbsoluteTemperature
{
 /// <summary>Temperature in kelvin.</summary>
 public double Kelvin {get;}
 /// <summary>Creates an absolute temperature; negative kelvin is rejected.</summary>
 public AbsoluteTemperature(double kelvin)
 {
  if(!double.IsFinite(kelvin)||kelvin<0)throw new ArgumentOutOfRangeException(nameof(kelvin));
  Kelvin=kelvin;
 }
 /// <summary>Creates an absolute temperature from Celsius.</summary>
 public static AbsoluteTemperature FromCelsius(double celsius)=>new(UnitConversion.Celsius.ToSi(celsius));
 /// <summary>Subtracts absolute temperatures to obtain a temperature difference.</summary>
 public static TemperatureDifference operator -(AbsoluteTemperature a,AbsoluteTemperature b)=>new(a.Kelvin-b.Kelvin);
 /// <summary>Adds a temperature difference to an absolute temperature.</summary>
 public static AbsoluteTemperature operator +(AbsoluteTemperature a,TemperatureDifference delta)=>new(a.Kelvin+delta.Kelvin);
 /// <summary>Subtracts a temperature difference from an absolute temperature.</summary>
 public static AbsoluteTemperature operator -(AbsoluteTemperature a,TemperatureDifference delta)=>new(a.Kelvin-delta.Kelvin);
}
/// <summary>Signed temperature difference, expressed in kelvin increments.</summary>
public readonly record struct TemperatureDifference(double Kelvin)
{
 /// <summary>Adds two temperature differences.</summary>
 public static TemperatureDifference operator +(TemperatureDifference a,TemperatureDifference b)=>new(a.Kelvin+b.Kelvin);
 /// <summary>Subtracts temperature differences.</summary>
 public static TemperatureDifference operator -(TemperatureDifference a,TemperatureDifference b)=>new(a.Kelvin-b.Kelvin);
}
