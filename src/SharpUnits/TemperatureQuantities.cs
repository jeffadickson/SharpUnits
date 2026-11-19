namespace SharpUnits;
/// <summary>Absolute thermodynamic temperature in kelvin.</summary>
/// <remarks>An absolute temperature behaves like a point: subtracting another absolute
/// yields a <see cref="TemperatureDifference"/>, while adding a difference yields an absolute.
/// No operator adds two absolute temperatures. For example, 20 degrees Celsius plus
/// a 10-kelvin difference is 30 degrees Celsius (303.15 kelvin).
/// This physical arithmetic policy differs from <see cref="JavaQuantityAdapter"/>,
/// which follows Indriya's permissive absolute arithmetic.</remarks>
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
/// <remarks>Unit offsets do not apply to intervals: a 10-degree Celsius difference
/// is 10 kelvin or 18 degrees Fahrenheit. Convert differences using scale only,
/// rather than an absolute Celsius or Fahrenheit converter.</remarks>
public readonly record struct TemperatureDifference(double Kelvin)
{
 /// <summary>Adds two temperature differences.</summary>
 public static TemperatureDifference operator +(TemperatureDifference a,TemperatureDifference b)=>new(a.Kelvin+b.Kelvin);
 /// <summary>Subtracts temperature differences.</summary>
 public static TemperatureDifference operator -(TemperatureDifference a,TemperatureDifference b)=>new(a.Kelvin-b.Kelvin);
}
