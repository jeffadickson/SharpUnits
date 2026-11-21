using System.Globalization;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SharpUnits;

/// <summary>Wire format used when serializing SharpUnits types to JSON.</summary>
public enum SharpUnitsJsonFormat
{
    /// <summary>Structured JSON object (for example <c>{"value":250,"unit":"mV","spec":"isq.voltage"}</c>).</summary>
    Structured,

    /// <summary>Compact human-readable string literal (for example <c>"250 mV"</c>). Deserializers always accept both formats.</summary>
    CompactString
}

/// <summary>Configuration for <see cref="SharpUnitsJsonExtensions.AddSharpUnits"/>.</summary>
public sealed class SharpUnitsJsonOptions
{
    /// <summary>Output serialization format (default: <see cref="SharpUnitsJsonFormat.Structured"/>).</summary>
    public SharpUnitsJsonFormat Format { get; init; } = SharpUnitsJsonFormat.Structured;

    /// <summary>Unit/origin resolution environment (default: <see cref="UnitEnvironment.Standard"/>).</summary>
    public UnitEnvironment Environment { get; init; } = UnitEnvironment.Standard;

    /// <summary>When true, writes ASCII-safe unit symbols (for example <c>uF</c>, <c>kOhm</c>, <c>degC</c>).</summary>
    public bool AsciiSymbols { get; init; }

    /// <summary>When true, rejects unexpected JSON properties inside structured quantity objects.</summary>
    public bool StrictUnmappedMembers { get; init; } = true;
}

/// <summary>Extension methods registering NativeAOT-safe <see cref="System.Text.Json"/> converters for all core SharpUnits types.</summary>
public static class SharpUnitsJsonExtensions
{
    /// <summary>
    /// Registers NativeAOT-safe <see cref="JsonConverter"/> instances for <see cref="SymbolicUnit"/>, <see cref="Quantity"/>,
    /// <see cref="Quantity{T}"/>, <see cref="QuantityPoint{T}"/>, <see cref="QuantitySeries{T}"/>, and all strongly-typed core structs.
    /// </summary>
    public static JsonSerializerOptions AddSharpUnits(this JsonSerializerOptions options, SharpUnitsJsonOptions? config = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        var cfg = config ?? new SharpUnitsJsonOptions();

        options.Converters.Add(new SymbolicUnitJsonConverter(cfg));
        options.Converters.Add(new QuantityJsonConverter(cfg));

        options.Converters.Add(new GenericQuantityJsonConverter<double>(cfg));
        options.Converters.Add(new GenericQuantityJsonConverter<float>(cfg));
        options.Converters.Add(new GenericQuantityJsonConverter<decimal>(cfg));
        options.Converters.Add(new GenericQuantityJsonConverter<int>(cfg));
        options.Converters.Add(new GenericQuantityJsonConverter<long>(cfg));

        options.Converters.Add(new QuantityPointJsonConverter<double>(cfg));
        options.Converters.Add(new QuantityPointJsonConverter<float>(cfg));
        options.Converters.Add(new QuantityPointJsonConverter<decimal>(cfg));

        options.Converters.Add(new QuantitySeriesJsonConverter<double>(cfg));
        options.Converters.Add(new QuantitySeriesJsonConverter<float>(cfg));
        options.Converters.Add(new QuantitySeriesJsonConverter<decimal>(cfg));
        options.Converters.Add(new QuantitySeriesJsonConverter<int>(cfg));

        // Strongly-typed core structs
        options.Converters.Add(new TypedQuantityJsonConverter<Length>(cfg, Units.Meter, v => new(v), q => q.Meters));
        options.Converters.Add(new TypedQuantityJsonConverter<Mass>(cfg, Units.Kilogram, v => new(v), q => q.Kilograms));
        options.Converters.Add(new TypedQuantityJsonConverter<Duration>(cfg, Units.Second, v => new(v), q => q.Seconds));
        options.Converters.Add(new TypedQuantityJsonConverter<Frequency>(cfg, Units.Hertz, v => new(v), q => q.Hertz));
        options.Converters.Add(new TypedQuantityJsonConverter<Force>(cfg, Units.Newton, v => new(v), q => q.Newtons));
        options.Converters.Add(new TypedQuantityJsonConverter<Pressure>(cfg, Units.Pascal, v => new(v), q => q.Pascals));
        options.Converters.Add(new TypedQuantityJsonConverter<Energy>(cfg, Units.Joule, v => new(v), q => q.Joules));
        options.Converters.Add(new TypedQuantityJsonConverter<Power>(cfg, Units.Watt, v => new(v), q => q.Watts));
        options.Converters.Add(new TypedQuantityJsonConverter<Area>(cfg, Units.Meter.Pow(2).ToUnitDefinition(), v => new(v), q => q.SquareMeters));
        options.Converters.Add(new TypedQuantityJsonConverter<Volume>(cfg, Units.Meter.Pow(3).ToUnitDefinition(), v => new(v), q => q.CubicMeters));
        options.Converters.Add(new TypedQuantityJsonConverter<Velocity>(cfg, (Units.Meter / Units.Second).ToUnitDefinition(), v => new(v), q => q.MetersPerSecond));
        options.Converters.Add(new TypedQuantityJsonConverter<Acceleration>(cfg, (Units.Meter / Units.Second.Pow(2)).ToUnitDefinition(), v => new(v), q => q.MetersPerSecondSquared));
        options.Converters.Add(new TypedQuantityJsonConverter<Angle>(cfg, Units.Radian, v => new(v), q => q.Radians));
        options.Converters.Add(new TypedQuantityJsonConverter<Torque>(cfg, Units.NewtonMeter, v => new(v), q => q.NewtonMeters));
        options.Converters.Add(new TypedQuantityJsonConverter<MagneticFluxDensity>(cfg, Units.Tesla, v => new(v), q => q.Tesla));
        options.Converters.Add(new AbsoluteTemperatureJsonConverter(cfg));
        options.Converters.Add(new TemperatureDifferenceJsonConverter(cfg));

        return options;
    }
}

