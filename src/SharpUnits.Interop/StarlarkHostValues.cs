using System.Globalization;
using System.Numerics;

namespace SharpUnits.Interop.Starlark;

/// <summary>Binary operator subset supported by Starlark unit host values (matching <c>Starlark.Syntax.BinOp</c>).</summary>
public enum StarBinOp
{
    /// <summary>Addition (<c>+</c>).</summary>
    Add,

    /// <summary>Subtraction (<c>-</c>).</summary>
    Subtract,

    /// <summary>Multiplication (<c>*</c>).</summary>
    Multiply,

    /// <summary>Division (<c>/</c>).</summary>
    Divide
}

/// <summary>
/// Thread-safe, immutable host value contract mirroring <c>Starlark.Eval.IThreadSafeHostValue</c>
/// so <c>StarLark-cs</c> can expose SharpUnits natively (including across <c>spawn</c>/<c>parallel_map</c> futures)
/// through a thin adapter.
/// </summary>
public interface IStarUnitHostValue
{
    /// <summary>Type name returned by Starlark <c>type()</c>.</summary>
    string TypeName { get; }

    /// <summary>Text returned by Starlark <c>repr()</c> and <c>str()</c>.</summary>
    string Repr();

    /// <summary>Truthiness for Starlark boolean contexts.</summary>
    bool Truthy() => true;

    /// <summary>Looks up a field or bound method attribute.</summary>
    bool TryGetAttribute(string name, out object? value);

    /// <summary>Attribute names listed by Starlark <c>dir()</c>.</summary>
    IEnumerable<string> Attributes();

    /// <summary>Dispatches a binary operator; <paramref name="reflected"/> is true when this object is on the right-hand side.</summary>
    bool TryBinary(StarBinOp op, object? other, bool reflected, out object? result);

    /// <summary>Dispatches unary negation.</summary>
    bool TryNegate(out object? result) { result = null; return false; }

    /// <summary>Value equality.</summary>
    bool EqualsValue(object? other);

    /// <summary>Hash code consistent with <see cref="EqualsValue"/>.</summary>
    int HashValue();

    /// <summary>Three-way comparison, or null when not orderable with <paramref name="other"/>.</summary>
    int? CompareTo(object? other) => null;

    /// <summary>Length for Starlark <c>len()</c>, or null if unsized.</summary>
    BigInteger? Length() => null;

    /// <summary>Indexing for Starlark <c>x[i]</c>.</summary>
    bool TryIndex(object? index, out object? result) { result = null; return false; }

    /// <summary>Iteration for Starlark <c>for</c> loops and <c>list()</c>.</summary>
    IEnumerable<object?>? Iterate() => null;
}

/// <summary>Numeric conversion helpers matching StarLark-cs's ties-to-even <see cref="BigInteger"/>-to-<see cref="double"/> rules.</summary>
public static class StarNumerics
{
    /// <summary>Attempts to extract a finite <see cref="double"/> from a Starlark numeric box (<see cref="BigInteger"/>, <see cref="double"/>, <see cref="int"/>, <see cref="long"/>).</summary>
    public static bool TryToDouble(object? value, out double number)
    {
        switch (value)
        {
            case double d:
                number = d;
                return true;
            case BigInteger bi:
                number = BigToDouble(bi);
                return true;
            case int i:
                number = i;
                return true;
            case long l:
                number = l;
                return true;
            case float f:
                number = f;
                return true;
            case decimal m:
                number = (double)m;
                return true;
            default:
                number = 0d;
                return false;
        }
    }

    /// <summary>Converts a <see cref="BigInteger"/> to the nearest <see cref="double"/> using ties-to-even rounding.</summary>
    public static double BigToDouble(BigInteger i)
    {
        if (i >= long.MinValue && i <= long.MaxValue) return (double)(long)i;
        var magnitude = BigInteger.Abs(i);
        int shift = (int)magnitude.GetBitLength() - 64;
        ulong top = (ulong)(magnitude >> shift);
        if (!(magnitude & ((BigInteger.One << shift) - 1)).IsZero) top |= 1;
        double r = shift > 1100 ? double.PositiveInfinity : Math.ScaleB((double)top, shift);
        return i.Sign < 0 ? -r : r;
    }
}

