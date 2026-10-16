using SharpUnits;
using Xunit;
namespace SharpUnits.Tests;
/// <summary>Instrument observation and acceptance limit tests.</summary>
public sealed class InstrumentMeasurementTests
{
 private static readonly QuantityKindRegistry Kinds=QuantityKindRegistry.CreateStandard();
 private static InstrumentMeasurement Sample(double volts,double uncertainty=0.02)=>
  new(new SemanticQuantity(Electrical.Volts(volts),QuantityKind.Voltage,Kinds),uncertainty,"meter-1",DateTimeOffset.Parse("2026-01-01T00:00:00Z"),"cal-1");
 private static QuantityAcceptanceLimit Limits()=>new(
  new SemanticQuantity(Electrical.Volts(4.9),QuantityKind.Voltage,Kinds),
  new SemanticQuantity(Electrical.Volts(5.1),QuantityKind.Voltage,Kinds));
 [Fact] public void NominalCanPassWhileGuardedFails()
 {
  Assert.True(Limits().ContainsNominal(Sample(5.09)));
  Assert.False(Limits().ContainsUncertaintyInterval(Sample(5.09),2));
 }
 [Fact] public void MeasurementRoundTrip()
 {
  var original=Sample(5.01);
  var restored=InstrumentMeasurementJson.Deserialize(InstrumentMeasurementJson.Serialize(original,Kinds),Kinds);
  Assert.Equal(original,restored);
 }
 [Fact] public void RejectNegativeUncertainty()=>Assert.Throws<ArgumentOutOfRangeException>(()=>Sample(5,-1));
}