/// <summary>NativeAOT-safe JSON converter for <see cref="SymbolicUnit"/>.</summary>
public sealed class SymbolicUnitJsonConverter : JsonConverter<SymbolicUnit>
{
    private readonly SharpUnitsJsonOptions cfg;

    /// <summary>Creates a converter with explicit options.</summary>
    public SymbolicUnitJsonConverter(SharpUnitsJsonOptions? config = null) => cfg = config ?? new();

    /// <inheritdoc/>
    public override SymbolicUnit Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected a string unit expression.");
        string text = reader.GetString() ?? throw new JsonException("Unit string cannot be null.");
        try { return cfg.Environment.ResolveUnit(text); }
        catch (Exception ex) { throw new JsonException($"Invalid unit '{text}': {ex.Message}", ex); }
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, SymbolicUnit value, JsonSerializerOptions options) =>
        writer.WriteStringValue(cfg.AsciiSymbols ? value.AsciiSymbol : value.Symbol);
}

/// <summary>NativeAOT-safe JSON converter for unified <see cref="Quantity"/> (reads both structured objects and compact strings).</summary>
public sealed class QuantityJsonConverter : JsonConverter<Quantity>
{
    private readonly SharpUnitsJsonOptions cfg;

    /// <summary>Creates a converter with explicit options.</summary>
    public QuantityJsonConverter(SharpUnitsJsonOptions? config = null) => cfg = config ?? new();

