using System.Reflection;
using SharpUnits;
using Xunit;
namespace SharpUnits.Tests;
/// <summary>Assertion-specific CLR signature analogues for the pinned Java UnitInterfaceTest.</summary>
/// <remarks>Java generic signatures, Number precision and unit identity differ from CLR; these tests do not certify JSR 385 compliance.</remarks>
public sealed class TckUnitExactSurfaceTests
{
 private static readonly Type T=typeof(JavaUnitAdapter);
 private static void Method(string name,params Type[] parameters)=>
  Assert.NotNull(T.GetMethod(name,BindingFlags.Public|BindingFlags.Instance,null,parameters,null));
 private static void Property(string name)=>Assert.NotNull(T.GetProperty(name,BindingFlags.Public|BindingFlags.Instance));
 [Fact] public void Tck421A1_Equals()=>Method("Equals",typeof(object));
 [Fact] public void Tck421A2_Dimension()=>Property("Dimension");
 [Fact] public void Tck421A3_SystemUnit()=>Property("SystemUnit");
 [Fact] public void Tck421A4_BaseUnits()=>Property("BaseUnits");
 [Fact] public void Tck421A5_Name()=>Property("Name");
 [Fact] public void Tck421A6_Symbol()=>Property("Symbol");
 [Fact] public void Tck421A7_HashCode()=>Method("GetHashCode");
 [Fact] public void Tck421A8_Equivalent()=>Method("IsCompatible",T);
 [Fact] public void Tck421A9_ToString()=>Method("ToString");
 [Fact] public void Tck42121A1_Alternate()=>Method("Alternate",typeof(string));
 [Fact] public void Tck42121A2_DivideUnit()=>Method("Divide",T);
 [Fact] public void Tck42121A3_DivideDouble()=>Method("Divide",typeof(double));
 [Fact] public void Tck42121A4_DivideNumber()=>Method("Divide",typeof(IConvertible));
 [Fact] public void Tck42121A5_MultiplyUnit()=>Method("Multiply",T);
 [Fact] public void Tck42121A6_MultiplyDouble()=>Method("Multiply",typeof(double));
 [Fact] public void Tck42121A7_MultiplyNumber()=>Method("Multiply",typeof(IConvertible));
 [Fact] public void Tck42121A8_Prefix()=>Method("Prefix",typeof(SiPrefix));
 [Fact] public void Tck42121A9_ShiftDouble()=>Method("Shift",typeof(double));
 [Fact] public void Tck42121A10_ShiftNumber()=>Method("Shift",typeof(IConvertible));
 [Fact] public void Tck42122A1_Pow()=>Method("Pow",typeof(int));
 [Fact] public void Tck42122A2_Root()=>Method("Root",typeof(int));
 [Fact] public void Tck42122A3_Transform()=>Method("Transform",typeof(JavaUnitConverterAdapter));
 [Fact] public void Tck42123A1_Inverse()=>Method("Inverse");
 [Fact] public void Tck42124A1_Compatible()=>Method("IsCompatible",T);
 [Fact] public void Tck42124A2_AsType()=>Method("AsType",typeof(RationalDimension));
 [Fact] public void Tck42124A3_ConverterTo()=>Method("GetConverterTo",T);
 [Fact] public void Tck42124A4_ConverterToAny()=>Method("GetConverterToAny",T);
}
