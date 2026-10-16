using System.Text.Json;
using System.Text.Json.Serialization;
namespace SharpUnits;
/// <summary>Versioned persistence envelope for semantic quantities.</summary>
public sealed record SemanticQuantityRecord
{
 /// <summary>Schema version.</summary>
 public int Version {get;init;}=1;
 /// <summary>Semantic kind identifier.</summary>
 public string Kind {get;init;}="";
 /// <summary>Coherent SI magnitude.</summary>
 public double SiValue {get;init;}
 /// <summary>Optional presentation unit.</summary>
 public string? DisplayUnit {get;init;}
}
/// <summary>Strict semantic quantity JSON persistence boundary.</summary>
public static class SemanticQuantityJson
{
 private static readonly JsonSerializerOptions Options=new()
 {
  PropertyNamingPolicy=JsonNamingPolicy.CamelCase,
  NumberHandling=JsonNumberHandling.Strict
 };
 /// <summary>Serializes a validated quantity.</summary>
 public static string Serialize(SemanticQuantity value,QuantityKindRegistry kinds,string? displayUnit=null,UnitRegistry? units=null)
 {
  ArgumentNullException.ThrowIfNull(kinds);
  if(value.Value.RationalDimension!=kinds.DimensionOf(value.Kind))throw new InvalidOperationException("Kind and dimensions disagree.");
  ValidateUnit(displayUnit,value.Value,units);
  if(!double.IsFinite(value.Value.SiValue))throw new ArgumentOutOfRangeException(nameof(value));
  return JsonSerializer.Serialize(new SemanticQuantityRecord{Kind=value.Kind.Id,SiValue=value.Value.SiValue,DisplayUnit=displayUnit},Options);
 }
 /// <summary>Deserializes using trusted semantic and unit registries.</summary>
 public static SemanticQuantity Deserialize(string json,QuantityKindRegistry kinds,UnitRegistry? units=null)
 {
  ArgumentNullException.ThrowIfNull(json);
  ArgumentNullException.ThrowIfNull(kinds);
  var record=JsonSerializer.Deserialize<SemanticQuantityRecord>(json,Options)??throw new JsonException("Missing record.");
  if(record.Version!=1)throw new NotSupportedException("Unsupported schema version.");
  if(string.IsNullOrWhiteSpace(record.Kind)||!double.IsFinite(record.SiValue))throw new JsonException("Invalid record.");
  var kind=new QuantityKind(record.Kind);
  var value=new Quantity(record.SiValue,kinds.DimensionOf(kind));
  ValidateUnit(record.DisplayUnit,value,units);
  return new SemanticQuantity(value,kind,kinds);
 }
 private static void ValidateUnit(string? symbol,Quantity value,UnitRegistry? units)
 {
  if(symbol is null)return;
  if(units is null)throw new ArgumentException("Unit registry required.",nameof(units));
  if(units.Resolve(symbol).RationalDimension!=value.RationalDimension)throw new InvalidOperationException("Display unit dimension mismatch.");
 }
}
