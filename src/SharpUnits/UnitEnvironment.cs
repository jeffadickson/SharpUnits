using System.Collections.Concurrent;
using System.Globalization;

namespace SharpUnits;

/// <summary>
/// Deterministic, culture-invariant, zero-reflection unit evaluation environment designed for embedding SharpUnits
/// inside scripting and DSL hosts (such as StarLark-cs, Teal, Cameo, or instrument test-bench scripts).
/// </summary>
public sealed class UnitEnvironment
{
    private readonly UnitExpressionParser expressionParser;
    private readonly QuantityTextFormat textFormat;
    private readonly ConcurrentDictionary<string, PointOrigin> origins = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Underlying unit registry.</summary>
    public UnitRegistry Registry { get; }

    /// <summary>Underlying semantic kind registry.</summary>
    public QuantityKindRegistry Kinds { get; }

    /// <summary>Shared default deterministic standard environment.</summary>
    public static UnitEnvironment Standard { get; } = CreateStandard();

    /// <summary>Creates an isolated environment backed by explicit registries.</summary>
    public UnitEnvironment(UnitRegistry registry, QuantityKindRegistry kinds)
    {
        Registry = registry ?? throw new ArgumentNullException(nameof(registry));
        Kinds = kinds ?? throw new ArgumentNullException(nameof(kinds));
        expressionParser = new UnitExpressionParser(Registry);
        textFormat = new QuantityTextFormat(Registry);
        RegisterDefaultOrigins();
    }

    /// <summary>Creates a fresh standard environment safe for per-script customization.</summary>
    public static UnitEnvironment CreateStandard() =>
        new(UnitRegistry.CreateStandard(), QuantityKindRegistry.CreateStandard());

