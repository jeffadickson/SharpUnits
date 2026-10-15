// Adapted behavioral assertions from JSR 385 TCK UnitConversionTest, section 4.2.3.
// Original copyright (c) 2005-2023 Jean-Marie Dautelle, Werner Keil, Otavio Santana.
// Upstream BSD-3-Clause license; source at
// https://github.com/unitsofmeasurement/unit-tck/blob/96da910db2a9aca61580a14125dde94b5f1c0285/src/main/java/tech/units/tck/tests/unit/UnitConversionTest.java
using SharpUnits;
using Xunit;
namespace SharpUnits.Tests;
/// <summary>Behavioral xUnit equivalents for selected TCK converter assertions.</summary>
public sealed class TckUnitConversionTests
{
 [Fact] public void Tck423A2_ConverterValueEquality()=>Assert.Equal(UnitConversion.Celsius,new UnitConversion(1,273.15));
 [Fact] public void Tck423A3_ConverterHashCode()=>Assert.Equal(UnitConversion.Celsius.GetHashCode(),new UnitConversion(1,273.15).GetHashCode());
 [Fact] public void Tck423A4_Inverse()=>Assert.Equal(20,UnitConversion.Celsius.Inverse().ToSi(UnitConversion.Celsius.ToSi(20)),10);
 [Fact] public void Tck423A5_Identity()=>Assert.True(UnitConversion.Linear(1).IsIdentity);
 [Fact] public void Tck423A6_Linearity()
 {
  Assert.True(UnitConversion.Linear(1000).IsLinear);
  Assert.False(UnitConversion.Celsius.IsLinear);
 }
 [Fact] public void Tck423A7A8_Convert()=>Assert.Equal(373.15,UnitConversion.Celsius.ToSi(100),10);
 [Fact] public void Tck423A9_Composition()=>Assert.Equal(22,new UnitConversion(2,3).Then(new UnitConversion(5,7)).ToSi(0),10);
}
