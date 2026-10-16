using SharpUnits;
using Xunit;
namespace SharpUnits.Tests;
/// <summary>Java unit API adapter behavior and CLR reflection tests.</summary>
public sealed class JavaUnitAdapterTests
{
 [Fact] public void UnitMetadataAndConversion()
 {
  var registry=UnitRegistry.CreateStandard();
  var ma=new JavaUnitAdapter(registry.Resolve("mA"));
  var a=new JavaUnitAdapter(registry.Resolve("A"));
  Assert.Equal("mA",ma.Symbol);
  Assert.True(ma.IsCompatible(a));
  Assert.Equal(.25,ma.GetConverterTo(a).Convert(250),10);
 }
 [Fact] public void ElectricalUnitProducts()
 {
  var registry=UnitRegistry.CreateStandard();
  var current=new JavaUnitAdapter(registry.Resolve("A"));
  var resistance=new JavaUnitAdapter(registry.Resolve("Ω"));
  Assert.Equal(RationalDimension.From(Dimension.Voltage),current.Multiply(resistance).Dimension);
 }
 [Fact] public void UnitPrefixes()
 {
  var ampere=new JavaUnitAdapter(UnitRegistry.CreateStandard().Resolve("A"));
  Assert.Equal(.001,ampere.Prefix(SiPrefixCatalog.Resolve("m")).ToSystemConverter().Convert(1),12);
 }
 [Fact] public void UnitShiftAndAffineConversion()
 {
  var kelvin=new JavaUnitAdapter(UnitRegistry.CreateStandard().Resolve("K"));
  var celsius=kelvin.Shift(273.15);
  Assert.Equal(273.15,celsius.ToSystemConverter().Convert(0),10);
 }
 [Fact] public void IncompatibleUnitsRejected()
 {
  var registry=UnitRegistry.CreateStandard();
  Assert.Throws<InvalidOperationException>(()=>new JavaUnitAdapter(registry.Resolve("V")).GetConverterTo(new JavaUnitAdapter(registry.Resolve("A"))));
 }
 [Fact] public void RequiredApiPropertiesExist()
 {
  var type=typeof(JavaUnitAdapter);
  foreach(var name in new[]{"Symbol","Name","Dimension","SystemUnit","BaseUnits"})
   Assert.NotNull(type.GetProperty(name));
  foreach(var name in new[]{"Multiply","Divide","Pow","Root","Prefix","Shift","Inverse","Alternate","GetConverterTo","GetConverterToAny","IsCompatible"})
   Assert.Contains(type.GetMethods(),m=>m.Name==name);
 }
}
