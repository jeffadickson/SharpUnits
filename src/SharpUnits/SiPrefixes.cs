namespace SharpUnits;

/// <summary>SI decimal prefixes used to derive linear units without changing their dimensions.</summary>
public static class SiPrefixes
{
    /// <summary>Creates a prefixed linear unit. Affine units cannot be prefixed.</summary>
    public static UnitDefinition Apply(string prefix, UnitDefinition unit)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        ArgumentNullException.ThrowIfNull(unit);
        if (!SiPrefixCatalog.TryResolve(prefix, out var meta) || meta is null)
            throw new ArgumentException($"Unknown SI prefix: {prefix}", nameof(prefix));
        if (!unit.Converter.IsLinear)
            throw new InvalidOperationException("Cannot prefix an affine unit.");
        string canonicalSymbol = meta.Symbol + unit.Symbol;
        string canonicalName = meta.Name + unit.Name;
        return new UnitDefinition(
            canonicalSymbol,
            unit.RationalDimension,
            UnitConversion.Linear(unit.Converter.Scale * meta.Value),
            name: canonicalName,
            allowPrefixes: false,
            defaultKind: unit.DefaultKind);
    }

    /// <summary>Returns a decimal prefix's exponent.</summary>
    public static int Exponent(string prefix)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        if (!SiPrefixCatalog.TryResolve(prefix, out var meta) || meta is null)
            throw new ArgumentException($"Unknown SI prefix: {prefix}", nameof(prefix));
        return meta.Exponent;
    }
}