/// <summary>Immutable Starlark <c>"unit"</c> value wrapping <see cref="SymbolicUnit"/>.</summary>
public sealed class StarUnitObject : IStarUnitHostValue
{
    private static readonly string[] AttrNames = ["symbol", "ascii", "scale_to_si", "dimension", "pow", "root", "to_si"];

    /// <summary>Wrapped symbolic unit.</summary>
    public SymbolicUnit Unit { get; }

    /// <summary>Parent deterministic environment.</summary>
    public UnitEnvironment Environment { get; }

    /// <summary>Creates a Starlark unit host value.</summary>
    public StarUnitObject(SymbolicUnit unit, UnitEnvironment environment)
    {
        Unit = unit ?? throw new ArgumentNullException(nameof(unit));
        Environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    /// <inheritdoc/>
    public string TypeName => "unit";

    /// <inheritdoc/>
    public string Repr() => $"unit(\"{Unit.Symbol}\")";

    /// <inheritdoc/>
    public IEnumerable<string> Attributes() => AttrNames;

    /// <inheritdoc/>
    public bool TryGetAttribute(string name, out object? value)
    {
        switch (name)
        {
            case "symbol": value = Unit.Symbol; return true;
            case "ascii": value = Unit.AsciiSymbol; return true;
            case "scale_to_si": value = Unit.ScaleToSi; return true;
            case "dimension": value = Unit.Dimension.ToCoherentSiSymbol(); return true;
            case "pow": value = new Func<BigInteger, StarUnitObject>(p => new(Unit.Pow((int)p), Environment)); return true;
            case "root": value = new Func<BigInteger, StarUnitObject>(d => new(Unit.Root((int)d), Environment)); return true;
            case "to_si": value = new Func<StarUnitObject>(() => new(Unit.ToCoherentSi(), Environment)); return true;
            default: value = null; return false;
        }
    }

    /// <inheritdoc/>
    public bool TryBinary(StarBinOp op, object? other, bool reflected, out object? result)
    {
        if (op == StarBinOp.Multiply && StarNumerics.TryToDouble(other, out double scalar))
        {
            result = new StarQuantityObject(Quantity<double>.Of(scalar, Unit), Environment);
            return true;
        }
        if (other is StarUnitObject otherUnit)
        {
            var lhs = reflected ? otherUnit.Unit : Unit;
            var rhs = reflected ? Unit : otherUnit.Unit;
            if (op == StarBinOp.Multiply) { result = new StarUnitObject(lhs * rhs, Environment); return true; }
            if (op == StarBinOp.Divide) { result = new StarUnitObject(lhs / rhs, Environment); return true; }
        }
        result = null;
        return false;
    }

    /// <inheritdoc/>
    public bool EqualsValue(object? other) => other is StarUnitObject u && Unit.Equals(u.Unit);

    /// <inheritdoc/>
    public int HashValue() => Unit.GetHashCode();
}

/// <summary>Immutable Starlark <c>"quantity"</c> value wrapping <see cref="Quantity{T}"/> (<see cref="double"/>).</summary>
public sealed class StarQuantityObject : IStarUnitHostValue
{
    private static readonly string[] AttrNames = ["value", "si_value", "unit", "spec", "to", "to_si", "format"];

    /// <summary>Wrapped generic quantity.</summary>
    public Quantity<double> Quantity { get; }

    /// <summary>Parent deterministic environment.</summary>
    public UnitEnvironment Environment { get; }

