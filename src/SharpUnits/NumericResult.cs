namespace SharpUnits;

/// <summary>Stable recovery categories for the additive CLR numeric boundaries.</summary>
/// <remarks>Numeric identifiers are explicit. Display wording is not part of code identity.</remarks>
public enum NumericErrorCode
{
    /// <summary>Supply a non-null input.</summary>
    NullInput = 1,
    /// <summary>Use a decimal token between one and 4,096 characters.</summary>
    TokenLength = 2,
    /// <summary>Remove the invalid mantissa character.</summary>
    InvalidDigit = 3,
    /// <summary>Supply at least one mantissa digit.</summary>
    MissingDigits = 4,
    /// <summary>Supply a signed Int32 exponent without whitespace.</summary>
    InvalidExponent = 5,
    /// <summary>Reduce the decimal input scale to the supported range.</summary>
    ScaleBudget = 6,
    /// <summary>Reduce the exact numerator or denominator to the numeric bit budget.</summary>
    NumericBudget = 7,
    /// <summary>Supply a finite floating-point value.</summary>
    NonFinite = 8,
    /// <summary>Supply a supported built-in numeric, BigInteger, Rational or JavaNumber.</summary>
    UnsupportedNumericType = 9,
    /// <summary>Supply a rational with a nonzero denominator.</summary>
    ZeroDenominator = 10
}

/// <summary>Immutable expected failure without retaining the caller's input object.</summary>
public sealed record NumericError
{
    /// <summary>Stable category callers can use for recovery.</summary>
    public NumericErrorCode Code { get; }
    /// <summary>Public input parameter that needs correction.</summary>
    public string Parameter { get; }
    /// <summary>Zero-based character offset, or null when no character is responsible.</summary>
    /// <remarks>Missing digits may point just past the input. InvalidExponent points
    /// to the beginning of the exponent field, not necessarily its first invalid digit.</remarks>
    public int? Position { get; }

    /// <summary>Constructs a valid structured error; rejects undefined codes and negative offsets.</summary>
    public NumericError(NumericErrorCode code, string parameter, int? position = null)
    {
        if (!Enum.IsDefined(code)) throw new ArgumentOutOfRangeException(nameof(code));
        ArgumentException.ThrowIfNullOrWhiteSpace(parameter);
        if (position < 0) throw new ArgumentOutOfRangeException(nameof(position));
        Code = code; Parameter = parameter; Position = position;
    }

    // Keep legacy exception types/parameter names at the throwing boundary.
    // Typed callers never have to classify or parse these English messages.
    internal Exception ToException(string? legacyParameter = null) => Code switch
    {
        NumericErrorCode.NullInput => new ArgumentNullException(legacyParameter ?? Parameter),
        NumericErrorCode.TokenLength => new FormatException("Decimal token length is outside the supported budget."),
        NumericErrorCode.InvalidDigit => new FormatException("Invalid decimal digit."),
        NumericErrorCode.MissingDigits => new FormatException("Decimal digits required."),
        NumericErrorCode.InvalidExponent => new FormatException("Invalid decimal exponent."),
        NumericErrorCode.ScaleBudget => new ArgumentOutOfRangeException(legacyParameter ?? Parameter, "Decimal scale budget exceeded."),
        NumericErrorCode.NumericBudget => new ArgumentOutOfRangeException(legacyParameter ?? Parameter, "Numeric bit budget exceeded."),
        NumericErrorCode.NonFinite => new ArgumentOutOfRangeException(legacyParameter ?? Parameter),
        NumericErrorCode.UnsupportedNumericType => new ArgumentException("A supported numeric value is required.", legacyParameter ?? Parameter),
        NumericErrorCode.ZeroDenominator => new DivideByZeroException(),
        _ => throw new InvalidOperationException("Undefined numeric error code.")
    };
}

/// <summary>A closed success or expected numeric failure, with explicit railway composition.</summary>
/// <remarks>A reference type with private construction prevents default structs from
/// masquerading as successful values. Unexpected exceptions and cancellation propagate.</remarks>
public sealed class NumericResult<T> where T : notnull
{
    private readonly T? value;
    private readonly NumericError? error;
    private NumericResult(T? value, NumericError? error) { this.value = value; this.error = error; }

    /// <summary>Whether the result contains a non-null value.</summary>
    public bool IsSuccess => error is null;

    /// <summary>Creates a success; null is not a value.</summary>
    public static NumericResult<T> Success(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new(value, null);
    }

    /// <summary>Creates a failure with a non-null structured error.</summary>
    public static NumericResult<T> Failure(NumericError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new(default, error);
    }

    /// <summary>Handles exactly one outcome. Both handlers must be supplied.</summary>
    public TResult Match<TResult>(Func<T, TResult> success, Func<NumericError, TResult> failure)
    {
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);
        return error is null ? success(value!) : failure(error);
    }

    /// <summary>Runs the next stage only on success; forwards the original error on failure.</summary>
    /// <remarks>A null continuation result is a programming fault. No exception or
    /// cancellation from a continuation is converted into an expected failure.</remarks>
    public NumericResult<TNext> Bind<TNext>(Func<T, NumericResult<TNext>> next) where TNext : notnull
    {
        ArgumentNullException.ThrowIfNull(next);
        return error is not null ? NumericResult<TNext>.Failure(error)
            : next(value!) ?? throw new InvalidOperationException("A continuation must return a result.");
    }
}
