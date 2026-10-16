using SharpUnits;
using Xunit;
namespace SharpUnits.Tests;
/// <summary>Explicit temperature conversion boundary tests.</summary>
public sealed class TemperatureQuantityBridgeTests
{
 [Fact] public void AbsoluteRoundTrip()
 {
  var absolute=AbsoluteTemperature.FromCelsius(25);
  Assert.Equal(absolute,absolute.ToQuantity().AsAbsoluteTemperature());
 }
 [Fact] public void SignedDifferenceRoundTrip()
 {
  var difference=new TemperatureDifference(-5);
  Assert.Equal(difference,difference.ToQuantity().AsTemperatureDifference());
 }
 [Fact] public void WrongDimensionsRejected()
 {
  Assert.Throws<InvalidOperationException>(()=>Electrical.Volts(5).AsAbsoluteTemperature());
 }
}