    /// <summary>Creates a Starlark quantity host value.</summary>
    public StarQuantityObject(Quantity<double> quantity, UnitEnvironment environment)
    {
        Quantity = quantity;
        Environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    /// <inheritdoc/>
    public string TypeName => "quantity";

    /// <inheritdoc/>
    public string Repr() => Quantity.ToString();

    /// <inheritdoc/>
    public bool Truthy() => Quantity.Value != 0d;

    /// <inheritdoc/>
    public IEnumerable<string> Attributes() => AttrNames;

    /// <inheritdoc/>
    public bool TryGetAttribute(string name, out object? value)
    {
        switch (name)
        {
            case "value": value = Quantity.Value; return true;
            case "si_value": value = Quantity.SiValue; return true;
            case "unit": value = new StarUnitObject(Quantity.Unit, Environment); return true;
            case "spec": value = Quantity.Spec?.Id; return true;
            case "to": value = new Func<object, StarQuantityObject>(To); return true;
            case "to_si": value = new Func<StarQuantityObject>(() => new(Quantity.ToCoherentSi(), Environment)); return true;
            case "format": value = new Func<string?, string>(fmt => Quantity.ToQuantity().ToString(fmt, CultureInfo.InvariantCulture)); return true;
            default: value = null; return false;
        }
    }

    /// <summary>Converts to a target unit symbol or <see cref="StarUnitObject"/>.</summary>
    public StarQuantityObject To(object target) => target switch
    {
        string expr => new(Environment.Convert(Quantity, expr), Environment),
        StarUnitObject u => new(Quantity.In(u.Unit), Environment),
        _ => throw new ArgumentException("Target unit must be a string expression or unit value.", nameof(target))
    };

    /// <inheritdoc/>
    public bool TryBinary(StarBinOp op, object? other, bool reflected, out object? result)
    {
        if (other is StarQuantityObject otherQty)
        {
            var lhs = reflected ? otherQty.Quantity : Quantity;
            var rhs = reflected ? Quantity : otherQty.Quantity;
            switch (op)
            {
                case StarBinOp.Add: result = new StarQuantityObject(lhs + rhs, Environment); return true;
                case StarBinOp.Subtract: result = new StarQuantityObject(lhs - rhs, Environment); return true;
                case StarBinOp.Multiply: result = new StarQuantityObject(lhs * rhs, Environment); return true;
                case StarBinOp.Divide: result = new StarQuantityObject(lhs / rhs, Environment); return true;
            }
        }
        if (other is StarQuantityPointObject pt && op == StarBinOp.Add)
        {
            result = new StarQuantityPointObject(pt.Point + Quantity, Environment);
            return true;
        }
        if (StarNumerics.TryToDouble(other, out double scalar))
        {
            if (op == StarBinOp.Multiply)
            {
                result = new StarQuantityObject(Quantity * scalar, Environment);
                return true;
            }
            if (op == StarBinOp.Divide)
            {
                result = reflected
                    ? new StarQuantityObject(Quantity<double>.Dimensionless(scalar) / Quantity, Environment)
                    : new StarQuantityObject(Quantity / scalar, Environment);
                return true;
            }
        }
        result = null;
        return false;
    }

    /// <inheritdoc/>
    public bool TryNegate(out object? result)
    {
        result = new StarQuantityObject(-Quantity, Environment);
        return true;
    }

    /// <inheritdoc/>
    public bool EqualsValue(object? other) =>
        other is StarQuantityObject q && Quantity.IsEquivalentTo(q.Quantity);

    /// <inheritdoc/>
    public int HashValue() => HashCode.Combine(Quantity.SiValue, Quantity.Dimension, Quantity.Spec?.KindRoot);

    /// <inheritdoc/>
    public int? CompareTo(object? other) =>
        other is StarQuantityObject q ? Quantity.CompareTo(q.Quantity) : null;
}

/// <summary>Immutable Starlark <c>"quantity_point"</c> value wrapping affine <see cref="QuantityPoint{T}"/>.</summary>
public sealed class StarQuantityPointObject : IStarUnitHostValue
{
    private static readonly string[] AttrNames = ["value", "unit", "origin", "to", "relative_to", "to_c", "to_f", "to_k"];

    /// <summary>Wrapped affine quantity point.</summary>
    public QuantityPoint<double> Point { get; }

    /// <summary>Parent deterministic environment.</summary>
    public UnitEnvironment Environment { get; }

