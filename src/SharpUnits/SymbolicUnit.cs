using System.Text;

namespace SharpUnits;

/// <summary>One named unit factor raised to an exact rational exponent inside a <see cref="SymbolicUnit"/>.</summary>
public readonly record struct UnitFactor(UnitDefinition Unit, Rational Exponent);

/// <summary>Algebraic symbolic unit expression preserving exact unit structure, scale, and rational dimensions.</summary>
/// <remarks>
/// Supports unit products, quotients, integer/rational powers, and roots (for example <c>m/s²</c>, <c>N·m</c>,
/// <c>kW·h</c>, <c>nV/Hz^(1/2)</c>) without collapsing display unit identity.
/// </remarks>
public sealed class SymbolicUnit : IEquatable<SymbolicUnit>
{
    private readonly UnitFactor[] factors;

    /// <summary>Dimensionless symbolic unity (<c>1</c>).</summary>
    public static SymbolicUnit One { get; } = new([], RationalDimension.None, 1d, null);

    /// <summary>Ordered normalized unit factors with nonzero rational exponents.</summary>
    public IReadOnlyList<UnitFactor> Factors => Array.AsReadOnly(factors);

    /// <summary>Canonical exact rational dimension of this symbolic unit.</summary>
    public RationalDimension Dimension { get; }

    /// <summary>Multiplicative scale factor to coherent SI.</summary>
    public double ScaleToSi { get; }

    /// <summary>Single underlying affine unit when this symbolic unit represents an uncomposed affine unit.</summary>
    public UnitDefinition? AffineUnit { get; }

    /// <summary>True when this symbolic unit has a non-zero additive offset.</summary>
    public bool IsAffine => AffineUnit is not null;

    /// <summary>Underlying affine or linear conversion to coherent SI.</summary>
    public UnitConversion Converter => AffineUnit is not null ? AffineUnit.Converter : UnitConversion.Linear(ScaleToSi);

    /// <summary>Optional default ISQ specification (preserved for single named units).</summary>
    public QuantitySpec? DefaultSpec { get; }

    /// <summary>Formatted Unicode unit symbol (for example <c>m/s²</c>, <c>nV/Hz^(1/2)</c>).</summary>
    public string Symbol { get; }

    /// <summary>Formatted ASCII-safe unit symbol (for example <c>m/s^2</c>, <c>nV/Hz^(1/2)</c>).</summary>
    public string AsciiSymbol { get; }

    private SymbolicUnit(UnitFactor[] factors, RationalDimension dimension, double scaleToSi, UnitDefinition? affineUnit, QuantitySpec? defaultSpec = null)
    {
        this.factors = factors;
        Dimension = dimension;
        ScaleToSi = scaleToSi;
        AffineUnit = affineUnit;
        DefaultSpec = defaultSpec;
        Symbol = FormatFactors(factors, asciiOnly: false);
        AsciiSymbol = FormatFactors(factors, asciiOnly: true);
    }

    /// <summary>Creates a symbolic unit from a single <see cref="UnitDefinition"/>.</summary>
    public static SymbolicUnit From(UnitDefinition unit)
    {
        ArgumentNullException.ThrowIfNull(unit);
        if (unit.Symbol == "1" && unit.RationalDimension.IsDimensionless && unit.Converter.IsIdentity)
            return One;
        if (unit.IsAffine)
            return new([new UnitFactor(unit, Rational.One)], unit.RationalDimension, unit.Converter.Scale, unit, unit.DefaultSpec);
        return new([new UnitFactor(unit, Rational.One)], unit.RationalDimension, unit.Converter.Scale, null, unit.DefaultSpec);
    }

