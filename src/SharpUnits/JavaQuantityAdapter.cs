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
/// <remarks>Preserves the coherent-SI Quantity model. This is a CLR analogue, not Java binary compatibility.</remarks>
public sealed class JavaQuantityAdapter : IEquatable<JavaQuantityAdapter>, IComparable<JavaQuantityAdapter>
{
 private readonly Quantity value;
 private readonly UnitDefinition unit;
 /// <summary>Explicit absolute or relative scale.</summary>
 public JavaQuantityScale Scale {get;}
 /// <summary>Display unit.</summary>
 public UnitDefinition Unit=>unit;
 /// <summary>Magnitude expressed in the display unit.</summary>
 public double Value=>Scale==JavaQuantityScale.Relative && unit.Converter.Offset!=0
  ? value.SiValue/unit.Converter.Scale : unit.FromQuantity(value);
 /// <summary>Coherent SI representation.</summary>
 public Quantity SiQuantity=>value;
 /// <summary>Creates a quantity with the specified display unit.</summary>
 public JavaQuantityAdapter(double magnitude,UnitDefinition unit,JavaQuantityScale scale=JavaQuantityScale.Absolute)
  :this(ToCoherent(magnitude,unit,scale),unit,scale){}
 private static Quantity ToCoherent(double magnitude,UnitDefinition unit,JavaQuantityScale scale)
 {
  ArgumentNullException.ThrowIfNull(unit);
  if(!Enum.IsDefined(scale))throw new ArgumentOutOfRangeException(nameof(scale));
  if(!double.IsFinite(magnitude))throw new ArgumentOutOfRangeException(nameof(magnitude));
  return scale==JavaQuantityScale.Relative
   ?new Quantity(magnitude*unit.Converter.Scale,unit.RationalDimension)
   :unit.ToQuantity(magnitude);
 }
 private JavaQuantityAdapter(Quantity coherent,UnitDefinition unit,JavaQuantityScale scale)
 {
  ArgumentNullException.ThrowIfNull(unit);
  if(!Enum.IsDefined(scale))throw new ArgumentOutOfRangeException(nameof(scale));
  if(coherent.RationalDimension!=unit.RationalDimension)throw new InvalidOperationException("Quantity and unit dimensions differ.");
  if(!double.IsFinite(coherent.SiValue))throw new ArgumentOutOfRangeException(nameof(coherent));
  value=coherent;this.unit=unit;Scale=scale;
  if(!double.IsFinite(Value))throw new ArgumentOutOfRangeException(nameof(coherent));
 }
 /// <summary>Converts to a compatible display unit, preserving the scale kind.</summary>
 public JavaQuantityAdapter To(UnitDefinition target)=>new(value,target,Scale);
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
 /// <summary>Adds compatible quantities in coherent SI; only two relative operands yield relative scale.</summary>
 public JavaQuantityAdapter Add(JavaQuantityAdapter other)
 {
  RequireCompatible(other);
  return new(value+other.value,unit,ResultScale(other));
 }
 /// <summary>Subtracts compatible quantities using Indriya scale semantics.</summary>
 /// <remarks>Absolute minus absolute remains absolute, including for affine units.
 /// Use the dedicated temperature APIs when a physical temperature interval is required.</remarks>
 public JavaQuantityAdapter Subtract(JavaQuantityAdapter other)
 {
  RequireCompatible(other);
  return new(value-other.value,unit,ResultScale(other));
 }
 private JavaQuantityScale ResultScale(JavaQuantityAdapter other)=>
  Scale==JavaQuantityScale.Relative&&other.Scale==JavaQuantityScale.Relative
   ?JavaQuantityScale.Relative:JavaQuantityScale.Absolute;
 /// <summary>Multiplies quantities in coherent SI.</summary>
 public JavaQuantityAdapter Multiply(JavaQuantityAdapter other)
 {
  ArgumentNullException.ThrowIfNull(other);
  var result=value*other.value;
  return new(result,DerivedUnit(result.RationalDimension),JavaQuantityScale.Absolute);
 }
 /// <summary>Divides quantities in coherent SI.</summary>
 public JavaQuantityAdapter Divide(JavaQuantityAdapter other)
 {
  ArgumentNullException.ThrowIfNull(other);
  if(other.value.SiValue==0)throw new DivideByZeroException();
  var result=value/other.value;
  return new(result,DerivedUnit(result.RationalDimension),JavaQuantityScale.Absolute);
 }
 /// <summary>Multiplies by a boxed numeric scalar, converted to double.</summary>
 public JavaQuantityAdapter Multiply(IConvertible scalar)
 {
  ArgumentNullException.ThrowIfNull(scalar);
  return ScaleBy(scalar.ToDouble(CultureInfo.InvariantCulture),false);
 }
 /// <summary>Divides by a boxed numeric scalar, converted to double.</summary>
 public JavaQuantityAdapter Divide(IConvertible scalar)
 {
  ArgumentNullException.ThrowIfNull(scalar);
  return ScaleBy(scalar.ToDouble(CultureInfo.InvariantCulture),true);
 }
 private JavaQuantityAdapter ScaleBy(double factor,bool divide)
 {
  if(!double.IsFinite(factor))throw new ArgumentOutOfRangeException(nameof(factor));
  if(divide&&factor==0)throw new DivideByZeroException();
  var result=divide?value.SiValue/factor:value.SiValue*factor;
  return new(new Quantity(result,value.RationalDimension),unit,Scale);
 }
 /// <summary>Returns the inverse of a linear quantity, retaining scale and reciprocal display-unit factor.</summary>
 /// <remarks>For example, 250 mA becomes 0.004 (1/mA), whose coherent magnitude is 4 (1/A).
 /// Affine unit inversion is not supported.</remarks>
 public JavaQuantityAdapter Inverse()
 {
  RequireLinearAlgebra(this);
  if(value.SiValue==0)throw new DivideByZeroException();
  var result=new Quantity(1d/value.SiValue,RationalDimension.From(Dimension.None)-value.RationalDimension);
  var reciprocalUnit=new UnitDefinition($"1/({unit.Symbol})",result.RationalDimension,
   UnitConversion.Linear(1d/unit.Converter.Scale));
  return new(result,reciprocalUnit,Scale);
 }
 /// <summary>Compares display values after converting the right operand to this unit, as Indriya does.</summary>
 public int CompareTo(JavaQuantityAdapter? other)
 {
  if(other is null)return 1;
  RequireCompatible(other);
  return Value.CompareTo(other.To(unit).Value);
 }
 /// <summary>Equality includes representation, unlike physical equivalence.</summary>
 public bool Equals(JavaQuantityAdapter? other)=>other is not null&&value.Equals(other.value)&&unit.Equals(other.unit)&&Scale==other.Scale;
 /// <summary>Object equality.</summary>
 public override bool Equals(object? obj)=>obj is JavaQuantityAdapter other&&Equals(other);
 /// <summary>Hash of quantity and representation.</summary>
 public override int GetHashCode()=>HashCode.Combine(value,unit,Scale);
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
