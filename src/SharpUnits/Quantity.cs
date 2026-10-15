namespace SharpUnits;
/// <summary>Scalar quantity expressed in coherent SI units.</summary>
public readonly record struct Quantity(double SiValue, Dimension Dimension)
{
 public static Quantity operator +(Quantity a,Quantity b)=>a.Dimension==b.Dimension?new(a.SiValue+b.SiValue,a.Dimension):throw new InvalidOperationException("Incompatible dimensions");
 public static Quantity operator -(Quantity a,Quantity b)=>a.Dimension==b.Dimension?new(a.SiValue-b.SiValue,a.Dimension):throw new InvalidOperationException("Incompatible dimensions");
 public static Quantity operator *(Quantity a,Quantity b)=>new(a.SiValue*b.SiValue,a.Dimension+b.Dimension);
 public static Quantity operator /(Quantity a,Quantity b)=>new(a.SiValue/b.SiValue,a.Dimension-b.Dimension);
}
/// <summary>Electrical SI quantity factories.</summary>
public static class Electrical
{
 public static Quantity Volts(double value)=>new(value,Dimension.Voltage);
 public static Quantity Ohms(double value)=>new(value,Dimension.Resistance);
 public static Quantity Amperes(double value)=>new(value,Dimension.ElectricCurrent);
}