    /// <summary>Creates a symbolic unit from a collection of factors, combining duplicates and dropping zero exponents.</summary>
    public static SymbolicUnit FromFactors(IEnumerable<UnitFactor> rawFactors)
    {
        ArgumentNullException.ThrowIfNull(rawFactors);
        var map = new Dictionary<UnitDefinition, Rational>();
        var order = new List<UnitDefinition>();
        foreach (var f in rawFactors)
        {
            ArgumentNullException.ThrowIfNull(f.Unit);
            if (f.Exponent.IsZero) continue;
            if (f.Unit.Symbol == "1" && f.Unit.RationalDimension.IsDimensionless && f.Unit.Converter.IsIdentity) continue;
            if (f.Unit.IsAffine)
                throw new InvalidOperationException("Affine units cannot participate in symbolic unit products or powers.");
            if (!map.TryGetValue(f.Unit, out var existing))
            {
                order.Add(f.Unit);
                map[f.Unit] = f.Exponent;
            }
            else
            {
                map[f.Unit] = existing + f.Exponent;
            }
        }

        var normalized = new List<UnitFactor>(order.Count);
        var dim = RationalDimension.None;
        double scale = 1d;
        foreach (var u in order)
        {
            var exp = map[u];
            if (exp.IsZero) continue;
            normalized.Add(new UnitFactor(u, exp));
            dim += u.RationalDimension.Pow(exp);
            scale *= Math.Pow(u.Converter.Scale, exp.ToDouble());
        }

        if (normalized.Count == 0) return One;
        QuantitySpec? spec = normalized.Count == 1 && normalized[0].Exponent.IsOne ? normalized[0].Unit.DefaultSpec : null;
        return new(normalized.ToArray(), dim, scale, null, spec);
    }

    /// <summary>Returns true when both symbolic units share the same physical dimension.</summary>
    public bool IsCompatibleWith(SymbolicUnit other) => other is not null && Dimension == other.Dimension;

    /// <summary>Multiplies two linear symbolic units.</summary>
    public static SymbolicUnit operator *(SymbolicUnit a, SymbolicUnit b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        RequireLinear(a);
        RequireLinear(b);
        return FromFactors(a.factors.Concat(b.factors));
    }

    /// <summary>Divides two linear symbolic units.</summary>
    public static SymbolicUnit operator /(SymbolicUnit a, SymbolicUnit b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        RequireLinear(a);
        RequireLinear(b);
        return FromFactors(a.factors.Concat(b.factors.Select(f => new UnitFactor(f.Unit, -f.Exponent))));
    }

    /// <summary>Raises this linear symbolic unit to an integer power.</summary>
    public SymbolicUnit Pow(int exponent) => Pow(new Rational(exponent, 1));

    /// <summary>Raises this linear symbolic unit to an exact rational power.</summary>
    public SymbolicUnit Pow(Rational exponent)
    {
        RequireLinear(this);
        if (exponent.IsZero) return One;
        return FromFactors(factors.Select(f => new UnitFactor(f.Unit, f.Exponent * exponent)));
    }

    /// <summary>Returns the positive integer-degree root of this linear symbolic unit.</summary>
    public SymbolicUnit Root(int degree)
    {
        if (degree <= 0) throw new ArgumentOutOfRangeException(nameof(degree));
        return Pow(new Rational(1, degree));
    }

    /// <summary>Returns the multiplicative inverse of this linear symbolic unit.</summary>
    public SymbolicUnit Inverse() => Pow(-1);

    /// <summary>Converts this symbolic unit to the canonical coherent SI unit when one exists, or coherent SI base symbols.</summary>
    public SymbolicUnit ToCoherentSi()
    {
        if (Units.TryGetCoherentSiUnit(Dimension, out var coherent) && coherent is not null)
            return From(coherent);
        var siFactors = new List<UnitFactor>();
        if (!Dimension.Length.IsZero) siFactors.Add(new(Units.Meter, Dimension.Length));
        if (!Dimension.Mass.IsZero) siFactors.Add(new(Units.Kilogram, Dimension.Mass));
        if (!Dimension.Time.IsZero) siFactors.Add(new(Units.Second, Dimension.Time));
        if (!Dimension.Current.IsZero) siFactors.Add(new(Units.Ampere, Dimension.Current));
        if (!Dimension.Temperature.IsZero) siFactors.Add(new(Units.Kelvin, Dimension.Temperature));
        if (!Dimension.Amount.IsZero) siFactors.Add(new(Units.Mole, Dimension.Amount));
        if (!Dimension.Luminosity.IsZero) siFactors.Add(new(Units.Candela, Dimension.Luminosity));
        return FromFactors(siFactors);
    }

