using SharpUnits;
using Xunit;
namespace SharpUnits.Tests;
/// <summary>Provider discovery and quantity factory regression tests.</summary>
public sealed class ProviderFactoryTests
{
 [Fact] public void FactoryCreatesScaledQuantity()
 {
  IQuantityFactory factory=new QuantityFactory(UnitRegistry.CreateStandard());
  var q=factory.Create(250,"mA");
  Assert.Equal(.25,q.SiValue,10);
  Assert.Equal(Dimension.ElectricCurrent,q.Dimension);
 }
 [Fact] public void ProviderIsDiscoverable()
 {
  var catalog=new UnitProviderCatalog();
  var provider=new RegistryUnitProvider("SI",UnitRegistry.CreateStandard());
  Assert.True(catalog.Register(provider));
  Assert.Same(provider,catalog.Resolve("SI"));
  Assert.Contains("SI",catalog.Names);
 }
 [Fact] public void ProviderRejectsDuplicateName()
 {
  var catalog=new UnitProviderCatalog();
  Assert.True(catalog.Register(new RegistryUnitProvider("SI",UnitRegistry.CreateStandard())));
  Assert.False(catalog.Register(new RegistryUnitProvider("SI",UnitRegistry.CreateStandard())));
 }
 [Fact] public void ProvidersAreIsolated()
 {
  var catalog=new UnitProviderCatalog();
  var first=new UnitRegistry();
  var second=new UnitRegistry();
  Assert.True(first.Register(new UnitDefinition("x",Dimension.Voltage,UnitConversion.Linear(1))));
  Assert.True(second.Register(new UnitDefinition("x",Dimension.ElectricCurrent,UnitConversion.Linear(1))));
  catalog.Register(new RegistryUnitProvider("a",first));
  catalog.Register(new RegistryUnitProvider("b",second));
  Assert.NotEqual(catalog.Resolve("a").CreateQuantity(1,"x").RationalDimension,catalog.Resolve("b").CreateQuantity(1,"x").RationalDimension);
 }
}
