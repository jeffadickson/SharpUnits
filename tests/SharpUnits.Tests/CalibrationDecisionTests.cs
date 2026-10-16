using SharpUnits;
using Xunit;
namespace SharpUnits.Tests;
/// <summary>Calibration validity and guarded acceptance decision tests.</summary>
public sealed class CalibrationDecisionTests
{
 private static readonly QuantityKindRegistry Kinds=QuantityKindRegistry.CreateStandard();
 private static InstrumentMeasurement Sample(double value,double uncertainty=0.01)=>
  new(new SemanticQuantity(Electrical.Volts(value),QuantityKind.Voltage,Kinds),uncertainty,"meter-1",DateTimeOffset.Parse("2026-01-01T00:00:00Z"),"cal-1");
 private static CalibrationRecord Calibration()=>new("cal-1","meter-1",DateTimeOffset.Parse("2025-01-01T00:00:00Z"),DateTimeOffset.Parse("2027-01-01T00:00:00Z"));
 private static QuantityAcceptanceLimit Limits()=>new(
  new SemanticQuantity(Electrical.Volts(4.9),QuantityKind.Voltage,Kinds),
  new SemanticQuantity(Electrical.Volts(5.1),QuantityKind.Voltage,Kinds));
 [Fact] public void CenterPasses()=>Assert.Equal(MeasurementDisposition.Pass,new MeasurementDecisionRule(2,.01).Evaluate(Sample(5),Limits(),Calibration()));
 [Fact] public void OutsideFails()=>Assert.Equal(MeasurementDisposition.Fail,new MeasurementDecisionRule(2,.01).Evaluate(Sample(6),Limits(),Calibration()));
 [Fact] public void BoundaryIndeterminate()=>Assert.Equal(MeasurementDisposition.Indeterminate,new MeasurementDecisionRule(2,.01).Evaluate(Sample(5.09),Limits(),Calibration()));
 [Fact] public void WrongCalibrationInvalid()=>Assert.Equal(MeasurementDisposition.InvalidCalibration,new MeasurementDecisionRule(2).Evaluate(Sample(5),Limits(),new CalibrationRecord("other","meter-1",DateTimeOffset.Parse("2025-01-01T00:00:00Z"),DateTimeOffset.Parse("2027-01-01T00:00:00Z"))));
}
