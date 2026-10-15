using SharpUnits;
using Xunit;
namespace SharpUnits.Tests;
/// <summary>Behavioral SI prefix compatibility and electrical scale regression tests.</summary>
public sealed class TckPrefixBehaviorTests
{
 [Theory]
 [InlineData("m",.001)]
 [InlineData("k",1000)]
 [InlineData("µ",.000001)]
 [InlineData("M",1000000)]
 public void PrefixPreservesDimensionAndChangesScale(string prefix,double expected)
 {
  var baseUnit=UnitRegistry.CreateStandard().Resolve("A");
  var result=SiPrefixes.Apply(prefix,baseUnit);
  Assert.Equal(baseUnit.RationalDimension,result.RationalDimension);
  Assert.Equal(expected,result.Converter.Scale,12);
 }
 [Fact] public void MilliampTimesKiloohmIsVoltage()
 {
  var registry=UnitRegistry.CreateStandard();
  var mA=SiPrefixes.Apply("m",registry.Resolve("A"));
  var kOhm=SiPrefixes.Apply("k",registry.Resolve("Ω"));
  var result=RationalUnitExpression.From(mA)*RationalUnitExpression.From(kOhm);
  Assert.Equal(RationalDimension.From(Dimension.Voltage),result.Dimension);
  Assert.Equal(1,result.Scale,12);
 }
 [Fact] public void RejectPrefixOnAffineTemperature()
 {
  Assert.Throws<InvalidOperationException>(()=>SiPrefixes.Apply("m",UnitRegistry.CreateStandard().Resolve("°C")));
 }
 [Fact] public void RejectUnknownPrefix()
 {
  Assert.Throws<ArgumentException>(()=>SiPrefixes.Exponent("invalid"));
 }
}