    /// <inheritdoc/>
    public override Quantity Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string lit = reader.GetString() ?? throw new JsonException("Quantity literal cannot be null.");
            try { return cfg.Environment.ParseQuantity(lit); }
            catch (Exception ex) { throw new JsonException($"Invalid quantity literal '{lit}': {ex.Message}", ex); }
        }
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected a JSON object or string for Quantity.");

        double? value = null;
        string? unitText = null;
        string? specId = null;
        string? scaleText = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException("Expected property name.");
            string prop = reader.GetString()!;
            reader.Read();
            if (prop.Equals("value", StringComparison.OrdinalIgnoreCase)) value = JsonNumberHelper.ReadFiniteDouble(ref reader);
            else if (prop.Equals("unit", StringComparison.OrdinalIgnoreCase)) unitText = reader.GetString();
            else if (prop.Equals("spec", StringComparison.OrdinalIgnoreCase)) specId = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
            else if (prop.Equals("scale", StringComparison.OrdinalIgnoreCase)) scaleText = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
            else if (cfg.StrictUnmappedMembers) throw new JsonException($"Unexpected property '{prop}' on Quantity.");
            else reader.Skip();
        }

        if (value is null || string.IsNullOrWhiteSpace(unitText))
            throw new JsonException("Quantity object requires finite 'value' and 'unit' properties.");

        var scale = QuantityScale.Absolute;
        if (!string.IsNullOrWhiteSpace(scaleText))
        {
            if (scaleText.Equals("relative", StringComparison.OrdinalIgnoreCase)) scale = QuantityScale.Relative;
            else if (!scaleText.Equals("absolute", StringComparison.OrdinalIgnoreCase))
                throw new JsonException($"Unknown QuantityScale '{scaleText}'.");
        }

        try
        {
            var unit = cfg.Environment.ResolveUnit(unitText);
            QuantitySpec? spec = null;
            if (!string.IsNullOrWhiteSpace(specId))
            {
                if (!Isq.TryGetById(specId, out spec))
                    spec = new QuantitySpec(specId, specId, unit.Dimension);
            }
            return Quantity.Of(value.Value, unit, scale, spec);
        }
        catch (Exception ex) { throw new JsonException($"Invalid Quantity JSON: {ex.Message}", ex); }
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, Quantity value, JsonSerializerOptions options)
    {
        if (!double.IsFinite(value.Value))
            throw new JsonException("Cannot serialize non-finite Quantity magnitude.");

        if (cfg.Format == SharpUnitsJsonFormat.CompactString)
        {
            writer.WriteStringValue(value.Format("G", CultureInfo.InvariantCulture, cfg.AsciiSymbols));
            return;
        }

        string sym = value.Unit is not null
            ? (cfg.AsciiSymbols ? SymbolicUnit.From(value.Unit).AsciiSymbol : value.Unit.Symbol)
            : value.UnitSymbol;

        writer.WriteStartObject();
        writer.WriteNumber("value", value.Value);
        writer.WriteString("unit", sym);
        if (value.Spec is not null) writer.WriteString("spec", value.Spec.Id);
        if (value.Scale == QuantityScale.Relative) writer.WriteString("scale", "relative");
        writer.WriteEndObject();
    }
}

/// <summary>NativeAOT-safe JSON converter for generic <see cref="Quantity{T}"/>.</summary>
public sealed class GenericQuantityJsonConverter<T> : JsonConverter<Quantity<T>> where T : INumber<T>
{
    private readonly SharpUnitsJsonOptions cfg;

    /// <summary>Creates a converter with explicit options.</summary>
    public GenericQuantityJsonConverter(SharpUnitsJsonOptions? config = null) => cfg = config ?? new();

    /// <inheritdoc/>
    public override Quantity<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string lit = reader.GetString() ?? throw new JsonException("Quantity literal cannot be null.");
            try { return cfg.Environment.ParseQuantity(lit).AsGeneric<T>(); }
            catch (Exception ex) { throw new JsonException($"Invalid quantity literal '{lit}': {ex.Message}", ex); }
        }
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"Expected JSON object or string for Quantity<{typeof(T).Name}>.");

        bool hasValue = false;
        T value = T.Zero;
        string? unitText = null;
        string? specId = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException("Expected property name.");
            string prop = reader.GetString()!;
            reader.Read();
            if (prop.Equals("value", StringComparison.OrdinalIgnoreCase)) { value = JsonNumberHelper.ReadNumber<T>(ref reader); hasValue = true; }
            else if (prop.Equals("unit", StringComparison.OrdinalIgnoreCase)) unitText = reader.GetString();
            else if (prop.Equals("spec", StringComparison.OrdinalIgnoreCase)) specId = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
            else if (cfg.StrictUnmappedMembers) throw new JsonException($"Unexpected property '{prop}' on Quantity<{typeof(T).Name}>.");
            else reader.Skip();
        }

        if (!hasValue || string.IsNullOrWhiteSpace(unitText))
            throw new JsonException("Quantity object requires 'value' and 'unit' properties.");

        try
        {
            var unit = cfg.Environment.ResolveUnit(unitText);
            QuantitySpec? spec = null;
            if (!string.IsNullOrWhiteSpace(specId))
            {
                if (!Isq.TryGetById(specId, out spec))
                    spec = new QuantitySpec(specId, specId, unit.Dimension);
            }
            return Quantity<T>.Of(value, unit, spec);
        }
        catch (Exception ex) { throw new JsonException($"Invalid Quantity<{typeof(T).Name}> JSON: {ex.Message}", ex); }
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, Quantity<T> value, JsonSerializerOptions options)
    {
        if (cfg.Format == SharpUnitsJsonFormat.CompactString)
        {
            writer.WriteStringValue(value.Format("G", CultureInfo.InvariantCulture, cfg.AsciiSymbols));
            return;
        }

        writer.WriteStartObject();
        writer.WritePropertyName("value");
        JsonNumberHelper.WriteNumber(writer, value.Value);
        writer.WriteString("unit", cfg.AsciiSymbols ? value.Unit.AsciiSymbol : value.Unit.Symbol);
        if (value.Spec is not null) writer.WriteString("spec", value.Spec.Id);
        writer.WriteEndObject();
    }
}

