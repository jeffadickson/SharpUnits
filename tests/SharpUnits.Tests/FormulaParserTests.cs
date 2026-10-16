using SharpUnits;
using Xunit;
namespace SharpUnits.Tests;
/// <summary>End-to-end textual formula parsing and dimensional diagnostic tests.</summary>
public sealed class FormulaParserTests
{
 private static readonly IReadOnlyDictionary<string,FormulaType> Symbols=new Dictionary<string,FormulaType>
 {
  ["voltage"]=new(RationalDimension.From(Dimension.Voltage),QuantityKind.Voltage),
  ["current"]=new(RationalDimension.From(Dimension.ElectricCurrent),QuantityKind.Current),
  ["resistance"]=new(RationalDimension.From(Dimension.Resistance),QuantityKind.Resistance),
  ["x"]=new(RationalDimension.From(Dimension.None))
 };
 [Fact] public void OhmsLawEqualityIsValid()
 {
  var result=FormulaAnalyzer.Analyze("voltage = current * resistance",Symbols);
  Assert.Empty(result.Diagnostics);
  Assert.Equal(RationalDimension.From(Dimension.Voltage),result.Result!.Dimension);
 }
 [Fact] public void InvalidAdditionIsDiagnosed()
 {
  var result=FormulaAnalyzer.Analyze("voltage + current",Symbols);
  Assert.Contains(result.Diagnostics,d=>d.Code=="SU-FORM-003");
 }
 [Fact] public void EqualityWithWrongDimensionsIsDiagnosed()
 {
  var result=FormulaAnalyzer.Analyze("voltage = current",Symbols);
  Assert.Contains(result.Diagnostics,d=>d.Code=="SU-FORM-013");
 }
 [Fact] public void FunctionArgumentsAreChecked()
 {
  Assert.Contains(FormulaAnalyzer.Analyze("sin(voltage)",Symbols).Diagnostics,d=>d.Code=="SU-FORM-009");
  Assert.Empty(FormulaAnalyzer.Analyze("sin(x)",Symbols).Diagnostics);
 }
 [Fact] public void ParenthesesAndPowersWork()
 {
  var result=FormulaAnalyzer.Analyze("(current * resistance)^2",Symbols);
  Assert.Equal(RationalDimension.From(Dimension.Voltage).Pow(2),result.Result!.Dimension);
 }
 [Fact] public void ScientificNotationIsDimensionless()
 {
  var result=FormulaAnalyzer.Analyze("1.2e-3 * voltage",Symbols);
  Assert.Equal(RationalDimension.From(Dimension.Voltage),result.Result!.Dimension);
 }
 [Fact] public void MalformedFormulaHasStructuredDiagnostic()
 {
  var result=FormulaAnalyzer.Analyze("voltage + (current",Symbols);
  Assert.Contains(result.Diagnostics,d=>d.Code=="SU-FORM-015");
 }
 [Fact] public void MultipleEqualitiesAreRejected()
 {
  var result=FormulaAnalyzer.Analyze("voltage = current = resistance",Symbols);
  Assert.Contains(result.Diagnostics,d=>d.Code=="SU-FORM-012");
 }
 [Fact] public void UnknownSymbolIsReported()
 {
  var result=FormulaAnalyzer.Analyze("voltage / mystery",Symbols);
  Assert.Contains(result.Diagnostics,d=>d.Code=="SU-FORM-001");
 }
 [Fact] public void UnaryMinusDoesNotChangeDimensions()
 {
  var result=FormulaAnalyzer.Analyze("-voltage",Symbols);
  Assert.Equal(RationalDimension.From(Dimension.Voltage),result.Result!.Dimension);
 }
}
