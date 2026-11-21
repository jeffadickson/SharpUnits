using System.Globalization;

namespace SharpUnits;

/// <summary>Culture-aware quantity formatting and parsing supporting single units, dynamic SI prefixes, and compound unit expressions.</summary>
public sealed class QuantityTextFormat
{
    private readonly UnitRegistry registry;
    private readonly UnitExpressionParser expressionParser;

    /// <summary>Creates a formatter using an explicit unit registry (defaults to <see cref="UnitRegistry.Standard"/>).</summary>
    public QuantityTextFormat(UnitRegistry? registry = null)
    {
        this.registry = registry ?? UnitRegistry.Standard;
        expressionParser = new UnitExpressionParser(this.registry);
    }

    /// <summary>Formats a quantity in a selected unit symbol (supporting dynamic SI prefixes).</summary>
    public string Format(Quantity quantity, string symbol, string? numberFormat = "R", IFormatProvider? provider = null)
    {
        var unit = registry.Resolve(symbol);
        double value = unit.FromQuantity(quantity);
        string num = value.ToString(numberFormat, provider ?? CultureInfo.InvariantCulture);
        return $"{num} {unit.Symbol}";
    }

    /// <summary>Formats a quantity using automatic engineering prefix scaling.</summary>
    public string FormatEngineering(Quantity quantity, string? numberFormat = "G4", IFormatProvider? provider = null, bool asciiOnly = false) =>
        quantity.FormatEngineering(numberFormat, provider, asciiOnly);

    /// <summary>Parses a numeric magnitude and unit symbol or compound unit expression (whitespace between number and unit is optional).</summary>
    public Quantity Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        string trimmed = text.Trim();
        if (trimmed.Length == 0) throw new FormatException("Expected '<number> <unit>'.");

        int numberEnd = ScanNumberEnd(trimmed);
        if (numberEnd <= 0 || numberEnd >= trimmed.Length)
            throw new FormatException("Expected '<number> <unit>'.");

        string numberPart = trimmed[..numberEnd];
        string unitPart = trimmed[numberEnd..].Trim();
        if (unitPart.Length == 0)
            throw new FormatException("Expected '<number> <unit>'.");

        if (!double.TryParse(numberPart, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) || !double.IsFinite(value))
            throw new FormatException("Invalid numeric value.");

        // Check for relative delta prefix (e.g. "10 Δ°C").
        var scale = QuantityScale.Absolute;
        if (unitPart.StartsWith('Δ'))
        {
            scale = QuantityScale.Relative;
            unitPart = unitPart[1..].TrimStart();
        }

        if (registry.TryResolve(unitPart, out var singleUnit) && singleUnit is not null)
            return singleUnit.ToQuantity(value, scale);

        // Fall back to compound unit expressions (e.g. "m/s^2", "V/Hz^(1/2)", "mA*kΩ").
        if (ContainsExpressionOperator(unitPart))
        {
            var expr = expressionParser.Parse(unitPart);
            var compoundUnit = new UnitDefinition(unitPart, expr.Dimension, UnitConversion.Linear(expr.Scale));
            return compoundUnit.ToQuantity(value, scale);
        }

        throw new KeyNotFoundException($"Unknown unit: {unitPart}");
    }

    private static int ScanNumberEnd(string s)
    {
        int i = 0;
        if (i < s.Length && (s[i] == '+' || s[i] == '-')) i++;
        bool digits = false;
        while (i < s.Length && char.IsAsciiDigit(s[i])) { i++; digits = true; }
        if (i < s.Length && s[i] == '.')
        {
            i++;
            while (i < s.Length && char.IsAsciiDigit(s[i])) { i++; digits = true; }
        }
        if (!digits) return 0;
        if (i < s.Length && (s[i] == 'e' || s[i] == 'E'))
        {
            int expStart = i;
            int j = i + 1;
            if (j < s.Length && (s[j] == '+' || s[j] == '-')) j++;
            int digitStart = j;
            while (j < s.Length && char.IsAsciiDigit(s[j])) j++;
            if (j > digitStart) i = j;
            else i = expStart;
        }
        return i;
    }

    private static bool ContainsExpressionOperator(string s)
    {
        foreach (char c in s)
            if (c is '*' or '/' or '·' or '^' or '(' or ')' or '⁰' or '¹' or '²' or '³' or '⁴' or '⁵' or '⁶' or '⁷' or '⁸' or '⁹' or '⁺' or '⁻')
                return true;
        return false;
    }
}
