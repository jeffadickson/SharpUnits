using System.Globalization;
namespace SharpUnits;

/// <summary>Whether a quantity represents an absolute value or a relative interval.</summary>
public enum JavaQuantityScale
{
 /// <summary>An absolute value, including the unit offset.</summary>
 Absolute,
 /// <summary>An interval, converted using only the unit scale factor.</summary>
 Relative
}

/// <summary>JSR 385-inspired .NET quantity adapter retaining the display unit.</summary>
/// <remarks>Preserves the coherent-SI Quantity model. This is a CLR analogue, not Java binary compatibility.
/// Arithmetic follows Indriya rather than the physical policy of <see cref="AbsoluteTemperature"/>
/// and <see cref="TemperatureDifference"/>. Two absolute Celsius operands are added in kelvin
/// and converted back: 20 degrees Celsius plus 10 degrees Celsius yields 303.15 degrees Celsius
/// (576.30 kelvin). For heating by ten degrees, mark the second operand
/// <see cref="JavaQuantityScale.Relative"/>; the result is then 30 degrees Celsius.
/// The standard Fahrenheit converter is evaluated using its defining 5/9 scale and 459.67-degree shift.
/// Other converters use their supplied coefficients. This does not implement general Java Number precision.</remarks>
public sealed class JavaQuantityAdapter : IEquatable<JavaQuantityAdapter>, IComparable<JavaQuantityAdapter>
{
 private readonly Quantity value;
 private readonly UnitDefinition unit;
 private readonly double displayMagnitude;
 /// <summary>Explicit absolute or relative scale.</summary>
 public JavaQuantityScale Scale {get;}
 /// <summary>Display unit.</summary>
 public UnitDefinition Unit=>unit;
 /// <summary>Magnitude expressed in the display unit, retaining the supplied double representation.</summary>
 public double Value=>displayMagnitude;
 /// <summary>Coherent SI representation in double precision.</summary>
 /// <remarks>Scaling and offsets may round or underflow independently of the retained display magnitude.
 /// This property does not retain Java arbitrary Number precision.</remarks>
 public Quantity SiQuantity=>value;
 /// <summary>Creates a quantity with the specified display unit.</summary>
 public JavaQuantityAdapter(double magnitude,UnitDefinition unit,JavaQuantityScale scale=JavaQuantityScale.Absolute)
 {
  var coherent=ToCoherent(magnitude,unit,scale);
  if(!double.IsFinite(coherent.SiValue))throw new ArgumentOutOfRangeException(nameof(magnitude),"Coherent representation must be finite.");
  value=coherent;this.unit=unit;Scale=scale;displayMagnitude=magnitude;
 }
 private static Quantity ToCoherent(double magnitude,UnitDefinition unit,JavaQuantityScale scale)
 {
  ArgumentNullException.ThrowIfNull(unit);
  if(!Enum.IsDefined(scale))throw new ArgumentOutOfRangeException(nameof(scale));
  if(!double.IsFinite(magnitude))throw new ArgumentOutOfRangeException(nameof(magnitude));
  return new Quantity(JavaDoubleArithmetic.Convert(magnitude,unit.Converter,UnitConversion.Linear(1),
   scale==JavaQuantityScale.Relative),unit.RationalDimension);
 }
 /// <summary>Converts to a compatible display unit, preserving the scale kind.</summary>
 public JavaQuantityAdapter To(UnitDefinition target)
 {
  ArgumentNullException.ThrowIfNull(target);
  if(unit.RationalDimension!=target.RationalDimension)throw new InvalidOperationException("Incompatible dimensions.");
  if(unit.Equals(target))return this;
  return new(JavaDoubleArithmetic.Convert(Value,unit.Converter,target.Converter,Scale==JavaQuantityScale.Relative),target,Scale);
 }
 /// <summary>Checks a registered semantic kind against the quantity dimension.</summary>
 public JavaQuantityAdapter AsType(QuantityKind kind,QuantityKindRegistry registry)
 {
  ArgumentNullException.ThrowIfNull(registry);
  if(value.RationalDimension!=registry.DimensionOf(kind))throw new InvalidOperationException("Quantity kind dimensions disagree.");
  return this;
 }
 /// <summary>Checks equivalence using Indriya's left-unit comparison semantics.</summary>
 /// <remarks>Scale is not compared directly. The right operand is converted according to its own scale.
 /// Incompatible dimensions return false as a .NET convenience.</remarks>
 public bool IsEquivalentTo(JavaQuantityAdapter? other)=>
  other is not null&&value.RationalDimension==other.value.RationalDimension&&CompareTo(other)==0;
 /// <summary>Adds compatible quantities with Indriya scale semantics; only two relative operands yield relative scale.</summary>
 /// <remarks>Two absolute operands are permitted for Indriya compatibility, including affine units.
 /// Use a relative second operand for a temperature increase, or use the dedicated temperature types
 /// to enforce physical absolute-versus-difference arithmetic.</remarks>
 public JavaQuantityAdapter Add(JavaQuantityAdapter other)
 {
  RequireCompatible(other);
  return AddOrSubtract(other,false);
 }
 /// <summary>Subtracts compatible quantities using Indriya scale semantics.</summary>
 /// <remarks>Absolute minus absolute remains absolute, including for affine units.
 /// Use the dedicated temperature APIs when a physical temperature interval is required.</remarks>
 public JavaQuantityAdapter Subtract(JavaQuantityAdapter other)
 {
  RequireCompatible(other);
  return AddOrSubtract(other,true);
 }
 private JavaQuantityAdapter AddOrSubtract(JavaQuantityAdapter other,bool subtract)
 {
  var resultScale=ResultScale(other);
  return new(JavaDoubleArithmetic.Add(Value,other.Value,unit.Converter,other.unit.Converter,
   Scale==JavaQuantityScale.Absolute,other.Scale==JavaQuantityScale.Absolute,
   resultScale==JavaQuantityScale.Absolute,subtract),unit,resultScale);
 }
 private JavaQuantityScale ResultScale(JavaQuantityAdapter other)=>
  Scale==JavaQuantityScale.Relative&&other.Scale==JavaQuantityScale.Relative
   ?JavaQuantityScale.Relative:JavaQuantityScale.Absolute;
 /// <summary>Multiplies quantities, retaining linear display-unit factors and converting affine operands to SI.</summary>
 /// <remarks>Identical A, V and ohm operands expose their squared-unit representation.
 /// General compound-unit identity and normalization remain incomplete.</remarks>
 public JavaQuantityAdapter Multiply(JavaQuantityAdapter other)
 {
  ArgumentNullException.ThrowIfNull(other);
  return Combine(other,false);
 }
 /// <summary>Divides quantities, retaining linear display-unit factors and converting affine operands to SI.</summary>
 /// <remarks>Identical operand units cancel to the dimensionless unit representation.</remarks>
 public JavaQuantityAdapter Divide(JavaQuantityAdapter other)
 {
  ArgumentNullException.ThrowIfNull(other);
  return Combine(other,true);
 }
 private JavaQuantityAdapter Combine(JavaQuantityAdapter other,bool divide)
 {
  var left=MultiplicativeOperand();var right=other.MultiplicativeOperand();
  if(divide&&right.Magnitude==0)throw new DivideByZeroException();
  var dimension=divide?unit.RationalDimension-other.unit.RationalDimension:unit.RationalDimension+other.unit.RationalDimension;
  var factor=divide?left.Unit.Converter.Scale/right.Unit.Converter.Scale:left.Unit.Converter.Scale*right.Unit.Converter.Scale;
  var sameUnit=left.Unit.Equals(right.Unit);
  var knownSquare=sameUnit&&!divide&&left.Unit.Symbol is "A" or "V" or "Ω";
  var symbol=sameUnit&&divide?"1":knownSquare?$"{left.Unit.Symbol}²"
   :$"({left.Unit.Symbol}{(divide?"/":"*")}{right.Unit.Symbol})";
  var resultUnit=new UnitDefinition(symbol,dimension,UnitConversion.Linear(factor));
  return new(divide?left.Magnitude/right.Magnitude:left.Magnitude*right.Magnitude,resultUnit,JavaQuantityScale.Absolute);
 }
 private (double Magnitude,UnitDefinition Unit) MultiplicativeOperand()=>unit.Converter.IsLinear
  ?(Value,unit):(value.SiValue,DerivedUnit(unit.RationalDimension));
 /// <summary>Multiplies by a boxed numeric scalar, converted to double.</summary>
 /// <remarks>Accepts built-in integer, floating-point and decimal types. Strings, booleans,
 /// characters, enums and other IConvertible implementations are not numeric scalar arguments.
 /// Decimal and large integer precision may be lost when converted to double.</remarks>
 public JavaQuantityAdapter Multiply(IConvertible scalar)
 {
  return ScaleBy(NumericScalar(scalar),false);
 }
 /// <summary>Divides by a boxed numeric scalar, converted to double.</summary>
 /// <remarks>Uses the same built-in numeric type restrictions as scalar multiplication.</remarks>
 public JavaQuantityAdapter Divide(IConvertible scalar)
 {
  return ScaleBy(NumericScalar(scalar),true);
 }
 private static double NumericScalar(IConvertible scalar)
 {
  ArgumentNullException.ThrowIfNull(scalar);
  if(scalar is not (sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal))
   throw new ArgumentException("A built-in numeric scalar is required.",nameof(scalar));
  return scalar.ToDouble(CultureInfo.InvariantCulture);
 }
 private JavaQuantityAdapter ScaleBy(double factor,bool divide)
 {
  if(!double.IsFinite(factor))throw new ArgumentOutOfRangeException(nameof(factor));
  if(divide&&factor==0)throw new DivideByZeroException();
  if(factor==1)return this;
  if(divide||(Scale==JavaQuantityScale.Absolute&&!unit.Converter.IsLinear))
   return new(JavaDoubleArithmetic.Scale(Value,factor,unit.Converter,Scale==JavaQuantityScale.Relative,divide),unit,Scale);
  return new(Value*factor,unit,Scale);
 }
 /// <summary>Returns the inverse of a linear quantity, retaining scale and reciprocal display-unit factor.</summary>
 /// <remarks>For example, 250 mA becomes 0.004 (1/mA), whose coherent magnitude is 4 (1/A).
 /// Absolute affine inversion is rejected. Relative affine inversion uses a linear reciprocal factor
 /// and does not preserve Java's non-linear inverse unit representation.</remarks>
 public JavaQuantityAdapter Inverse()
 {
  RequireLinearAlgebra(this);
  if(Value==0)throw new DivideByZeroException();
  var dimension=RationalDimension.From(Dimension.None)-unit.RationalDimension;
  var reciprocalUnit=new UnitDefinition($"1/({unit.Symbol})",dimension,
   UnitConversion.Linear(1d/unit.Converter.Scale));
  return new(JavaDoubleArithmetic.Reciprocal(Value),reciprocalUnit,Scale);
 }
 /// <summary>Compares display values after converting the right operand to this unit, as Indriya does.</summary>
 public int CompareTo(JavaQuantityAdapter? other)
 {
  if(other is null)return 1;
  RequireCompatible(other);
  return Value.CompareTo(other.To(unit).Value);
 }
 /// <summary>Equality includes representation, unlike physical equivalence.</summary>
 public bool Equals(JavaQuantityAdapter? other)=>other is not null&&Value.Equals(other.Value)&&unit.Equals(other.unit)&&Scale==other.Scale;
 /// <summary>Object equality.</summary>
 public override bool Equals(object? obj)=>obj is JavaQuantityAdapter other&&Equals(other);
 /// <summary>Hash of quantity and representation.</summary>
 public override int GetHashCode()=>HashCode.Combine(Value,unit,Scale);
 /// <summary>Invariant magnitude and display symbol.</summary>
 public override string ToString()=>$"{Value.ToString("R",CultureInfo.InvariantCulture)} {unit.Symbol}";
 private void RequireCompatible(JavaQuantityAdapter other)
 {
  ArgumentNullException.ThrowIfNull(other);
  if(value.RationalDimension!=other.value.RationalDimension)throw new InvalidOperationException("Incompatible dimensions.");
 }
 private static void RequireLinearAlgebra(JavaQuantityAdapter q)
 {
  if(q.Scale==JavaQuantityScale.Absolute&&q.unit.Converter.Offset!=0)
   throw new InvalidOperationException("Absolute affine quantities cannot participate in multiplicative algebra.");
 }
 private static UnitDefinition DerivedUnit(RationalDimension dimension)=>new("SI-derived",dimension,UnitConversion.Linear(1));
}