    /// <summary>Creates a Starlark affine quantity-point host value.</summary>
    public StarQuantityPointObject(QuantityPoint<double> point, UnitEnvironment environment)
    {
        Point = point;
        Environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    /// <inheritdoc/>
    public string TypeName => "quantity_point";

    /// <inheritdoc/>
    public string Repr() => Point.ToString();

    /// <inheritdoc/>
    public IEnumerable<string> Attributes() => AttrNames;

    /// <inheritdoc/>
    public bool TryGetAttribute(string name, out object? value)
    {
        switch (name)
        {
            case "value": value = Point.Value; return true;
            case "unit": value = new StarUnitObject(Point.Unit, Environment); return true;
            case "origin": value = Point.Origin.Id; return true;
            case "to": value = new Func<string, StarQuantityPointObject>(u => new(Environment.ConvertPoint(Point, u), Environment)); return true;
            case "relative_to": value = new Func<string, StarQuantityPointObject>(o => new(Point.RelativeTo(Environment.ResolveOrigin(o)), Environment)); return true;
            case "to_c": value = new Func<StarQuantityPointObject>(() => new(Point.ToCelsius(), Environment)); return true;
            case "to_f": value = new Func<StarQuantityPointObject>(() => new(Point.ToFahrenheit(), Environment)); return true;
            case "to_k": value = new Func<StarQuantityPointObject>(() => new(Point.ToKelvin(), Environment)); return true;
            default: value = null; return false;
        }
    }

    /// <inheritdoc/>
    public bool TryBinary(StarBinOp op, object? other, bool reflected, out object? result)
    {
        if (other is StarQuantityPointObject otherPt && op == StarBinOp.Subtract)
        {
            var lhs = reflected ? otherPt.Point : Point;
            var rhs = reflected ? Point : otherPt.Point;
            result = new StarQuantityObject(lhs - rhs, Environment);
            return true;
        }
        if (other is StarQuantityObject delta)
        {
            if (op == StarBinOp.Add)
            {
                result = new StarQuantityPointObject(Point + delta.Quantity, Environment);
                return true;
            }
            if (op == StarBinOp.Subtract && !reflected)
            {
                result = new StarQuantityPointObject(Point - delta.Quantity, Environment);
                return true;
            }
        }
        result = null;
        return false;
    }

    /// <inheritdoc/>
    public bool EqualsValue(object? other) =>
        other is StarQuantityPointObject p && Point.Origin.Dimension == p.Point.Origin.Dimension && Point.CompareTo(p.Point) == 0;

    /// <inheritdoc/>
    public int HashValue() => HashCode.Combine(Point.AbsoluteSiValue, Point.Origin.Dimension);

    /// <inheritdoc/>
    public int? CompareTo(object? other) =>
        other is StarQuantityPointObject p ? Point.CompareTo(p.Point) : null;
}

/// <summary>Immutable, SIMD-backed Starlark <c>"quantity_series"</c> value wrapping <see cref="QuantitySeries{T}"/>.</summary>
public sealed class StarQuantitySeriesObject : IStarUnitHostValue
{
    private static readonly string[] AttrNames = ["unit", "count", "to", "auto_scale", "min", "max", "mean", "rms", "peak_to_peak", "relative_to"];

    /// <summary>Wrapped SIMD-accelerated series.</summary>
    public QuantitySeries<double> Series { get; }

    /// <summary>Parent deterministic environment.</summary>
    public UnitEnvironment Environment { get; }

