namespace SharpUnits;
/// <summary>Unit expression with exact rational SI dimensions and multiplicative scale.</summary>
/// <remarks>Affine units must be converted to coherent SI before composing units.</remarks>
public readonly record struct RationalUnitExpression(RationalDimension Dimension,double Scale)
{
 /// <summary>Creates a rational-dimensional expression from a linear unit.</summary>
 public static RationalUnitExpression From(UnitDefinition unit)
 {
  ArgumentNullException.ThrowIfNull(unit);
  if(unit.Converter.Offset!=0)throw new InvalidOperationException("Affine units cannot participate in unit products.");
  return new(RationalDimension.From(unit.Dimension),unit.Converter.Scale);
 }
 /// <summary>Combines unit dimensions and scale by multiplication.</summary>
 public static RationalUnitExpression operator *(RationalUnitExpression a,RationalUnitExpression b)=>
  new(a.Dimension+b.Dimension,a.Scale*b.Scale);
 /// <summary>Combines unit dimensions and scale by division.</summary>
 public static RationalUnitExpression operator /(RationalUnitExpression a,RationalUnitExpression b)=>
  new(a.Dimension-b.Dimension,a.Scale/b.Scale);
 /// <summary>Raises a unit expression to a rational power.</summary>
 /// <remarks>Dimension exponents are exact; numeric scale uses floating-point Math.Pow.</remarks>
 public RationalUnitExpression Pow(Rational exponent)
 {
  var power=(double)exponent.Numerator/(double)exponent.Denominator;
  return new(Dimension.Pow(exponent),Math.Pow(Scale,power));
 }
 /// <summary>Returns an exact rational-dimensional root.</summary>
 public RationalUnitExpression Root(int degree)
 {
  if(degree<=0)throw new ArgumentOutOfRangeException(nameof(degree));
  return Pow(new Rational(1,degree));
 }
 /// <summary>Tests dimensional compatibility independently of numeric scale.</summary>
 public bool IsCompatibleWith(RationalUnitExpression other)=>Dimension==other.Dimension;
}