/// <summary>NativeAOT-safe JSON converter for affine <see cref="QuantityPoint{T}"/>.</summary>
public sealed class QuantityPointJsonConverter<T> : JsonConverter<QuantityPoint<T>> where T : INumber<T>
{
    private readonly SharpUnitsJsonOptions cfg;

    /// <summary>Creates a converter with explicit options.</summary>
    public QuantityPointJsonConverter(SharpUnitsJsonOptions? config = null) => cfg = config ?? new();

    /// <inheritdoc/>
    public override QuantityPoint<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string lit = reader.GetString() ?? throw new JsonException("QuantityPoint literal cannot be null.");
            try
            {
                var q = cfg.Environment.ParseQuantity(lit);
                if (q.Unit == Units.Celsius) return QuantityPoint<T>.FromCelsius(T.CreateChecked(q.Value));
                if (q.Unit == Units.Fahrenheit) return QuantityPoint<T>.FromFahrenheit(T.CreateChecked(q.Value));
                if (q.Unit == Units.Kelvin) return QuantityPoint<T>.FromKelvin(T.CreateChecked(q.Value));
                var linear = q.AsGeneric<T>();
                return QuantityPoint<T>.At(linear.Value, linear.Unit, PointOrigins.NaturalZero(linear.Dimension, linear.Spec));
            }
            catch (Exception ex) { throw new JsonException($"Invalid QuantityPoint literal '{lit}': {ex.Message}", ex); }
        }
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"Expected JSON object or string for QuantityPoint<{typeof(T).Name}>.");

        bool hasValue = false;
        T value = T.Zero;
        string? unitText = null;
        string? originText = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException("Expected property name.");
            string prop = reader.GetString()!;
            reader.Read();
            if (prop.Equals("value", StringComparison.OrdinalIgnoreCase)) { value = JsonNumberHelper.ReadNumber<T>(ref reader); hasValue = true; }
            else if (prop.Equals("unit", StringComparison.OrdinalIgnoreCase)) unitText = reader.GetString();
            else if (prop.Equals("origin", StringComparison.OrdinalIgnoreCase)) originText = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
            else if (cfg.StrictUnmappedMembers) throw new JsonException($"Unexpected property '{prop}' on QuantityPoint.");
            else reader.Skip();
        }

        if (!hasValue || string.IsNullOrWhiteSpace(unitText))
            throw new JsonException("QuantityPoint object requires 'value' and 'unit' properties.");

        try
        {
            var unit = cfg.Environment.ResolveUnit(unitText);
            var origin = ResolveOrigin(originText, unit);
            return QuantityPoint<T>.At(value, unit, origin);
        }
        catch (Exception ex) { throw new JsonException($"Invalid QuantityPoint JSON: {ex.Message}", ex); }
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, QuantityPoint<T> value, JsonSerializerOptions options)
    {
        if (cfg.Format == SharpUnitsJsonFormat.CompactString)
        {
            writer.WriteStringValue(value.ToString());
            return;
        }
        writer.WriteStartObject();
        writer.WritePropertyName("value");
        JsonNumberHelper.WriteNumber(writer, value.Value);
        writer.WriteString("unit", cfg.AsciiSymbols ? value.Unit.AsciiSymbol : value.Unit.Symbol);
        writer.WriteString("origin", value.Origin.Id);
        writer.WriteEndObject();
    }

    private PointOrigin ResolveOrigin(string? originText, SymbolicUnit unit)
    {
        if (string.IsNullOrWhiteSpace(originText))
            return PointOrigins.NaturalZero(unit.Dimension, unit.DefaultSpec);
        return originText switch
        {
            "origin.absolute_zero" => PointOrigins.AbsoluteZero,
            "origin.ice_point" => PointOrigins.IcePoint,
            "origin.fahrenheit_zero" => PointOrigins.FahrenheitZero,
            "origin.vacuum" => PointOrigins.Vacuum,
            "origin.standard_atmosphere" => PointOrigins.StandardAtmosphere,
            "origin.sea_level" => PointOrigins.SeaLevel,
            _ => cfg.Environment.ResolveOrigin(originText)
        };
    }
}

