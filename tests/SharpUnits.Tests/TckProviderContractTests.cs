using SharpUnits;
using Xunit;
namespace SharpUnits.Tests;
/// <summary>Contract tests for Java-inspired provider and factory abstractions.</summary>
public sealed class TckProviderContractTests
{
 [Fact] public void FactoryProducesQuantityFromUnit()
 {
  IQuantityFactory factory=new QuantityFactory(UnitRegistry.CreateStandard());
  Assert.Equal(4000,factory.Create(4,"kΩ").SiValue,10);
 }
 [Fact] public void ProviderResolvesStandardUnit()
 {
  IUnitProvider provider=new RegistryUnitProvider("SI",UnitRegistry.CreateStandard());
  Assert.Equal(Dimension.Voltage,provider.Resolve("V").Dimension);
 }
 [Fact] public void CatalogRejectsDuplicateProviders()
 {
  var c=new UnitProviderCatalog();
  Assert.True(c.Register(new RegistryUnitProvider("SI",UnitRegistry.CreateStandard())));
  Assert.False(c.Register(new RegistryUnitProvider("SI",UnitRegistry.CreateStandard())));
 }
 [Fact] public void MissingProviderThrows()
 {
  Assert.Throws<KeyNotFoundException>(()=>new UnitProviderCatalog().Resolve("missing"));
 }
 [Fact] public void ProviderFactoryMaintainsScale()
 {
  var p=new RegistryUnitProvider("SI",UnitRegistry.CreateStandard());
  Assert.Equal(.25,p.CreateQuantity(250,"mA").SiValue,10);
 }
 [Fact] public void UnitSystemPreservesRationalDimensions()
 {
  var d=RationalDimension.From(Dimension.Voltage).Root(2);
  var registry=new UnitRegistry();
  registry.Register(new UnitDefinition("sqrtV",d,UnitConversion.Linear(1)));
  var provider=new RegistryUnitProvider("custom",registry);
  Assert.Equal(d,provider.CreateQuantity(2,"sqrtV").RationalDimension);
 }
}
