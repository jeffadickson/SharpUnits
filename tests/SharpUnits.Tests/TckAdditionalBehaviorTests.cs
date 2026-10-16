using SharpUnits;
using Xunit;
namespace SharpUnits.Tests;
/// <summary>Additional Java TCK-inspired behavioral regression cases; not API-level conformance certification.</summary>
public sealed class TckAdditionalBehaviorTests
{
 [Fact] public void QuantityAdd()=>Assert.Equal(7,(Electrical.Volts(3)+Electrical.Volts(4)).SiValue,10);
 [Fact] public void QuantityMultiplyScalarViaDimensionless()
 {
  var q=Electrical.Volts(4)*new Quantity(3,Dimension.None);
  Assert.Equal(12,q.SiValue,10);
  Assert.Equal(Dimension.Voltage,q.Dimension);
 }
 [Fact] public void QuantityDivideScalarViaDimensionless()
 {
  var q=Electrical.Volts(12)/new Quantity(3,Dimension.None);
  Assert.Equal(4,q.SiValue,10);
  Assert.Equal(Dimension.Voltage,q.Dimension);
 }
 [Fact] public void QuantityValueAndUnit()
 {
  var q=new QuantityFactory(UnitRegistry.CreateStandard()).Create(250,"mA");
  Assert.Equal(.25,q.SiValue,10);
  Assert.Equal(Dimension.ElectricCurrent,q.Dimension);
 }
 [Fact] public void QuantityHashing()
 {
  Assert.Equal(Electrical.Volts(5).GetHashCode(),Electrical.Volts(5).GetHashCode());
 }
 [Fact] public void UnitMultiplication()
 {
  var units=UnitRegistry.CreateStandard();
  var v=UnitExpression.From(units.Resolve("A"))*UnitExpression.From(units.Resolve("Ω"));
  Assert.Equal(Dimension.Voltage,v.Dimension);
 }
 [Fact] public void UnitDivision()
 {
  var units=UnitRegistry.CreateStandard();
  var i=UnitExpression.From(units.Resolve("V"))/UnitExpression.From(units.Resolve("Ω"));
  Assert.Equal(Dimension.ElectricCurrent,i.Dimension);
 }
 [Fact] public void UnitPowAndRoot()
 {
  var u=UnitExpression.From(UnitRegistry.CreateStandard().Resolve("V"));
  Assert.Equal(u.RationalDimension,u.Pow(2).Root(2).RationalDimension);
 }
 [Fact] public void UnitShift()
 {
  Assert.Equal(273.15,UnitConversion.Celsius.ToSi(0),10);
 }
 [Fact] public void UnitSystemLookup()
 {
  var provider=new RegistryUnitProvider("SI",UnitRegistry.CreateStandard());
  Assert.Equal("A",provider.Resolve("A").Symbol);
 }
 [Fact] public void ProviderNameDiscovery()
 {
  var catalog=new UnitProviderCatalog();
  catalog.Register(new RegistryUnitProvider("SI",UnitRegistry.CreateStandard()));
  Assert.Contains("SI",catalog.Names);
 }
 [Fact] public void FormatRoundTrip()
 {
  var format=new QuantityTextFormat(UnitRegistry.CreateStandard());
  Assert.Equal(1,format.Parse(format.Format(Electrical.Volts(1),"V")).SiValue,10);
 }
 [Fact] public void PrefixScale()
 {
  var kilo=SiPrefixes.Apply("k",UnitRegistry.CreateStandard().Resolve("A"));
  Assert.Equal(1000,kilo.Converter.Scale,10);
 }
}