    /// <summary>Registers a named affine origin (for example <c>"tare"</c> or <c>"room"</c>) for script lookups.</summary>
    public bool RegisterOrigin(string name, PointOrigin origin)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Origin name required.", nameof(name));
        ArgumentNullException.ThrowIfNull(origin);
        return origins.TryAdd(name, origin);
    }

    /// <summary>Resolves a named affine origin (such as <c>"ice"</c>, <c>"absolute_zero"</c>, <c>"gauge"</c>, <c>"vacuum"</c>, <c>"sea_level"</c>).</summary>
    public PointOrigin ResolveOrigin(string name) =>
        origins.TryGetValue(name, out var origin)
            ? origin
            : throw new KeyNotFoundException($"Unknown affine origin: '{name}'.");

    /// <summary>Resolves a single unit symbol, SI-prefixed symbol, alias, or compound expression into a <see cref="SymbolicUnit"/>.</summary>
    public SymbolicUnit ResolveUnit(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            throw new FormatException("Unit expression cannot be empty.");
        string trimmed = expression.Trim();
        if (Registry.TryResolve(trimmed, out var single) && single is not null)
            return SymbolicUnit.From(single);
        return expressionParser.ParseSymbolic(trimmed);
    }

    /// <summary>Attempts to resolve a unit expression without throwing.</summary>
    public bool TryResolveUnit(string? expression, out SymbolicUnit? unit)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            unit = null;
            return false;
        }
        try
        {
            unit = ResolveUnit(expression);
            return true;
        }
        catch (Exception ex) when (ex is FormatException or KeyNotFoundException or InvalidOperationException or ArgumentException)
        {
            unit = null;
            return false;
        }
    }

    /// <summary>Creates a linear <see cref="Quantity{T}"/> in <see cref="double"/> precision from a value and unit expression.</summary>
    public Quantity<double> CreateQuantity(double value, string unitExpression, QuantitySpec? spec = null)
    {
        var unit = ResolveUnit(unitExpression);
        return Quantity<double>.Of(value, unit, spec);
    }

    /// <summary>Creates a unified <see cref="Quantity"/> (supporting both linear and affine display units such as <c>"°C"</c>).</summary>
    public Quantity CreateUnifiedQuantity(double value, string unitExpression)
    {
        var unit = ResolveUnit(unitExpression);
        return Quantity.Of(value, unit);
    }

    /// <summary>
    /// Creates an affine <see cref="QuantityPoint{T}"/> from a script call such as
    /// <c>CreatePoint(25, "°C")</c>, <c>CreatePoint(212, "degF")</c>, or <c>CreatePoint(2.0, "bar", "gauge")</c>.
    /// </summary>
    public QuantityPoint<double> CreatePoint(double value, string unitExpression, string? originName = null)
    {
        if (string.IsNullOrWhiteSpace(unitExpression))
            throw new FormatException("Unit expression required.");
        string trimmed = unitExpression.Trim();

        if (originName is null)
        {
            if (trimmed is "°C" or "degC") return QuantityPoint<double>.FromCelsius(value);
            if (trimmed is "°F" or "degF") return QuantityPoint<double>.FromFahrenheit(value);
            if (trimmed is "K") return QuantityPoint<double>.FromKelvin(value);
        }

        var unit = ResolveUnit(trimmed);
        var origin = originName is not null
            ? ResolveOrigin(originName.Trim())
            : PointOrigins.NaturalZero(unit.Dimension, unit.DefaultSpec);
        return QuantityPoint<double>.At(value, unit, origin);
    }

    /// <summary>Parses a quantity literal string deterministically using invariant culture.</summary>
    public Quantity ParseQuantity(string text) => textFormat.Parse(text);

    /// <summary>Attempts to parse a quantity literal string without throwing.</summary>
    public bool TryParseQuantity(string? text, out Quantity quantity) =>
        Quantity.TryParse(text, Registry, out quantity);

    /// <summary>Converts a <see cref="Quantity{T}"/> to a target unit expression.</summary>
    public Quantity<double> Convert(Quantity<double> quantity, string targetUnitExpression) =>
        quantity.In(ResolveUnit(targetUnitExpression));

    /// <summary>Converts an affine <see cref="QuantityPoint{T}"/> to a target scale or origin (for example <c>"°C"</c>, <c>"°F"</c>, <c>"K"</c>, or <c>"bar"</c> + <c>"vacuum"</c>).</summary>
    public QuantityPoint<double> ConvertPoint(QuantityPoint<double> point, string targetUnitExpression, string? targetOriginName = null)
    {
        string trimmed = targetUnitExpression.Trim();
        if (targetOriginName is null)
        {
            if (trimmed is "°C" or "degC") return point.ToCelsius();
            if (trimmed is "°F" or "degF") return point.ToFahrenheit();
            if (trimmed is "K") return point.ToKelvin();
        }
        var targetUnit = ResolveUnit(trimmed);
        var result = targetOriginName is not null
            ? point.RelativeTo(ResolveOrigin(targetOriginName.Trim()))
            : point;
        return result.In(targetUnit);
    }

    /// <summary>Formats a quantity deterministically using invariant culture (supports <c>"eng"</c>, <c>"ascii"</c>, <c>"eng:ascii"</c>, or numeric formats).</summary>
    public string Format(Quantity quantity, string? format = null) =>
        quantity.ToString(format, CultureInfo.InvariantCulture);

    private void RegisterDefaultOrigins()
    {
        origins["absolute_zero"] = PointOrigins.AbsoluteZero;
        origins["kelvin"] = PointOrigins.AbsoluteZero;
        origins["AbsoluteZero"] = PointOrigins.AbsoluteZero;
        origins[PointOrigins.AbsoluteZero.Id] = PointOrigins.AbsoluteZero;

        origins["ice"] = PointOrigins.IcePoint;
        origins["celsius"] = PointOrigins.IcePoint;
        origins["IcePoint"] = PointOrigins.IcePoint;
        origins[PointOrigins.IcePoint.Id] = PointOrigins.IcePoint;

        origins["fahrenheit"] = PointOrigins.FahrenheitZero;
        origins["FahrenheitZero"] = PointOrigins.FahrenheitZero;
        origins[PointOrigins.FahrenheitZero.Id] = PointOrigins.FahrenheitZero;

        origins["vacuum"] = PointOrigins.Vacuum;
        origins["absolute"] = PointOrigins.Vacuum;
        origins["Vacuum"] = PointOrigins.Vacuum;
        origins[PointOrigins.Vacuum.Id] = PointOrigins.Vacuum;

        origins["gauge"] = PointOrigins.StandardAtmosphere;
        origins["atm"] = PointOrigins.StandardAtmosphere;
        origins["StandardAtmosphere"] = PointOrigins.StandardAtmosphere;
        origins[PointOrigins.StandardAtmosphere.Id] = PointOrigins.StandardAtmosphere;

        origins["sea_level"] = PointOrigins.SeaLevel;
        origins["SeaLevel"] = PointOrigins.SeaLevel;
        origins[PointOrigins.SeaLevel.Id] = PointOrigins.SeaLevel;
    }
}

