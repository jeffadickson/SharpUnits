using SharpUnits;
using Xunit;
namespace SharpUnits.Tests;
/// <summary>CLR API equivalents for Java TCK UnitConversionTest assertions 423-A1 through A10.</summary>
public sealed class TckConverterApiSurfaceTests
{
 [Fact] public void Tck423A1_ProviderAvailable()=>Assert.NotNull(new JavaUnitConverterAdapter(UnitConversion.Linear(1)));
 [Fact] public void Tck423A2_EqualityMethod()=>Assert.Equal(typeof(JavaUnitConverterAdapter),typeof(JavaUnitConverterAdapter).GetMethod("Equals",new[]{typeof(object)})!.DeclaringType);
 [Fact] public void Tck423A3_HashCodeMethod()=>Assert.Equal(typeof(JavaUnitConverterAdapter),typeof(JavaUnitConverterAdapter).GetMethod("GetHashCode",Type.EmptyTypes)!.DeclaringType);
 [Fact] public void Tck423A4_InverseMethod()=>Assert.NotNull(typeof(JavaUnitConverterAdapter).GetMethod("Inverse",Type.EmptyTypes));
 [Fact] public void Tck423A5_IdentityProperty()=>Assert.NotNull(typeof(JavaUnitConverterAdapter).GetProperty("IsIdentity"));
 [Fact] public void Tck423A6_LinearProperty()=>Assert.NotNull(typeof(JavaUnitConverterAdapter).GetProperty("IsLinear"));
 [Fact] public void Tck423A7_NumericOverload()
 {
  Assert.NotNull(typeof(JavaUnitConverterAdapter).GetMethod("Convert",new[]{typeof(IConvertible)}));
  Assert.NotNull(typeof(JavaUnitConverterAdapter).GetMethod("Convert",new[]{typeof(JavaNumber)}));
 }
 [Fact] public void Tck423A8_DoubleOverload()=>Assert.NotNull(typeof(JavaUnitConverterAdapter).GetMethod("Convert",new[]{typeof(double)}));
 [Fact] public void Tck423A9_Concatenation()=>Assert.NotNull(typeof(JavaUnitConverterAdapter).GetMethod("Concatenate",new[]{typeof(JavaUnitConverterAdapter)}));
 [Fact] public void Tck423A10_ConversionSteps()=>Assert.NotNull(typeof(JavaUnitConverterAdapter).GetMethod("GetConversionSteps",Type.EmptyTypes));
 [Fact] public void AdapterInverseRoundTrip()
 {
  var c=new JavaUnitConverterAdapter(UnitConversion.Celsius);
  Assert.Equal(20,c.Inverse().Convert(c.Convert(20)),10);
 }
 [Fact] public void AdapterConcatenationPreservesSteps()
 {
  var a=new JavaUnitConverterAdapter(new UnitConversion(2,3));
  var b=new JavaUnitConverterAdapter(new UnitConversion(5,7));
  var combined=a.Concatenate(b);
  Assert.Equal(4,combined.GetConversionSteps().Count);
  Assert.Equal(67,combined.Convert(5),10);
 }
 [Fact] public void AdapterNumericOverload()
 {
  var c=new JavaUnitConverterAdapter(UnitConversion.Linear(1000));
  Assert.Equal(2000,c.Convert((IConvertible)2m),10);
 }
}
