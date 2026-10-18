using System.Globalization;
namespace SharpUnits;

/// <summary>Whether a quantity represents an absolute value or a relative interval.</summary>
public enum JavaQuantityScale { Absolute, Relative }

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
 /// <summary>Checks coherent SI physical equivalence independent of display unit.</summary>
 public bool IsEquivalentTo(JavaQuantityAdapter? other)=>
  other is not null&&Scale==other.Scale&&value.RationalDimension==other.value.RationalDimension&&value.SiValue==other.value.SiValue;
 /// <summary>Adds quantities of compatible dimensions and scale.</summary>
 public JavaQuantityAdapter Add(JavaQuantityAdapter other)
 {
  RequireCompatible(other);
  if(Scale!=other.Scale)throw new InvalidOperationException("Quantity scale mismatch.");
  if(Scale==JavaQuantityScale.Absolute&&unit.Converter.Offset!=0)
   throw new InvalidOperationException("Absolute affine quantities cannot be added.");
  return new(value+other.value,unit,Scale);
 }
 /// <summary>Subtracts quantities; absolute subtraction yields a relative interval.</summary>
 public JavaQuantityAdapter Subtract(JavaQuantityAdapter other)
 {
  RequireCompatible(other);
  if(Scale!=other.Scale)throw new InvalidOperationException("Quantity scale mismatch.");
  if(Scale==JavaQuantityScale.Absolute&&unit.Converter.Offset!=0)
   return new(value-other.value,new UnitDefinition("Δ"+unit.Symbol,unit.RationalDimension,UnitConversion.Linear(unit.Converter.Scale)),JavaQuantityScale.Relative);
  return new(value-other.value,unit,Scale);
 }
 /// <summary>Multiplies quantities in coherent SI.</summary>
 public JavaQuantityAdapter Multiply(JavaQuantityAdapter other)
 {
  ArgumentNullException.ThrowIfNull(other);
  RequireLinearAlgebra(this);RequireLinearAlgebra(other);
  var result=value*other.value;
  return new(result,DerivedUnit(result.RationalDimension),JavaQuantityScale.Relative);
 }
 /// <summary>Divides quantities in coherent SI.</summary>
 public JavaQuantityAdapter Divide(JavaQuantityAdapter other)
 {
  ArgumentNullException.ThrowIfNull(other);
  RequireLinearAlgebra(this);RequireLinearAlgebra(other);
  if(other.value.SiValue==0)throw new DivideByZeroException();
  var result=value/other.value;
  return new(result,DerivedUnit(result.RationalDimension),JavaQuantityScale.Relative);
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
  if(Scale==JavaQuantityScale.Absolute&&unit.Converter.Offset!=0)
   throw new InvalidOperationException("Cannot scale absolute affine quantities.");
  var result=divide?value.SiValue/factor:value.SiValue*factor;
  return new(new Quantity(result,value.RationalDimension),unit,Scale);
 }
 /// <summary>Returns the multiplicative inverse of a linear quantity.</summary>
 public JavaQuantityAdapter Inverse()
 {
  RequireLinearAlgebra(this);
  if(value.SiValue==0)throw new DivideByZeroException();
  var result=new Quantity(1d/value.SiValue,RationalDimension.From(Dimension.None)-value.RationalDimension);
  return new(result,DerivedUnit(result.RationalDimension),JavaQuantityScale.Relative);
 }
 /// <summary>Compares compatible coherent SI values.</summary>
 public int CompareTo(JavaQuantityAdapter? other)
 {
  if(other is null)return 1;
  RequireCompatible(other);
  return value.SiValue.CompareTo(other.value.SiValue);
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
