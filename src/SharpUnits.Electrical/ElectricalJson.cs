using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SharpUnits.Electrical;

/// <summary>Extension methods registering NativeAOT-safe <see cref="System.Text.Json"/> converters for <see cref="SharpUnits.Electrical"/> types.</summary>
public static class ElectricalJsonExtensions
{
    /// <summary>Registers NativeAOT-safe converters for electrical and RF types (including dB, dBm, and dBV).</summary>
    public static JsonSerializerOptions AddSharpUnitsElectrical(this JsonSerializerOptions options, SharpUnitsJsonOptions? config = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        var cfg = config ?? new SharpUnitsJsonOptions();

        options.Converters.Add(new TypedQuantityJsonConverter<Voltage>(cfg, Units.Volt, v => new(v), q => q.Volts));
        options.Converters.Add(new TypedQuantityJsonConverter<Current>(cfg, Units.Ampere, v => new(v), q => q.Amperes));
        options.Converters.Add(new TypedQuantityJsonConverter<Resistance>(cfg, Units.Ohm, v => new(v), q => q.Ohms));
        options.Converters.Add(new TypedQuantityJsonConverter<ElectricPower>(cfg, Units.Watt, v => new(v), q => q.Watts));
        options.Converters.Add(new TypedQuantityJsonConverter<ElectricCharge>(cfg, Units.Coulomb, v => new(v), q => q.Coulombs));
        options.Converters.Add(new TypedQuantityJsonConverter<Capacitance>(cfg, Units.Farad, v => new(v), q => q.Farads));
        options.Converters.Add(new TypedQuantityJsonConverter<Inductance>(cfg, Units.Henry, v => new(v), q => q.Henries));
        options.Converters.Add(new TypedQuantityJsonConverter<Conductance>(cfg, Units.Siemens, v => new(v), q => q.Siemens));
        options.Converters.Add(new TypedQuantityJsonConverter<FluxLinkage>(cfg, Units.Weber, v => new(v), q => q.Webers));

        options.Converters.Add(new LogarithmicLevelJsonConverter<Decibels>(cfg, "dB", v => new(v), x => x.Value));
        options.Converters.Add(new LogarithmicLevelJsonConverter<PowerLevelDbm>(cfg, "dBm", v => new(v), x => x.Value));
        options.Converters.Add(new LogarithmicLevelJsonConverter<VoltageLevelDbv>(cfg, "dBV", v => new(v), x => x.Value));

        return options;
    }
}

internal sealed class LogarithmicLevelJsonConverter<TLevel> : JsonConverter<TLevel> where TLevel : struct
{
    private readonly SharpUnitsJsonOptions cfg;
    private readonly string expectedUnit;
    private readonly Func<double, TLevel> create;
    private readonly Func<TLevel, double> getValue;

    public LogarithmicLevelJsonConverter(SharpUnitsJsonOptions cfg, string expectedUnit, Func<double, TLevel> create, Func<TLevel, double> getValue)
    {
        this.cfg = cfg;
        this.expectedUnit = expectedUnit;
        this.create = create;
        this.getValue = getValue;
    }

    public override TLevel Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
            return create(ReadFinite(ref reader));

        if (reader.TokenType == JsonTokenType.String)
        {
            string text = (reader.GetString() ?? "").Trim();
            if (!text.EndsWith(expectedUnit, StringComparison.Ordinal))
                throw new JsonException($"Expected '{expectedUnit}' suffix in '{text}'.");
            string numPart = text[..^expectedUnit.Length].Trim();
            if (!double.TryParse(numPart, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) || !double.IsFinite(v))
                throw new JsonException($"Invalid numeric value in '{text}'.");
            return create(v);
        }

        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"Expected object, string, or number for {typeof(TLevel).Name}.");

        double? val = null;
        string? unit = null;
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            string p = reader.GetString()!;
            reader.Read();
            if (p.Equals("value", StringComparison.OrdinalIgnoreCase)) val = ReadFinite(ref reader);
            else if (p.Equals("unit", StringComparison.OrdinalIgnoreCase)) unit = reader.GetString();
            else if (cfg.StrictUnmappedMembers) throw new JsonException($"Unexpected property '{p}'.");
            else reader.Skip();
        }
        if (val is null) throw new JsonException($"{typeof(TLevel).Name} requires 'value'.");
        if (!string.IsNullOrWhiteSpace(unit) && !unit.Equals(expectedUnit, StringComparison.Ordinal))
            throw new JsonException($"Expected unit '{expectedUnit}', got '{unit}'.");
        return create(val.Value);
    }

    public override void Write(Utf8JsonWriter writer, TLevel value, JsonSerializerOptions options)
    {
        double v = getValue(value);
        if (!double.IsFinite(v)) throw new JsonException($"Cannot serialize non-finite {typeof(TLevel).Name}.");
        if (cfg.Format == SharpUnitsJsonFormat.CompactString)
        {
            writer.WriteStringValue($"{v.ToString("G", CultureInfo.InvariantCulture)} {expectedUnit}");
            return;
        }
        writer.WriteStartObject();
        writer.WriteNumber("value", v);
        writer.WriteString("unit", expectedUnit);
        writer.WriteEndObject();
    }

    private static double ReadFinite(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.Number) throw new JsonException("Expected JSON number.");
        double v = reader.GetDouble();
        if (!double.IsFinite(v)) throw new JsonException("Non-finite numbers are not allowed.");
        return v;
    }
}
