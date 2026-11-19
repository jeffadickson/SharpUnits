using System.Collections.ObjectModel;

namespace SharpUnits;

/// <summary>Immutable CLR dimension facade with signed roots and Java's nullable base map.</summary>
/// <remarks>Value retains exact rational exponents. Java's Map&lt;Dimension,int&gt;
/// exposes numerator powers for fractional roots; callers needing the denominator
/// must use Value. This leaves the native positive-root API unchanged.</remarks>
public sealed class JavaDimensionAdapter : IEquatable<JavaDimensionAdapter>
{
    // @relation(JP-WP1-DIMENSION, scope=file)
    /// <summary>The exact native dimension representation.</summary>
    public RationalDimension Value { get; }
    /// <summary>Creates a facade from normalized rational exponents.</summary>
    public JavaDimensionAdapter(RationalDimension value)
    {
        if(Components(value).Any(power=>power.Denominator.Sign<=0))throw new ArgumentException("Normalized exponents required.",nameof(value));
        Value=value;
    }
    private static Rational[] Components(RationalDimension value)=>new[]{value.Length,value.Mass,value.Time,value.Current,value.Temperature,value.Amount,value.Luminosity};
    /// <summary>Multiplies dimensions by adding exponents.</summary>
    public JavaDimensionAdapter Multiply(JavaDimensionAdapter other){ArgumentNullException.ThrowIfNull(other);return new(Value+other.Value);}
    /// <summary>Divides dimensions by subtracting exponents.</summary>
    public JavaDimensionAdapter Divide(JavaDimensionAdapter other){ArgumentNullException.ThrowIfNull(other);return new(Value-other.Value);}
    /// <summary>Raises dimensions to an integral power.</summary>
    public JavaDimensionAdapter Pow(int exponent)=>new(Value.Pow(exponent));
    /// <summary>Returns a signed-degree root; zero is invalid.</summary>
    public JavaDimensionAdapter Root(int degree)
    {
        if(degree==0)throw new DivideByZeroException("Root degree must not be zero.");return new(Value.Pow(new Rational(1,degree)));
    }
    /// <summary>Returns null for fundamental dimensions and an immutable map for composites.</summary>
    public IReadOnlyDictionary<JavaDimensionAdapter,int>? GetBaseDimensions()
    {
        var values=Components(Value);var active=values.Select((power,index)=>(power,index)).Where(pair=>!pair.power.Numerator.IsZero).ToArray();
        if(active.Length==1&&active[0].power==new Rational(1,1))return null;
        var map=new Dictionary<JavaDimensionAdapter,int>();
        foreach(var (power,index) in active)
        {
            var vector=new int[7];vector[index]=1;
            var dimension=RationalDimension.From(new Dimension(vector[0],vector[1],vector[2],vector[3],vector[4],vector[5],vector[6]));
            map.Add(new(dimension),checked((int)power.Numerator));
        }
        return new ReadOnlyDictionary<JavaDimensionAdapter,int>(map);
    }
    /// <summary>Compares exact dimensions independently of metadata or converter factors.</summary>
    public bool Equals(JavaDimensionAdapter? other)=>other is not null&&Value==other.Value;
    /// <inheritdoc/>
    public override bool Equals(object? obj)=>obj is JavaDimensionAdapter other&&Equals(other);
    /// <inheritdoc/>
    public override int GetHashCode()=>Value.GetHashCode();
}
