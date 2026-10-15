namespace SharpUnits;
/// <summary>Measured SI quantity and independent standard uncertainty (1σ).</summary>
/// <remarks>Assumes uncorrelated measurements; correlated quantities require covariance-aware propagation.</remarks>
public readonly record struct Measurement(Quantity Value,double StandardUncertainty)
{
 /// <summary>Constructs a measurement with finite nonnegative standard uncertainty.</summary>
 public Measurement
 {
  if(!double.IsFinite(StandardUncertainty)||StandardUncertainty<0)throw new ArgumentOutOfRangeException(nameof(StandardUncertainty));
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