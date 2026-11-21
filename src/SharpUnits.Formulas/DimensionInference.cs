using System.Collections.ObjectModel;

namespace SharpUnits.Formulas;

/// <summary>A multiplicative dimensional expression with named unknown unit dimensions.</summary>
/// <remarks>Exponents are exact rational values; numerical magnitudes are not evaluated.</remarks>
public sealed class DimensionExpression
{
    private readonly Dictionary<string, Rational> coefficients;

    /// <summary>Known dimensional contribution.</summary>
    public RationalDimension Known { get; }

    /// <summary>Unknown symbols and their rational exponents.</summary>
    public IReadOnlyDictionary<string, Rational> Unknowns => new ReadOnlyDictionary<string, Rational>(coefficients);

    private DimensionExpression(RationalDimension known, Dictionary<string, Rational> terms)
    {
        Known = known;
        coefficients = terms.Where(p => !p.Value.IsZero).ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal);
    }

    /// <summary>Creates a known dimensional expression.</summary>
    public static DimensionExpression Of(RationalDimension known) => new(known, new(StringComparer.Ordinal));

    /// <summary>Creates an expression for a known integer-dimensional quantity.</summary>
    public static DimensionExpression Of(Dimension known) => Of(RationalDimension.From(known));

    /// <summary>Creates an unknown dimensional symbol.</summary>
    public static DimensionExpression Variable(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Symbol required.", nameof(name));
        return new(RationalDimension.None, new(StringComparer.Ordinal) { { name, Rational.One } });
    }

    /// <summary>Multiplies dimensional expressions, adding exponents.</summary>
    public static DimensionExpression operator *(DimensionExpression a, DimensionExpression b) => Combine(a, b, Rational.One);

    /// <summary>Divides dimensional expressions, subtracting exponents.</summary>
    public static DimensionExpression operator /(DimensionExpression a, DimensionExpression b) => Combine(a, b, -Rational.One);

    private static DimensionExpression Combine(DimensionExpression a, DimensionExpression b, Rational sign)
    {
        var result = new Dictionary<string, Rational>(a.coefficients, StringComparer.Ordinal);
        foreach (var term in b.coefficients)
        {
            result.TryGetValue(term.Key, out var old); // default(Rational) is canonical 0/1
            result[term.Key] = old + term.Value * sign;
        }
        return new(sign.Numerator > 0 ? a.Known + b.Known : a.Known - b.Known, result);
    }

    /// <summary>Raises dimensional exponents to an exact rational power.</summary>
    public DimensionExpression Pow(Rational power) =>
        new(Known.Pow(power), coefficients.ToDictionary(p => p.Key, p => p.Value * power, StringComparer.Ordinal));
}

/// <summary>One dimensional equality used by the inference engine.</summary>
public sealed record DimensionEquation(DimensionExpression Left, DimensionExpression Right);

/// <summary>Infers unknown SI dimensions from multiplicative equations using exact rational elimination.</summary>
/// <remarks>Requires a unique solution. Inconsistent or underdetermined systems are rejected.</remarks>
public static class DimensionInference
{
    /// <summary>Solves all unknown dimensions simultaneously.</summary>
    public static IReadOnlyDictionary<string, RationalDimension> Solve(IEnumerable<DimensionEquation> equations)
    {
        ArgumentNullException.ThrowIfNull(equations);
        var eqs = equations.ToArray();
        if (eqs.Any(e => e is null || e.Left is null || e.Right is null)) throw new ArgumentException("Null equation.");
        var names = eqs.SelectMany(e => e.Left.Unknowns.Keys.Concat(e.Right.Unknowns.Keys))
            .Distinct(StringComparer.Ordinal).OrderBy(s => s, StringComparer.Ordinal).ToArray();
        int n = names.Length, m = eqs.Length;
        if (n == 0)
        {
            if (eqs.Any(e => e.Left.Known != e.Right.Known))
                throw new InvalidOperationException("Inconsistent dimensional equality.");
            return new ReadOnlyDictionary<string, RationalDimension>(new Dictionary<string, RationalDimension>());
        }

        // One coefficient matrix, seven exact right-hand-side vectors.
        var a = new Rational[m, n + 7]; // default(Rational) is canonical 0/1
        for (int i = 0; i < m; i++)
        {
            for (int j = 0; j < n; j++)
            {
                string name = names[j];
                eqs[i].Left.Unknowns.TryGetValue(name, out var lc);
                eqs[i].Right.Unknowns.TryGetValue(name, out var rc);
                a[i, j] = lc - rc;
            }
            var delta = eqs[i].Right.Known - eqs[i].Left.Known;
            Rational[] d = [delta.Length, delta.Mass, delta.Time, delta.Current, delta.Temperature, delta.Amount, delta.Luminosity];
            for (int k = 0; k < 7; k++) a[i, n + k] = d[k];
        }

        int rank = 0;
        var pivots = new List<int>();
        for (int col = 0; col < n && rank < m; col++)
        {
            int pivot = rank;
            while (pivot < m && a[pivot, col].IsZero) pivot++;
            if (pivot == m) continue;
            for (int k = 0; k < n + 7; k++) (a[rank, k], a[pivot, k]) = (a[pivot, k], a[rank, k]);
            var divisor = a[rank, col];
            for (int k = 0; k < n + 7; k++) a[rank, k] /= divisor;
            for (int row = 0; row < m; row++)
            {
                if (row == rank || a[row, col].IsZero) continue;
                var factor = a[row, col];
                for (int k = 0; k < n + 7; k++) a[row, k] -= factor * a[rank, k];
            }
            pivots.Add(col);
            rank++;
        }

        for (int i = rank; i < m; i++)
        {
            if (Enumerable.Range(0, 7).Any(k => !a[i, n + k].IsZero))
                throw new InvalidOperationException("Inconsistent dimensional equations.");
        }
        if (rank < n) throw new InvalidOperationException("Underdetermined dimensional equations.");

        var output = new Dictionary<string, RationalDimension>(StringComparer.Ordinal);
        for (int i = 0; i < n; i++)
        {
            var d = Enumerable.Range(0, 7).Select(k => a[i, n + k]).ToArray();
            output[names[pivots[i]]] = new(d[0], d[1], d[2], d[3], d[4], d[5], d[6]);
        }
        return new ReadOnlyDictionary<string, RationalDimension>(output);
    }
}
