namespace SharpUnits;
/// <summary>Unit converter supporting linear and affine transformations to SI.</summary>
/// <remarks>Absolute affine quantities such as Celsius must not be multiplied directly as unit expressions.</remarks>
public readonly record struct UnitConversion
{
 /// <summary>Multiplicative factor to coherent SI.</summary>
 public double Scale { get; }
 /// <summary>Additive offset in coherent SI units.</summary>
 public double Offset { get; }
 /// <summary>Creates a converter; scale must be finite and nonzero.</summary>
 public UnitConversion(double scale, double offset)
 {
   Scale = scale;
   Offset = offset;
   if (!double.IsFinite(scale) || scale == 0 || !double.IsFinite(offset))
      throw new ArgumentOutOfRangeException(nameof(Scale), "Scale must be finite and nonzero; offset finite.");
 }
 /// <summary>Converts a value to the coherent SI representation.</summary>
 public double ToSi(double value) => checked(value * Scale + Offset);
 /// <summary>Converts an SI value back to the source unit.</summary>
 public double FromSi(double siValue) => (siValue - Offset) / Scale;
 /// <summary>Returns the inverse affine mapping, from SI back to the original unit.</summary>
 public UnitConversion Inverse() => new(1d / Scale, -Offset / Scale);
 /// <summary>Composes this mapping with a subsequent affine mapping.</summary>
 /// <remarks>The returned mapping applies this conversion first, then the next conversion.</remarks>
 public UnitConversion Then(UnitConversion next) =>
   new(Scale * next.Scale, Offset * next.Scale + next.Offset);
 /// <summary>Linear SI scale conversion.</summary>
 public static UnitConversion Linear(double scale) => new(scale, 0);
 /// <summary>Absolute degrees Celsius to kelvin.</summary>
 public static UnitConversion Celsius => new(1, 273.15);
 /// <summary>Absolute degrees Fahrenheit to kelvin.</summary>
 public static UnitConversion Fahrenheit => new(5d/9d, 459.67*5d/9d);
}
/// <summary>Describes a named unit with dimension and conversion to coherent SI.</summary>
public sealed record UnitDefinition(string Symbol, Dimension Dimension, UnitConversion Converter)
{
 /// <summary>Convert a numerical value to an SI quantity.</summary>
 public Quantity ToQuantity(double value) => new(Converter.ToSi(value), Dimension);
 /// <summary>Read a quantity expressed in this unit.</summary>
 public double FromQuantity(Quantity quantity) =>
   quantity.Dimension == Dimension ? Converter.FromSi(quantity.SiValue)
      : throw new InvalidOperationException("Incompatible dimensions");
}