/// <summary>NativeAOT-safe SoA JSON converter for <see cref="QuantitySeries{T}"/> writing one shared unit header plus a compact numeric array.</summary>
public sealed class QuantitySeriesJsonConverter<T> : JsonConverter<QuantitySeries<T>> where T : INumber<T>
{
    private readonly SharpUnitsJsonOptions cfg;

    /// <summary>Creates a converter with explicit options.</summary>
    public QuantitySeriesJsonConverter(SharpUnitsJsonOptions? config = null) => cfg = config ?? new();

    /// <inheritdoc/>
    public override QuantitySeries<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"Expected JSON object for QuantitySeries<{typeof(T).Name}>.");

        string? unitText = null;
        string? specId = null;
        string? originText = null;
        List<T>? values = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException("Expected property name.");
            string prop = reader.GetString()!;
            reader.Read();
            if (prop.Equals("unit", StringComparison.OrdinalIgnoreCase)) unitText = reader.GetString();
            else if (prop.Equals("spec", StringComparison.OrdinalIgnoreCase)) specId = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
            else if (prop.Equals("origin", StringComparison.OrdinalIgnoreCase)) originText = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
            else if (prop.Equals("values", StringComparison.OrdinalIgnoreCase))
            {
                if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException("'values' must be a JSON array.");
                values = new List<T>();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                    values.Add(JsonNumberHelper.ReadNumber<T>(ref reader));
            }
            else if (cfg.StrictUnmappedMembers) throw new JsonException($"Unexpected property '{prop}' on QuantitySeries.");
            else reader.Skip();
        }

        if (string.IsNullOrWhiteSpace(unitText) || values is null)
            throw new JsonException("QuantitySeries object requires 'unit' and 'values' properties.");

        try
        {
            var unit = cfg.Environment.ResolveUnit(unitText);
            QuantitySpec? spec = null;
            if (!string.IsNullOrWhiteSpace(specId) && !Isq.TryGetById(specId, out spec))
                spec = new QuantitySpec(specId, specId, unit.Dimension);

            PointOrigin? origin = originText switch
            {
                null or "" => null,
                "origin.absolute_zero" => PointOrigins.AbsoluteZero,
                "origin.ice_point" => PointOrigins.IcePoint,
                "origin.fahrenheit_zero" => PointOrigins.FahrenheitZero,
                "origin.vacuum" => PointOrigins.Vacuum,
                "origin.standard_atmosphere" => PointOrigins.StandardAtmosphere,
                "origin.sea_level" => PointOrigins.SeaLevel,
                _ => cfg.Environment.ResolveOrigin(originText)
            };
            return QuantitySeries<T>.FromValues(values.ToArray(), unit, spec, origin);
        }
        catch (Exception ex) { throw new JsonException($"Invalid QuantitySeries JSON: {ex.Message}", ex); }
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, QuantitySeries<T> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("unit", cfg.AsciiSymbols ? value.Unit.AsciiSymbol : value.Unit.Symbol);
        if (value.Spec is not null) writer.WriteString("spec", value.Spec.Id);
        if (value.Origin is not null) writer.WriteString("origin", value.Origin.Id);
        writer.WriteStartArray("values");
        var span = value.Values;
        for (int i = 0; i < span.Length; i++)
            JsonNumberHelper.WriteNumber(writer, span[i]);
        writer.WriteEndArray();
        writer.WriteEndObject();
    }
}

/// <summary>NativeAOT-safe JSON converter for any coherent-SI strongly-typed struct (reads both objects and unit strings like <c>"500 mV"</c>).</summary>
public sealed class TypedQuantityJsonConverter<TQuantity> : JsonConverter<TQuantity> where TQuantity : struct
{
    private readonly SharpUnitsJsonOptions cfg;
    private readonly UnitDefinition siUnit;
    private readonly Func<double, TQuantity> fromSi;
    private readonly Func<TQuantity, double> toSi;

