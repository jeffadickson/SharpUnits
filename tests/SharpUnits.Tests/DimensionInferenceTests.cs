using SharpUnits;
using Xunit;
namespace SharpUnits.Tests;
/// <summary>Dimensional inference for engineering equations using exact rational algebra.</summary>
public sealed class DimensionInferenceTests
{
 [Fact] public void OhmsLawInfersResistance()
 {
  var v=DimensionExpression.Of(Dimension.Voltage);
  var i=DimensionExpression.Of(Dimension.ElectricCurrent);
  var r=DimensionExpression.Variable("R");
  var result=DimensionInference.Solve(new[]{new DimensionEquation(v,i*r)});
  Assert.Equal(RationalDimension.From(Dimension.Resistance),result["R"]);
 }
 [Fact] public void InfersTwoUnknownsFromTwoIndependentEquations()
 {
  var v=DimensionExpression.Of(Dimension.Voltage);
  var i=DimensionExpression.Of(Dimension.ElectricCurrent);
  var r=DimensionExpression.Variable("R");
  var g=DimensionExpression.Variable("G");
  var result=DimensionInference.Solve(new[]{
   new DimensionEquation(v,i*r),
   new DimensionEquation(g*r,DimensionExpression.Of(Dimension.None))
  });
  Assert.Equal(RationalDimension.From(Dimension.Resistance),result["R"]);
  Assert.Equal(RationalDimension.From(Dimension.Resistance).Pow(new Rational(-1,1)),result["G"]);
 }
 [Fact] public void FractionalDimensionIsInferredExactly()
 {
  var x=DimensionExpression.Variable("x");
  var result=DimensionInference.Solve(new[]{
   new DimensionEquation(x.Pow(new Rational(2,1)),DimensionExpression.Of(Dimension.Voltage))
  });
  Assert.Equal(new Rational(1,2),result["x"].Mass);
  Assert.Equal(new Rational(-3,2),result["x"].Time);
 }
 [Fact] public void ContradictoryEquationsAreRejected()
 {
  var x=DimensionExpression.Variable("x");
  Assert.Throws<InvalidOperationException>(()=>DimensionInference.Solve(new[]{
   new DimensionEquation(x,DimensionExpression.Of(Dimension.Voltage)),
   new DimensionEquation(x,DimensionExpression.Of(Dimension.ElectricCurrent))
  }));
 }
 [Fact] public void UnderdeterminedSystemIsRejected()
 {
  var a=DimensionExpression.Variable("a");
  var b=DimensionExpression.Variable("b");
  Assert.Throws<InvalidOperationException>(()=>DimensionInference.Solve(new[]{
   new DimensionEquation(a*b,DimensionExpression.Of(Dimension.Voltage))
  }));
 }
 [Fact] public void DimensionallyConsistentKnownEquationHasNoUnknowns()
 {
  var result=DimensionInference.Solve(new[]{
   new DimensionEquation(DimensionExpression.Of(Dimension.ElectricCurrent)*DimensionExpression.Of(Dimension.Resistance),
                         DimensionExpression.Of(Dimension.Voltage))
  });
  Assert.Empty(result);
 }
 [Fact] public void InconsistentKnownEquationIsRejected()
 {
  Assert.Throws<InvalidOperationException>(()=>DimensionInference.Solve(new[]{
   new DimensionEquation(DimensionExpression.Of(Dimension.Voltage),DimensionExpression.Of(Dimension.ElectricCurrent))
  }));
 }
 [Fact] public void UnknownTermsCancelExactly()
 {
  var x=DimensionExpression.Variable("x");
  var result=DimensionInference.Solve(new[]{
   new DimensionEquation(x/x,DimensionExpression.Of(Dimension.None)),
   new DimensionEquation(x,DimensionExpression.Of(Dimension.Voltage))
  });
  Assert.Equal(RationalDimension.From(Dimension.Voltage),result["x"]);
 }
}
