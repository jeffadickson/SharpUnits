using System.Text.Json;
using System.Text.Json.Serialization;

namespace SharpUnits.SysMl;

/// <summary>Imports and exports Cameo / SysML QUDV catalogs to and from <see cref="UnitEnvironment"/>.</summary>
public static class QudvBridge
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>Serializes a <see cref="QudvCatalog"/> to JSON.</summary>
    public static string ToJson(QudvCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        return JsonSerializer.Serialize(catalog, JsonOptions);
    }

    /// <summary>Deserializes a <see cref="QudvCatalog"/> from JSON.</summary>
    public static QudvCatalog FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        var catalog = JsonSerializer.Deserialize<QudvCatalog>(json, JsonOptions)
                      ?? throw new JsonException("Empty QUDV catalog.");
        if (catalog.SchemaVersion != 1)
            throw new NotSupportedException($"Unsupported QUDV schema version: {catalog.SchemaVersion}.");
        return catalog;
    }

    /// <summary>
    /// Loads a <see cref="QudvCatalog"/> into a <see cref="UnitEnvironment"/> (optionally starting from the standard SI environment)
    /// and returns the resolved custom <see cref="QuantitySpec"/> lookup.
    /// </summary>
    public static (UnitEnvironment Environment, IReadOnlyDictionary<string, QuantitySpec> Specs) Import(
        QudvCatalog catalog,
        bool includeStandardUnits = true)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        var env = includeStandardUnits
            ? UnitEnvironment.CreateStandard()
            : new UnitEnvironment(new UnitRegistry(), new QuantityKindRegistry());

        var specs = new Dictionary<string, QuantitySpec>(StringComparer.Ordinal);

        // Register quantity kinds (supporting parent references in declaration order or standard ISQ).
        foreach (var qk in catalog.QuantityKinds)
        {
            if (string.IsNullOrWhiteSpace(qk.Id))
                throw new InvalidOperationException("QUDV quantity kind ID is required.");
            var dim = qk.Dimensions.ToRationalDimension();
            QuantitySpec? parent = null;
            if (!string.IsNullOrWhiteSpace(qk.ParentId))
            {
                if (!specs.TryGetValue(qk.ParentId, out parent) && !Isq.TryGetById(qk.ParentId, out parent))
                    throw new KeyNotFoundException($"Unknown parent QUDV quantity kind '{qk.ParentId}'.");
            }
            var spec = new QuantitySpec(qk.Id, string.IsNullOrWhiteSpace(qk.Name) ? qk.Id : qk.Name, dim, parent, qk.IsDistinctKind);
            specs[qk.Id] = spec;
            env.Kinds.Register(spec.ToKind(), dim);
        }

        // Register units (simple/linear/affine first, then prefixed/derived).
        foreach (var u in catalog.Units.OrderBy(u => u.Kind is QudvUnitKind.Prefixed or QudvUnitKind.Derived ? 1 : 0))
        {
            var unitDef = BuildUnitDefinition(u, env, specs);
            env.Registry.Register(unitDef);
            if (u.Kind == QudvUnitKind.AffineConversion && unitDef.IsAffine)
            {
                // Register a matching PointOrigin for affine QUDV units (e.g. custom temperature scales).
                var origin = new PointOrigin(
                    $"qudv.origin.{u.Symbol}",
                    $"{unitDef.Name} zero",
                    unitDef.RationalDimension,
                    unitDef.Converter.Offset,
                    unitDef.DefaultSpec);
                env.RegisterOrigin(u.Symbol, origin);
            }
        }

        return (env, specs);
    }

    /// <summary>Exports the standard SharpUnits catalog into a SysML QUDV interchange catalog.</summary>
    public static QudvCatalog ExportStandardCatalog(string name = "SharpUnits.Standard.QUDV")
    {
        var units = new List<QudvUnit>();
        var kindIds = new Dictionary<string, QudvQuantityKind>(StringComparer.Ordinal);

        foreach (var u in Units.AllStandard)
        {
            if (u.DefaultSpec is not null && !kindIds.ContainsKey(u.DefaultSpec.Id))
            {
                kindIds[u.DefaultSpec.Id] = new QudvQuantityKind
                {
                    Id = u.DefaultSpec.Id,
                    Name = u.DefaultSpec.Name,
                    ParentId = u.DefaultSpec.Parent?.Id,
                    IsDistinctKind = u.DefaultSpec.IsKind,
                    Dimensions = QudvDimensionExponents.From(u.DefaultSpec.Dimension)
                };
            }

            QudvUnitKind kind = u.IsAffine
                ? QudvUnitKind.AffineConversion
                : u.Converter.IsIdentity ? QudvUnitKind.Simple : QudvUnitKind.LinearConversion;

            units.Add(new QudvUnit
            {
                Symbol = u.Symbol,
                Name = u.Name,
                Kind = kind,
                QuantityKindId = u.DefaultSpec?.Id,
                Dimensions = QudvDimensionExponents.From(u.RationalDimension),
                Factor = u.Converter.Scale,
                Offset = u.Converter.Offset,
                OffsetConvention = QudvOffsetConvention.TargetSiOffset,
                AllowPrefixes = u.AllowPrefixes
            });
        }

        return new QudvCatalog
        {
            SchemaVersion = 1,
            Name = name,
            QuantityKinds = kindIds.Values.ToArray(),
            Units = units
        };
    }

    private static UnitDefinition BuildUnitDefinition(
        QudvUnit u,
        UnitEnvironment env,
        Dictionary<string, QuantitySpec> customSpecs)
    {
        if (string.IsNullOrWhiteSpace(u.Symbol))
            throw new InvalidOperationException("QUDV unit symbol is required.");

        QuantitySpec? spec = null;
        if (!string.IsNullOrWhiteSpace(u.QuantityKindId))
        {
            if (!customSpecs.TryGetValue(u.QuantityKindId, out spec) &&
                !Isq.TryGetById(u.QuantityKindId, out spec) &&
                !Isq.TryFromKind(new QuantityKind(u.QuantityKindId), out spec))
            {
                spec = null;
            }
        }

        string name = string.IsNullOrWhiteSpace(u.Name) ? u.Symbol : u.Name;
        QuantityKind? qk = !string.IsNullOrWhiteSpace(u.QuantityKindId) ? new QuantityKind(u.QuantityKindId) : spec?.ToKind();

        switch (u.Kind)
        {
            case QudvUnitKind.Prefixed:
            {
                if (string.IsNullOrWhiteSpace(u.PrefixSymbol) || string.IsNullOrWhiteSpace(u.BaseUnitSymbol))
                    throw new InvalidOperationException($"Prefixed QUDV unit '{u.Symbol}' requires PrefixSymbol and BaseUnitSymbol.");
                var baseUnit = env.Registry.Resolve(u.BaseUnitSymbol);
                var prefixed = BinaryPrefixCatalog.TryResolve(u.PrefixSymbol, out _)
                    ? BinaryPrefixes.Apply(u.PrefixSymbol, baseUnit)
                    : SiPrefixes.Apply(u.PrefixSymbol, baseUnit);
                return new UnitDefinition(u.Symbol, prefixed.RationalDimension, prefixed.Converter, name, allowPrefixes: false, qk, spec ?? prefixed.DefaultSpec);
            }
            case QudvUnitKind.Derived:
            {
                if (string.IsNullOrWhiteSpace(u.Expression))
                    throw new InvalidOperationException($"Derived QUDV unit '{u.Symbol}' requires Expression.");
                var sym = env.ResolveUnit(u.Expression);
                return new UnitDefinition(u.Symbol, sym.Dimension, sym.Converter, name, u.AllowPrefixes, qk, spec);
            }
            case QudvUnitKind.AffineConversion:
            {
                var dim = ResolveDimension(u, spec);
                double siOffset = u.OffsetConvention == QudvOffsetConvention.SourceUnitOffset
                    ? u.Offset * u.Factor
                    : u.Offset;
                return new UnitDefinition(u.Symbol, dim, new UnitConversion(u.Factor, siOffset), name, allowPrefixes: false, qk, spec);
            }
            case QudvUnitKind.LinearConversion:
            {
                var dim = ResolveDimension(u, spec);
                return new UnitDefinition(u.Symbol, dim, UnitConversion.Linear(u.Factor), name, u.AllowPrefixes, qk, spec);
            }
            default:
            {
                var dim = ResolveDimension(u, spec);
                return new UnitDefinition(u.Symbol, dim, UnitConversion.Identity, name, u.AllowPrefixes, qk, spec);
            }
        }
    }

    private static RationalDimension ResolveDimension(QudvUnit u, QuantitySpec? spec)
    {
        if (u.Dimensions is not null) return u.Dimensions.ToRationalDimension();
        if (spec is not null) return spec.Dimension;
        throw new InvalidOperationException($"QUDV unit '{u.Symbol}' must specify Dimensions or a known QuantityKindId.");
    }
}
