using System.Globalization;

namespace SharpUnits;

/// <summary>One formatted axis tick mark pairing coherent SI position, display-unit value, and clean label.</summary>
public readonly record struct PlotTick(double SiValue, double DisplayValue, string Label);

/// <summary>Complete unit-aware linear or logarithmic plot axis layout ready for rendering.</summary>
public sealed record PlotAxisLayout(
    UnitDefinition DisplayUnit,
    double ScaleFromSi,
    double MinSi,
    double MaxSi,
    double MinDisplay,
    double MaxDisplay,
    IReadOnlyList<PlotTick> MajorTicks,
    IReadOnlyList<double> MinorSiValues)
{
    /// <summary>Converts a coherent SI coordinate into display-unit coordinates.</summary>
    public double ToDisplay(double siValue) => siValue * ScaleFromSi;
}

/// <summary>Generates human-readable, unit-aware linear and logarithmic decade tick marks for scientific and Bode plots.</summary>
public static class PlotAxisScaler
{
    private static readonly double[] NiceSteps = [1d, 2d, 2.5d, 5d, 10d];

    /// <summary>
    /// Selects a shared engineering SI unit for <c>[minSi, maxSi]</c> and computes clean "nice number" linear major ticks in that display unit.
    /// </summary>
    public static PlotAxisLayout CreateLinearAxis(
        double minSi,
        double maxSi,
        UnitDefinition baseUnit,
        int targetTickCount = 6,
        bool asciiOnly = false)
    {
        ArgumentNullException.ThrowIfNull(baseUnit);
        if (!double.IsFinite(minSi) || !double.IsFinite(maxSi))
            throw new ArgumentOutOfRangeException(nameof(minSi), "Axis bounds must be finite.");
        if (maxSi < minSi)
            throw new ArgumentException("Maximum axis bound cannot be less than minimum.", nameof(maxSi));
        if (targetTickCount is < 2 or > 100)
            throw new ArgumentOutOfRangeException(nameof(targetTickCount));

        var selection = UnitAutoScaler.SelectForAxis(minSi, maxSi, baseUnit);
        double minDisp = selection.FromSi(minSi);
        double maxDisp = selection.FromSi(maxSi);
        double range = maxDisp - minDisp;

        if (range == 0d)
        {
            double pad = minDisp == 0d ? 1d : Math.Abs(minDisp) * 0.1d;
            minDisp -= pad;
            maxDisp += pad;
            range = maxDisp - minDisp;
        }

        double roughStep = range / (targetTickCount - 1);
        double magnitude = Math.Pow(10d, Math.Floor(Math.Log10(roughStep)));
        double normalized = roughStep / magnitude;
        double niceFactor = NiceSteps.First(s => normalized <= s + 1e-12);
        double step = niceFactor * magnitude;

        double niceMin = Math.Floor(minDisp / step + 1e-12) * step;
        double niceMax = Math.Ceiling(maxDisp / step - 1e-12) * step;

        var ticks = new List<PlotTick>();
        int count = Math.Clamp((int)Math.Round((niceMax - niceMin) / step) + 1, 2, 200);
        for (int i = 0; i < count; i++)
        {
            double displayVal = niceMin + i * step;
            if (Math.Abs(displayVal) < step * 1e-12) displayVal = 0d; // Avoid "-0"
            double siVal = displayVal / selection.ScaleFromSi;
            string label = displayVal.ToString("G6", CultureInfo.InvariantCulture);
            ticks.Add(new PlotTick(siVal, displayVal, label));
        }

        string symbol = asciiOnly ? ToAscii(selection.Unit.Symbol) : selection.Unit.Symbol;
        var displayUnit = asciiOnly && symbol != selection.Unit.Symbol
            ? new UnitDefinition(symbol, selection.Unit.RationalDimension, selection.Unit.Converter, selection.Unit.Name)
            : selection.Unit;

        return new PlotAxisLayout(
            displayUnit,
            selection.ScaleFromSi,
            niceMin / selection.ScaleFromSi,
            niceMax / selection.ScaleFromSi,
            niceMin,
            niceMax,
            ticks.AsReadOnly(),
            Array.Empty<double>());
    }

    /// <summary>
    /// Computes clean logarithmic decade major ticks (each formatted with its own engineering prefix such as <c>10 Hz</c>, <c>1 kHz</c>, <c>1 MHz</c>)
    /// and optional <c>2..9</c> logarithmic minor ticks for Bode and spectrum plots.
    /// </summary>
    public static PlotAxisLayout CreateLogarithmicAxis(
        double minPositiveSi,
        double maxPositiveSi,
        UnitDefinition baseUnit,
        bool includeMinorTicks = true,
        bool asciiOnly = false)
    {
        ArgumentNullException.ThrowIfNull(baseUnit);
        if (!double.IsFinite(minPositiveSi) || minPositiveSi <= 0d)
            throw new ArgumentOutOfRangeException(nameof(minPositiveSi), "Logarithmic axis minimum must be positive and finite.");
        if (!double.IsFinite(maxPositiveSi) || maxPositiveSi <= minPositiveSi)
            throw new ArgumentOutOfRangeException(nameof(maxPositiveSi), "Logarithmic axis maximum must exceed minimum.");
        if (!baseUnit.Converter.IsLinear)
            throw new InvalidOperationException("Logarithmic axes require a linear unit.");

        int startDecade = (int)Math.Floor(Math.Log10(minPositiveSi) + 1e-12);
        int endDecade = (int)Math.Ceiling(Math.Log10(maxPositiveSi) - 1e-12);

        var majorTicks = new List<PlotTick>();
        var minorTicks = new List<double>();
        for (int exp = startDecade; exp <= endDecade; exp++)
        {
            double siVal = Math.Pow(10d, exp);
            var q = Quantity.Of(siVal / baseUnit.Converter.Scale, baseUnit);
            string label = q.FormatEngineering("G4", CultureInfo.InvariantCulture, asciiOnly);
            majorTicks.Add(new PlotTick(siVal, siVal / baseUnit.Converter.Scale, label));

            if (includeMinorTicks && exp < endDecade)
            {
                for (int m = 2; m <= 9; m++)
                    minorTicks.Add(m * siVal);
            }
        }

        double minTickSi = Math.Pow(10d, startDecade);
        double maxTickSi = Math.Pow(10d, endDecade);
        return new PlotAxisLayout(
            baseUnit,
            1d / baseUnit.Converter.Scale,
            minTickSi,
            maxTickSi,
            minTickSi / baseUnit.Converter.Scale,
            maxTickSi / baseUnit.Converter.Scale,
            majorTicks.AsReadOnly(),
            minorTicks.AsReadOnly());
    }

    private static string ToAscii(string s) =>
        s.Replace("µ", "u", StringComparison.Ordinal)
         .Replace("Ω", "Ohm", StringComparison.Ordinal)
         .Replace("°", "deg", StringComparison.Ordinal);
}
