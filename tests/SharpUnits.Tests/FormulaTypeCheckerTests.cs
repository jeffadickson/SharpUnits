using SharpUnits;
using Xunit;
namespace SharpUnits.Tests;
/// <summary>Engineering formula static type-checking regression tests.</summary>
public sealed class FormulaTypeCheckerTests
{
 private static readonly IReadOnlyDictionary<string,FormulaType> Variables=new Dictionary<string,FormulaType>
 {
  ["v"]=new(RationalDimension.From(Dimension.Voltage),QuantityKind.Voltage),
  ["i"]=new(RationalDimension.From(Dimension.ElectricCurrent),QuantityKind.Current),
  ["r"]=new(RationalDimension.From(Dimension.Resistance),QuantityKind.Resistance),
  ["x"]=new(RationalDimension.From(Dimension.None))
 };
 [Fact] public void VoltageDividedByResistanceIsCurrent()
 {
  var expression=new FormulaBinary(FormulaOperator.Divide,new FormulaVariable("v"),new FormulaVariable("r"));
  var result=FormulaTypeChecker.Check(expression,Variables);
  Assert.Empty(result.Diagnostics);
  Assert.Equal(RationalDimension.From(Dimension.ElectricCurrent),result.Result!.Dimension);
 }
 [Fact] public void VoltagePlusCurrentRejected()
 {
  var result=FormulaTypeChecker.Check(new FormulaBinary(FormulaOperator.Add,new FormulaVariable("v"),new FormulaVariable("i")),Variables);
  Assert.Contains(result.Diagnostics,d=>d.Code=="SU-FORM-003");
 }
 [Fact] public void SineOfVoltageRejected()
 {
  var result=FormulaTypeChecker.Check(new FormulaFunction("sin",new FormulaVariable("v")),Variables);
  Assert.Contains(result.Diagnostics,d=>d.Code=="SU-FORM-009");
 }
 [Fact] public void SineOfDimensionlessAccepted()
 {
  var result=FormulaTypeChecker.Check(new FormulaFunction("sin",new FormulaVariable("x")),Variables);
  Assert.Empty(result.Diagnostics);
  Assert.Equal(RationalDimension.From(Dimension.None),result.Result!.Dimension);
 }
 [Fact] public void SquareRootOfVoltageHasFractionalDimensions()
 {
  var result=FormulaTypeChecker.Check(new FormulaFunction("sqrt",new FormulaVariable("v")),Variables);
  Assert.Equal(new Rational(1,2),result.Result!.Dimension.Mass);
 }
 [Fact] public void UnknownSymbolProducesDiagnostic()
 {
  var result=FormulaTypeChecker.Check(new FormulaVariable("unknown"),Variables);
  Assert.Contains(result.Diagnostics,d=>d.Code=="SU-FORM-001");
 }
 [Fact] public void DimensionalExponentRejected()
 {
  var result=FormulaTypeChecker.Check(new FormulaBinary(FormulaOperator.Power,new FormulaVariable("v"),new FormulaVariable("i")),Variables);
  Assert.Contains(result.Diagnostics,d=>d.Code=="SU-FORM-005");
 }
 [Fact] public void IntegerPowerProducesSquaredDimensions()
 {
  var result=FormulaTypeChecker.Check(new FormulaBinary(FormulaOperator.Power,new FormulaVariable("i"),new FormulaConstant(2)),Variables);
  Assert.Equal(RationalDimension.From(Dimension.ElectricCurrent).Pow(2),result.Result!.Dimension);
 }
}
