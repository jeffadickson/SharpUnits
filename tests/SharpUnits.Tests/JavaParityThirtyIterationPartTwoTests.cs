using SharpUnits;
using Xunit;
namespace SharpUnits.Tests;
/// <summary>JSR 385 behavioral analogues, local iterations 11 through 20.</summary>
public sealed class JavaParityThirtyIterationPartTwoTests
{
 private static JavaUnitAdapter U(string s)=>new(UnitRegistry.CreateStandard().Resolve(s));
 [Fact] public void KelvinToCelsius()=>Assert.Equal(0,U("K").GetConverterTo(U("°C")).Convert(273.15),9);
 [Fact] public void KelvinToFahrenheit()=>Assert.Equal(32,U("K").GetConverterTo(U("°F")).Convert(273.15),8);
 [Fact] public void CelsiusToFahrenheit()=>Assert.Equal(32,U("°C").GetConverterTo(U("°F")).Convert(0),8);
 [Fact] public void FahrenheitToKelvin()=>Assert.Equal(273.15,U("°F").GetConverterTo(U("K")).Convert(32),8);
 [Fact] public void InverseRoundTrip()
 {
  var c=U("°F").GetConverterTo(U("K"));
  Assert.Equal(451.23,c.Inverse().Convert(c.Convert(451.23)),8);
 }
 [Fact] public void ConcatenationOrder()
 {
  var a=new JavaUnitConverterAdapter(new UnitConversion(2,3));
  var b=new JavaUnitConverterAdapter(new UnitConversion(5,7));
  Assert.Equal(22,a.Concatenate(b).Convert(0),10);
 }
 [Fact] public void ConversionStepsRetained()
 {
  var a=new JavaUnitConverterAdapter(UnitConversion.Linear(2));
  var b=new JavaUnitConverterAdapter(UnitConversion.Celsius);
  Assert.Equal(2,a.Concatenate(b).GetConversionSteps().Count);
 }
 [Fact] public void ConversionStepsReadOnly()
 {
  var steps=new JavaUnitConverterAdapter(UnitConversion.Linear(2)).GetConversionSteps();
  Assert.Throws<NotSupportedException>(()=>((System.Collections.Generic.IList<ConversionStep>)steps)[0]=new ConversionStep(3,0));
 }
 [Fact] public void LinearityOfPrefix()=>Assert.True(U("mA").GetConverterTo(U("A")).IsLinear);
 [Fact] public void NonlinearityOfAffine()=>Assert.False(U("°C").GetConverterTo(U("K")).IsLinear);
}