    /// <summary>Creates a Starlark quantity series host value.</summary>
    public StarQuantitySeriesObject(QuantitySeries<double> series, UnitEnvironment environment)
    {
        Series = series ?? throw new ArgumentNullException(nameof(series));
        Environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    /// <inheritdoc/>
    public string TypeName => "quantity_series";

    /// <inheritdoc/>
    public string Repr() => $"quantity_series(count={Series.Count}, unit=\"{Series.Unit.Symbol}\")";

    /// <inheritdoc/>
    public bool Truthy() => Series.Count > 0;

    /// <inheritdoc/>
    public IEnumerable<string> Attributes() => AttrNames;

    /// <inheritdoc/>
    public bool TryGetAttribute(string name, out object? value)
    {
        switch (name)
        {
            case "unit": value = new StarUnitObject(Series.Unit, Environment); return true;
            case "count": value = new BigInteger(Series.Count); return true;
            case "to": value = new Func<string, StarQuantitySeriesObject>(u => new(Series.In(Environment.ResolveUnit(u)), Environment)); return true;
            case "auto_scale": value = new Func<StarQuantitySeriesObject>(() => new(Series.AutoScale(), Environment)); return true;
            case "min": value = new Func<StarQuantityObject>(() => new(Series.MinMax().Min, Environment)); return true;
            case "max": value = new Func<StarQuantityObject>(() => new(Series.MinMax().Max, Environment)); return true;
            case "mean": value = new Func<StarQuantityObject>(() => new(Series.Mean(), Environment)); return true;
            case "rms": value = new Func<StarQuantityObject>(() => new(Series.Rms(), Environment)); return true;
            case "peak_to_peak": value = new Func<StarQuantityObject>(() => new(Series.PeakToPeak(), Environment)); return true;
            case "relative_to": value = new Func<string, StarQuantitySeriesObject>(o => new(Series.RelativeTo(Environment.ResolveOrigin(o)), Environment)); return true;
            default: value = null; return false;
        }
    }

    /// <inheritdoc/>
    public bool TryBinary(StarBinOp op, object? other, bool reflected, out object? result)
    {
        if (other is StarQuantitySeriesObject otherSeries)
        {
            var lhs = reflected ? otherSeries.Series : Series;
            var rhs = reflected ? Series : otherSeries.Series;
            switch (op)
            {
                case StarBinOp.Add: result = new StarQuantitySeriesObject(lhs + rhs, Environment); return true;
                case StarBinOp.Subtract: result = new StarQuantitySeriesObject(lhs - rhs, Environment); return true;
                case StarBinOp.Multiply: result = new StarQuantitySeriesObject(lhs * rhs, Environment); return true;
                case StarBinOp.Divide: result = new StarQuantitySeriesObject(lhs / rhs, Environment); return true;
            }
        }
        if (StarNumerics.TryToDouble(other, out double scalar))
        {
            if (op == StarBinOp.Multiply) { result = new StarQuantitySeriesObject(Series * scalar, Environment); return true; }
            if (op == StarBinOp.Divide && !reflected) { result = new StarQuantitySeriesObject(Series / scalar, Environment); return true; }
        }
        result = null;
        return false;
    }

    /// <inheritdoc/>
    public BigInteger? Length() => new BigInteger(Series.Count);

    /// <inheritdoc/>
    public bool TryIndex(object? index, out object? result)
    {
        int i;
        if (index is BigInteger bi && bi >= int.MinValue && bi <= int.MaxValue) i = (int)bi;
        else if (index is int raw) i = raw;
        else { result = null; return false; }

        if (i < 0) i += Series.Count;
        if (i < 0 || i >= Series.Count)
            throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} out of range for series of length {Series.Count}.");

        result = Series.Origin is not null
            ? new StarQuantityPointObject(Series.PointAt(i), Environment)
            : new StarQuantityObject(Series[i], Environment);
        return true;
    }

    /// <inheritdoc/>
    public IEnumerable<object?> Iterate()
    {
        for (int i = 0; i < Series.Count; i++)
            yield return Series.Origin is not null
                ? new StarQuantityPointObject(Series.PointAt(i), Environment)
                : new StarQuantityObject(Series[i], Environment);
    }

    /// <inheritdoc/>
    public bool EqualsValue(object? other) => ReferenceEquals(this, other);

    /// <inheritdoc/>
    public int HashValue() => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);
}

/// <summary>Root Starlark <c>units</c> module exposing <c>unit</c>, <c>qty</c>, <c>parse</c>, <c>point</c>, and <c>series</c>.</summary>
public sealed class StarUnitsModule : IStarUnitHostValue
{
    private static readonly string[] AttrNames = ["unit", "qty", "parse", "point", "series"];
    private readonly Action<long>? onAllocateBytes;

