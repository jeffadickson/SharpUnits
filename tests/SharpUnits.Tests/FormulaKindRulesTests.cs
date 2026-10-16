using SharpUnits;
using Xunit;
namespace SharpUnits.Tests;
/// <summary>Semantic electrical formula kind inference tests.</summary>
public sealed class FormulaKindRulesTests
{
 private static readonly IReadOnlyDictionary<string,FormulaType> Symbols=new Dictionary<string,FormulaType>
 {
  ["v"]=new(RationalDimension.From(Dimension.Voltage),QuantityKind.Voltage),
  ["i"]=new(RationalDimension.From(Dimension.ElectricCurrent),QuantityKind.Current),
  ["r"]=new(RationalDimension.From(Dimension.Resistance),QuantityKind.Resistance)
 };
 [Fact] public void CurrentTimesResistanceInfersVoltageKind()
 {
  var result=FormulaAnalyzer.Analyze("i*r",Symbols);
  Assert.Empty(result.Diagnostics);
  Assert.Equal(QuantityKind.Voltage,result.Result!.Kind);
 }
 [Fact] public void ResistanceTimesCurrentIsCommutative()
 {
  Assert.Equal(QuantityKind.Voltage,FormulaAnalyzer.Analyze("r*i",Symbols).Result!.Kind);
 }
 [Fact] public void VoltageDividedByResistanceInfersCurrentKind()
 {
  Assert.Equal(QuantityKind.Current,FormulaAnalyzer.Analyze("v/r",Symbols).Result!.Kind);
 }
 [Fact] public void VoltageDividedByCurrentInfersResistanceKind()
 {
  Assert.Equal(QuantityKind.Resistance,FormulaAnalyzer.Analyze("v/i",Symbols).Result!.Kind);
 }
 [Fact] public void EqualityChecksInferredKind()
 {
  var result=FormulaAnalyzer.Analyze("v = i*r",Symbols);
  Assert.Empty(result.Diagnostics);
  Assert.Equal(QuantityKind.Voltage,result.Result!.Kind);
 }
}
