using SharpUnits;
using Xunit;
namespace SharpUnits.Tests;
/// <summary>Semantic quantity JSON regression tests.</summary>
public sealed class SemanticQuantityJsonTests
{
 [Fact] public void VoltageRoundTripWithDisplayUnit()
 {
  var k=QuantityKindRegistry.CreateStandard();var u=UnitRegistry.CreateStandard();
  var q=new SemanticQuantity(Electrical.Volts(5.25),QuantityKind.Voltage,k);
  var json=SemanticQuantityJson.Serialize(q,k,"mV",u);
  Assert.Equal(q,SemanticQuantityJson.Deserialize(json,k,u));
 }
 [Fact] public void InvalidDisplayUnitRejected()
 {
  var k=QuantityKindRegistry.CreateStandard();var u=UnitRegistry.CreateStandard();
  var q=new SemanticQuantity(Electrical.Volts(5),QuantityKind.Voltage,k);
  Assert.Throws<InvalidOperationException>(()=>SemanticQuantityJson.Serialize(q,k,"A",u));
 }
 [Fact] public void UnsupportedVersionRejected()
 {
  var k=QuantityKindRegistry.CreateStandard();
  Assert.Throws<NotSupportedException>(()=>SemanticQuantityJson.Deserialize("{\"version\":2,\"kind\":\"si.voltage\",\"siValue\":5}",k));
 }
}
