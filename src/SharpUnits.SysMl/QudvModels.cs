namespace SharpUnits.SysMl;

/// <summary>Classification of a SysML QUDV unit definition.</summary>
public enum QudvUnitKind
{
    /// <summary>Coherent SI base or simple named unit (<c>factor = 1, offset = 0</c>).</summary>
    Simple,

    /// <summary>Linearly scaled unit (<c>si = value * factor</c>).</summary>
    LinearConversion,

    /// <summary>Affine offset unit such as °C or °F.</summary>
    AffineConversion,

    /// <summary>SI-prefixed unit referencing a base symbol and prefix symbol.</summary>
    Prefixed,

    /// <summary>Derived product/quotient unit composed of symbolic factors.</summary>
    Derived
}

/// <summary>Offset convention for <see cref="QudvUnitKind.AffineConversion"/> units.</summary>
public enum QudvOffsetConvention
{
    /// <summary>Offset is in target coherent SI units: <c>si = value * factor + offset</c> (SharpUnits / FMI convention).</summary>
    TargetSiOffset,

    /// <summary>Offset is in source units before scaling: <c>si = (value + offset) * factor</c>.</summary>
    SourceUnitOffset
}

/// <summary>Exact rational dimensional exponent entry inside a QUDV quantity kind.</summary>
public sealed record QudvDimensionExponents
{
    /// <summary>Length exponent (numerator/denominator string such as <c>"1"</c> or <c>"1/2"</c>).</summary>
    public string Length { get; init; } = "0";

    /// <summary>Mass exponent.</summary>
    public string Mass { get; init; } = "0";

    /// <summary>Time exponent.</summary>
    public string Time { get; init; } = "0";

    /// <summary>Electric current exponent.</summary>
    public string Current { get; init; } = "0";

    /// <summary>Thermodynamic temperature exponent.</summary>
    public string Temperature { get; init; } = "0";

    /// <summary>Amount of substance exponent.</summary>
    public string Amount { get; init; } = "0";

    /// <summary>Luminous intensity exponent.</summary>
    public string Luminosity { get; init; } = "0";

    /// <summary>Converts to a <see cref="RationalDimension"/>.</summary>
    public RationalDimension ToRationalDimension() => new(
        ParseRational(Length),
        ParseRational(Mass),
        ParseRational(Time),
        ParseRational(Current),
        ParseRational(Temperature),
        ParseRational(Amount),
        ParseRational(Luminosity));

    /// <summary>Creates from a <see cref="RationalDimension"/>.</summary>
    public static QudvDimensionExponents From(RationalDimension d) => new()
    {
        Length = d.Length.ToString(),
        Mass = d.Mass.ToString(),
        Time = d.Time.ToString(),
        Current = d.Current.ToString(),
        Temperature = d.Temperature.ToString(),
        Amount = d.Amount.ToString(),
        Luminosity = d.Luminosity.ToString()
    };

    private static Rational ParseRational(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Rational.Zero;
        string trimmed = text.Trim();
        int slash = trimmed.IndexOf('/');
        if (slash < 0) return new Rational(long.Parse(trimmed, System.Globalization.CultureInfo.InvariantCulture), 1);
        long num = long.Parse(trimmed[..slash].Trim(), System.Globalization.CultureInfo.InvariantCulture);
        long den = long.Parse(trimmed[(slash + 1)..].Trim(), System.Globalization.CultureInfo.InvariantCulture);
        return new Rational(num, den);
    }
}

/// <summary>SysML QUDV QuantityKind interchange record.</summary>
public sealed record QudvQuantityKind
{
    /// <summary>Stable identifier (for example <c>isq.voltage</c> or Cameo URI).</summary>
    public string Id { get; init; } = "";

    /// <summary>Human-readable quantity kind name.</summary>
    public string Name { get; init; } = "";

    /// <summary>Optional parent quantity kind ID for ISQ specialization hierarchies.</summary>
    public string? ParentId { get; init; }

    /// <summary>True when this quantity kind establishes a distinct kind boundary (for example Torque vs. Energy).</summary>
    public bool IsDistinctKind { get; init; } = true;

    /// <summary>Exact rational SI base dimension exponents.</summary>
    public QudvDimensionExponents Dimensions { get; init; } = new();
}

/// <summary>SysML QUDV Unit interchange record.</summary>
public sealed record QudvUnit
{
    /// <summary>Unit symbol (for example <c>V</c>, <c>psi</c>, <c>°C</c>).</summary>
    public string Symbol { get; init; } = "";

    /// <summary>Human-readable unit name.</summary>
    public string Name { get; init; } = "";

    /// <summary>QUDV unit classification.</summary>
    public QudvUnitKind Kind { get; init; } = QudvUnitKind.Simple;

    /// <summary>Associated QUDV quantity kind ID, if any.</summary>
    public string? QuantityKindId { get; init; }

    /// <summary>Explicit SI dimension exponents (optional when <see cref="Expression"/> or <see cref="BaseUnitSymbol"/> is supplied).</summary>
    public QudvDimensionExponents? Dimensions { get; init; }

    /// <summary>Multiplicative factor to coherent SI (for Linear/Affine conversion units).</summary>
    public double Factor { get; init; } = 1d;

