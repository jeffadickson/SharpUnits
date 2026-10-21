using SharpUnits;
using Xunit;
namespace SharpUnits.Tests;
/// <summary>34 behavioral tests developed during local 50-iteration QuantityInterfaceTest review.</summary>
public sealed class QuantityParityBehaviorTests
{
 private static UnitDefinition U(string symbol)=>UnitRegistry.CreateStandard().Resolve(symbol);
 private static JavaQuantityAdapter Q(double magnitude,string symbol)=>new(magnitude,U(symbol));
 [Fact] public void I17_DisplayValue()
 {
  Assert.Equal(250,Q(250,"mA").Value,10);
 }
 [Fact] public void I18_ConvertToAmpere()
 {
  Assert.Equal(.25,Q(250,"mA").To(U("A")).Value,10);
 }
 [Fact] public void I19_ConvertToMilliampere()
 {
  Assert.Equal(250,Q(.25,"A").To(U("mA")).Value,10);
 }
 [Fact] public void I20_ConversionPreservesSi()
 {
  Assert.Equal(Q(250,"mA").SiQuantity,Q(250,"mA").To(U("A")).SiQuantity);
 }
 [Fact] public void I21_RejectIncompatibleTarget()
 {
  Assert.Throws<InvalidOperationException>(()=>Q(5,"V").To(U("A")));
 }
 [Fact] public void I22_EquivalentAcrossUnits()
 {
  Assert.True(Q(250,"mA").IsEquivalentTo(Q(.25,"A")));
 }
 [Fact] public void I23_NotEquivalentAcrossDimensions()
 {
  Assert.False(Q(5,"V").IsEquivalentTo(Q(5,"A")));
 }
 [Fact] public void I24_DifferentRepresentationsNotEqual()
 {
  Assert.False(Q(250,"mA").Equals(Q(.25,"A")));
 }
 [Fact] public void I25_SameRepresentationEqual()
 {
  Assert.True(Q(250,"mA").Equals(Q(250,"mA")));
 }
 [Fact] public void I26_EqualHashes()
 {
  Assert.Equal(Q(5,"V").GetHashCode(),Q(5,"V").GetHashCode());
 }
 [Fact] public void I27_CompareAcrossUnits()
 {
  Assert.Equal(0,Q(1000,"mA").CompareTo(Q(1,"A")));
 }
 [Fact] public void I28_CompareIncompatibleReject()
 {
  Assert.Throws<InvalidOperationException>(()=>Q(1,"A").CompareTo(Q(1,"V")));
 }
 [Fact] public void I29_AddCompatible()
 {
  Assert.Equal(1.25,Q(1,"A").Add(Q(250,"mA")).Value,10);
 }
 [Fact] public void I30_AddIncompatibleReject()
 {
  Assert.Throws<InvalidOperationException>(()=>Q(1,"A").Add(Q(1,"V")));
 }
 [Fact] public void I31_SubtractCompatible()
 {
  Assert.Equal(.75,Q(1,"A").Subtract(Q(250,"mA")).Value,10);
 }
 [Fact] public void I32_SubtractAffineRetainsAbsoluteScale()
 {
  var d=Q(100,"°C").Subtract(Q(20,"°C")); Assert.Equal(JavaQuantityScale.Absolute,d.Scale); Assert.Equal(-193.15,d.Value,9);
 }
 [Fact] public void I33_MultiplyElectrical()
 {
  var p=Q(.25,"A").Multiply(Q(4,"kΩ")); Assert.Equal(RationalDimension.From(Dimension.Voltage),p.SiQuantity.RationalDimension); Assert.Equal(1000,p.Value,8);
 }
 [Fact] public void I34_DivideElectrical()
 {
  var p=Q(12,"V").Divide(Q(4,"Ω")); Assert.Equal(RationalDimension.From(Dimension.ElectricCurrent),p.SiQuantity.RationalDimension); Assert.Equal(3,p.Value,8);
 }
 [Fact] public void I35_DivideQuantityByZero()
 {
  Assert.Throws<DivideByZeroException>(()=>Q(12,"V").Divide(Q(0,"Ω")));
 }
 [Fact] public void I36_InverseResistance()
 {
  var p=Q(2,"Ω").Inverse(); Assert.Equal(.5,p.Value,9); Assert.Equal(RationalDimension.From(Dimension.None)-RationalDimension.From(Dimension.Resistance),p.SiQuantity.RationalDimension);
 }
 [Fact] public void I37_InverseZeroRejected()
 {
  Assert.Throws<DivideByZeroException>(()=>Q(0,"A").Inverse());
 }
 [Fact] public void I38_ScalarMultiply()
 {
  Assert.Equal(6,Q(2,"A").Multiply((IConvertible)3m).Value,10);
 }
 [Fact] public void I39_ScalarDivide()
 {
  Assert.Equal(2,Q(6,"A").Divide((IConvertible)3m).Value,10);
 }
 [Fact] public void I40_ScalarDivideZero()
 {
  Assert.Throws<DivideByZeroException>(()=>Q(6,"A").Divide((IConvertible)0m));
 }
 [Fact] public void I41_AffineAbsoluteScalarUsesSystemValue()
 {
  Assert.Equal(313.15,Q(20,"°C").Multiply((IConvertible)2m).Value,9);
 }
 [Fact] public void I42_KelvinScalarAllowed()
 {
  Assert.Equal(600,Q(300,"K").Multiply((IConvertible)2m).Value,10);
 }
 [Fact] public void I43_AbsoluteAffineAdditionUsesSystemValues()
 {
  Assert.Equal(303.15,Q(20,"°C").Add(Q(10,"°C")).Value,9);
 }
 [Fact] public void I44_NonfiniteValueRejected()
 {
  Assert.Throws<ArgumentOutOfRangeException>(()=>Q(double.NaN,"A"));
 }
 [Fact] public void I45_InvalidScaleRejected()
 {
  Assert.Throws<ArgumentOutOfRangeException>(()=>new JavaQuantityAdapter(1,U("A"),(JavaQuantityScale)99));
 }
 [Fact] public void I46_WrongKindRejected()
 {
  Assert.Throws<InvalidOperationException>(()=>Q(5,"V").AsType(QuantityKind.Current,QuantityKindRegistry.CreateStandard()));
 }
 [Fact] public void I47_CorrectKindAccepted()
 {
  var q=Q(5,"V"); Assert.Same(q,q.AsType(QuantityKind.Voltage,QuantityKindRegistry.CreateStandard()));
 }
 [Fact] public void I48_NegativeQuantityAllowed()
 {
  Assert.Equal(-5,Q(-5,"A").Value,10);
 }
 [Fact] public void I49_ToStringContainsUnit()
 {
  Assert.Contains("mA",Q(5,"mA").ToString());
 }
 [Fact] public void I50_ConversionDoesNotMutateOriginal()
 {
  var q=Q(500,"mA"); _=q.To(U("A")); Assert.Equal(500,q.Value,10);
 }
}
