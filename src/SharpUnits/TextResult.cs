namespace SharpUnits;

/// <summary>Stable expected-failure categories for bounded exact unit and quantity text.</summary>
public enum TextErrorCode
{
    /// <summary>Supply a non-null value.</summary>
    NullInput=1,
    /// <summary>Use text/output within the documented length limit.</summary>
    InputLimit=2,
    /// <summary>Supply the expected token or remove unconsumed input.</summary>
    UnexpectedToken=3,
    /// <summary>Register the unit name in the formatter instance.</summary>
    UnknownUnit=4,
    /// <summary>Supply a valid finite exact numeric literal.</summary>
    InvalidNumber=5,
    /// <summary>Supply a nonzero divisor.</summary>
    ZeroDivisor=6,
    /// <summary>Supply a bounded integral/rational exponent with a nonzero root.</summary>
    InvalidExponent=7,
    /// <summary>Reduce nested groups or converter expressions.</summary>
    DepthLimit=8,
    /// <summary>Reduce the numeric token, decimal scale or exact bit count.</summary>
    NumericLimit=9,
    /// <summary>Reduce graph depth, atom count or exponent magnitude.</summary>
    GraphLimit=10,
    /// <summary>Use a supported graph/atomic converter.</summary>
    UnsupportedGraph=11,
    /// <summary>Use the absolute or relative quantity-scale marker.</summary>
    InvalidScale=12,
    /// <summary>The exact graph cannot be projected to a finite native affine definition.</summary>
    Projection=13
}

/// <summary>Immutable recoverable text failure; never retains the caller's source string.</summary>
public sealed record TextError
{
    /// <summary>Stable recovery category.</summary>
    public TextErrorCode Code { get; }
    /// <summary>Public argument requiring correction.</summary>
    public string Parameter { get; }
    /// <summary>Zero-based UTF16 position, including end-of-input; null for nontext failures.</summary>
    public int? Position { get; }
    /// <summary>Creates a validated typed diagnostic.</summary>
    public TextError(TextErrorCode code,string parameter,int? position=null)
    {
        if(!Enum.IsDefined(code))throw new ArgumentOutOfRangeException(nameof(code));
        ArgumentException.ThrowIfNullOrWhiteSpace(parameter);
        if(position<0)throw new ArgumentOutOfRangeException(nameof(position));
        Code=code;Parameter=parameter;Position=position;
    }
    internal Exception ToException()=>Code switch
    {
        TextErrorCode.NullInput=>new ArgumentNullException(Parameter),
        TextErrorCode.UnknownUnit=>new KeyNotFoundException($"Unknown unit at {Position}."),
        TextErrorCode.ZeroDivisor=>new DivideByZeroException(),
        _=>new FormatException($"{Code} at UTF16 position {Position}.")
    };
}

/// <summary>A non-null success or typed expected text failure, with railway composition.</summary>
/// <remarks>Private reference-type construction prevents default structs from pretending to
/// succeed. Faults and cancellation from handlers/continuations propagate normally.</remarks>
public sealed class TextResult<T> where T:notnull
{
    private readonly T? value;
    private readonly TextError? error;
    private TextResult(T? value,TextError? error){this.value=value;this.error=error;}
    /// <summary>Whether this result contains a value.</summary>
    public bool IsSuccess=>error is null;
    /// <summary>Creates a success with a non-null value.</summary>
    public static TextResult<T> Success(T value){ArgumentNullException.ThrowIfNull(value);return new(value,null);}
    /// <summary>Creates a failure with an immutable diagnostic.</summary>
    public static TextResult<T> Failure(TextError error){ArgumentNullException.ThrowIfNull(error);return new(default,error);}
    /// <summary>Dispatches to exactly one handler; both handlers must be supplied.</summary>
    public TResult Match<TResult>(Func<T,TResult> success,Func<TextError,TResult> failure)
    {
        ArgumentNullException.ThrowIfNull(success);ArgumentNullException.ThrowIfNull(failure);
        return error is null?success(value!):failure(error);
    }
    /// <summary>Runs the continuation only on success, forwarding the identical error otherwise.</summary>
    public TextResult<TNext> Bind<TNext>(Func<T,TextResult<TNext>> next) where TNext:notnull
    {
        ArgumentNullException.ThrowIfNull(next);
        return error is not null?TextResult<TNext>.Failure(error):next(value!)??throw new InvalidOperationException("A continuation must return a result.");
    }
}

// Only explicitly classified parser/formatter failures cross the typed boundary.
// This is internal control flow, never a catch-all for faults or cancellation.
internal sealed class TextReadFailure(TextError error):Exception
{
    internal TextError Error { get; }=error;
}