    /// <summary>Projects this symbolic unit to a <see cref="UnitDefinition"/> usable throughout the library.</summary>
    public UnitDefinition ToUnitDefinition()
    {
        if (AffineUnit is not null) return AffineUnit;
        if (factors.Length == 1 && factors[0].Exponent.IsOne) return factors[0].Unit;
        return new UnitDefinition(Symbol, Dimension, UnitConversion.Linear(ScaleToSi), name: Symbol, allowPrefixes: false, defaultKind: DefaultSpec?.ToKind());
    }

    /// <summary>Selects the finer (smaller scale) of two compatible linear units for lossless mixed-unit addition.</summary>
    public static SymbolicUnit CommonUnit(SymbolicUnit a, SymbolicUnit b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        if (!a.IsCompatibleWith(b)) throw new InvalidOperationException("Incompatible unit dimensions.");
        RequireLinear(a);
        RequireLinear(b);
        if (a.Equals(b) || a.ScaleToSi == b.ScaleToSi) return a;
        return a.ScaleToSi < b.ScaleToSi ? a : b;
    }

    /// <summary>Implicitly wraps a <see cref="UnitDefinition"/> as a <see cref="SymbolicUnit"/>.</summary>
    public static implicit operator SymbolicUnit(UnitDefinition unit) => From(unit);

    /// <summary>Structural equality of symbolic unit representations.</summary>
    public bool Equals(SymbolicUnit? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (AffineUnit is not null || other.AffineUnit is not null)
            return Equals(AffineUnit, other.AffineUnit);
        if (factors.Length != other.factors.Length) return false;
        // Compare factor multisets independent of insertion order.
        foreach (var f in factors)
        {
            if (!other.factors.Contains(f)) return false;
        }
        return true;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is SymbolicUnit other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        if (AffineUnit is not null) return AffineUnit.GetHashCode();
        int hash = Dimension.GetHashCode();
        foreach (var f in factors) hash ^= f.GetHashCode();
        return hash;
    }

    /// <summary>Returns the formatted Unicode symbol.</summary>
    public override string ToString() => Symbol;

    private static void RequireLinear(SymbolicUnit u)
    {
        if (u.IsAffine)
            throw new InvalidOperationException("Affine units cannot participate in symbolic unit algebra.");
    }

    private static string FormatFactors(UnitFactor[] factors, bool asciiOnly)
    {
        if (factors.Length == 0) return "1";
        var num = factors.Where(f => f.Exponent > Rational.Zero).ToArray();
        var den = factors.Where(f => f.Exponent < Rational.Zero).Select(f => new UnitFactor(f.Unit, -f.Exponent)).ToArray();

        string mult = asciiOnly ? "*" : "·";
        string FormatGroup(UnitFactor[] group)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < group.Length; i++)
            {
                if (i > 0) sb.Append(mult);
                string sym = asciiOnly ? ToAscii(group[i].Unit.Symbol) : group[i].Unit.Symbol;
                sb.Append(sym);
                var exp = group[i].Exponent;
                if (!exp.IsOne)
                {
                    if (!asciiOnly && exp.IsInteger)
                        sb.Append(ToSuperscript(exp.Numerator));
                    else if (exp.IsInteger)
                        sb.Append('^').Append(exp.Numerator);
                    else
                        sb.Append("^(").Append(exp).Append(')');
                }
            }
            return sb.ToString();
        }

        if (den.Length == 0) return FormatGroup(num);
        string numText = num.Length == 0 ? "1" : FormatGroup(num);
        string denText = FormatGroup(den);
        return den.Length > 1 ? $"{numText}/({denText})" : $"{numText}/{denText}";
    }

    private static string ToSuperscript(long value)
    {
        string s = value.ToString();
        var sb = new StringBuilder(s.Length);
        foreach (char c in s)
        {
            sb.Append(c switch
            {
                '-' => '⁻',
                '0' => '⁰',
                '1' => '¹',
                '2' => '²',
                '3' => '³',
                '4' => '⁴',
                '5' => '⁵',
                '6' => '⁶',
                '7' => '⁷',
                '8' => '⁸',
                '9' => '⁹',
                _ => c
            });
        }
        return sb.ToString();
    }

    private static string ToAscii(string symbol) =>
        symbol.Replace("µ", "u", StringComparison.Ordinal)
              .Replace("Ω", "Ohm", StringComparison.Ordinal)
              .Replace("°", "deg", StringComparison.Ordinal)
              .Replace("·", "*", StringComparison.Ordinal);
}
