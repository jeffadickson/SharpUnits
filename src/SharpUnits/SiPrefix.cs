namespace SharpUnits;

/// <summary>Immutable decimal SI prefix metadata.</summary>
public sealed record SiPrefix
{
    /// <summary>Human-readable prefix name (e.g. <c>milli</c>, <c>kilo</c>).</summary>
    public string Name { get; }

    /// <summary>Canonical prefix symbol (e.g. <c>m</c>, <c>k</c>, <c>µ</c>).</summary>
    public string Symbol { get; }

    /// <summary>ASCII-safe symbol (e.g. <c>u</c> for micro, otherwise <see cref="Symbol"/>).</summary>
    public string AsciiSymbol { get; }

    /// <summary>Decimal exponent of ten.</summary>
    public int Exponent { get; }

    /// <summary>Prefix numeric factor in double precision.</summary>
    public double Value => Math.Pow(10d, Exponent);

    /// <summary>Constructs a valid SI prefix.</summary>
    public SiPrefix(string name, string symbol, int exponent, string? asciiSymbol = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name required.", nameof(name));
        if (string.IsNullOrWhiteSpace(symbol)) throw new ArgumentException("Symbol required.", nameof(symbol));
        if (exponent is < -30 or > 30) throw new ArgumentOutOfRangeException(nameof(exponent));
        Name = name;
        Symbol = symbol;
        AsciiSymbol = string.IsNullOrWhiteSpace(asciiSymbol) ? symbol : asciiSymbol;
        Exponent = exponent;
    }

    /// <summary>Applies this prefix to a linear unit.</summary>
    public UnitDefinition Apply(UnitDefinition unit) => SiPrefixes.Apply(Symbol, unit);
}

/// <summary>Canonical SI decimal prefix metadata including the 2022 BIPM additions.</summary>
public static class SiPrefixCatalog
{
    private static readonly SiPrefix[] Items =
    [
        new("quetta", "Q", 30),
        new("ronna", "R", 27),
        new("yotta", "Y", 24),
        new("zetta", "Z", 21),
        new("exa", "E", 18),
        new("peta", "P", 15),
        new("tera", "T", 12),
        new("giga", "G", 9),
        new("mega", "M", 6),
        new("kilo", "k", 3),
        new("hecto", "h", 2),
        new("deca", "da", 1),
        new("deci", "d", -1),
        new("centi", "c", -2),
        new("milli", "m", -3),
        new("micro", "µ", -6, "u"),
        new("nano", "n", -9),
        new("pico", "p", -12),
        new("femto", "f", -15),
        new("atto", "a", -18),
        new("zepto", "z", -21),
        new("yocto", "y", -24),
        new("ronto", "r", -27),
        new("quecto", "q", -30)
    ];

    private static readonly SiPrefix[] EngineeringItems =
        Items.Where(p => p.Exponent % 3 == 0).OrderByDescending(p => p.Exponent).ToArray();

    /// <summary>Returns all 24 canonical SI prefixes ordered from largest to smallest.</summary>
    public static IReadOnlyList<SiPrefix> All => Array.AsReadOnly(Items);

    /// <summary>Returns the engineering SI prefixes (exponents that are multiples of 3).</summary>
    public static IReadOnlyList<SiPrefix> Engineering => Array.AsReadOnly(EngineeringItems);

    /// <summary>Looks up a prefix by symbol, also accepting ASCII <c>u</c> and Greek <c>μ</c> for micro.</summary>
    public static SiPrefix Resolve(string symbol) =>
        TryResolve(symbol, out var prefix) ? prefix! : throw new KeyNotFoundException($"Unknown SI prefix: {symbol}");

    /// <summary>Attempts to resolve a prefix symbol (including <c>u</c> and <c>μ</c> aliases for micro).</summary>
    public static bool TryResolve(string symbol, out SiPrefix? prefix)
    {
        if (symbol is "u" or "μ")
        {
            prefix = Items[15]; // micro
            return true;
        }
        prefix = Items.FirstOrDefault(p => p.Symbol == symbol);
        return prefix is not null;
    }
}

/// <summary>Immutable IEC 80000-13 binary prefix metadata (powers of 1024 = 2¹⁰).</summary>
public sealed record BinaryPrefix
{
    /// <summary>Human-readable binary prefix name (e.g. <c>kibi</c>, <c>mebi</c>, <c>gibi</c>).</summary>
    public string Name { get; }

    /// <summary>Canonical IEC symbol (e.g. <c>Ki</c>, <c>Mi</c>, <c>Gi</c>).</summary>
    public string Symbol { get; }

    /// <summary>Base-2 exponent (10, 20, ..., 80).</summary>
    public int BinaryExponent { get; }

    /// <summary>Exact scale factor (<c>2^BinaryExponent</c>).</summary>
    public double Value => Math.Pow(2d, BinaryExponent);

    /// <summary>Constructs a valid IEC binary prefix.</summary>
    public BinaryPrefix(string name, string symbol, int binaryExponent)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name required.", nameof(name));
        if (string.IsNullOrWhiteSpace(symbol)) throw new ArgumentException("Symbol required.", nameof(symbol));
        if (binaryExponent is < 10 or > 80) throw new ArgumentOutOfRangeException(nameof(binaryExponent));
        Name = name;
        Symbol = symbol;
        BinaryExponent = binaryExponent;
    }

    /// <summary>Applies this binary prefix to a linear information unit.</summary>
    public UnitDefinition Apply(UnitDefinition unit) => BinaryPrefixes.Apply(Symbol, unit);
}

/// <summary>Canonical IEC 80000-13 binary prefix catalog (<c>Ki</c> through <c>Yi</c>).</summary>
public static class BinaryPrefixCatalog
{
    private static readonly BinaryPrefix[] Items =
    [
        new("yobi", "Yi", 80),
        new("zebi", "Zi", 70),
        new("exbi", "Ei", 60),
        new("pebi", "Pi", 50),
        new("tebi", "Ti", 40),
        new("gibi", "Gi", 30),
        new("mebi", "Mi", 20),
        new("kibi", "Ki", 10)
    ];

    /// <summary>Returns all 8 IEC binary prefixes ordered from largest to smallest.</summary>
    public static IReadOnlyList<BinaryPrefix> All => Array.AsReadOnly(Items);

    /// <summary>Looks up a binary prefix by symbol (e.g. <c>Ki</c>, <c>Mi</c>, <c>Gi</c>).</summary>
    public static BinaryPrefix Resolve(string symbol) =>
        TryResolve(symbol, out var prefix) ? prefix! : throw new KeyNotFoundException($"Unknown IEC binary prefix: {symbol}");

    /// <summary>Attempts to resolve an IEC binary prefix symbol.</summary>
    public static bool TryResolve(string symbol, out BinaryPrefix? prefix)
    {
        prefix = Items.FirstOrDefault(p => p.Symbol == symbol);
        return prefix is not null;
    }
}
