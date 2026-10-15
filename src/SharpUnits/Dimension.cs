namespace SharpUnits;
/// <summary>Exponents of the seven SI base dimensions.</summary>
public readonly record struct Dimension(int Length,int Mass,int Time,int Current,int Temperature,int Amount,int Luminosity)
{
 public static Dimension None => default;
 public static Dimension Voltage => new(2,1,-3,-1,0,0,0);
 public static Dimension Resistance => new(2,1,-3,-2,0,0,0);
 public static Dimension ElectricCurrent => new(0,0,0,1,0,0,0);
 public static Dimension operator +(Dimension a,Dimension b)=>new(a.Length+b.Length,a.Mass+b.Mass,a.Time+b.Time,a.Current+b.Current,a.Temperature+b.Temperature,a.Amount+b.Amount,a.Luminosity+b.Luminosity);
 public static Dimension operator -(Dimension a,Dimension b)=>new(a.Length-b.Length,a.Mass-b.Mass,a.Time-b.Time,a.Current-b.Current,a.Temperature-b.Temperature,a.Amount-b.Amount,a.Luminosity-b.Luminosity);
}