    /// <summary>Additive offset (for Affine conversion units).</summary>
    public double Offset { get; init; }

    /// <summary>Offset sign/application convention for affine units.</summary>
    public QudvOffsetConvention OffsetConvention { get; init; } = QudvOffsetConvention.TargetSiOffset;

    /// <summary>SI prefix symbol for <see cref="QudvUnitKind.Prefixed"/> units.</summary>
    public string? PrefixSymbol { get; init; }

    /// <summary>Base unit symbol for <see cref="QudvUnitKind.Prefixed"/> units.</summary>
    public string? BaseUnitSymbol { get; init; }

    /// <summary>Compound unit expression for <see cref="QudvUnitKind.Derived"/> units (for example <c>nV/Hz^(1/2)</c>).</summary>
    public string? Expression { get; init; }

    /// <summary>Whether SI prefixes may be dynamically applied to this unit.</summary>
    public bool AllowPrefixes { get; init; }
}

/// <summary>SysML ValueType binding a quantity kind, unit symbol, and point-vs-interval interpretation.</summary>
public sealed record QudvValueType
{
    /// <summary>ValueType name in Cameo (for example <c>SupplyVoltage</c>, <c>AmbientTempC</c>).</summary>
    public string Name { get; init; } = "";

    /// <summary>Referenced unit symbol.</summary>
    public string UnitSymbol { get; init; } = "";

    /// <summary>Referenced quantity kind ID.</summary>
    public string? QuantityKindId { get; init; }

    /// <summary>True when values represent affine points (such as °C or gauge pressure) rather than intervals.</summary>
    public bool IsAffinePoint { get; init; }

    /// <summary>Optional named affine origin (for example <c>ice</c>, <c>gauge</c>, <c>vacuum</c>).</summary>
    public string? OriginName { get; init; }
}

/// <summary>Exported Cameo ConstraintBlock parametric parameter.</summary>
public sealed record QudvConstraintParameter(string Name, string? QuantityKindId = null, string? UnitSymbol = null);

/// <summary>Exported Cameo ConstraintBlock containing parametric equations.</summary>
public sealed record QudvConstraintBlock
{
    /// <summary>Constraint block name.</summary>
    public string Name { get; init; } = "";

    /// <summary>Declared parameters (leave kind/unit null to infer dimensions statically).</summary>
    public IReadOnlyList<QudvConstraintParameter> Parameters { get; init; } = Array.Empty<QudvConstraintParameter>();

    /// <summary>Parametric equations (for example <c>v = i * r</c>, <c>p = v * i</c>).</summary>
    public IReadOnlyList<string> Equations { get; init; } = Array.Empty<string>();
}

/// <summary>Exported Cameo requirement acceptance limit for automated test-bench verification.</summary>
public sealed record QudvRequirementLimit
{
    /// <summary>Requirement ID (for example <c>REQ-PWR-001</c>).</summary>
    public string RequirementId { get; init; } = "";

    /// <summary>Required semantic quantity kind ID.</summary>
    public string QuantityKindId { get; init; } = "";

    /// <summary>Unit symbol in which <see cref="Minimum"/>, <see cref="Maximum"/>, and <see cref="GuardBand"/> are expressed.</summary>
    public string UnitSymbol { get; init; } = "";

    /// <summary>Minimum acceptable value in <see cref="UnitSymbol"/>.</summary>
    public double Minimum { get; init; }

    /// <summary>Maximum acceptable value in <see cref="UnitSymbol"/>.</summary>
    public double Maximum { get; init; }

    /// <summary>Optional guard band in <see cref="UnitSymbol"/> (linear scale applied).</summary>
    public double GuardBand { get; init; }

    /// <summary>Coverage factor <c>k</c> for expanded uncertainty evaluation (default 2.0).</summary>
    public double CoverageFactor { get; init; } = 2d;
}

/// <summary>Versioned Cameo / SysML QUDV model interchange catalog.</summary>
public sealed record QudvCatalog
{
    /// <summary>Interchange schema version.</summary>
    public int SchemaVersion { get; init; } = 1;

    /// <summary>Model or library name.</summary>
    public string Name { get; init; } = "SharpUnits.QUDV";

    /// <summary>Declared QUDV quantity kinds.</summary>
    public IReadOnlyList<QudvQuantityKind> QuantityKinds { get; init; } = Array.Empty<QudvQuantityKind>();

    /// <summary>Declared QUDV units.</summary>
    public IReadOnlyList<QudvUnit> Units { get; init; } = Array.Empty<QudvUnit>();

    /// <summary>Declared SysML ValueTypes.</summary>
    public IReadOnlyList<QudvValueType> ValueTypes { get; init; } = Array.Empty<QudvValueType>();

    /// <summary>Exported parametric ConstraintBlocks.</summary>
    public IReadOnlyList<QudvConstraintBlock> Constraints { get; init; } = Array.Empty<QudvConstraintBlock>();

    /// <summary>Exported requirement acceptance limits.</summary>
    public IReadOnlyList<QudvRequirementLimit> RequirementLimits { get; init; } = Array.Empty<QudvRequirementLimit>();
}
