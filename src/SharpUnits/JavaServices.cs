using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace SharpUnits;

/// <summary>A named entry in the supported pinned-Indriya system catalog.</summary>
/// <param name="Field">Upstream Units constant identifier.</param>
/// <param name="Name">Case-sensitive upstream human-readable name.</param>
/// <param name="Unit">Immutable unit definition, including its display label.</param>
public sealed record JavaUnitEntry(string Field,string Name,UnitDefinition Unit);

/// <summary>Immutable built-in system of units with separate text and quantity-type lookups.</summary>
/// <remarks>Quantity identifiers are Java simple type names, not CLR type identities.
/// The native standard registry is deliberately independent of this catalog.</remarks>
public sealed class JavaUnitSystem
{
    // @relation(JP-020, scope=file)
    internal static JavaUnitSystem Default { get; }=new();
    private readonly ReadOnlyCollection<JavaUnitEntry> entries;
    private readonly IReadOnlyDictionary<string,UnitDefinition> labels;
    private readonly IReadOnlyDictionary<string,UnitDefinition> types;

    private JavaUnitSystem()
    {
        entries=Array.AsReadOnly(JavaUnitCatalogData.Entries());
        labels=entries.ToDictionary(entry=>entry.Unit.Symbol,entry=>entry.Unit,StringComparer.Ordinal);
        types=JavaUnitCatalogData.Types().ToDictionary(pair=>pair.Key,pair=>labels[pair.Value],StringComparer.Ordinal);
    }

    /// <summary>Gets the exact upstream system name, used for case-sensitive discovery.</summary>
    public string Name=>"Units";
    /// <summary>Gets immutable named unit entries; caller mutation cannot affect services.</summary>
    public IReadOnlyList<JavaUnitEntry> Entries=>entries;
    /// <summary>Gets an immutable snapshot of all units in this system.</summary>
    public IReadOnlyList<UnitDefinition> Units=>Array.AsReadOnly(entries.Select(entry=>entry.Unit).ToArray());
    /// <summary>Looks up a unit by exact formatted text, returning null when absent.</summary>
    public UnitDefinition? GetUnit(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return labels.GetValueOrDefault(text);
    }
    /// <summary>Looks up a Java quantity-type identifier; null and unknown types return null.</summary>
    /// <remarks>Matches the pinned system's HashMap lookup, unlike its null-rejecting text lookup.</remarks>
    public UnitDefinition? GetUnitForQuantity(string? quantityType)
    {
        return quantityType is null?null:types.GetValueOrDefault(quantityType);
    }
    /// <summary>Returns a snapshot filtered by dimension, preserving distinct alternate units.</summary>
    public IReadOnlyList<UnitDefinition> GetUnits(RationalDimension dimension)=>
        Array.AsReadOnly(entries.Where(entry=>entry.Unit.RationalDimension==dimension).Select(entry=>entry.Unit).ToArray());
    /// <summary>Builds an isolated parser/factory registry from this system's definitions.</summary>
    public UnitRegistry CreateRegistry()
    {
        var registry=new UnitRegistry();
        foreach(var entry in entries)registry.Register(entry.Unit);
        return registry;
    }
}

/// <summary>Metadata for one exact decimal or binary prefix.</summary>
/// <param name="Symbol">Case-sensitive prefix symbol.</param>
/// <param name="Base">Radix of the prefix factor: 10 for metric, 1024 for pinned binary prefixes.</param>
/// <param name="Exponent">Power of the radix; no early floating-point evaluation is required.</param>
public sealed record JavaPrefix(string Symbol,int Base,int Exponent);

/// <summary>Built-in system discovery and immutable prefix catalogs.</summary>
public sealed class JavaSystemOfUnitsService
{
    private static readonly IReadOnlyList<JavaUnitSystem> systems=Array.AsReadOnly(new[]{JavaUnitSystem.Default});
    private static readonly IReadOnlyList<JavaPrefix> metric=Array.AsReadOnly(SiPrefixes.Definitions
        .Select(pair=>new JavaPrefix(pair.Key,10,pair.Value)).ToArray());
    private static readonly IReadOnlyList<JavaPrefix> binary=Array.AsReadOnly(new[]{"Ki","Mi","Gi","Ti","Pi","Ei","Zi","Yi"}
        .Select((symbol,index)=>new JavaPrefix(symbol,1024,index+1)).ToArray());
    /// <summary>Gets the built-in default system.</summary>
    public JavaUnitSystem GetSystemOfUnits()=>JavaUnitSystem.Default;
    /// <summary>Looks up an exact system name; unknown names return null.</summary>
    public JavaUnitSystem? GetSystemOfUnits(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return name==JavaUnitSystem.Default.Name?JavaUnitSystem.Default:null;
    }
    /// <summary>Gets an immutable snapshot of built-in systems.</summary>
    public IReadOnlyList<JavaUnitSystem> AvailableSystems=>systems;
    /// <summary>Gets the supported SI metric prefixes.</summary>
    public IReadOnlyList<JavaPrefix> MetricPrefixes=>metric;
    /// <summary>Gets the eight binary prefixes in the pinned Java API.</summary>
    public IReadOnlyList<JavaPrefix> BinaryPrefixes=>binary;
}

