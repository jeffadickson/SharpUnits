namespace SharpUnits;

/// <summary>A standard-uncertainty contribution with its sensitivity coefficient.</summary>
public sealed record UncertaintyComponent
{
    /// <summary>Unique component identifier.</summary>
    public string Id { get; }
    /// <summary>Nonnegative standard uncertainty of the input.</summary>
    public double StandardUncertainty { get; }
    /// <summary>Sensitivity coefficient mapping the input to the measurand.</summary>
    public double Sensitivity { get; }
    /// <summary>Creates a validated uncertainty contribution.</summary>
    public UncertaintyComponent(string id, double standardUncertainty, double sensitivity)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("ID required.", nameof(id));
        if (!double.IsFinite(standardUncertainty) || standardUncertainty < 0)
            throw new ArgumentOutOfRangeException(nameof(standardUncertainty));
        if (!double.IsFinite(sensitivity)) throw new ArgumentOutOfRangeException(nameof(sensitivity));
        Id = id;
        StandardUncertainty = standardUncertainty;
        Sensitivity = sensitivity;
    }
}

/// <summary>First-order uncertainty budget with explicit pairwise correlations.</summary>
/// <remarks>Correlation matrices must be positive semidefinite. This implementation is
/// intended for small laboratory budgets rather than ill-conditioned numerical problems.</remarks>
public sealed class UncertaintyBudget
{
    private readonly UncertaintyComponent[] components;
    private readonly double[,] correlation;

    /// <summary>Creates a budget with optional correlation matrix.</summary>
    public UncertaintyBudget(IEnumerable<UncertaintyComponent> components, double[,]? correlationMatrix = null)
    {
        ArgumentNullException.ThrowIfNull(components);
        this.components = components.ToArray();
        if (this.components.Length == 0) throw new ArgumentException("At least one contribution is required.", nameof(components));
        if (this.components.Any(c => c is null)) throw new ArgumentException("Null contribution.", nameof(components));
        if (this.components.Select(c => c.Id).Distinct(StringComparer.Ordinal).Count() != this.components.Length)
            throw new ArgumentException("Contribution IDs must be unique.", nameof(components));
        int n = this.components.Length;
        correlation = new double[n, n];
        if (correlationMatrix is null)
        {
            for (int i = 0; i < n; i++) correlation[i, i] = 1;
        }
        else
        {
            if (correlationMatrix.GetLength(0) != n || correlationMatrix.GetLength(1) != n)
                throw new ArgumentException("Correlation matrix size does not match components.", nameof(correlationMatrix));
            for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
            {
                double value = correlationMatrix[i, j];
                if (!double.IsFinite(value) || Math.Abs(value) > 1)
                    throw new ArgumentException("Invalid correlation coefficient.", nameof(correlationMatrix));
                correlation[i, j] = value;
            }
            for (int i = 0; i < n; i++)
            {
                if (correlation[i, i] != 1)
                    throw new ArgumentException("Correlation diagonal must be 1.", nameof(correlationMatrix));
                for (int j = i + 1; j < n; j++)
                    if (Math.Abs(correlation[i, j] - correlation[j, i]) > 1e-12)
                        throw new ArgumentException("Correlation matrix must be symmetric.", nameof(correlationMatrix));
            }
            ValidatePositiveSemidefinite(correlation);
        }
    }

    /// <summary>Immutable snapshot of input components.</summary>
    public IReadOnlyList<UncertaintyComponent> Components => Array.AsReadOnly(components);
    /// <summary>Defensive copy of the correlation matrix.</summary>
    public double[,] Correlations => (double[,])correlation.Clone();

    /// <summary>Combined standard uncertainty in coherent SI units.</summary>
    public double CombinedStandardUncertainty
    {
        get
        {
            double variance = 0;
            for (int i = 0; i < components.Length; i++)
            for (int j = 0; j < components.Length; j++)
            {
                double contribution = components[i].Sensitivity * components[i].StandardUncertainty
                    * correlation[i, j] * components[j].Sensitivity * components[j].StandardUncertainty;
                variance += contribution;
            }
            if (!double.IsFinite(variance)) throw new OverflowException("Uncertainty variance overflow.");
            if (variance < -1e-12 * Math.Max(1, components.Sum(c => Math.Pow(c.Sensitivity * c.StandardUncertainty, 2))))
                throw new InvalidOperationException("Computed negative variance.");
            return Math.Sqrt(Math.Max(0, variance));
        }
    }

    /// <summary>Expanded uncertainty at the specified coverage factor.</summary>
    public double ExpandedUncertainty(double coverageFactor)
    {
        if (!double.IsFinite(coverageFactor) || coverageFactor < 0)
            throw new ArgumentOutOfRangeException(nameof(coverageFactor));
        double expanded = coverageFactor * CombinedStandardUncertainty;
        if (!double.IsFinite(expanded)) throw new OverflowException("Expanded uncertainty overflow.");
        return expanded;
    }

    /// <summary>Returns an observation using this budget's standard uncertainty.</summary>
    public InstrumentMeasurement ApplyTo(InstrumentMeasurement observation)
    {
        ArgumentNullException.ThrowIfNull(observation);
        return new InstrumentMeasurement(observation.Reading, CombinedStandardUncertainty,
            observation.InstrumentId, observation.Timestamp, observation.CalibrationId);
    }

    private static void ValidatePositiveSemidefinite(double[,] matrix)
    {
        int n = matrix.GetLength(0);
        var lower = new double[n, n];
        const double tolerance = 1e-10;
        for (int i = 0; i < n; i++)
        for (int j = 0; j <= i; j++)
        {
            double value = matrix[i, j];
            for (int k = 0; k < j; k++) value -= lower[i, k] * lower[j, k];
            if (i == j)
            {
                if (value < -tolerance) throw new ArgumentException("Correlation matrix is not positive semidefinite.");
                lower[i, j] = Math.Sqrt(Math.Max(0, value));
            }
            else if (lower[j, j] > tolerance) lower[i, j] = value / lower[j, j];
            else if (Math.Abs(value) > tolerance)
                throw new ArgumentException("Correlation matrix is not positive semidefinite.");
        }
    }
}