/// <summary>
/// Deterministic unary and binary operator dispatch for dynamic scripting runtimes (StarLark-cs, Teal, REPLs)
/// operating over <see cref="Quantity{T}"/> (<c>double</c>), <see cref="QuantityPoint{T}"/> (<c>double</c>),
/// <see cref="SymbolicUnit"/>, and <see cref="double"/> scalars.
/// </summary>
public static class UnitScriptOps
{
    /// <summary>Dispatches <c>left + right</c> across quantities, affine points, and displacements.</summary>
    public static object Add(object left, object right) => (left, right) switch
    {
        (Quantity<double> a, Quantity<double> b) => a + b,
        (QuantityPoint<double> p, Quantity<double> d) => p + d,
        (Quantity<double> d, QuantityPoint<double> p) => d + p,
        (Quantity a, Quantity b) => a + b,
        (double a, double b) => a + b,
        (QuantityPoint<double>, QuantityPoint<double>) =>
            throw new InvalidOperationException("Two affine points cannot be added."),
        _ => throw new InvalidOperationException($"Unsupported addition: {left.GetType().Name} + {right.GetType().Name}.")
    };

    /// <summary>Dispatches <c>left - right</c> across quantities and affine points (<c>point - point => displacement</c>).</summary>
    public static object Subtract(object left, object right) => (left, right) switch
    {
        (Quantity<double> a, Quantity<double> b) => a - b,
        (QuantityPoint<double> a, QuantityPoint<double> b) => a - b,
        (QuantityPoint<double> p, Quantity<double> d) => p - d,
        (Quantity a, Quantity b) => a - b,
        (double a, double b) => a - b,
        _ => throw new InvalidOperationException($"Unsupported subtraction: {left.GetType().Name} - {right.GetType().Name}.")
    };

    /// <summary>Dispatches <c>left * right</c> across scalars, units (<c>10 * unit("mV")</c>), and quantities.</summary>
    public static object Multiply(object left, object right) => (left, right) switch
    {
        (Quantity<double> a, Quantity<double> b) => a * b,
        (Quantity<double> a, double s) => a * s,
        (double s, Quantity<double> a) => s * a,
        (double s, SymbolicUnit u) => Quantity<double>.Of(s, u),
        (SymbolicUnit u, double s) => Quantity<double>.Of(s, u),
        (SymbolicUnit a, SymbolicUnit b) => a * b,
        (Quantity a, Quantity b) => a * b,
        (Quantity a, double s) => a * s,
        (double s, Quantity a) => s * a,
        (double a, double b) => a * b,
        _ => throw new InvalidOperationException($"Unsupported multiplication: {left.GetType().Name} * {right.GetType().Name}.")
    };

    /// <summary>Dispatches <c>left / right</c> across scalars, units, and quantities.</summary>
    public static object Divide(object left, object right) => (left, right) switch
    {
        (Quantity<double> a, Quantity<double> b) => a / b,
        (Quantity<double> a, double s) => a / s,
        (double s, Quantity<double> a) => Quantity<double>.Dimensionless(s) / a,
        (SymbolicUnit a, SymbolicUnit b) => a / b,
        (Quantity a, Quantity b) => a / b,
        (Quantity a, double s) => a / s,
        (double a, double b) => a / b,
        _ => throw new InvalidOperationException($"Unsupported division: {left.GetType().Name} / {right.GetType().Name}.")
    };

    /// <summary>Dispatches unary negation.</summary>
    public static object Negate(object value) => value switch
    {
        Quantity<double> q => -q,
        Quantity q => -q,
        double d => -d,
        _ => throw new InvalidOperationException($"Unsupported negation for {value.GetType().Name}.")
    };

    /// <summary>Dispatches ordered comparison across compatible quantities or affine points.</summary>
    public static int Compare(object left, object right) => (left, right) switch
    {
        (Quantity<double> a, Quantity<double> b) => a.CompareTo(b),
        (QuantityPoint<double> a, QuantityPoint<double> b) => a.CompareTo(b),
        (Quantity a, Quantity b) => a.CompareTo(b),
        (double a, double b) => a.CompareTo(b),
        _ => throw new InvalidOperationException($"Unsupported comparison: {left.GetType().Name} vs {right.GetType().Name}.")
    };
}
