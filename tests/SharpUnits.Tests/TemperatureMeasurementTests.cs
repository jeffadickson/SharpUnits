using System.Text.Json;
using SharpUnits;
using Xunit;
namespace SharpUnits.Tests;
/// <summary>Temperature kind preservation and arithmetic tests.</summary>
public sealed class TemperatureMeasurementTests
{
 [Fact] public void AbsoluteJsonRoundTrip()
 {
  var t=TemperatureMeasurement.From(AbsoluteTemperature.FromCelsius(25));
  var copy=JsonSerializer.Deserialize<TemperatureMeasurement>(JsonSerializer.Serialize(t));
  Assert.Equal(t,copy);
 }
 [Fact] public void AbsoluteSubtractionProducesDifference()
 {
  var a=TemperatureMeasurement.From(AbsoluteTemperature.FromCelsius(30));
  var b=TemperatureMeasurement.From(AbsoluteTemperature.FromCelsius(20));
  Assert.Equal(10,(a-b).AsDifference().Kelvin,10);
 }
 [Fact] public void AbsoluteAdditionRejected()
 {
  var a=TemperatureMeasurement.From(AbsoluteTemperature.FromCelsius(20));
  Assert.Throws<InvalidOperationException>(()=>_ = a+a);
 }
 [Fact] public void NegativeKelvinRejected()=>Assert.Throws<ArgumentOutOfRangeException>(()=>new TemperatureMeasurement(-1,TemperatureKind.Absolute));
}
