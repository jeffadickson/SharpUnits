using SharpUnits;
using Xunit;
namespace SharpUnits.Tests;
/// <summary>JSR 385 behavioral analogues, local iterations 1 through 10.</summary>
public sealed class JavaParityThirtyIterationPartOneTests
{
 private static JavaUnitAdapter U(string s)=>new(UnitRegistry.CreateStandard().Resolve(s));
 [Fact] public void IdentityAmpere()=>Assert.True(U("A").GetConverterTo(U("A")).IsIdentity);
 [Fact] public void IdentityKelvin()=>Assert.Equal(123.5,U("K").GetConverterTo(U("K")).Convert(123.5),10);
 [Fact] public void MilliampereToAmpere()=>Assert.Equal(.125,U("mA").GetConverterTo(U("A")).Convert(125),10);
 [Fact] public void AmpereToMilliampere()=>Assert.Equal(125,U("A").GetConverterTo(U("mA")).Convert(.125),10);
 [Fact] public void MillivoltToVolt()=>Assert.Equal(.075,U("mV").GetConverterTo(U("V")).Convert(75),10);
 [Fact] public void VoltToMillivolt()=>Assert.Equal(750,U("V").GetConverterTo(U("mV")).Convert(.75),10);
 [Fact] public void KiloohmToOhm()=>Assert.Equal(2200,U("kΩ").GetConverterTo(U("Ω")).Convert(2.2),9);
 [Fact] public void OhmToKiloohm()=>Assert.Equal(2.2,U("Ω").GetConverterTo(U("kΩ")).Convert(2200),9);
 [Fact] public void CelsiusToKelvin()=>Assert.Equal(273.15,U("°C").GetConverterTo(U("K")).Convert(0),9);
 [Fact] public void FahrenheitToCelsius()=>Assert.Equal(100,U("°F").GetConverterTo(U("°C")).Convert(212),9);
}
