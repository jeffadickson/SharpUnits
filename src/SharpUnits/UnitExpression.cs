using System.Collections.Generic;
namespace SharpUnits;
/// <summary>Canonical multiplicative unit expression with SI dimension and scale.</summary>
/// <remarks>Affine units must be converted to coherent SI before participating in unit algebra.</remarks>
public readonly record struct UnitExpression(Dimension Dimension, double Scale)
{
 /// <summary>Creates an expression from a linear unit.</summary>
 public static UnitExpression From(UnitDefinition unit)
 {
  ArgumentNullException.ThrowIfNull(unit);
  if(unit.Converter.Offset!=0)throw new InvalidOperationException("Affine units cannot be multiplied or divided.");
  return new(unit.Dimension,unit.Converter.Scale);
 }
 /// <summary>Multiplies unit expressions.</summary>
 public static UnitExpression operator *(UnitExpression a,UnitExpression b)=>new(a.Dimension+b.Dimension,a.Scale*b.Scale);
 /// <summary>Divides unit expressions.</summary>
 public static UnitExpression operator /(UnitExpression a,UnitExpression b)=>new(a.Dimension-b.Dimension,a.Scale/b.Scale);
 /// <summary>Raises a unit to an integer power.</summary>
 public UnitExpression Pow(int exponent)
 {
  static int CheckedMultiply(int x,int y)=>checked(x*y);
  var d=Dimension;
  return new(new Dimension(CheckedMultiply(d.Length,exponent),CheckedMultiply(d.Mass,exponent),CheckedMultiply(d.Time,exponent),CheckedMultiply(d.Current,exponent),CheckedMultiply(d.Temperature,exponent),CheckedMultiply(d.Amount,exponent),CheckedMultiply(d.Luminosity,exponent)),Math.Pow(Scale,exponent));
 }
 /// <summary>Checks dimensional compatibility independently of unit scale.</summary>
 public bool IsCompatibleWith(UnitExpression other)=>Dimension==other.Dimension;
}