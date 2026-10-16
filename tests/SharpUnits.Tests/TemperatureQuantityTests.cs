using SharpUnits;
using Xunit;
namespace SharpUnits.Tests;
/// <summary>Absolute temperature and temperature-difference semantic safety tests.</summary>
public sealed class TemperatureQuantityTests
{
 [Fact] public void AbsoluteSubtractionProducesDifference()
 {
  var difference=AbsoluteTemperature.FromCelsius(100)-AbsoluteTemperature.FromCelsius(20);
  Assert.Equal(80,difference.Kelvin,10);
 }
 [Fact] public void AddingDifferenceProducesAbsolute()
 {
  var result=AbsoluteTemperature.FromCelsius(20)+new TemperatureDifference(10);
  Assert.Equal(303.15,result.Kelvin,10);
 }
 [Fact] public void RejectNegativeAbsoluteKelvin()
 {
  Assert.Throws<ArgumentOutOfRangeException>(()=>new AbsoluteTemperature(-1));
 }
 [Fact] public void TemperatureDifferenceCanBeNegative()
 {
  var result=new TemperatureDifference(2)-new TemperatureDifference(5);
  Assert.Equal(-3,result.Kelvin,10);
 }
}
