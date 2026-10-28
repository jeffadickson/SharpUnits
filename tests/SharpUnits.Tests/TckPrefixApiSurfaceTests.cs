using SharpUnits;
using Xunit;
namespace SharpUnits.Tests;
/// <summary>JSR 385 PrefixInterfaceTest 424-A1 through A6 CLR API analogues.</summary>
public sealed class TckPrefixApiSurfaceTests
{
 [Fact] public void Tck424A1_Equals()=>Assert.NotNull(typeof(SiPrefix).GetMethod("Equals",new[]{typeof(object)}));
 [Fact] public void Tck424A2_HashCode()=>Assert.NotNull(typeof(SiPrefix).GetMethod("GetHashCode",Type.EmptyTypes));
 [Fact] public void Tck424A3_Name()=>Assert.NotNull(typeof(SiPrefix).GetProperty("Name"));
 [Fact] public void Tck424A4_Symbol()=>Assert.NotNull(typeof(SiPrefix).GetProperty("Symbol"));
 [Fact] public void Tck424A5_Value()
 {
  Assert.NotNull(typeof(SiPrefix).GetProperty("Value"));
  Assert.Equal(typeof(JavaNumber),typeof(SiPrefix).GetMethod("GetValue")!.ReturnType);
 }
 [Fact] public void Tck424A6_Exponent()=>Assert.NotNull(typeof(SiPrefix).GetProperty("Exponent"));
 [Fact] public void PrefixMetadataIsConsistent()
 {
  Assert.Equal(24,SiPrefixCatalog.All.Count);
  foreach(var prefix in SiPrefixCatalog.All)
  {
   Assert.Equal(prefix.Exponent,SiPrefixes.Exponent(prefix.Symbol));
   Assert.Equal(Math.Pow(10,prefix.Exponent),prefix.Value,12);
  }
 }
 [Fact] public void MilliampereHasCorrectScale()
 {
  var unit=SiPrefixCatalog.Resolve("m").Apply(UnitRegistry.CreateStandard().Resolve("A"));
  Assert.Equal(.001,unit.Converter.Scale,12);
 }
}
