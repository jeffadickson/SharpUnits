using System.Text.Json;
namespace SharpUnits;
/// <summary>Instrument observation with semantic kind, uncertainty and provenance.</summary>
public sealed record InstrumentMeasurement
{
 /// <summary>Measured semantic quantity.</summary>
 public SemanticQuantity Reading {get;}
 /// <summary>Standard uncertainty in coherent SI units.</summary>
 public double StandardUncertainty {get;}
 /// <summary>Instrument identifier.</summary>
 public string InstrumentId {get;}
 /// <summary>Calibration record identifier, if available.</summary>
 public string? CalibrationId {get;}
 /// <summary>Observation timestamp.</summary>
 public DateTimeOffset Timestamp {get;}
 /// <summary>Creates a validated observation.</summary>
 public InstrumentMeasurement(SemanticQuantity reading,double standardUncertainty,string instrumentId,DateTimeOffset timestamp,string? calibrationId=null)
 {
  if(!double.IsFinite(reading.Value.SiValue))throw new ArgumentOutOfRangeException(nameof(reading));
  if(!double.IsFinite(standardUncertainty)||standardUncertainty<0)throw new ArgumentOutOfRangeException(nameof(standardUncertainty));
  if(string.IsNullOrWhiteSpace(instrumentId))throw new ArgumentException("Instrument ID required.",nameof(instrumentId));
  if(calibrationId is not null&&string.IsNullOrWhiteSpace(calibrationId))throw new ArgumentException("Calibration ID cannot be blank.",nameof(calibrationId));
  Reading=reading;StandardUncertainty=standardUncertainty;InstrumentId=instrumentId;Timestamp=timestamp;CalibrationId=calibrationId;
 }
}
/// <summary>Inclusive limits for a single semantic quantity kind.</summary>
public sealed record QuantityAcceptanceLimit
{
 /// <summary>Minimum SI magnitude.</summary>
 public double MinimumSi {get;}
 /// <summary>Maximum SI magnitude.</summary>
 public double MaximumSi {get;}
 /// <summary>Required physical kind.</summary>
 public QuantityKind Kind {get;}
 /// <summary>Constructs checked limits.</summary>
 public QuantityAcceptanceLimit(SemanticQuantity minimum,SemanticQuantity maximum)
 {
  if(minimum.Kind!=maximum.Kind||minimum.Value.RationalDimension!=maximum.Value.RationalDimension)
   throw new InvalidOperationException("Limit endpoints have different physical meanings.");
  if(!double.IsFinite(minimum.Value.SiValue)||!double.IsFinite(maximum.Value.SiValue)||minimum.Value.SiValue>maximum.Value.SiValue)
   throw new ArgumentException("Limits must be finite and ordered.");
  Kind=minimum.Kind;MinimumSi=minimum.Value.SiValue;MaximumSi=maximum.Value.SiValue;
 }
 /// <summary>Checks the central estimate only.</summary>
 public bool ContainsNominal(InstrumentMeasurement measurement)
 {
  if(measurement.Reading.Kind!=Kind)throw new InvalidOperationException("Incompatible kind.");
  var v=measurement.Reading.Value.SiValue;
  return v>=MinimumSi&&v<=MaximumSi;
 }
 /// <summary>Requires the entire symmetric uncertainty interval to be inside limits.</summary>
 public bool ContainsUncertaintyInterval(InstrumentMeasurement measurement,double coverageFactor)
 {
  if(measurement.Reading.Kind!=Kind)throw new InvalidOperationException("Incompatible kind.");
  if(!double.IsFinite(coverageFactor)||coverageFactor<0)throw new ArgumentOutOfRangeException(nameof(coverageFactor));
  var margin=coverageFactor*measurement.StandardUncertainty;
  if(!double.IsFinite(margin))throw new OverflowException("Expanded uncertainty overflow.");
  var v=measurement.Reading.Value.SiValue;
  return v-margin>=MinimumSi&&v+margin<=MaximumSi;
 }
}
/// <summary>Versioned persisted instrument measurement.</summary>
public sealed record InstrumentMeasurementRecord
{
 /// <summary>Schema version.</summary>
 public int Version {get;init;}=1;
 /// <summary>Semantic kind ID.</summary>
 public string Kind {get;init;}="";
 /// <summary>SI magnitude.</summary>
 public double SiValue {get;init;}
 /// <summary>Standard uncertainty.</summary>
 public double StandardUncertainty {get;init;}
 /// <summary>Instrument ID.</summary>
 public string InstrumentId {get;init;}="";
 /// <summary>Calibration ID.</summary>
 public string? CalibrationId {get;init;}
 /// <summary>Timestamp.</summary>
 public DateTimeOffset Timestamp {get;init;}
}
/// <summary>Strict JSON serialization of instrument observations.</summary>
public static class InstrumentMeasurementJson
{
 private static readonly JsonSerializerOptions Options=new()
 {
  PropertyNamingPolicy=JsonNamingPolicy.CamelCase,
  UnmappedMemberHandling=System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow
 };
 /// <summary>Serializes a checked measurement.</summary>
 public static string Serialize(InstrumentMeasurement measurement,QuantityKindRegistry kinds)
 {
  ArgumentNullException.ThrowIfNull(measurement);
  ArgumentNullException.ThrowIfNull(kinds);
  if(measurement.Reading.Value.RationalDimension!=kinds.DimensionOf(measurement.Reading.Kind))
   throw new InvalidOperationException("Kind and dimensions disagree.");
  return JsonSerializer.Serialize(new InstrumentMeasurementRecord
  {
   Kind=measurement.Reading.Kind.Id,SiValue=measurement.Reading.Value.SiValue,
   StandardUncertainty=measurement.StandardUncertainty,InstrumentId=measurement.InstrumentId,
   CalibrationId=measurement.CalibrationId,Timestamp=measurement.Timestamp
  },Options);
 }
 /// <summary>Deserializes and validates physical identity.</summary>
 public static InstrumentMeasurement Deserialize(string json,QuantityKindRegistry kinds)
 {
  ArgumentNullException.ThrowIfNull(json);
  ArgumentNullException.ThrowIfNull(kinds);
  var record=JsonSerializer.Deserialize<InstrumentMeasurementRecord>(json,Options)??throw new JsonException("Missing record.");
  if(record.Version!=1)throw new NotSupportedException("Unsupported schema version.");
  if(string.IsNullOrWhiteSpace(record.Kind))throw new JsonException("Missing kind.");
  var kind=new QuantityKind(record.Kind);
  var quantity=new Quantity(record.SiValue,kinds.DimensionOf(kind));
  return new InstrumentMeasurement(new SemanticQuantity(quantity,kind,kinds),record.StandardUncertainty,record.InstrumentId,record.Timestamp,record.CalibrationId);
 }
}
