// Behavioral test adaptations informed by the JSR 385 TCK and Indriya.
// Upstream: https://github.com/unitsofmeasurement/unit-tck
// Upstream: https://github.com/unitsofmeasurement/indriya
// Copyright for original upstream material remains with its respective authors.
// BSD 3-Clause attribution and license apply to adapted upstream material.
using SharpUnits;
using Xunit;
namespace SharpUnits.Tests;
/// <summary>Cross-language compatibility cases for dimension, conversion and quantity operations.</summary>
public sealed class JavaParityPortTests
{
 [Fact] public void UnitDimension_ProductMatchesVoltage()
 {
  Assert.Equal(Dimension.Voltage,Dimension.ElectricCurrent+Dimension.Resistance);
 }
 [Fact] public void UnitDimension_DivisionRecoversCurrent()
 {
  Assert.Equal(Dimension.ElectricCurrent,Dimension.Voltage-Dimension.Resistance);
 }
 [Fact] public void UnitConversion_AffineRoundTrip()
 {
  foreach(var value in new[]{-40d,0d,20d,100d})
    Assert.Equal(value,UnitConversion.Celsius.FromSi(UnitConversion.Celsius.ToSi(value)),9);
 }
 [Fact] public void UnitConversion_IncompatibleUnitsRejected()
 {
  Assert.Throws<InvalidOperationException>(()=>UnitRegistry.CreateStandard().Convert(1,"V","Ω"));
 }
 [Fact] public void QuantityInterface_CompatibleAddition()
 {
  var q=Electrical.Volts(2)+Electrical.Volts(3);
  Assert.Equal(5,q.SiValue,10);
  Assert.Equal(Dimension.Voltage,q.Dimension);
 }
 [Fact] public void QuantityInterface_IncompatibleAdditionRejected()
 {
  Assert.Throws<InvalidOperationException>(()=>Electrical.Volts(2)+Electrical.Amperes(3));
 }
 [Fact] public void PrefixInterface_EquivalentScale()
 {
  Assert.Equal(.25,UnitRegistry.CreateStandard().Convert(250,"mA","A"),10);
  Assert.Equal(4000,UnitRegistry.CreateStandard().Convert(4,"kΩ","Ω"),10);
 }
}
