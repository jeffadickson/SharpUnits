using SharpUnits;
using Xunit;
namespace SharpUnits.Tests;
/// <summary>Basic format/parse round-trip tests preparing JSR 385 quantity-format coverage.</summary>
public sealed class QuantityTextFormatTests
{
 [Theory]
 [InlineData("250 mA",.25)]
 [InlineData("4 kΩ",4000)]
 [InlineData("100 °C",373.15)]
 public void ParsesRegisteredQuantities(string text,double expected)
 {
  var formatter=new QuantityTextFormat(UnitRegistry.CreateStandard());
  Assert.Equal(expected,formatter.Parse(text).SiValue,9);
 }
 [Fact] public void FormatThenParseRoundTrip()
 {
  var formatter=new QuantityTextFormat(UnitRegistry.CreateStandard());
  var quantity=Electrical.Volts(5.125);
  var text=formatter.Format(quantity,"mV");
  var restored=formatter.Parse(text);
  Assert.Equal(quantity.RationalDimension,restored.RationalDimension);
  Assert.Equal(quantity.SiValue,restored.SiValue,9);
 }
 [Fact] public void RejectUnknownUnit()
 {
  var formatter=new QuantityTextFormat(UnitRegistry.CreateStandard());
  Assert.Throws<KeyNotFoundException>(()=>formatter.Parse("3 furlongs"));
 }
 [Fact] public void RejectMalformedQuantity()
 {
  var formatter=new QuantityTextFormat(UnitRegistry.CreateStandard());
  Assert.Throws<FormatException>(()=>formatter.Parse("not a quantity"));
 }
}
