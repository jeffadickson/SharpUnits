namespace SharpUnits;
/// <summary>Exponents of the seven SI base dimensions.</summary>
public readonly record struct Dimension(int Length,int Mass,int Time,int Current,int Temperature,int Amount,int Luminosity)
{
 public static Dimension None => default;
 public static Dimension Voltage => new(2,1,-3,-1,0,0,0);
 public static Dimension Resistance => new(2,1,-3,-2,0,0,0);
 public static Dimension ElectricCurrent => new(0,0,0,1,0,0,0);
 /// <summary>Raises each base-dimension exponent to an integer power.</summary>
 public Dimension Pow(int power)=>new(checked(Length*power),checked(Mass*power),checked(Time*power),checked(Current*power),checked(Temperature*power),checked(Amount*power),checked(Luminosity*power));
 /// <summary>Computes an exact integer-exponent root; rejects nonintegral results.</summary>
 /// <remarks>Fractional dimensional exponents require the planned rational dimension model.</remarks>
 public Dimension Root(int root)
 {
  if(root<=0)throw new ArgumentOutOfRangeException(nameof(root));
  if(Length%root!=0||Mass%root!=0||Time%root!=0||Current%root!=0||Temperature%root!=0||Amount%root!=0||Luminosity%root!=0)
   throw new InvalidOperationException("Root requires fractional dimensional exponents.");
  return new(Length/root,Mass/root,Time/root,Current/root,Temperature/root,Amount/root,Luminosity/root);
 }
 /// <summary>Returns nonzero base dimensions as an immutable snapshot.</summary>
 public IReadOnlyDictionary<string,int> GetBaseDimensions()
 {
  var values=new Dictionary<string,int>();
  if(Length!=0)values["L"]=Length;
  if(Mass!=0)values["M"]=Mass;
  if(Time!=0)values["T"]=Time;
  if(Current!=0)values["I"]=Current;
  if(Temperature!=0)values["Θ"]=Temperature;
  if(Amount!=0)values["N"]=Amount;
  if(Luminosity!=0)values["J"]=Luminosity;
  return new System.Collections.ObjectModel.ReadOnlyDictionary<string,int>(values);
 }
 public static Dimension operator +(Dimension a,Dimension b)=>new(a.Length+b.Length,a.Mass+b.Mass,a.Time+b.Time,a.Current+b.Current,a.Temperature+b.Temperature,a.Amount+b.Amount,a.Luminosity+b.Luminosity);
 public static Dimension operator -(Dimension a,Dimension b)=>new(a.Length-b.Length,a.Mass-b.Mass,a.Time-b.Time,a.Current-b.Current,a.Temperature-b.Temperature,a.Amount-b.Amount,a.Luminosity-b.Luminosity);
}