    /// <summary>Underlying isolated unit environment.</summary>
    public UnitEnvironment Environment { get; }

    /// <summary>Creates an isolated Starlark <c>units</c> module with an optional memory-budget hook.</summary>
    public StarUnitsModule(UnitEnvironment? environment = null, Action<long>? onAllocateBytes = null)
    {
        Environment = environment ?? UnitEnvironment.CreateStandard();
        this.onAllocateBytes = onAllocateBytes;
    }

    /// <inheritdoc/>
    public string TypeName => "units_module";

    /// <inheritdoc/>
    public string Repr() => "<module 'units'>";

    /// <inheritdoc/>
    public IEnumerable<string> Attributes() => AttrNames;

    /// <summary>Resolves a unit expression into a <see cref="StarUnitObject"/>.</summary>
    public StarUnitObject Unit(string expression) => new(Environment.ResolveUnit(expression), Environment);

    /// <summary>Creates a linear <see cref="StarQuantityObject"/>.</summary>
    public StarQuantityObject Qty(object value, string unitExpression)
    {
        if (!StarNumerics.TryToDouble(value, out double d))
            throw new ArgumentException("Quantity magnitude must be numeric.", nameof(value));
        return new(Environment.CreateQuantity(d, unitExpression), Environment);
    }

    /// <summary>Parses a quantity string literal into a <see cref="StarQuantityObject"/>.</summary>
    public StarQuantityObject Parse(string literal)
    {
        var q = Environment.ParseQuantity(literal);
        return new(q.AsGeneric<double>(), Environment);
    }

    /// <summary>Creates an affine <see cref="StarQuantityPointObject"/> (for example <c>point(25, "°C")</c> or <c>point(2, "bar", "gauge")</c>).</summary>
    public StarQuantityPointObject Point(object value, string unitExpression, string? originName = null)
    {
        if (!StarNumerics.TryToDouble(value, out double d))
            throw new ArgumentException("Point magnitude must be numeric.", nameof(value));
        return new(Environment.CreatePoint(d, unitExpression, originName), Environment);
    }

    /// <summary>Creates a SIMD-backed <see cref="StarQuantitySeriesObject"/> from a sequence of Starlark numeric values.</summary>
    public StarQuantitySeriesObject Series(IEnumerable<object?> values, string unitExpression, string? originName = null)
    {
        ArgumentNullException.ThrowIfNull(values);
        var list = values as ICollection<object?> ?? values.ToList();
        onAllocateBytes?.Invoke((long)list.Count * sizeof(double));
        var buffer = new double[list.Count];
        int i = 0;
        foreach (var item in list)
        {
            if (!StarNumerics.TryToDouble(item, out double d))
                throw new ArgumentException($"Series element at index {i} is not numeric.", nameof(values));
            buffer[i++] = d;
        }
        var unit = Environment.ResolveUnit(unitExpression);
        var origin = originName is not null ? Environment.ResolveOrigin(originName) : null;
        return new(QuantitySeries<double>.FromValues(buffer, unit, origin?.Spec, origin), Environment);
    }

    /// <inheritdoc/>
    public bool TryGetAttribute(string name, out object? value)
    {
        switch (name)
        {
            case "unit": value = new Func<string, StarUnitObject>(Unit); return true;
            case "qty": value = new Func<object, string, StarQuantityObject>(Qty); return true;
            case "parse": value = new Func<string, StarQuantityObject>(Parse); return true;
            case "point": value = new Func<object, string, string?, StarQuantityPointObject>(Point); return true;
            case "series": value = new Func<IEnumerable<object?>, string, string?, StarQuantitySeriesObject>(Series); return true;
            default: value = null; return false;
        }
    }

    /// <inheritdoc/>
    public bool TryBinary(StarBinOp op, object? other, bool reflected, out object? result) { result = null; return false; }

    /// <inheritdoc/>
    public bool EqualsValue(object? other) => ReferenceEquals(this, other);

    /// <inheritdoc/>
    public int HashValue() => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);
}