    /// <summary>Creates a typed quantity converter.</summary>
    public TypedQuantityJsonConverter(
        SharpUnitsJsonOptions config,
        UnitDefinition siUnit,
        Func<double, TQuantity> fromSi,
        Func<TQuantity, double> toSi)
    {
        cfg = config ?? new();
        this.siUnit = siUnit;
        this.fromSi = fromSi;
        this.toSi = toSi;
    }

    /// <inheritdoc/>
    public override TQuantity Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string lit = reader.GetString() ?? throw new JsonException("Quantity string cannot be null.");
            try
            {
                var q = cfg.Environment.ParseQuantity(lit);
                return fromSi(siUnit.FromQuantity(q));
            }
            catch (Exception ex) { throw new JsonException($"Invalid {typeof(TQuantity).Name} literal '{lit}': {ex.Message}", ex); }
        }
        if (reader.TokenType == JsonTokenType.Number)
            return fromSi(JsonNumberHelper.ReadFiniteDouble(ref reader));

        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"Expected JSON object, string, or number for {typeof(TQuantity).Name}.");

        double? val = null;
        string? unitText = null;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException("Expected property name.");
            string prop = reader.GetString()!;
            reader.Read();
            if (prop.Equals("value", StringComparison.OrdinalIgnoreCase)) val = JsonNumberHelper.ReadFiniteDouble(ref reader);
            else if (prop.Equals("unit", StringComparison.OrdinalIgnoreCase)) unitText = reader.GetString();
            else if (cfg.StrictUnmappedMembers) throw new JsonException($"Unexpected property '{prop}' on {typeof(TQuantity).Name}.");
            else reader.Skip();
        }
        if (val is null) throw new JsonException($"{typeof(TQuantity).Name} object requires 'value'.");
        if (string.IsNullOrWhiteSpace(unitText)) return fromSi(val.Value);
        try
        {
            var q = cfg.Environment.CreateUnifiedQuantity(val.Value, unitText);
            return fromSi(siUnit.FromQuantity(q));
        }
        catch (Exception ex) { throw new JsonException($"Invalid {typeof(TQuantity).Name} unit '{unitText}': {ex.Message}", ex); }
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, TQuantity value, JsonSerializerOptions options)
    {
        double si = toSi(value);
        if (!double.IsFinite(si)) throw new JsonException($"Cannot serialize non-finite {typeof(TQuantity).Name}.");
        string sym = cfg.AsciiSymbols ? SymbolicUnit.From(siUnit).AsciiSymbol : siUnit.Symbol;
        if (cfg.Format == SharpUnitsJsonFormat.CompactString)
        {
            writer.WriteStringValue($"{si.ToString("G", CultureInfo.InvariantCulture)} {sym}");
            return;
        }
        writer.WriteStartObject();
        writer.WriteNumber("value", si);
        writer.WriteString("unit", sym);
        writer.WriteEndObject();
    }
}

internal sealed class AbsoluteTemperatureJsonConverter : JsonConverter<AbsoluteTemperature>
{
    private readonly SharpUnitsJsonOptions cfg;
    public AbsoluteTemperatureJsonConverter(SharpUnitsJsonOptions cfg) => this.cfg = cfg;

    public override AbsoluteTemperature Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var q = cfg.Environment.ParseQuantity(reader.GetString()!);
            return q.AsAbsoluteTemperature();
        }
        if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException("Expected object or string for AbsoluteTemperature.");
        double? val = null;
        string? unit = null;
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            string p = reader.GetString()!;
            reader.Read();
            if (p.Equals("value", StringComparison.OrdinalIgnoreCase)) val = JsonNumberHelper.ReadFiniteDouble(ref reader);
            else if (p.Equals("unit", StringComparison.OrdinalIgnoreCase)) unit = reader.GetString();
            else if (cfg.StrictUnmappedMembers) throw new JsonException($"Unexpected property '{p}'.");
            else reader.Skip();
        }
        if (val is null) throw new JsonException("AbsoluteTemperature requires 'value'.");
        return string.IsNullOrWhiteSpace(unit) ? new(val.Value) : cfg.Environment.CreateUnifiedQuantity(val.Value, unit).AsAbsoluteTemperature();
    }

    public override void Write(Utf8JsonWriter writer, AbsoluteTemperature value, JsonSerializerOptions options)
    {
        if (cfg.Format == SharpUnitsJsonFormat.CompactString)
        {
            writer.WriteStringValue($"{value.Kelvin.ToString("G", CultureInfo.InvariantCulture)} K");
            return;
        }
        writer.WriteStartObject();
        writer.WriteNumber("value", value.Kelvin);
        writer.WriteString("unit", "K");
        writer.WriteEndObject();
    }
}

