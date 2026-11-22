using System.Collections.Concurrent;

namespace SharpUnits;

/// <summary>Thread-safe registry for unit symbols, aliases, dynamic SI prefixes, and conversions.</summary>
public sealed class UnitRegistry
{
    private readonly ConcurrentDictionary<string, UnitDefinition> units = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, UnitDefinition> aliases = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, UnitDefinition> prefixCache = new(StringComparer.Ordinal);

    /// <summary>Shared default standard registry populated with SI, engineering, and imperial units.</summary>
    public static UnitRegistry Standard { get; } = CreateStandard();

    /// <summary>Registers a unit symbol; rejects duplicate primary symbols.</summary>
    public bool Register(UnitDefinition unit)
    {
        ArgumentNullException.ThrowIfNull(unit);
        if (string.IsNullOrWhiteSpace(unit.Symbol)) throw new ArgumentException("Unit symbol required.", nameof(unit));
        return units.TryAdd(unit.Symbol, unit);
    }

    /// <summary>Registers an alternative lookup alias (for example, <c>Ohm</c> for <c>Ω</c> or <c>degC</c> for <c>°C</c>).</summary>
    public bool RegisterAlias(string alias, UnitDefinition target)
    {
        if (string.IsNullOrWhiteSpace(alias)) throw new ArgumentException("Alias required.", nameof(alias));
        ArgumentNullException.ThrowIfNull(target);
        return aliases.TryAdd(alias, target);
    }

    /// <summary>Attempts to resolve an exact symbol, registered alias, or dynamically SI-prefixed unit.</summary>
    public bool TryResolve(string symbol, out UnitDefinition? unit)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            unit = null;
            return false;
        }
        if (units.TryGetValue(symbol, out unit)) return true;
        if (aliases.TryGetValue(symbol, out unit)) return true;
        if (prefixCache.TryGetValue(symbol, out unit)) return true;

        // Try longest prefix first ("da" before single-char prefixes like "d").
        if (TryResolvePrefixed(symbol, out unit) && unit is not null)
        {
            prefixCache.TryAdd(symbol, unit);
            return true;
        }
        unit = null;
        return false;
    }

    /// <summary>Resolves a unit symbol, alias, or SI-prefixed symbol; throws if unknown.</summary>
    public UnitDefinition Resolve(string symbol) =>
        TryResolve(symbol, out var unit) && unit is not null
            ? unit
            : throw new KeyNotFoundException($"Unknown unit: {symbol}");

    /// <summary>Converts between dimensionally compatible registered units.</summary>
    public double Convert(double value, string from, string to)
    {
        var source = Resolve(from);
        var target = Resolve(to);
        if (source.RationalDimension != target.RationalDimension)
            throw new InvalidOperationException("Incompatible dimensions.");
        return target.Converter.FromSi(source.Converter.ToSi(value));
    }

    /// <summary>Creates a fresh standard registry containing all SI, electrical, engineering, and common imperial units.</summary>
    public static UnitRegistry CreateStandard()
    {
        var r = new UnitRegistry();
        foreach (var unit in Units.AllStandard) r.Register(unit);

        // Common ASCII, engineering, and scientific aliases (prefixes also compose with prefixable aliases like Ohm and G).
        r.RegisterAlias("Ohm", Units.Ohm);
        r.RegisterAlias("ohm", Units.Ohm);
        r.RegisterAlias("degC", Units.Celsius);
        r.RegisterAlias("degF", Units.Fahrenheit);
        r.RegisterAlias("degR", Units.Rankine);
        r.RegisterAlias("deg", Units.Degree);
        r.RegisterAlias("hr", Units.Hour);
        r.RegisterAlias("sec", Units.Second);
        r.RegisterAlias("l", Units.Liter);
        r.RegisterAlias("cc", Units.Milliliter);
        r.RegisterAlias("mmHg", Units.Torr);
        r.RegisterAlias("Nm", Units.NewtonMeter);
        r.RegisterAlias("ftlbf", Units.FootPoundTorque);
        r.RegisterAlias("inlbf", Units.InchPoundTorque);
        r.RegisterAlias("thou", Units.Mil);
        r.RegisterAlias("micron", Units.Micrometer);
        r.RegisterAlias("NM", Units.NauticalMile);
        r.RegisterAlias("kt", Units.Knot);
        r.RegisterAlias("kmh", Units.KilometersPerHour);
        r.RegisterAlias("gn", Units.StandardGravity);
        r.RegisterAlias("gon", Units.Gradian);
        r.RegisterAlias("turn", Units.Revolution);
        r.RegisterAlias("′", Units.Arcminute);
        r.RegisterAlias("″", Units.Arcsecond);
        r.RegisterAlias("amu", Units.Dalton);
        r.RegisterAlias("kp", Units.KilogramForce);
        r.RegisterAlias("Btu", Units.Btu);
        r.RegisterAlias("gauss", Units.Gauss);
        r.RegisterAlias("CV", Units.MetricHorsepower);
        r.RegisterAlias("permille", Units.Permille);
        r.RegisterAlias("o", Units.Byte);
        return r;
    }

    private bool TryResolvePrefixed(string symbol, out UnitDefinition? result)
    {
        // Check 2-character IEC binary prefixes (Ki, Mi, Gi, Ti, Pi, Ei, Zi, Yi) first for binary-prefixable units.
        if (symbol.Length > 2)
        {
            string twoChar = symbol[..2];
            if (BinaryPrefixCatalog.TryResolve(twoChar, out var binPrefix) && binPrefix is not null)
            {
                string baseSymbol = symbol[2..];
                if (TryGetBinaryPrefixableBase(baseSymbol, out var binBase) && binBase is not null)
                {
                    result = BinaryPrefixes.Apply(binPrefix.Symbol, binBase);
                    return true;
                }
            }

            // Check two-character SI prefix "da" before single-char prefixes like "d".
            if (twoChar == "da")
            {
                string baseSymbol = symbol[2..];
                if (TryGetPrefixableBase(baseSymbol, out var baseUnit) && baseUnit is not null)
                {
                    result = SiPrefixes.Apply("da", baseUnit);
                    return true;
                }
            }
        }
        if (symbol.Length > 1)
        {
            string prefixToken = symbol[..1];
            string baseSymbol = symbol[1..];
            if (SiPrefixCatalog.TryResolve(prefixToken, out var prefix) && prefix is not null &&
                TryGetPrefixableBase(baseSymbol, out var baseUnit) && baseUnit is not null)
            {
                result = SiPrefixes.Apply(prefix.Symbol, baseUnit);
                return true;
            }
        }
        result = null;
        return false;
    }

    private bool TryGetPrefixableBase(string baseSymbol, out UnitDefinition? baseUnit)
    {
        if (units.TryGetValue(baseSymbol, out baseUnit) && baseUnit.AllowPrefixes) return true;
        if (aliases.TryGetValue(baseSymbol, out baseUnit) && baseUnit.AllowPrefixes) return true;
        baseUnit = null;
        return false;
    }

    private bool TryGetBinaryPrefixableBase(string baseSymbol, out UnitDefinition? baseUnit)
    {
        if (units.TryGetValue(baseSymbol, out baseUnit) && baseUnit.AllowBinaryPrefixes) return true;
        if (aliases.TryGetValue(baseSymbol, out baseUnit) && baseUnit.AllowBinaryPrefixes) return true;
        baseUnit = null;
        return false;
    }
}