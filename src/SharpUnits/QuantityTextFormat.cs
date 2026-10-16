using System.Globalization;
namespace SharpUnits;
/// <summary>Invariant-culture quantity formatting and parsing using a registered unit symbol.</summary>
/// <remarks>This is a deliberately limited formatter, not yet full JSR 385 locale or EBNF compatibility.</remarks>
public sealed class QuantityTextFormat
{
 private readonly UnitRegistry registry;
 /// <summary>Creates a formatter using an explicit unit registry.</summary>
 public QuantityTextFormat(UnitRegistry registry)=>this.registry=registry??throw new ArgumentNullException(nameof(registry));
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
