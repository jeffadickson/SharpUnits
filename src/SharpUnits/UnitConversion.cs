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
 /// <summary>True if the converter leaves every finite value unchanged.</summary>
 public bool IsIdentity => Scale == 1d && Offset == 0d;
 /// <summary>True if the conversion has no additive offset.</summary>
 public bool IsLinear => Offset == 0d;
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
/// <summary>Describes a named unit with canonical rational dimensions and conversion to coherent SI.</summary>
public sealed record UnitDefinition
{
 /// <summary>Unit symbol.</summary>
 public string Symbol { get; }
 /// <summary>Canonical rational dimension vector.</summary>
 public RationalDimension RationalDimension { get; }
 /// <summary>Legacy integer dimension; only valid for integral exponents.</summary>
 public Dimension Dimension => RationalDimension.ToIntegerDimension();
 /// <summary>Conversion to coherent SI.</summary>
 /// <remarks>This is a double projection. Java adapter operations can retain an exact linear
 /// factor on parser-created units; legacy conversion APIs continue using this projection.</remarks>
 public UnitConversion Converter { get; }
 // Non-null only when a parsed rational factor differs from its round-trip-decimal
 // double projection. Record equality then distinguishes units with different exact semantics.
 internal Rational? ExactLinearScale { get; init; }
 internal static UnitDefinition FromExactLinear(string symbol,RationalDimension dimension,Rational scale)
 {
  var approximation=JavaDoubleArithmetic.ToDouble(scale);
  var converter=UnitConversion.Linear(approximation);
  return new UnitDefinition(symbol,dimension,converter)
  {
   ExactLinearScale=JavaDoubleArithmetic.Fraction(approximation)==scale?null:scale
  };
 }
 /// <summary>Constructs from legacy integral dimensions.</summary>
 public UnitDefinition(string symbol,Dimension dimension,UnitConversion converter):this(symbol,RationalDimension.From(dimension),converter){}
 /// <summary>Constructs from canonical rational dimensions.</summary>
 public UnitDefinition(string symbol,RationalDimension dimension,UnitConversion converter)
 {
  Symbol=symbol ?? throw new ArgumentNullException(nameof(symbol));
  RationalDimension=dimension;
  Converter=converter;
 }
 /// <summary>Converts a value into an SI quantity.</summary>
 public Quantity ToQuantity(double value)=>new(Converter.ToSi(value),RationalDimension);
 /// <summary>Converts a compatible SI quantity to this unit.</summary>
 public double FromQuantity(Quantity quantity)=>quantity.RationalDimension==RationalDimension
  ?Converter.FromSi(quantity.SiValue):throw new InvalidOperationException("Incompatible dimensions");
}
