using SharpUnits;
using Xunit;
namespace SharpUnits.Tests;
/// <summary>Tests independent and correlated uncertainty propagation.</summary>
public sealed class UncertaintyBudgetTests
{
 [Fact] public void IndependentContributionsCombineInQuadrature()
 {
  var b=new UncertaintyBudget(new[]{new UncertaintyComponent("meter",.3,1),new UncertaintyComponent("cal",.4,1)});
  Assert.Equal(.5,b.CombinedStandardUncertainty,12);
  Assert.Equal(1,b.ExpandedUncertainty(2),12);
 }
 [Fact] public void FullyCorrelatedContributionsAddLinearly()
 {
  var b=new UncertaintyBudget(new[]{new UncertaintyComponent("a",1,1),new UncertaintyComponent("b",2,1)},new double[,]{{1,1},{1,1}});
  Assert.Equal(3,b.CombinedStandardUncertainty,12);
 }
 [Fact] public void AnticorrelationCanCancel()
 {
  var b=new UncertaintyBudget(new[]{new UncertaintyComponent("a",1,1),new UncertaintyComponent("b",1,1)},new double[,]{{1,-1},{-1,1}});
  Assert.Equal(0,b.CombinedStandardUncertainty,12);
 }
 [Fact] public void SensitivityCoefficientIsApplied()
 {
  var b=new UncertaintyBudget(new[]{new UncertaintyComponent("resistor",.1,3)});
  Assert.Equal(.3,b.CombinedStandardUncertainty,12);
 }
 [Fact] public void RejectDuplicateNames()=>Assert.Throws<ArgumentException>(()=>
  new UncertaintyBudget(new[]{new UncertaintyComponent("x",1,1),new UncertaintyComponent("x",1,1)}));
 [Fact] public void RejectNonSymmetricCorrelation()=>Assert.Throws<ArgumentException>(()=>
  new UncertaintyBudget(new[]{new UncertaintyComponent("x",1,1),new UncertaintyComponent("y",1,1)},new double[,]{{1,.2},{.3,1}}));
 [Fact] public void RejectNonPositiveSemidefiniteCorrelation()=>Assert.Throws<ArgumentException>(()=>
  new UncertaintyBudget(new[]{new UncertaintyComponent("x",1,1),new UncertaintyComponent("y",1,1),new UncertaintyComponent("z",1,1)},new double[,]{{1,-.9,-.9},{-.9,1,-.9},{-.9,-.9,1}}));
 [Fact] public void CorrelationSnapshotCannotMutateBudget()
 {
  var b=new UncertaintyBudget(new[]{new UncertaintyComponent("x",1,1)});
  var copy=b.Correlations;copy[0,0]=0;
  Assert.Equal(1,b.Correlations[0,0]);
 }
 [Fact] public void RejectNegativeCoverageFactor()
 {
  var b=new UncertaintyBudget(new[]{new UncertaintyComponent("x",1,1)});
  Assert.Throws<ArgumentOutOfRangeException>(()=>b.ExpandedUncertainty(-1));
 }
}
