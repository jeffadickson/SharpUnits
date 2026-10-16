using SharpUnits;
using Xunit;
namespace SharpUnits.Tests;
/// <summary>JSR 385 unit numeric overload and transform compatibility tests.</summary>
public sealed class JavaUnitAdapterOverloadTests
{
 [Fact] public void MultiplyBoxedDecimal()
 {
  var u=new JavaUnitAdapter(UnitRegistry.CreateStandard().Resolve("A"));
  Assert.Equal(2,u.Multiply((IConvertible)2m).ToSystemConverter().Convert(1),10);
 }
 [Fact] public void DivideBoxedDecimal()
 {
  var u=new JavaUnitAdapter(UnitRegistry.CreateStandard().Resolve("A"));
  Assert.Equal(.5,u.Divide((IConvertible)2m).ToSystemConverter().Convert(1),10);
 }
 [Fact] public void ShiftBoxedDecimal()
 {
  var u=new JavaUnitAdapter(UnitRegistry.CreateStandard().Resolve("K"));
  Assert.Equal(273.15,u.Shift((IConvertible)273.15m).ToSystemConverter().Convert(0),10);
 }
 [Fact] public void TransformAffineConverter()
 {
  var u=new JavaUnitAdapter(UnitRegistry.CreateStandard().Resolve("K"));
  var shifted=u.Transform(new JavaUnitConverterAdapter(new UnitConversion(1,273.15)));
  Assert.Equal(273.15,shifted.ToSystemConverter().Convert(0),10);
 }
 [Fact] public void NullNumberRejected()
 {
  var u=new JavaUnitAdapter(UnitRegistry.CreateStandard().Resolve("A"));
  Assert.Throws<ArgumentNullException>(()=>u.Multiply((IConvertible)null!));
 }
 [Fact] public void ApiSurfaceContainsNumberOverloads()
 {
  var type=typeof(JavaUnitAdapter);
  foreach(var method in new[]{"Multiply","Divide","Shift"})
   Assert.NotNull(type.GetMethod(method,new[]{typeof(IConvertible)}));
  Assert.NotNull(type.GetMethod("Transform",new[]{typeof(JavaUnitConverterAdapter)}));
 }
}
