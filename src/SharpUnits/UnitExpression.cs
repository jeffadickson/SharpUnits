namespace SharpUnits;
/// <summary>Backward-compatible unit-expression facade over canonical rational dimensional algebra.</summary>
/// <remarks>Fractional results are available via RationalDimension; legacy Dimension rejects fractional exponents.</remarks>
public readonly record struct UnitExpression
{
 private readonly RationalUnitExpression core;
 /// <summary>Constructs from legacy integral dimensions.</summary>
 public UnitExpression(Dimension dimension,double scale):this(new RationalUnitExpression(RationalDimension.From(dimension),scale)){}
 /// <summary>Constructs from canonical rational dimensions.</summary>
 public UnitExpression(RationalDimension dimension,double scale):this(new RationalUnitExpression(dimension,scale)){}
 private UnitExpression(RationalUnitExpression value)=>core=value;
 /// <summary>Legacy dimension projection, requiring integer exponents.</summary>
 public Dimension Dimension=>core.Dimension.ToIntegerDimension();
 /// <summary>Exact rational dimension vector.</summary>
 public RationalDimension RationalDimension=>core.Dimension;
 /// <summary>Multiplicative SI scale.</summary>
 public double Scale=>core.Scale;
 /// <summary>Creates an expression from a linear unit.</summary>
 public static UnitExpression From(UnitDefinition unit)=>new(RationalUnitExpression.From(unit));
 /// <summary>Multiplies expressions.</summary>
 public static UnitExpression operator *(UnitExpression a,UnitExpression b)=>new(a.core*b.core);
 /// <summary>Divides expressions.</summary>
 public static UnitExpression operator /(UnitExpression a,UnitExpression b)=>new(a.core/b.core);
 /// <summary>Raises a unit to an integer power.</summary>
 public UnitExpression Pow(int exponent)=>new(core.Pow(exponent));
 /// <summary>Raises a unit to an exact rational dimensional power.</summary>
 public UnitExpression Pow(Rational exponent)=>new(core.Pow(exponent));
 /// <summary>Returns a rational-dimensional root.</summary>
 public UnitExpression Root(int degree)=>new(core.Root(degree));
 /// <summary>Tests dimensional compatibility.</summary>
 public bool IsCompatibleWith(UnitExpression other)=>core.IsCompatibleWith(other.core);
}
