using SharpUnits;
using Xunit;
namespace SharpUnits.Tests;
/// <summary>JSR 385 behavioral analogues, local iterations 21 through 30.</summary>
public sealed class JavaParityThirtyIterationPartThreeTests
{
 private static JavaUnitAdapter U(string s)=>new(UnitRegistry.CreateStandard().Resolve(s));
 [Fact] public void VoltageDimension()=>Assert.Equal(RationalDimension.From(Dimension.Voltage),U("V").Dimension);
 [Fact] public void CompatibleScaleVariants()=>Assert.True(U("mA").IsCompatible(U("A")));
 [Fact] public void IncompatibleQuantities()=>Assert.False(U("V").IsCompatible(U("A")));
 [Fact] public void ElectricalProduct()=>Assert.Equal(RationalDimension.From(Dimension.Voltage),U("A").Multiply(U("Ω")).Dimension);
 [Fact] public void ElectricalQuotient()=>Assert.Equal(RationalDimension.From(Dimension.Resistance),U("V").Divide(U("A")).Dimension);
 [Fact] public void PowerRootScaleRoundTrip()
 {
  var original=U("kΩ").Expression;
  var result=U("kΩ").Pow(2).Root(2);
  Assert.Equal(original.Dimension,result.Dimension);
  Assert.Equal(original.Scale,result.Scale,8);
 }
 [Fact] public void MilliPrefix()
 {
  var unit=U("A").Prefix(SiPrefixCatalog.Resolve("m"));
  Assert.Equal(.001,unit.ToSystemConverter().Convert(1),10);
 }
 [Fact] public void RejectAffinePrefix()=>Assert.Throws<InvalidOperationException>(()=>U("°C").Prefix(SiPrefixCatalog.Resolve("m")));
 [Fact] public void AlternateUnitSymbol()
 {
  var alternate=U("mV").Alternate("custom-millivolt");
  Assert.Equal("custom-millivolt",alternate.Symbol);
  Assert.Equal(.025,alternate.GetConverterTo(U("V")).Convert(25),10);
 }
 [Fact] public void ShiftedKelvin()
 {
  var shifted=U("K").Shift(273.15);
  Assert.Equal(0,shifted.GetConverterTo(U("K")).Inverse().Convert(273.15),9);
 }
}
