using SharpUnits;
using Xunit;
namespace SharpUnits.Tests;
/// <summary>Backward-compatibility tests for canonical rational quantity migration.</summary>
public sealed class RationalQuantityMigrationTests
{
 [Fact] public void LegacyConstructionPreservesDimension()
 {
  var voltage=SiQuantities.Volts(12);
  Assert.Equal(Dimension.Voltage,voltage.Dimension);
  Assert.Equal(RationalDimension.From(Dimension.Voltage),voltage.RationalDimension);
 }
 [Fact] public void OhmsLawUsesCanonicalDimensions()
 {
  var voltage=SiQuantities.Amperes(.25)*SiQuantities.Ohms(4000);
  Assert.Equal(Dimension.Voltage,voltage.Dimension);
  Assert.Equal(1000,voltage.SiValue,10);
 }
 [Fact] public void FractionalPowerIsExactInDimensions()
 {
  var root=SiQuantities.Volts(16).Pow(new Rational(1,2));
  Assert.Equal(new Rational(1,2),root.RationalDimension.Mass);
  Assert.Equal(4,root.SiValue,10);
  Assert.Throws<InvalidOperationException>(()=>_ = root.Dimension);
  Assert.Equal(Dimension.Voltage,root.Pow(2).Dimension);
 }
 [Fact] public void IncompatibleAdditionStillRejected()
 {
  Assert.Throws<InvalidOperationException>(()=>SiQuantities.Volts(1)+SiQuantities.Amperes(1));
 }
 [Fact] public void ExactIntegerConversionRejectsOverflow()
 {
  var d=new RationalDimension(new Rational(long.MaxValue,1),0,0,0,0,0,0);
  Assert.Throws<OverflowException>(()=>d.ToIntegerDimension());
 }
}
