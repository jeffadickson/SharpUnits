using SharpUnits;
using Xunit;
namespace SharpUnits.Tests;
/// <summary>Exact assertion-ID mappings for 16 Java QuantityInterfaceTest public API requirements.</summary>
/// <remarks>CLR reflection analogues only; Java binary compatibility and official TCK closure are not implied.</remarks>
public sealed class QuantityParitySurfaceTests
{
 private static readonly Type T=typeof(JavaQuantityAdapter);
 private static void Method(string name,Type result,params Type[] arguments)
 {
  var method=T.GetMethod(name,arguments);
  Assert.NotNull(method);
  Assert.Equal(result,method.ReturnType);
  Assert.Equal(T,method.DeclaringType);
  Assert.True(method.IsPublic);
 }
 private static void Property(string name,Type result)
 {
  var property=T.GetProperty(name);
  Assert.NotNull(property);
  Assert.Equal(result,property.PropertyType);
  Assert.True(property.GetMethod!.IsPublic);
  Assert.Null(property.SetMethod);
 }
 [Fact] public void I01_431A1_AsType()=>Method("AsType",T,typeof(QuantityKind),typeof(QuantityKindRegistry));
 [Fact] public void I02_431A2_Equals()=>Method("Equals",typeof(bool),typeof(object));
 [Fact] public void I03_431A3_GetScale()=>Property("Scale",typeof(JavaQuantityScale));
 [Fact] public void I04_431A4_GetUnit()=>Property("Unit",typeof(UnitDefinition));
 [Fact] public void I05_431A5_GetValue()=>Property("Value",typeof(double));
 [Fact] public void I06_431A6_HashCode()=>Method("GetHashCode",typeof(int));
 [Fact] public void I07_431A7_Equivalent()=>Method("IsEquivalentTo",typeof(bool),T);
 [Fact] public void I08_431A8_Inverse()=>Method("Inverse",T);
 [Fact] public void I09_4311A1_Add()=>Method("Add",T,T);
 [Fact] public void I10_4311A4_MultiplyNumber()=>Method("Multiply",T,typeof(IConvertible));
 [Fact] public void I11_4311A5_DivideNumber()=>Method("Divide",T,typeof(IConvertible));
 [Fact] public void I12_4311A6_Subtract()=>Method("Subtract",T,T);
 [Fact] public void I13_4312A1_DivideQuantity()=>Method("Divide",T,T);
 [Fact] public void I14_4312A2_MultiplyQuantity()=>Method("Multiply",T,T);
 [Fact] public void I15_431A9_To()=>Method("To",T,typeof(UnitDefinition));
 [Fact] public void I16_431A10_Comparable()=>Assert.True(typeof(IComparable<JavaQuantityAdapter>).IsAssignableFrom(T));
}
