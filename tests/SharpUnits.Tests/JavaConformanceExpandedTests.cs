using SharpUnits;
using Xunit;
namespace SharpUnits.Tests;

/// <summary>Additional Java TCK-inspired conversion, dimension, and unit-registry conformance cases.</summary>
/// <remarks>Original upstream suites: unitsofmeasurement/unit-tck and unitsofmeasurement/indriya (BSD-3-Clause).
/// These are independent behavioral adaptations, not verbatim translations of individual Java methods.</remarks>
public sealed class JavaConformanceExpandedTests
{
 [Theory]
 [InlineData(-40,-40)]
 [InlineData(0,32)]
 [InlineData(100,212)]
 public void Temperature_CelsiusToFahrenheit(double c,double f)
 {
  var units=UnitRegistry.CreateStandard();
  Assert.Equal(f,units.Convert(c,"°C","°F"),8);
 }
 [Theory]
 [InlineData(0,273.15)]
 [InlineData(100,373.15)]
 [InlineData(-273.15,0)]
 public void Temperature_CelsiusToKelvin(double c,double k)
  =>Assert.Equal(k,UnitRegistry.CreateStandard().Convert(c,"°C","K"),8);

 [Theory]
 [InlineData(0)]
 [InlineData(0.125)]
 [InlineData(1000000)]
 [InlineData(-0.5)]
 public void Prefix_MilliampRoundTrip(double amps)
 {
  var registry=UnitRegistry.CreateStandard();
  Assert.Equal(amps,registry.Convert(registry.Convert(amps,"A","mA"),"mA","A"),8);
 }
 [Fact] public void Units_SameDimensionButDifferentScale()
 {
  var r=UnitRegistry.CreateStandard();
  Assert.Equal(1000,r.Convert(1,"V","mV"),9);
  Assert.Equal(0.001,r.Convert(1,"mV","V"),9);
 }
 [Fact] public void Units_UnknownSymbolThrows()
  =>Assert.Throws<KeyNotFoundException>(()=>UnitRegistry.CreateStandard().Resolve("not-a-unit"));
 [Fact] public void Units_DimensionEqualityIsStructural()
 {
  Assert.Equal(Dimension.Voltage,Dimension.ElectricCurrent+Dimension.Resistance);
  Assert.NotEqual(Dimension.Voltage,Dimension.Resistance);
 }
 [Fact] public void Quantity_MultiplicationAndDivisionRestoreDimensions()
 {
  var product=Electrical.Amperes(2)*Electrical.Ohms(5);
  Assert.Equal(Dimension.Voltage,product.Dimension);
  Assert.Equal(10,product.SiValue,9);
  var recovered=product/Electrical.Ohms(5);
  Assert.Equal(Dimension.ElectricCurrent,recovered.Dimension);
  Assert.Equal(2,recovered.SiValue,9);
 }
 [Fact] public void Measurement_UncertaintyAdditionIndependent()
 {
  var a=new Measurement(Electrical.Volts(2),.3);
  var b=new Measurement(Electrical.Volts(3),.4);
  var sum=a+b;
  Assert.Equal(5,sum.Value.SiValue,9);
  Assert.Equal(.5,sum.StandardUncertainty,9);
 }
 [Fact] public void Measurement_RejectNegativeUncertainty()
  =>Assert.Throws<ArgumentOutOfRangeException>(()=>new Measurement(Electrical.Volts(1),-1));
}
