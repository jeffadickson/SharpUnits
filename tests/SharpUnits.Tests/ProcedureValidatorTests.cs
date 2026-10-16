using SharpUnits;
using Xunit;
namespace SharpUnits.Tests;
/// <summary>Static engineering procedure validation and dimension inference tests.</summary>
public sealed class ProcedureValidatorTests
{
 [Fact] public void InferResistanceFromOhmsLaw()
 {
  var result=ProcedureValidator.Validate(new[]{
   new ProcedureVariable("voltage",RationalDimension.From(Dimension.Voltage)),
   new ProcedureVariable("current",RationalDimension.From(Dimension.ElectricCurrent)),
   new ProcedureVariable("resistance")
  },new[]{new ProcedureFormula("voltage = current * resistance")});
  Assert.True(result.IsValid,string.Join("; ",result.Diagnostics.Select(d=>d.Message)));
  Assert.Equal(RationalDimension.From(Dimension.Resistance),result.Symbols["resistance"].Dimension);
 }
 [Fact] public void RejectIncompatibleFormula()
 {
  var result=ProcedureValidator.Validate(new[]{
   new ProcedureVariable("voltage",RationalDimension.From(Dimension.Voltage)),
   new ProcedureVariable("current",RationalDimension.From(Dimension.ElectricCurrent))
  },new[]{new ProcedureFormula("voltage = current")});
  Assert.False(result.IsValid);
  Assert.Contains(result.Diagnostics,d=>d.Code=="SU-FORM-013");
 }
 [Fact] public void RejectUnderdeterminedVariables()
 {
  var result=ProcedureValidator.Validate(new[]{
   new ProcedureVariable("a"),new ProcedureVariable("b")
  },new[]{new ProcedureFormula("a * b = 1")});
  Assert.False(result.IsValid);
  Assert.Contains(result.Diagnostics,d=>d.Code=="SU-PROC-004");
 }
 [Fact] public void RejectDuplicateDeclarations()
 {
  var result=ProcedureValidator.Validate(new[]{
   new ProcedureVariable("x",RationalDimension.From(Dimension.Voltage)),
   new ProcedureVariable("x",RationalDimension.From(Dimension.Voltage))
  },Array.Empty<ProcedureFormula>());
  Assert.Contains(result.Diagnostics,d=>d.Code=="SU-PROC-002");
 }
}
