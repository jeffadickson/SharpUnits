using System.Globalization;
namespace SharpUnits;
/// <summary>Invariant-culture quantity formatting and parsing using a registered unit symbol.</summary>
/// <remarks>This is a deliberately limited formatter, not yet full JSR 385 locale or EBNF compatibility.</remarks>
public sealed class QuantityTextFormat
{
 private readonly UnitRegistry registry;
 /// <summary>Creates a formatter using an explicit unit registry.</summary>
 public QuantityTextFormat(UnitRegistry registry)=>this.registry=registry??throw new ArgumentNullException(nameof(registry));
 /// <summary>Parses invariant decimal or integer-rational text, retaining its unit and absolute scale.</summary>
 /// <remarks>Uses Java whitespace, registered symbols, the Celsius compatibility alias and linear
 /// unit expressions with right-associated operators, parentheses, caret/double-star powers and
 /// superscript digits 1/2/3 (exponent magnitude at most 1024). Pinned Java treats an explicit
 /// zero exponent as its default-one sentinel; native unit Pow(0) keeps conventional algebra.
 /// Colon-root exponents are supported with root magnitude at most 1024; zero roots likewise
 /// denote Java's default-one sentinel. Scale evaluation applies power and root sequentially.
 /// Groups/operators are limited to depth 64. Affine units cannot participate in compound algebra.
 /// Whitespace is permitted after ÷ before the denominator. A missing unit denotes dimensionless.
 /// Unlike <see cref="Parse"/>, this preserves the display magnitude. Input is limited to 4096 characters
 /// and the numeric token to 1024 characters. This is a subset of Indriya's grammar.</remarks>
 /// <exception cref="FormatException">The number is malformed, nonfinite or exceeds resource limits.</exception>
 /// <exception cref="KeyNotFoundException">The unit symbol is not registered.</exception>
 /// <exception cref="DivideByZeroException">A rational denominator is zero.</exception>
 /// <exception cref="ArgumentOutOfRangeException">The coherent representation cannot be stored as a finite double.</exception>
 public JavaQuantityAdapter ParseJavaQuantity(string text)=>JavaQuantityTextParser.Parse(text,registry);
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