internal sealed class TemperatureDifferenceJsonConverter : JsonConverter<TemperatureDifference>
{
    private readonly SharpUnitsJsonOptions cfg;
    public TemperatureDifferenceJsonConverter(SharpUnitsJsonOptions cfg) => this.cfg = cfg;

    public override TemperatureDifference Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var q = cfg.Environment.ParseQuantity(reader.GetString()!);
            return new TemperatureDifference(q.SiValue);
        }
        if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException("Expected object or string for TemperatureDifference.");
        double? val = null;
        string? unit = null;
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            string p = reader.GetString()!;
            reader.Read();
            if (p.Equals("value", StringComparison.OrdinalIgnoreCase)) val = JsonNumberHelper.ReadFiniteDouble(ref reader);
            else if (p.Equals("unit", StringComparison.OrdinalIgnoreCase)) unit = reader.GetString();
            else if (cfg.StrictUnmappedMembers) throw new JsonException($"Unexpected property '{p}'.");
            else reader.Skip();
        }
        if (val is null) throw new JsonException("TemperatureDifference requires 'value'.");
        if (string.IsNullOrWhiteSpace(unit) || unit is "K" or "°C" or "degC" or "Δ°C") return new(val.Value);
        if (unit is "°F" or "degF" or "Δ°F" or "°R") return TemperatureDifference.FromFahrenheitDelta(val.Value);
        throw new JsonException($"Unsupported temperature difference unit '{unit}'.");
    }

    public override void Write(Utf8JsonWriter writer, TemperatureDifference value, JsonSerializerOptions options)
    {
        if (cfg.Format == SharpUnitsJsonFormat.CompactString)
        {
            writer.WriteStringValue($"{value.Kelvin.ToString("G", CultureInfo.InvariantCulture)} ΔK");
            return;
        }
        writer.WriteStartObject();
        writer.WriteNumber("value", value.Kelvin);
        writer.WriteString("unit", "K");
        writer.WriteString("scale", "relative");
        writer.WriteEndObject();
    }
}

internal static class JsonNumberHelper
{
    public static double ReadFiniteDouble(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.Number) throw new JsonException("Expected JSON number.");
        double v = reader.GetDouble();
        if (!double.IsFinite(v)) throw new JsonException("Non-finite numbers are not allowed.");
        return v;
    }

    public static T ReadNumber<T>(ref Utf8JsonReader reader) where T : INumber<T>
    {
        if (reader.TokenType != JsonTokenType.Number) throw new JsonException("Expected JSON number.");
        if (typeof(T) == typeof(double)) return (T)(object)ReadFiniteDouble(ref reader);
        if (typeof(T) == typeof(float))
        {
            float f = reader.GetSingle();
            if (!float.IsFinite(f)) throw new JsonException("Non-finite float is not allowed.");
            return (T)(object)f;
        }
        if (typeof(T) == typeof(decimal)) return (T)(object)reader.GetDecimal();
        if (typeof(T) == typeof(int)) return (T)(object)reader.GetInt32();
        if (typeof(T) == typeof(long)) return (T)(object)reader.GetInt64();
        return T.CreateChecked(ReadFiniteDouble(ref reader));
    }

    public static void WriteNumber<T>(Utf8JsonWriter writer, T value) where T : INumber<T>
    {
        if (typeof(T) == typeof(double))
        {
            double d = (double)(object)value;
            if (!double.IsFinite(d)) throw new JsonException("Cannot serialize non-finite double.");
            writer.WriteNumberValue(d);
            return;
        }
        if (typeof(T) == typeof(float))
        {
            float f = (float)(object)value;
            if (!float.IsFinite(f)) throw new JsonException("Cannot serialize non-finite float.");
            writer.WriteNumberValue(f);
            return;
        }
        if (typeof(T) == typeof(decimal)) { writer.WriteNumberValue((decimal)(object)value); return; }
        if (typeof(T) == typeof(int)) { writer.WriteNumberValue((int)(object)value); return; }
        if (typeof(T) == typeof(long)) { writer.WriteNumberValue((long)(object)value); return; }
        writer.WriteNumberValue(double.CreateChecked(value));
    }
}
