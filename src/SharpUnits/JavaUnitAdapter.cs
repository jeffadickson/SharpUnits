namespace SharpUnits;
/// <summary>Java JSR 385-style unit adapter over a registered SI unit.</summary>
/// <remarks>Provides CLR equivalents for unit metadata, composition, scaling, shifts and conversion.
/// This adapter does not claim binary compatibility with Java interfaces.</remarks>
public sealed class JavaUnitAdapter
{
 private readonly UnitDefinition definition;
 /// <summary>Unit symbol.</summary>
 public string Symbol=>definition.Symbol;
 /// <summary>Unit name; defaults to the symbol until a named catalog is available.</summary>
 public string Name=>definition.Symbol;
 /// <summary>Exact rational dimensions.</summary>
 public RationalDimension Dimension=>definition.RationalDimension;
 /// <summary>Coherent SI system unit representation.</summary>
 public RationalUnitExpression SystemUnit=>new(definition.RationalDimension,1);
 /// <summary>Creates a unit adapter.</summary>
 public JavaUnitAdapter(UnitDefinition definition)=>this.definition=definition??throw new ArgumentNullException(nameof(definition));
 /// <summary>Returns the multiplicative representation, rejecting affine units.</summary>
 public RationalUnitExpression Expression=>RationalUnitExpression.From(definition);
 /// <summary>Tests dimensional compatibility.</summary>
 public bool IsCompatible(JavaUnitAdapter other)=>other is not null&&Dimension==other.Dimension;
 /// <summary>Converts this unit to a compatible target unit.</summary>
 public JavaUnitConverterAdapter GetConverterTo(JavaUnitAdapter target)
 {
  ArgumentNullException.ThrowIfNull(target);
  if(!IsCompatible(target))throw new InvalidOperationException("Incompatible dimensions.");
  return new JavaUnitConverterAdapter(definition.Converter.Then(target.definition.Converter.Inverse()));
 }
 /// <summary>Multiplies linear units.</summary>
 public RationalUnitExpression Multiply(JavaUnitAdapter other)=>Expression*other.Expression;
 /// <summary>Divides linear units.</summary>
 public RationalUnitExpression Divide(JavaUnitAdapter other)=>Expression/other.Expression;
 /// <summary>Raises a linear unit to an integer power.</summary>
 public RationalUnitExpression Pow(int exponent)=>Expression.Pow(new Rational(exponent,1));
 /// <summary>Returns a positive integer-degree root.</summary>
 public RationalUnitExpression Root(int degree)=>Expression.Root(degree);
 /// <summary>Applies an SI prefix to a linear unit.</summary>
 public JavaUnitAdapter Prefix(SiPrefix prefix)=>new(prefix.Apply(definition));
 /// <summary>Applies a scale multiplier to a linear unit.</summary>
 public JavaUnitAdapter Multiply(double multiplier)=>new(new UnitDefinition(Symbol,Dimension,UnitConversion.Linear(Expression.Scale*multiplier)));
 /// <summary>Divides a linear unit by a scalar.</summary>
 public JavaUnitAdapter Divide(double divisor)=>Multiply(1/divisor);
 /// <summary>Shifts the unit conversion by a numeric offset in source units.</summary>
 public JavaUnitAdapter Shift(double shift)=>new(new UnitDefinition(Symbol,Dimension,
  new UnitConversion(definition.Converter.Scale,definition.Converter.Offset+shift*definition.Converter.Scale)));
 /// <summary>Returns a unit with the inverse dimensional expression.</summary>
 public RationalUnitExpression Inverse()=>Expression.Pow(new Rational(-1,1));
 /// <summary>Scales a linear unit by a boxed numeric value.</summary>
 public JavaUnitAdapter Multiply(IConvertible factor)
 {
  ArgumentNullException.ThrowIfNull(factor);
  return Multiply(factor.ToDouble(System.Globalization.CultureInfo.InvariantCulture));
 }
 /// <summary>Divides a linear unit by a boxed numeric value.</summary>
 public JavaUnitAdapter Divide(IConvertible divisor)
 {
  ArgumentNullException.ThrowIfNull(divisor);
  return Divide(divisor.ToDouble(System.Globalization.CultureInfo.InvariantCulture));
 }
 /// <summary>Shifts a unit by a boxed numeric value.</summary>
 public JavaUnitAdapter Shift(IConvertible shift)
 {
  ArgumentNullException.ThrowIfNull(shift);
  return Shift(shift.ToDouble(System.Globalization.CultureInfo.InvariantCulture));
 }
 /// <summary>Applies a subsequent converter to the source unit mapping.</summary>
 public JavaUnitAdapter Transform(JavaUnitConverterAdapter converter)
 {
  ArgumentNullException.ThrowIfNull(converter);
  var composed=definition.Converter.Then(converter.ToAffine());
  return new(new UnitDefinition(Symbol,Dimension,composed));
 }
 /// <summary>Returns a unit compatible with the requested rational dimension, rejecting mismatches.</summary>
 /// <remarks>CLR dimension-based equivalent of Java's typed asType operation.</remarks>
 public JavaUnitAdapter AsType(RationalDimension requested)
 {
  if(Dimension!=requested)throw new InvalidOperationException("Requested quantity type is incompatible.");
  return this;
 }
 /// <summary>Returns an alternative symbol without changing the converter or dimension.</summary>
 public JavaUnitAdapter Alternate(string symbol)=>new(new UnitDefinition(symbol,Dimension,definition.Converter));
 /// <summary>Returns a representation of the registered base dimensions.</summary>
 public IReadOnlyDictionary<string,Rational> BaseUnits=>Dimension.GetBaseDimensions();
 /// <summary>Converts any compatible unit to this unit.</summary>
 public JavaUnitConverterAdapter GetConverterToAny(JavaUnitAdapter target)=>GetConverterTo(target);
 /// <summary>Returns the unit's converter to coherent SI.</summary>
 public JavaUnitConverterAdapter ToSystemConverter()=>new(definition.Converter);
 /// <summary>Returns the registered symbol.</summary>
 public override string ToString()=>Symbol;
}
