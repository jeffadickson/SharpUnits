namespace SharpUnits;
/// <summary>Seven SI base dimensions with exact rational exponents.</summary>
public readonly record struct RationalDimension(Rational Length,Rational Mass,Rational Time,Rational Current,Rational Temperature,Rational Amount,Rational Luminosity)
{
 /// <summary>Converts an integer dimension without precision loss.</summary>
 public static RationalDimension From(Dimension d)=>new(d.Length,d.Mass,d.Time,d.Current,d.Temperature,d.Amount,d.Luminosity);
 /// <summary>Converts exactly to the legacy integer dimension model, or rejects fractional/overflow exponents.</summary>
 public Dimension ToIntegerDimension()
 {
  static int Exact(Rational value)
  {
   if(value.Denominator!=System.Numerics.BigInteger.One)throw new InvalidOperationException("Dimension has fractional exponents.");
   return checked((int)value.Numerator);
  }
  return new(Exact(Length),Exact(Mass),Exact(Time),Exact(Current),Exact(Temperature),Exact(Amount),Exact(Luminosity));
 }
 /// <summary>Multiplies dimensions by adding exponents.</summary>
 public static RationalDimension operator +(RationalDimension a,RationalDimension b)=>new(a.Length+b.Length,a.Mass+b.Mass,a.Time+b.Time,a.Current+b.Current,a.Temperature+b.Temperature,a.Amount+b.Amount,a.Luminosity+b.Luminosity);
 /// <summary>Divides dimensions by subtracting exponents.</summary>
 public static RationalDimension operator -(RationalDimension a,RationalDimension b)=>new(a.Length-b.Length,a.Mass-b.Mass,a.Time-b.Time,a.Current-b.Current,a.Temperature-b.Temperature,a.Amount-b.Amount,a.Luminosity-b.Luminosity);
 /// <summary>Raises all exponents to a rational power.</summary>
 public RationalDimension Pow(Rational power)=>new(Length*power,Mass*power,Time*power,Current*power,Temperature*power,Amount*power,Luminosity*power);
 /// <summary>Raises all exponents to an integer power.</summary>
 public RationalDimension Pow(int power)=>Pow(new Rational(power,1));
 /// <summary>Returns nonzero SI base dimensions as an immutable snapshot.</summary>
 public IReadOnlyDictionary<string,Rational> GetBaseDimensions()
 {
  var values=new Dictionary<string,Rational>();
  if(!Length.Numerator.IsZero)values["L"]=Length;
  if(!Mass.Numerator.IsZero)values["M"]=Mass;
  if(!Time.Numerator.IsZero)values["T"]=Time;
  if(!Current.Numerator.IsZero)values["I"]=Current;
  if(!Temperature.Numerator.IsZero)values["Θ"]=Temperature;
  if(!Amount.Numerator.IsZero)values["N"]=Amount;
  if(!Luminosity.Numerator.IsZero)values["J"]=Luminosity;
  return new System.Collections.ObjectModel.ReadOnlyDictionary<string,Rational>(values);
 }
 /// <summary>Returns the exact positive integer-degree root.</summary>
 public RationalDimension Root(int degree)
 {
  if(degree<=0)throw new ArgumentOutOfRangeException(nameof(degree));
  return Pow(new Rational(1,degree));
 }
}