using System.Numerics;
namespace SharpUnits;
/// <summary>Normalized exact rational number for dimensional exponents.</summary>
public readonly record struct Rational
{
 /// <summary>Numerator in lowest terms.</summary>
 public BigInteger Numerator { get; }
 /// <summary>Positive denominator in lowest terms.</summary>
 public BigInteger Denominator { get; }
 /// <summary>Constructs a reduced rational; rejects zero denominators.</summary>
 public Rational(BigInteger numerator,BigInteger denominator)
 {
  if(denominator.IsZero)throw new DivideByZeroException();
  if(denominator.Sign<0){numerator=-numerator;denominator=-denominator;}
  var gcd=BigInteger.GreatestCommonDivisor(BigInteger.Abs(numerator),denominator);
  Numerator=numerator/gcd;Denominator=denominator/gcd;
 }
 /// <summary>Converts an integer exactly.</summary>
 public static implicit operator Rational(int value)=>new(value,1);
 /// <summary>Adds exact fractions.</summary>
 public static Rational operator +(Rational a,Rational b)=>new(a.Numerator*b.Denominator+b.Numerator*a.Denominator,a.Denominator*b.Denominator);
 /// <summary>Subtracts exact fractions.</summary>
 public static Rational operator -(Rational a,Rational b)=>new(a.Numerator*b.Denominator-b.Numerator*a.Denominator,a.Denominator*b.Denominator);
 /// <summary>Multiplies exact fractions.</summary>
 public static Rational operator *(Rational a,Rational b)=>new(a.Numerator*b.Numerator,a.Denominator*b.Denominator);
 /// <summary>Divides exact fractions.</summary>
 public static Rational operator /(Rational a,Rational b)=>new(a.Numerator*b.Denominator,a.Denominator*b.Numerator);
 /// <summary>Canonical fractional representation.</summary>
 public override string ToString()=>Denominator.IsOne?Numerator.ToString():$"{Numerator}/{Denominator}";
}