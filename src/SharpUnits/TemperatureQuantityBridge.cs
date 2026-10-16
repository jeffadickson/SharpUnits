namespace SharpUnits;
/// <summary>Bridges temperature-specific types and generic SI quantities explicitly.</summary>
/// <remarks>The generic Quantity loses absolute-versus-difference semantics.</remarks>
public static class TemperatureQuantityBridge
{
 private static readonly RationalDimension TemperatureDimension=RationalDimension.From(new Dimension(0,0,0,0,1,0,0));
 /// <summary>Converts an absolute temperature to a generic quantity.</summary>
 public static Quantity ToQuantity(this AbsoluteTemperature temperature)=>new(temperature.Kelvin,TemperatureDimension);
 /// <summary>Converts a temperature difference to a generic quantity.</summary>
 public static Quantity ToQuantity(this TemperatureDifference difference)=>new(difference.Kelvin,TemperatureDimension);
 /// <summary>Interprets a compatible generic quantity as an absolute temperature.</summary>
 public static AbsoluteTemperature AsAbsoluteTemperature(this Quantity quantity)
 {
  RequireTemperatureDimension(quantity);
  return new AbsoluteTemperature(quantity.SiValue);
 }
 /// <summary>Interprets a compatible generic quantity as a temperature difference.</summary>
 public static TemperatureDifference AsTemperatureDifference(this Quantity quantity)
 {
  RequireTemperatureDimension(quantity);
  return new TemperatureDifference(quantity.SiValue);
 }
 private static void RequireTemperatureDimension(Quantity quantity)
 {
  if(quantity.RationalDimension!=TemperatureDimension)throw new InvalidOperationException("Expected temperature dimensions.");
 }
}