/// <summary>Bounded invariant SIMPLE unit formatting and parsing.</summary>
/// <remarks>General Java unit normalization, mutable labels, ASCII/EBNF/locale formats and parse cursors
/// are not represented. Registry definitions and the existing bounded parser determine accepted input.</remarks>
public sealed class JavaUnitTextFormat
{
    private readonly QuantityTextFormat quantities;
    internal JavaUnitTextFormat(UnitRegistry registry)=>quantities=new(registry);
    /// <summary>Reports the invariant formatting convention.</summary>
    public bool IsLocaleSensitive=>false;
    /// <summary>Formats supported unit definitions, including canonical dimensionless text.</summary>
    public string Format(UnitDefinition unit)
    {
        ArgumentNullException.ThrowIfNull(unit);
        var symbol=QuantityTextFormat.JavaSymbol(unit);
        return symbol.Length==0?"one":symbol;
    }
    /// <summary>Parses a unit within the bounded Java parser grammar.</summary>
    public UnitDefinition Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return quantities.ParseJavaQuantity("1 "+text).Unit;
    }
}

/// <summary>Discovers the actually supported SIMPLE format profile.</summary>
/// <remarks>Indriya also registers ASCII, EBNF, LOCAL and NUMBERDELIMITER formats.
/// Unsupported names return null and are not advertised as implemented.</remarks>
public sealed class JavaFormatService
{
    private readonly QuantityTextFormat quantities;
    private readonly JavaUnitTextFormat units;
    private static readonly IReadOnlyList<string> quantityNames=Array.AsReadOnly(new[]{"SIMPLE"});
    private static readonly IReadOnlyList<string> unitNames=Array.AsReadOnly(new[]{"SIMPLE_DEFAULT"});
    internal JavaFormatService(UnitRegistry registry){quantities=new(registry);units=new(registry);}
    /// <summary>Gets the supported default quantity formatter.</summary>
    public QuantityTextFormat GetQuantityFormat()=>quantities;
    /// <summary>Looks up a supported quantity format name using Java's current-locale case folding.</summary>
    /// <remarks>As in pinned Indriya, lowercase simple is not recognized under Turkish casing.</remarks>
    public QuantityTextFormat? GetQuantityFormat(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return name.ToUpper(System.Globalization.CultureInfo.CurrentCulture)=="SIMPLE"?quantities:null;
    }
    /// <summary>Gets implemented quantity format names, excluding unsupported Java families.</summary>
    public IReadOnlyList<string> QuantityFormatNames=>quantityNames;
    /// <summary>Gets the supported default unit formatter.</summary>
    public JavaUnitTextFormat GetUnitFormat()=>units;
    /// <summary>Looks up SIMPLE_DEFAULT or its DEFAULT alias.</summary>
    public JavaUnitTextFormat? GetUnitFormat(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        var key=name.ToUpper(System.Globalization.CultureInfo.CurrentCulture);
        return key is "SIMPLE_DEFAULT" or "DEFAULT"?units:null;
    }
    /// <summary>Looks up a name and optional variant using Java's underscore convention.</summary>
    public JavaUnitTextFormat? GetUnitFormat(string name,string? variant)
    {
        ArgumentNullException.ThrowIfNull(name);
        return GetUnitFormat(string.IsNullOrEmpty(variant)?name:name+"_"+variant);
    }
    /// <summary>Gets implemented unit format names.</summary>
    public IReadOnlyList<string> UnitFormatNames=>unitNames;
}

/// <summary>Double-valued quantity factory with separate Java type metadata.</summary>
/// <remarks>Java enforces generic type admission at compile time. This CLR identifier-based
/// adaptation does not add a runtime dimension guard that Java's raw factory does not have.</remarks>
public sealed class JavaTypedQuantityFactory
{
    /// <summary>Gets the case-sensitive Java quantity-type identifier.</summary>
    public string QuantityType { get; }
    /// <summary>Gets the mapped coherent unit, or null for an unmapped/custom type.</summary>
    public UnitDefinition? SystemUnit { get; }
    internal JavaTypedQuantityFactory(string type,UnitDefinition? systemUnit){QuantityType=type;SystemUnit=systemUnit;}
    /// <summary>Creates a retained display quantity without converting it to the system unit.</summary>
    public JavaQuantityAdapter Create(double value,UnitDefinition unit,JavaQuantityScale scale=JavaQuantityScale.Absolute)=>new(value,unit,scale);
}

/// <summary>Discovers the built-in Java-inspired provider and its supported services.</summary>
/// <remarks>This supplies built-in discovery only. Java ServiceLoader, external assemblies,
/// provider replacement and Java Class/generic identity remain outside this profile.</remarks>
public sealed class JavaServiceProvider
{
    // @relation(JP-021, scope=file)
    private static readonly JavaServiceProvider current=new();
    private static readonly IReadOnlyList<JavaServiceProvider> available=Array.AsReadOnly(new[]{current});
    private readonly ConcurrentDictionary<string,JavaTypedQuantityFactory> factories=new(StringComparer.Ordinal);
    private JavaServiceProvider(){SystemOfUnitsService=new();FormatService=new(SystemOfUnitsService.GetSystemOfUnits().CreateRegistry());}
    /// <summary>Gets the immutable built-in provider list.</summary>
    public static IReadOnlyList<JavaServiceProvider> Available=>available;
    /// <summary>Gets the built-in current provider.</summary>
    public static JavaServiceProvider Current=>current;
    /// <summary>Gets the priority of the pinned default provider.</summary>
    public int Priority=>10;
    /// <summary>Gets the supported format service.</summary>
    public JavaFormatService FormatService { get; }
    /// <summary>Gets the supported system-of-units service.</summary>
    public JavaSystemOfUnitsService SystemOfUnitsService { get; }
    /// <summary>Gets a cached factory for a Java simple quantity-type identifier.</summary>
    /// <remarks>Unknown nonempty identifiers represent custom/unmapped types and have no system unit.</remarks>
    public JavaTypedQuantityFactory GetQuantityFactory(string quantityType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(quantityType);
        return factories.GetOrAdd(quantityType,type=>new(type,SystemOfUnitsService.GetSystemOfUnits().GetUnitForQuantity(type)));
    }
}
