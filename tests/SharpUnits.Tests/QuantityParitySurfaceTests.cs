using SharpUnits;
using Xunit;
namespace SharpUnits.Tests;
/// <summary>Exact assertion-ID mappings for 16 Java QuantityInterfaceTest public API requirements.</summary>
/// <remarks>CLR reflection analogues only; Java binary compatibility and official TCK closure are not implied.</remarks>
public sealed class QuantityParitySurfaceTests
{
 private static readonly Type T=typeof(JavaQuantityAdapter);
 [Fact] public void I01_431A1_AsType()=>Assert.NotNull(T.GetMethod("AsType",new[]{typeof(QuantityKind),typeof(QuantityKindRegistry)}));
 [Fact] public void I02_431A2_Equals()=>Assert.NotNull(T.GetMethod("Equals",new[]{typeof(object)}));
 [Fact] public void I03_431A3_GetScale()=>Assert.NotNull(T.GetProperty("Scale"));
 [Fact] public void I04_431A4_GetUnit()=>Assert.NotNull(T.GetProperty("Unit"));
 [Fact] public void I05_431A5_GetValue()=>Assert.NotNull(T.GetProperty("Value"));
 [Fact] public void I06_431A6_HashCode()=>Assert.NotNull(T.GetMethod("GetHashCode",Type.EmptyTypes));
 [Fact] public void I07_431A7_Equivalent()=>Assert.NotNull(T.GetMethod("IsEquivalentTo",new[]{T}));
 [Fact] public void I08_431A8_Inverse()=>Assert.NotNull(T.GetMethod("Inverse",Type.EmptyTypes));
 [Fact] public void I09_4311A1_Add()=>Assert.NotNull(T.GetMethod("Add",new[]{T}));
 [Fact] public void I10_4311A4_MultiplyNumber()=>Assert.NotNull(T.GetMethod("Multiply",new[]{typeof(IConvertible)}));
 [Fact] public void I11_4311A5_DivideNumber()=>Assert.NotNull(T.GetMethod("Divide",new[]{typeof(IConvertible)}));
 [Fact] public void I12_4311A6_Subtract()=>Assert.NotNull(T.GetMethod("Subtract",new[]{T}));
 [Fact] public void I13_4312A1_DivideQuantity()=>Assert.NotNull(T.GetMethod("Divide",new[]{T}));
 [Fact] public void I14_4312A2_MultiplyQuantity()=>Assert.NotNull(T.GetMethod("Multiply",new[]{T}));
 [Fact] public void I15_431A9_To()=>Assert.NotNull(T.GetMethod("To",new[]{typeof(UnitDefinition)}));
 [Fact] public void I16_431A10_Comparable()=>Assert.True(typeof(IComparable<JavaQuantityAdapter>).IsAssignableFrom(T));
}
