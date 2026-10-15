namespace SharpUnits;
/// <summary>Scalar SI quantity using exact rational dimensional exponents.</summary>
/// <remarks>The legacy Dimension property remains available for integral dimensions.</remarks>
public readonly record struct Quantity
{
 /// <summary>Numerical magnitude in coherent SI units.</summary>
 public double SiValue { get; }
 /// <summary>Canonical rational dimension vector.</summary>
 public RationalDimension RationalDimension { get; }
 /// <summary>Legacy integer dimension; throws for fractional exponents.</summary>
 public Dimension Dimension => RationalDimension.ToIntegerDimension();
 /// <summary>Creates a quantity with legacy integer dimensions.</summary>
 public Quantity(double siValue, Dimension dimension):this(siValue,RationalDimension.From(dimension)){}
 /// <summary>Creates a quantity with canonical rational dimensions.</summary>
 public Quantity(double siValue,RationalDimension dimension)
 {
  SiValue=siValue;
  RationalDimension=dimension;
 }
 /// <summary>Adds compatible quantities.</summary>
 public static Quantity operator +(Quantity a,Quantity b)=>a.RationalDimension==b.RationalDimension?new(a.SiValue+b.SiValue,a.RationalDimension):throw new InvalidOperationException("Incompatible dimensions");
 /// <summary>Subtracts compatible quantities.</summary>
 public static Quantity operator -(Quantity a,Quantity b)=>a.RationalDimension==b.RationalDimension?new(a.SiValue-b.SiValue,a.RationalDimension):throw new InvalidOperationException("Incompatible dimensions");
 /// <summary>Multiplies quantities and adds dimensional exponents.</summary>
 public static Quantity operator *(Quantity a,Quantity b)=>new(a.SiValue*b.SiValue,a.RationalDimension+b.RationalDimension);
 /// <summary>Divides quantities and subtracts dimensional exponents.</summary>
 public static Quantity operator /(Quantity a,Quantity b)=>new(a.SiValue/b.SiValue,a.RationalDimension-b.RationalDimension);
 /// <summary>Raises a quantity to a rational power.</summary>
 /// <remarks>Magnitude is calculated in double precision; dimensions remain exact.</remarks>
 public Quantity Pow(Rational power)=>new(Math.Pow(SiValue,(double)power.Numerator/(double)power.Denominator),RationalDimension.Pow(power));
}
/// <summary>Electrical SI quantity factories.</summary>
public static class Electrical
{
 /// <summary>Creates voltage in volts.</summary>
 public static Quantity Volts(double value)=>new(value,Dimension.Voltage);
 /// <summary>Creates resistance in ohms.</summary>
 public static Quantity Ohms(double value)=>new(value,Dimension.Resistance);
 /// <summary>Creates current in amperes.</summary>
 public static Quantity Amperes(double value)=>new(value,Dimension.ElectricCurrent);
}
