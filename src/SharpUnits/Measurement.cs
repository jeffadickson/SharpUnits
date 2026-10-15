namespace SharpUnits;
/// <summary>Measured SI quantity and independent standard uncertainty (1σ).</summary>
/// <remarks>Assumes uncorrelated measurements; correlated quantities require covariance-aware propagation.</remarks>
public readonly record struct Measurement
{
 /// <summary>Measured physical quantity.</summary>
 public Quantity Value { get; }
 /// <summary>Standard uncertainty expressed in coherent SI units.</summary>
 public double StandardUncertainty { get; }
 /// <summary>Constructs a measurement with finite nonnegative standard uncertainty.</summary>
 public Measurement(Quantity value,double standardUncertainty)
 {
  if(!double.IsFinite(standardUncertainty)||standardUncertainty<0)throw new ArgumentOutOfRangeException(nameof(standardUncertainty));
  Value=value;
  StandardUncertainty=standardUncertainty;
 }
 /// <summary>Adds independent measurements of compatible dimensions.</summary>
 public static Measurement operator +(Measurement a,Measurement b)
  =>new(a.Value+b.Value,Math.Sqrt(a.StandardUncertainty*a.StandardUncertainty+b.StandardUncertainty*b.StandardUncertainty));
 /// <summary>Multiplies independent measurements using first-order absolute uncertainty propagation.</summary>
 public static Measurement operator *(Measurement a,Measurement b)
 {
  var uncertainty=Math.Sqrt(Math.Pow(b.Value.SiValue*a.StandardUncertainty,2)+Math.Pow(a.Value.SiValue*b.StandardUncertainty,2));
  return new(a.Value*b.Value,uncertainty);
 }
}