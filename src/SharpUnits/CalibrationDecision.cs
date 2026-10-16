namespace SharpUnits;
/// <summary>Disposition of an observation against acceptance limits.</summary>
public enum MeasurementDisposition
{
 /// <summary>Entire interval lies within guarded acceptance region.</summary>
 Pass,
 /// <summary>Entire interval lies outside the original acceptance region.</summary>
 Fail,
 /// <summary>Interval overlaps an acceptance boundary.</summary>
 Indeterminate,
 /// <summary>Calibration provenance is invalid.</summary>
 InvalidCalibration
}
/// <summary>Calibration validity interval and instrument provenance.</summary>
public sealed record CalibrationRecord
{
 /// <summary>Calibration record identifier.</summary>
 public string Id {get;}
 /// <summary>Instrument identifier.</summary>
 public string InstrumentId {get;}
 /// <summary>Inclusive validity start.</summary>
 public DateTimeOffset ValidFrom {get;}
 /// <summary>Exclusive validity end.</summary>
 public DateTimeOffset ValidUntil {get;}
 /// <summary>Constructs a valid calibration interval.</summary>
 public CalibrationRecord(string id,string instrumentId,DateTimeOffset validFrom,DateTimeOffset validUntil)
 {
  if(string.IsNullOrWhiteSpace(id))throw new ArgumentException("Calibration ID required.",nameof(id));
  if(string.IsNullOrWhiteSpace(instrumentId))throw new ArgumentException("Instrument ID required.",nameof(instrumentId));
  if(validUntil<=validFrom)throw new ArgumentException("Calibration interval must be positive.",nameof(validUntil));
  Id=id;InstrumentId=instrumentId;ValidFrom=validFrom;ValidUntil=validUntil;
 }
 /// <summary>Validates observation identity and timestamp.</summary>
 public bool IsValidFor(InstrumentMeasurement observation)=>
  observation.CalibrationId==Id&&observation.InstrumentId==InstrumentId&&
  observation.Timestamp>=ValidFrom&&observation.Timestamp<ValidUntil;
}
/// <summary>Deterministic interval-based measurement decision rule.</summary>
/// <remarks>Does not establish a probability of conformity.</remarks>
public sealed record MeasurementDecisionRule
{
 /// <summary>Expanded uncertainty multiplier.</summary>
 public double CoverageFactor {get;}
 /// <summary>Guard band in coherent SI units.</summary>
 public double GuardBandSi {get;}
 /// <summary>Creates a validated decision rule.</summary>
 public MeasurementDecisionRule(double coverageFactor,double guardBandSi=0)
 {
  if(!double.IsFinite(coverageFactor)||coverageFactor<0)throw new ArgumentOutOfRangeException(nameof(coverageFactor));
  if(!double.IsFinite(guardBandSi)||guardBandSi<0)throw new ArgumentOutOfRangeException(nameof(guardBandSi));
  CoverageFactor=coverageFactor;GuardBandSi=guardBandSi;
 }
 /// <summary>Evaluates a measurement against limits and calibration.</summary>
 public MeasurementDisposition Evaluate(InstrumentMeasurement observation,QuantityAcceptanceLimit limits,CalibrationRecord calibration)
 {
  ArgumentNullException.ThrowIfNull(observation);
  ArgumentNullException.ThrowIfNull(limits);
  ArgumentNullException.ThrowIfNull(calibration);
  if(!calibration.IsValidFor(observation))return MeasurementDisposition.InvalidCalibration;
  if(observation.Reading.Kind!=limits.Kind)throw new InvalidOperationException("Measurement and limits have different kinds.");
  var margin=CoverageFactor*observation.StandardUncertainty;
  if(!double.IsFinite(margin))throw new OverflowException("Expanded uncertainty is not finite.");
  var low=observation.Reading.Value.SiValue-margin;
  var high=observation.Reading.Value.SiValue+margin;
  if(!double.IsFinite(low)||!double.IsFinite(high))throw new OverflowException("Expanded bounds are not finite.");
  if(high<limits.MinimumSi||low>limits.MaximumSi)return MeasurementDisposition.Fail;
  var guardedMinimum=limits.MinimumSi+GuardBandSi;
  var guardedMaximum=limits.MaximumSi-GuardBandSi;
  if(guardedMinimum<=guardedMaximum&&low>=guardedMinimum&&high<=guardedMaximum)return MeasurementDisposition.Pass;
  return MeasurementDisposition.Indeterminate;
 }
}
