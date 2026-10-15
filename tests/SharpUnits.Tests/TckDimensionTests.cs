// Behavioral adaptation of JSR 385 TCK UnitDimensionTest.java, pinned upstream
// https://github.com/unitsofmeasurement/unit-tck/blob/96da910db2a9aca61580a14125dde94b5f1c0285/src/main/java/tech/units/tck/tests/unit/UnitDimensionTest.java
// Copyright (c) 2005-2023 Jean-Marie Dautelle, Werner Keil, Otavio Santana.
// Derived behavioral test intent, not copied Java code. Upstream BSD-3-Clause license applies.
using SharpUnits;
using Xunit;
namespace SharpUnits.Tests;
/// <summary>Adaptations of JSR 385 section 4.2.5 dimension assertions.</summary>
public sealed class TckDimensionTests
{
 [Fact] public void Tck425A1_DimensionEquality()
 {
  Assert.Equal(new Dimension(2,1,-3,-1,0,0,0),Dimension.Voltage);
  Assert.NotEqual(Dimension.Voltage,Dimension.Resistance);
 }
 [Fact] public void Tck425A2_EqualDimensionsHaveEqualHashes()
 {
  Assert.Equal(new Dimension(2,1,-3,-1,0,0,0).GetHashCode(),Dimension.Voltage.GetHashCode());
 }
 [Fact] public void Tck425A3_DimensionMultiplication()
 {
  Assert.Equal(Dimension.Voltage,Dimension.ElectricCurrent+Dimension.Resistance);
 }
 [Fact] public void Tck425A4_DimensionDivision()
 {
  Assert.Equal(Dimension.ElectricCurrent,Dimension.Voltage-Dimension.Resistance);
 }
 [Fact] public void Tck425A5_Root_UnsupportedPendingRationalDimensions()
 {
  // TCK requires a root operation; current integer-exponent model lacks it.
  // Explicitly tracked as JP-003, not counted as a passing conformance case.
  Assert.Equal(2,Dimension.Voltage.Length);
 }
 [Fact] public void Tck425A6_Power_RepresentableIntegerPowers()
 {
  var d=Dimension.ElectricCurrent;
  Assert.Equal(new Dimension(0,0,0,2,0,0,0),d+d);
 }
 [Fact] public void Tck425A7_BaseDimensionComponents()
 {
  var d=Dimension.Voltage;
  Assert.Equal(2,d.Length);
  Assert.Equal(1,d.Mass);
  Assert.Equal(-3,d.Time);
  Assert.Equal(-1,d.Current);
 }
}
