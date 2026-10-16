using SharpUnits;
using Xunit;
namespace SharpUnits.Tests;
/// <summary>Compound unit parsing and electrical dimensional normalization tests.</summary>
public sealed class UnitExpressionParserTests
{
 [Theory]
 [InlineData("A*Ω")]
 [InlineData("(A·Ω)")]
 [InlineData("V")]
 public void ParsesVoltageDimension(string expression)
 {
  var parser=new UnitExpressionParser(UnitRegistry.CreateStandard());
  Assert.Equal(RationalDimension.From(Dimension.Voltage),parser.Parse(expression).Dimension);
 }
 [Fact] public void PreservesPrefixScale()
 {
  var parser=new UnitExpressionParser(UnitRegistry.CreateStandard());
  Assert.Equal(1,parser.Parse("mA*kΩ").Scale,12);
 }
 [Fact] public void DivisionRecoversCurrent()
 {
  var parser=new UnitExpressionParser(UnitRegistry.CreateStandard());
  Assert.Equal(RationalDimension.From(Dimension.ElectricCurrent),parser.Parse("V/Ω").Dimension);
 }
 [Fact] public void PowerCancelsToDimensionless()
 {
  var parser=new UnitExpressionParser(UnitRegistry.CreateStandard());
  var result=parser.Parse("A^2/A^2");
  Assert.Equal(RationalDimension.From(Dimension.None),result.Dimension);
 }
 [Fact] public void RejectAffineUnit()
 {
  var parser=new UnitExpressionParser(UnitRegistry.CreateStandard());
  Assert.Throws<InvalidOperationException>(()=>parser.Parse("°C*A"));
 }
 [Theory]
 [InlineData("(A*Ω")]
 [InlineData("A^foo")]
 [InlineData("A*")]
 public void RejectMalformed(string input)
 {
  var parser=new UnitExpressionParser(UnitRegistry.CreateStandard());
  Assert.ThrowsAny<Exception>(()=>parser.Parse(input));
 }
}
