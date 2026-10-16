using SharpUnits;
using Xunit;
namespace SharpUnits.Tests;
/// <summary>Java UnitInterfaceTest API analogues with explicit CLR compatibility limits.</summary>
public sealed class TckUnitApiSurfaceTests
{
 private static readonly JavaUnitAdapter Unit=new(UnitRegistry.CreateStandard().Resolve("V"));
 [Theory]
 [InlineData("Symbol")]
 [InlineData("Name")]
 [InlineData("Dimension")]
 [InlineData("SystemUnit")]
 [InlineData("BaseUnits")]
 public void MetadataPropertiesArePublic(string property)=>Assert.NotNull(typeof(JavaUnitAdapter).GetProperty(property));
 [Theory]
 [InlineData("Alternate")]
 [InlineData("Multiply")]
 [InlineData("Divide")]
 [InlineData("Pow")]
 [InlineData("Root")]
 [InlineData("Transform")]
 [InlineData("Inverse")]
 [InlineData("IsCompatible")]
 [InlineData("AsType")]
 [InlineData("GetConverterTo")]
 [InlineData("GetConverterToAny")]
 public void RequiredMethodsArePublic(string method)=>Assert.Contains(typeof(JavaUnitAdapter).GetMethods(),m=>m.Name==method);
 [Fact] public void AsTypePreservesCompatibleUnit()=>Assert.Same(Unit,Unit.AsType(RationalDimension.From(Dimension.Voltage)));
 [Fact] public void AsTypeRejectsWrongDimension()=>Assert.Throws<InvalidOperationException>(()=>Unit.AsType(RationalDimension.From(Dimension.ElectricCurrent)));
 [Fact] public void AlternatePreservesConversion()
 {
  var alternative=Unit.Alternate("electric-potential");
  Assert.Equal("electric-potential",alternative.Symbol);
  Assert.Equal(5,alternative.ToSystemConverter().Convert(5),10);
 }
 [Fact] public void BaseUnitsMatchVoltageDimensions()
 {
  var baseUnits=Unit.BaseUnits;
  Assert.Equal(new Rational(2,1),baseUnits["L"]);
  Assert.Equal(new Rational(-1,1),baseUnits["I"]);
 }
}
