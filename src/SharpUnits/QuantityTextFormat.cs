using System.Globalization;
namespace SharpUnits;
/// <summary>Invariant-culture quantity formatting and parsing using a registered unit symbol.</summary>
/// <remarks>This is a deliberately limited formatter, not yet full JSR 385 locale or EBNF compatibility.</remarks>
public sealed class QuantityTextFormat
{
 private readonly UnitRegistry registry;
 private readonly JavaUnitTextFormat? javaUnits;
 /// <summary>Creates a formatter using an explicit unit registry.</summary>
 public QuantityTextFormat(UnitRegistry registry)=>this.registry=registry??throw new ArgumentNullException(nameof(registry));
 // Service-created formatters share the unit-label database for both formatting and
 // parsing. The existing public constructor retains its original registry contract.
 internal QuantityTextFormat(UnitRegistry registry,JavaUnitTextFormat units):this(registry)=>javaUnits=units;
 /// <summary>Reports that the supported Java formatting profile is invariant rather than locale sensitive.</summary>
 public bool IsLocaleSensitive=>false;
 /// <summary>Formats retained display values using the supported pinned Java double/unit text conventions.</summary>
 /// <remarks>Does not retain arbitrary Java Number types or parsed decimal lexical precision.
 /// Scale markers are not emitted. Service-created formatters support the recognized standard Fahrenheit
 /// converter expression; standalone formatters retain registered symbols. General transformed-unit graphs remain incomplete.</remarks>
 public string FormatJavaQuantity(JavaQuantityAdapter quantity)
 {
  ArgumentNullException.ThrowIfNull(quantity);
  var number=JavaNumberText.Format(quantity.Value);var unit=JavaSymbol(quantity.Unit);
  if(unit.Length!=0&&javaUnits is not null)unit=javaUnits.Format(quantity.Unit);
  return unit.Length==0?number:number+" "+unit;
 }
 /// <summary>Appends Java-profile text and returns the original builder.</summary>
 public System.Text.StringBuilder FormatJavaQuantity(JavaQuantityAdapter quantity,System.Text.StringBuilder destination)
 {
  ArgumentNullException.ThrowIfNull(destination);
  return destination.Append(FormatJavaQuantity(quantity));
 }
 /// <summary>Writes Java-profile text, returning the original writer without flushing or closing it.</summary>
 /// <exception cref="IOException">The destination rejects a write; errors propagate to the caller.</exception>
 public TextWriter FormatJavaQuantity(JavaQuantityAdapter quantity,TextWriter destination)
 {
  ArgumentNullException.ThrowIfNull(destination);
  destination.Write(FormatJavaQuantity(quantity));
  return destination;
 }
 internal static string JavaSymbol(UnitDefinition unit)
 {
  // Canonicalize only recognized definitions. A caller-defined symbol alone does not
  // prove it denotes Celsius, a micro-prefixed SI unit, or the canonical dimensionless unit.
  if(unit.Symbol is "1" or "one"&&unit.RationalDimension==RationalDimension.From(Dimension.None)
   &&unit.Converter.IsIdentity&&unit.ExactLinearScale is null)return "";
  if(unit.Symbol is "°C" or "℃"&&unit.RationalDimension==new RationalDimension(0,0,0,0,1,0,0)
   &&unit.Converter==UnitConversion.Celsius)return "℃";
  if(unit.Symbol.Length==2&&unit.Symbol[0]=='µ'&&unit.Converter.IsLinear&&JavaDoubleArithmetic.ScaleOf(unit)==new Rational(1,1000000))
  {
   RationalDimension? expected=unit.Symbol[1] switch {'A'=>RationalDimension.From(Dimension.ElectricCurrent),'V'=>RationalDimension.From(Dimension.Voltage),
    'Ω'=>RationalDimension.From(Dimension.Resistance),'K'=>new RationalDimension(0,0,0,0,1,0,0),_=>null};
   if(expected is not null&&expected.Value==unit.RationalDimension)return "μ"+unit.Symbol[1];
  }
  return unit.Symbol;
 }
 /// <summary>Parses invariant decimal or integer-rational text, retaining its unit and absolute scale.</summary>
 /// <remarks>Uses Java whitespace, registered symbols, the Celsius compatibility alias and linear
 /// unit expressions with right-associated operators, parentheses, caret/double-star powers and
 /// superscript digits 1/2/3 (exponent magnitude at most 1024). Pinned Java treats an explicit
 /// zero or missing exponent as its default-one sentinel; native unit Pow(0) keeps conventional algebra.
 /// Colon-root exponents are supported with root magnitude at most 1024; zero roots likewise
 /// denote Java's default-one sentinel. Scale evaluation applies power and root sequentially.
 /// Nonzero signed 32-bit unit factors and single SI prefixes on registered coherent linear
 /// units are supported. Exact linear factors have a 16384-bit budget and are retained by Java
 /// adapter conversion; native converter APIs expose double projections. Affine/nested prefixes
 /// and fractional numeric unit factors remain outside this grammar subset.
 /// Groups/operators are limited to depth 64. Affine units cannot participate in compound algebra.
 /// Whitespace is permitted after ÷ before the denominator. A missing unit denotes dimensionless.
 /// Unlike <see cref="Parse"/>, this preserves the display magnitude. Input is limited to 4096 characters
 /// and the numeric token to 1024 characters. This is a subset of Indriya's grammar.</remarks>
 /// <exception cref="FormatException">The number is malformed, nonfinite or exceeds resource limits.</exception>
 /// <exception cref="KeyNotFoundException">The unit symbol is not registered.</exception>
 /// <exception cref="DivideByZeroException">A rational denominator is zero.</exception>
 /// <exception cref="ArgumentOutOfRangeException">The coherent representation cannot be stored as a finite double.</exception>
 public JavaQuantityAdapter ParseJavaQuantity(string text)=>javaUnits is null?JavaQuantityTextParser.Parse(text,registry):javaUnits.ParseQuantity(text);
 /// <summary>Formats an SI quantity in a selected registered unit.</summary>
 public string Format(Quantity quantity,string symbol)
 {
  var unit=registry.Resolve(symbol);
  var value=unit.FromQuantity(quantity);
  return value.ToString("R",CultureInfo.InvariantCulture)+" "+symbol;
 }
 /// <summary>Parses a numeric quantity and exact registered unit symbol.</summary>
 public Quantity Parse(string text)
 {
  ArgumentNullException.ThrowIfNull(text);
  var trimmed=text.Trim();
  var split=trimmed.IndexOf(' ');
  if(split<=0||split==trimmed.Length-1)throw new FormatException("Expected '<number> <unit>'.");
  var number=trimmed[..split];
  var symbol=trimmed[(split+1)..].Trim();
  if(!double.TryParse(number,NumberStyles.Float,CultureInfo.InvariantCulture,out var value))
   throw new FormatException("Invalid numeric value.");
  return registry.Resolve(symbol).ToQuantity(value);
 }
}
