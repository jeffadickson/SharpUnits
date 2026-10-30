namespace SharpUnits;

/// <summary>Java-style quantity retaining arbitrary finite numeric precision and structural units.</summary>
/// <remarks>This is the precision-preserving core behind the additive exact factory APIs.
/// Unlike the native double/SI projection, construction does not force evaluation of a
/// unit converter. Thus an affine inverse can exist even when its system converter is
/// unsupported by pinned Java. Native AbsoluteTemperature policies remain independent.</remarks>
public sealed class JavaNumberQuantity : IEquatable<JavaNumberQuantity>, IComparable<JavaNumberQuantity>
{
    // @relation(JP-WP1-QUANTITY, scope=file)
    /// <summary>The retained finite numeric representation.</summary>
    public JavaNumber Value { get; }
    /// <summary>The immutable compositional display unit.</summary>
    public JavaUnit Unit { get; }
    /// <summary>Absolute or relative scale.</summary>
    public JavaQuantityScale Scale { get; }
    /// <summary>Creates a quantity without projecting its value or unit to binary64.</summary>
    public JavaNumberQuantity(JavaNumber value,JavaUnit unit,JavaQuantityScale scale=JavaQuantityScale.Absolute)
    {
        ArgumentNullException.ThrowIfNull(value);ArgumentNullException.ThrowIfNull(unit);
        if(!Enum.IsDefined(scale))throw new ArgumentOutOfRangeException(nameof(scale));
        Value=value;Unit=unit;Scale=scale;
    }
    /// <summary>Converts to a compatible unit, applying offsets only for absolute values.</summary>
    public JavaNumberQuantity To(JavaUnit target)
    {
        ArgumentNullException.ThrowIfNull(target);if(Unit.Equals(target))return this;
        var converter=Unit.GetConverterTo(target);
        var value=Scale==JavaQuantityScale.Relative?Value.Multiply(LinearFactor(converter)):converter.Convert(Value);
        return new(value,target,Scale);
    }
    private static JavaNumber LinearFactor(JavaUnitConverterAdapter converter)
    {
        if(!converter.IsAffine)throw new NotSupportedException("Relative conversion requires a constant derivative.");
        return converter.AffineCoefficients().Scale;
    }
    /// <summary>Adds with pinned absolute/relative scale semantics, retaining the left display unit.</summary>
    public JavaNumberQuantity Add(JavaNumberQuantity other)=>Addition(other,false);
    /// <summary>Subtracts with pinned scale semantics; absolute minus absolute remains absolute.</summary>
    public JavaNumberQuantity Subtract(JavaNumberQuantity other)=>Addition(other,true);
    private JavaNumberQuantity Addition(JavaNumberQuantity other,bool subtract)
    {
        ArgumentNullException.ThrowIfNull(other);
        var target=Unit.SystemUnit;
        var left=To(target).Value;var right=other.To(target).Value;
        var result=subtract?left.Subtract(right):left.Add(right);
        var scale=Scale==JavaQuantityScale.Relative&&other.Scale==JavaQuantityScale.Relative?JavaQuantityScale.Relative:JavaQuantityScale.Absolute;
        if(scale==JavaQuantityScale.Relative)result=result.Divide(LinearFactor(Unit.SystemConverter));
        else if(!Unit.Equals(target)||!other.Unit.Equals(target))result=Unit.SystemConverter.Inverse().Convert(result);
        return new(result,Unit,scale);
    }
    /// <summary>Multiplies quantities, converting non-linear units to their system units first.</summary>
    public JavaNumberQuantity Multiply(JavaNumberQuantity other)=>Combination(other,false);
    /// <summary>Divides quantities, converting non-linear units to their system units first.</summary>
    public JavaNumberQuantity Divide(JavaNumberQuantity other)=>Combination(other,true);
    private JavaNumberQuantity Combination(JavaNumberQuantity other,bool divide)
    {
        ArgumentNullException.ThrowIfNull(other);
        JavaNumberQuantity Linear(JavaNumberQuantity q)=>q.Unit.SystemConverter.IsLinear?q:q.To(q.Unit.SystemUnit);
        var a=Linear(this);var b=Linear(other);
        return new(divide?a.Value.Divide(b.Value):a.Value.Multiply(b.Value),divide?a.Unit.Divide(b.Unit):a.Unit.Multiply(b.Unit));
    }
    /// <summary>Multiplies by a numeric scalar, using system values for absolute quantities.</summary>
    public JavaNumberQuantity Multiply(JavaNumber scalar)=>Scalar(scalar,false);
    /// <summary>Divides by a scalar with exact reciprocal arithmetic.</summary>
    public JavaNumberQuantity Divide(JavaNumber scalar)=>Scalar(scalar,true);
    private JavaNumberQuantity Scalar(JavaNumber scalar,bool divide)
    {
        ArgumentNullException.ThrowIfNull(scalar);
        var value=Scale==JavaQuantityScale.Relative?Value:Unit.SystemConverter.Convert(Value);
        var result=divide?value.Divide(scalar):value.Multiply(scalar);
        if(Scale==JavaQuantityScale.Absolute)result=Unit.SystemConverter.Inverse().Convert(result);
        return new(result,Unit,Scale);
    }
    /// <summary>Returns the numeric reciprocal and structural inverse unit without evaluating its converter.</summary>
    public JavaNumberQuantity Inverse()=>new(Value.Reciprocal(),Unit.Pow(-1),Scale);
    /// <summary>Negates the display value, preserving unit and scale.</summary>
    public JavaNumberQuantity Negate()=>new(Value.Negate(),Unit,Scale);
    /// <summary>Checks dimensions as Java asType does; native semantic-kind policy is separate.</summary>
    public JavaNumberQuantity AsType(RationalDimension dimension)
    {
        if(Unit.Dimension!=dimension)throw new InvalidOperationException("Requested quantity type is incompatible.");return this;
    }
    /// <summary>Checks a registered kind's dimension without conflating it with native kind equality.</summary>
    public JavaNumberQuantity AsType(QuantityKind kind,QuantityKindRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);return AsType(registry.DimensionOf(kind));
    }
    /// <summary>Compares after converting the right operand according to its own scale.</summary>
    public int CompareTo(JavaNumberQuantity? other)=>other is null?1:Value.CompareTo(other.To(Unit).Value);
    /// <summary>Checks numeric equivalence in the left unit; incompatible dimensions return false.</summary>
    public bool IsEquivalentTo(JavaNumberQuantity? other)=>other is not null&&Unit.Dimension==other.Unit.Dimension&&CompareTo(other)==0;
    /// <summary>Representation equality includes numeric family/decimal scale, unit graph and scale kind.</summary>
    public bool Equals(JavaNumberQuantity? other)=>other is not null&&Value.Equals(other.Value)&&Unit.Equals(other.Unit)&&Scale==other.Scale;
    /// <inheritdoc/>
    public override bool Equals(object? obj)=>obj is JavaNumberQuantity other&&Equals(other);
    /// <inheritdoc/>
    public override int GetHashCode()=>HashCode.Combine(Value,Unit,Scale);
    /// <summary>Projects to the legacy finite double/SI adapter, explicitly losing numeric precision.</summary>
    /// <exception cref="NotSupportedException">The unit has no native affine projection.</exception>
    public JavaQuantityAdapter ToDoubleQuantity()=>new(Value.ToDouble(),Unit.ToDefinition(),Scale);
}
