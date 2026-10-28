namespace SharpUnits;

/// <summary>JSR 385-style unit facade over an immutable compositional unit graph.</summary>
/// <remarks>Legacy expression-returning operations remain available. Unit-returning variants
/// preserve structural identity and affine graphs; native expression algebra still rejects
/// affine units. Java binary signatures and text rendering are separate compatibility concerns.</remarks>
public sealed class JavaUnitAdapter : IEquatable<JavaUnitAdapter>
{
    // @relation(JP-WP1-UNIT, scope=file)
    private readonly UnitDefinition? original;
    /// <summary>The immutable graph, including parent/converter and normalized product identity.</summary>
    public JavaUnit Model {get;}
    /// <summary>Intrinsic symbol, or null for unnamed transformed/product units.</summary>
    public string? Symbol=>original?.Symbol??Model.Symbol;
    /// <summary>Intrinsic name, or null for unnamed units.</summary>
    public string? Name=>Model.Name;
    /// <summary>Exact rational dimensions.</summary>
    public RationalDimension Dimension=>Model.Dimension;
    /// <summary>Legacy coherent dimensional expression; alternate identity is exposed by CoherentUnit.</summary>
    public RationalUnitExpression SystemUnit=>new(Dimension,1);
    /// <summary>The structural Java system unit, retaining alternate identities.</summary>
    public JavaUnitAdapter CoherentUnit=>new(Model.SystemUnit);
    /// <summary>Creates an adapter from a native registered affine definition.</summary>
    public JavaUnitAdapter(UnitDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);original=definition;Model=JavaUnit.FromDefinition(definition);
    }
    /// <summary>Creates an adapter over a compositional unit.</summary>
    public JavaUnitAdapter(JavaUnit model)=>Model=model??throw new ArgumentNullException(nameof(model));
    /// <summary>Projects an affine graph to a native definition, retaining graph metadata.</summary>
    public UnitDefinition Definition=>original??Model.ToDefinition();
    /// <summary>Returns the multiplicative expression, rejecting affine/nonlinear unit conversions.</summary>
    public RationalUnitExpression Expression=>RationalUnitExpression.From(Definition);
    /// <summary>Tests dimensional compatibility independently of unit identity.</summary>
    public bool IsCompatible(JavaUnitAdapter? other)=>other is not null&&Dimension==other.Dimension;
    /// <summary>Tests converter identity independently of structural equality.</summary>
    public bool IsEquivalentTo(JavaUnitAdapter? other)=>other is not null&&Model.IsEquivalentTo(other.Model);
    /// <summary>Converts this unit to a compatible target with exact converter coefficients.</summary>
    public JavaUnitConverterAdapter GetConverterTo(JavaUnitAdapter target)
    {
        ArgumentNullException.ThrowIfNull(target);return Model.GetConverterTo(target.Model);
    }
    /// <summary>Multiplies native linear expressions; use MultiplyUnit to retain the Java unit graph.</summary>
    public RationalUnitExpression Multiply(JavaUnitAdapter other){ArgumentNullException.ThrowIfNull(other);return Expression*other.Expression;}
    /// <summary>Divides native linear expressions; use DivideUnit to retain the Java unit graph.</summary>
    public RationalUnitExpression Divide(JavaUnitAdapter other){ArgumentNullException.ThrowIfNull(other);return Expression/other.Expression;}
    /// <summary>Raises a native linear expression to a power.</summary>
    public RationalUnitExpression Pow(int exponent)=>Expression.Pow(new Rational(exponent,1));
    /// <summary>Returns a native positive-degree root expression.</summary>
    public RationalUnitExpression Root(int degree)=>Expression.Root(degree);
    /// <summary>Returns a native reciprocal expression, rejecting affine units.</summary>
    public RationalUnitExpression Inverse()=>Expression.Pow(new Rational(-1,1));
    /// <summary>Multiplies structural units, including lazy affine/nonlinear products.</summary>
    public JavaUnitAdapter MultiplyUnit(JavaUnitAdapter other){ArgumentNullException.ThrowIfNull(other);return new(Model.Multiply(other.Model));}
    /// <summary>Divides structural units without discarding product identity.</summary>
    public JavaUnitAdapter DivideUnit(JavaUnitAdapter other){ArgumentNullException.ThrowIfNull(other);return new(Model.Divide(other.Model));}
    /// <summary>Returns an integral-power structural unit.</summary>
    public JavaUnitAdapter PowerUnit(int exponent)=>new(Model.Pow(exponent));
    /// <summary>Returns a signed-degree structural root, rejecting zero.</summary>
    public JavaUnitAdapter RootUnit(int degree)=>new(Model.Root(degree));
    /// <summary>Returns a reciprocal structural unit; requesting a nonlinear product converter may fail later.</summary>
    public JavaUnitAdapter InverseUnit()=>new(Model.Pow(-1));
    /// <summary>Applies an exact SI prefix before the parent's converter, including affine units.</summary>
    public JavaUnitAdapter Prefix(SiPrefix prefix)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        return new(Model.Transform(JavaUnitConverterAdapter.ForPrefix(10,prefix.Exponent)));
    }
    /// <summary>Applies a decimal or binary prefix, retaining its integer radix/exponent identity.</summary>
    public JavaUnitAdapter Prefix(JavaPrefix prefix)
    {
        ArgumentNullException.ThrowIfNull(prefix);return new(Model.Transform(JavaUnitConverterAdapter.ForPrefix(prefix.Base,prefix.Exponent)));
    }
    /// <summary>Scales a unit using the Java double's exact decimal representation.</summary>
    public JavaUnitAdapter Multiply(double factor)=>Multiply(JavaNumber.FromDoubleAsRational(factor));
    /// <summary>Divides a unit by a scalar, rejecting zero.</summary>
    public JavaUnitAdapter Divide(double divisor)=>Divide(JavaNumber.FromDoubleAsRational(divisor));
    /// <summary>Shifts in source units before conversion through the parent.</summary>
    public JavaUnitAdapter Shift(double shift)
    {
        // AbstractUnit.shift(double) explicitly promotes to RationalNumber;
        // shift(Number) deliberately retains the caller's numeric family.
        return Shift(JavaNumber.FromDoubleAsRational(shift));
    }
    /// <summary>Scales without first projecting to binary64.</summary>
    public JavaUnitAdapter Multiply(JavaNumber factor)=>new(Model.Multiply(factor));
    /// <summary>Divides without first projecting to binary64.</summary>
    public JavaUnitAdapter Divide(JavaNumber divisor)=>new(Model.Divide(divisor));
    /// <summary>Shifts without first projecting to binary64.</summary>
    public JavaUnitAdapter Shift(JavaNumber shift)=>new(Model.Shift(shift));
    /// <summary>Scales by a supported built-in boxed numeric value.</summary>
    public JavaUnitAdapter Multiply(IConvertible factor)=>Multiply(JavaNumber.FromNumeric(factor));
    /// <summary>Divides by a supported built-in boxed numeric value.</summary>
    public JavaUnitAdapter Divide(IConvertible divisor)=>Divide(JavaNumber.FromNumeric(divisor));
    /// <summary>Shifts by a supported built-in boxed numeric value.</summary>
    public JavaUnitAdapter Shift(IConvertible shift)=>Shift(JavaNumber.FromNumeric(shift));
    /// <summary>Transforms the source value before applying this unit's converter.</summary>
    public JavaUnitAdapter Transform(JavaUnitConverterAdapter converter)=>new(Model.Transform(converter));
    /// <summary>Checks dimension compatibility; does not impose native semantic-kind equality.</summary>
    public JavaUnitAdapter AsType(RationalDimension requested)
    {
        if(Dimension!=requested)throw new InvalidOperationException("Requested quantity type is incompatible.");return this;
    }
    /// <summary>Creates a distinct alternate of an unscaled system unit.</summary>
    public JavaUnitAdapter Alternate(string symbol)=>new(Model.Alternate(symbol));
    /// <summary>Legacy exact dimension map; use GetBaseUnits for Java's unit-element mapping.</summary>
    public IReadOnlyDictionary<string,Rational> BaseUnits=>Dimension.GetBaseDimensions();
    /// <summary>Returns Java's nullable base-unit map, preserving unit elements rather than dimension labels.</summary>
    public IReadOnlyDictionary<JavaUnit,int>? GetBaseUnits()=>Model.GetBaseUnits();
    /// <summary>Converts between any dimensionally compatible units in this SI profile.</summary>
    public JavaUnitConverterAdapter GetConverterToAny(JavaUnitAdapter target)
    {
        ArgumentNullException.ThrowIfNull(target);return Model.GetConverterToAny(target.Model);
    }
    /// <summary>Returns the exact ordered converter to the structural system unit.</summary>
    public JavaUnitConverterAdapter ToSystemConverter()=>Model.SystemConverter;
    /// <summary>Structural graph equality, independent of native registry record identity.</summary>
    public bool Equals(JavaUnitAdapter? other)=>other is not null&&Model.Equals(other.Model);
    /// <inheritdoc/>
    public override bool Equals(object? obj)=>obj is JavaUnitAdapter other&&Equals(other);
    /// <inheritdoc/>
    public override int GetHashCode()=>Model.GetHashCode();
    /// <summary>Returns an intrinsic symbol or a diagnostic category; general Java rendering belongs to package 2.</summary>
    public override string ToString()=>Symbol??"Java-"+Model.Kind.ToString().ToLowerInvariant();
}
