using System.Globalization;

namespace SharpUnits.Interop.Lua;

/// <summary>
/// Lua / Teal runtime bridge wrapping <see cref="UnitEnvironment"/> and <see cref="UnitScriptOps"/> with
/// 1-based Lua sequence indexing and Teal-compatible method signatures (matching <c>bindings/teal/sharpunits.d.tl</c>).
/// </summary>
public sealed class LuaUnitsBridge
{
    /// <summary>Underlying deterministic unit environment.</summary>
    public UnitEnvironment Environment { get; }

    /// <summary>Creates a Lua/Teal bridge backed by an explicit or fresh standard environment.</summary>
    public LuaUnitsBridge(UnitEnvironment? environment = null) =>
        Environment = environment ?? UnitEnvironment.CreateStandard();

    /// <summary>Resolves a unit expression (<c>SharpUnits.unit(expr)</c>).</summary>
    public SymbolicUnit Unit(string expression) => Environment.ResolveUnit(expression);

    /// <summary>Creates a linear quantity (<c>SharpUnits.qty(value, unit_expr)</c>).</summary>
    public Quantity<double> Qty(double value, string unitExpression) => Environment.CreateQuantity(value, unitExpression);

    /// <summary>Parses a quantity literal (<c>SharpUnits.parse(literal)</c>).</summary>
    public Quantity<double> Parse(string literal) => Environment.ParseQuantity(literal).AsGeneric<double>();

    /// <summary>Creates an affine point (<c>SharpUnits.point(value, unit_expr, origin?)</c>).</summary>
    public QuantityPoint<double> Point(double value, string unitExpression, string? originName = null) =>
        Environment.CreatePoint(value, unitExpression, originName);

    /// <summary>Creates a SIMD-backed series (<c>SharpUnits.series(values, unit_expr, origin?)</c>).</summary>
    public QuantitySeries<double> Series(double[] values, string unitExpression, string? originName = null)
    {
        ArgumentNullException.ThrowIfNull(values);
        var unit = Environment.ResolveUnit(unitExpression);
        var origin = originName is not null ? Environment.ResolveOrigin(originName) : null;
        return QuantitySeries<double>.FromValues(values, unit, origin?.Spec, origin);
    }

    /// <summary>Converts a quantity to a target unit symbol or <see cref="SymbolicUnit"/>.</summary>
    public Quantity<double> InUnit(Quantity<double> quantity, object target) => target switch
    {
        string expr => Environment.Convert(quantity, expr),
        SymbolicUnit u => quantity.In(u),
        _ => throw new ArgumentException("Target unit must be string or SymbolicUnit.", nameof(target))
    };

    /// <summary>Converts a quantity point to a target unit or named origin.</summary>
    public QuantityPoint<double> PointRelativeTo(QuantityPoint<double> point, string originName) =>
        point.RelativeTo(Environment.ResolveOrigin(originName));

    /// <summary>Shifts an affine point by a displacement (<c>point:shift(delta)</c>).</summary>
    public QuantityPoint<double> PointShift(QuantityPoint<double> point, Quantity<double> delta) => point + delta;

    /// <summary>Returns the element at 1-based Lua index <paramref name="luaIndex"/> from a series.</summary>
    public Quantity<double> SeriesAt(QuantitySeries<double> series, int luaIndex)
    {
        ArgumentNullException.ThrowIfNull(series);
        if (luaIndex < 1 || luaIndex > series.Count)
            throw new ArgumentOutOfRangeException(nameof(luaIndex), $"Lua index {luaIndex} out of range 1..{series.Count}.");
        return series[luaIndex - 1];
    }

    /// <summary>Converts all samples in a series to a target unit expression.</summary>
    public QuantitySeries<double> SeriesInUnit(QuantitySeries<double> series, string targetUnit) =>
        series.In(Environment.ResolveUnit(targetUnit));

    /// <summary>Formats a quantity using invariant culture and optional format specifier (<c>"eng"</c>, <c>"ascii"</c>, etc.).</summary>
    public string Format(Quantity<double> quantity, string? format = null) =>
        quantity.ToQuantity().ToString(format, CultureInfo.InvariantCulture);

    /// <summary>Metamethod <c>__add</c>.</summary>
    public object Add(object a, object b) => UnitScriptOps.Add(a, b);

    /// <summary>Metamethod <c>__sub</c>.</summary>
    public object Sub(object a, object b) => UnitScriptOps.Subtract(a, b);

    /// <summary>Metamethod <c>__mul</c>.</summary>
    public object Mul(object a, object b) => UnitScriptOps.Multiply(a, b);

    /// <summary>Metamethod <c>__div</c>.</summary>
    public object Div(object a, object b) => UnitScriptOps.Divide(a, b);

    /// <summary>Metamethod <c>__unm</c>.</summary>
    public object Unm(object a) => UnitScriptOps.Negate(a);

    /// <summary>Metamethod <c>__lt</c>.</summary>
    public bool Lt(object a, object b) => UnitScriptOps.Compare(a, b) < 0;

    /// <summary>Metamethod <c>__le</c>.</summary>
    public bool Le(object a, object b) => UnitScriptOps.Compare(a, b) <= 0;

    /// <summary>Metamethod <c>__eq</c> using physical equivalence.</summary>
    public bool Eq(object a, object b) => (a, b) switch
    {
        (Quantity<double> x, Quantity<double> y) => x.IsEquivalentTo(y),
        (QuantityPoint<double> x, QuantityPoint<double> y) => x.Origin.Dimension == y.Origin.Dimension && x.CompareTo(y) == 0,
        (SymbolicUnit x, SymbolicUnit y) => x.Equals(y),
        _ => Equals(a, b)
    };
}
