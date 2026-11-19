namespace SharpUnits;

/// <summary>Immutable converter with exact finite numbers and Java composition order.</summary>
/// <remarks>Concatenate applies its argument first. Then provides native left-then-right order.
/// Operation equality does not claim Java runtime class or hash-code identity.</remarks>
public sealed class JavaUnitConverterAdapter : IEquatable<JavaUnitConverterAdapter>
{
    // @relation(JP-WP1-CONVERTER, scope=file)
    private enum Operation {Scale,Offset,Log,Exp,Power}
    private sealed record Step(Operation Kind,JavaNumber Parameter,int Radix=0,int Exponent=0);
    private readonly Step[] steps; // Composition order; apply from right to left.
    /// <summary>Constructs an affine converter from its retained decimal coefficients.</summary>
    public JavaUnitConverterAdapter(UnitConversion converter)
    {
        if(!double.IsFinite(converter.Scale)||converter.Scale==0||!double.IsFinite(converter.Offset))
            throw new ArgumentException("A valid affine converter is required.",nameof(converter));
        var list=new List<Step>();
        if(converter.Offset!=0)list.Add(new(Operation.Offset,JavaNumber.FromDouble(converter.Offset)));
        if(converter.Scale!=1)
        {
            list.Add(new(Operation.Scale,JavaNumber.FromDoubleAsRational(converter.Scale)));
        }
        steps=list.ToArray();
    }
    private JavaUnitConverterAdapter(IEnumerable<Step> source)=>steps=Normalize(source).ToArray();
    /// <summary>The identity converter.</summary>
    public static JavaUnitConverterAdapter Identity { get; }=new(Array.Empty<Step>());
    /// <summary>Creates an exact rational multiplier; zero is allowed but has no inverse.</summary>
    public static JavaUnitConverterAdapter Multiply(JavaNumber factor)
    {
        ArgumentNullException.ThrowIfNull(factor);
        return new(new[]{new Step(Operation.Scale,JavaNumber.Ratio(factor.Fraction.Numerator,factor.Fraction.Denominator))});
    }
    /// <summary>Creates a pinned integer-radix prefix converter, preserving radix/exponent identity.</summary>
    /// <remarks>Exponent one uses Java's rational-multiplier shortcut. Other powers
    /// remain structurally distinct from an equal rational factor. Radix must be at
    /// least two and exponent magnitude at most 1024; numeric bit budgets also apply.</remarks>
    public static JavaUnitConverterAdapter ForPrefix(int radix,int exponent)
    {
        if(radix<2)throw new ArgumentOutOfRangeException(nameof(radix));
        if(exponent is < -1024 or >1024)throw new ArgumentOutOfRangeException(nameof(exponent));
        return exponent==1?Multiply(JavaNumber.Integer(radix)):new(new[]{PowerStep(radix,exponent)});
    }
    private static Step PowerStep(int radix,int exponent)
    {
        var value=System.Numerics.BigInteger.Pow(radix,Math.Abs(exponent));
        var factor=exponent>=0?JavaNumber.Integer(value):JavaNumber.Ratio(1,value);
        return new(Operation.Power,factor,radix,exponent);
    }
    /// <summary>Creates an additive converter retaining its numeric representation.</summary>
    public static JavaUnitConverterAdapter Add(JavaNumber offset)
    {
        ArgumentNullException.ThrowIfNull(offset);return new(new[]{new Step(Operation.Offset,offset)});
    }
    /// <summary>Creates a logarithm for a finite positive base other than one.</summary>
    public static JavaUnitConverterAdapter Logarithm(double @base)
    {
        if(!double.IsFinite(@base)||@base<=0||@base==1)throw new ArgumentOutOfRangeException(nameof(@base));
        return new(new[]{new Step(Operation.Log,JavaNumber.FromDouble(@base))});
    }
    /// <summary>Returns inverse operations in reverse composition order.</summary>
    public JavaUnitConverterAdapter Inverse()=>new(steps.Reverse().Select(step=>step.Kind switch
    {
        Operation.Scale=>new Step(Operation.Scale,step.Parameter.Reciprocal()),
        Operation.Offset=>new Step(Operation.Offset,step.Parameter.Negate()),
        Operation.Log=>new Step(Operation.Exp,step.Parameter),
        Operation.Power=>PowerStep(step.Radix,checked(-step.Exponent)),
        _=>new Step(Operation.Log,step.Parameter)
    }));
    /// <summary>True when normalized composition has no operations.</summary>
    public bool IsIdentity=>steps.Length==0;
    /// <summary>True when every operation is a multiplier.</summary>
    public bool IsLinear=>steps.All(step=>step.Kind is Operation.Scale or Operation.Power);
    /// <summary>True when every operation is a scale or offset.</summary>
    public bool IsAffine=>steps.All(step=>step.Kind is Operation.Scale or Operation.Power or Operation.Offset);
    /// <summary>Immutable atomic operation metadata in Java composition order.</summary>
    /// <remarks>Preserves prefix radix/exponent identity separately from its numeric factor.</remarks>
    public IReadOnlyList<(string Kind,JavaNumber Parameter,int Radix,int Exponent)> Operations=>Array.AsReadOnly(
        steps.Select(step=>(step.Kind.ToString(),step.Parameter,step.Radix,step.Exponent)).ToArray());
    /// <summary>Converts a double through pinned numeric promotion.</summary>
    public double Convert(double value)=>IsIdentity?value:Convert(JavaNumber.FromDouble(value)).ToDouble();
    /// <summary>Legacy numeric overload, projecting the exact result to binary64.</summary>
    public double Convert(IConvertible value)=>Convert(JavaNumber.FromNumeric(value)).ToDouble();
    /// <summary>Converts without discarding integer, rational or decimal precision.</summary>
    public JavaNumber Convert(JavaNumber value)
    {
        ArgumentNullException.ThrowIfNull(value);var result=value;
        for(var index=steps.Length-1;index>=0;index--)
        {
            var step=steps[index];
            result=step.Kind switch
            {
                Operation.Scale or Operation.Power=>step.Parameter.Multiply(result),
                Operation.Offset=>step.Parameter.Add(result),
                Operation.Log=>JavaNumber.FromDouble(Math.Log(result.ToDouble())).Divide(JavaNumber.FromDouble(Math.Log(step.Parameter.ToDouble()))),
                _=>JavaNumber.FromDouble(Math.Exp(JavaNumber.FromDouble(Math.Log(step.Parameter.ToDouble())).Multiply(result).ToDouble()))
            };
        }
        return result;
    }
    /// <summary>Composes this converter after the argument: this(argument(x)).</summary>
    public JavaUnitConverterAdapter Concatenate(JavaUnitConverterAdapter argument)
    {
        ArgumentNullException.ThrowIfNull(argument);return new(steps.Concat(argument.steps));
    }
    /// <summary>Applies this converter first, then the argument, matching native pipeline order.</summary>
    public JavaUnitConverterAdapter Then(JavaUnitConverterAdapter next)
    {
        ArgumentNullException.ThrowIfNull(next);return next.Concatenate(this);
    }
    /// <summary>Returns immutable atomic converter steps in Java composition order.</summary>
    /// <remarks>The identity has one identity step, as pinned Java does.</remarks>
    public IReadOnlyList<JavaUnitConverterAdapter> ConversionSteps=>Array.AsReadOnly(IsIdentity
        ?new[]{Identity}:steps.Select(step=>new JavaUnitConverterAdapter(new[]{step})).ToArray());
    /// <summary>Returns affine step projections in Java composition order.</summary>
    /// <exception cref="NotSupportedException">A nonlinear step has no affine projection; use ConversionSteps.</exception>
    public IReadOnlyList<ConversionStep> GetConversionSteps()=>Array.AsReadOnly(ConversionSteps.Select(step=>
    {
        var affine=step.ToAffine();return new ConversionStep(affine.Scale,affine.Offset);
    }).ToArray());
    /// <summary>Returns exact affine coefficients, rejecting nonlinear converters.</summary>
    public (JavaNumber Scale,JavaNumber Offset) AffineCoefficients()
    {
        if(!IsAffine)throw new NotSupportedException("Nonlinear converter has no affine coefficients.");
        var scale=JavaNumber.One;var offset=JavaNumber.Zero;
        for(var i=steps.Length-1;i>=0;i--)
            if(steps[i].Kind is Operation.Scale or Operation.Power){scale=scale.Multiply(steps[i].Parameter);offset=offset.Multiply(steps[i].Parameter);}
            else offset=offset.Add(steps[i].Parameter);
        return (scale,offset);
    }
    /// <summary>Returns an affine double projection, rejecting nonlinear or zero-scale mappings.</summary>
    public UnitConversion ToAffine(){var c=AffineCoefficients();return new(c.Scale.ToDouble(),c.Offset.ToDouble());}
    private static IEnumerable<Step> Normalize(IEnumerable<Step> source)
    {
        var result=new List<Step>();
        foreach(var incoming in source)
        {
            var current=incoming;
            if(current.Kind is Operation.Scale or Operation.Power&&current.Parameter.CompareTo(JavaNumber.One)==0
                ||current.Kind==Operation.Offset&&current.Parameter.CompareTo(JavaNumber.Zero)==0)continue;
            if(result.Count>0)
            {
                var previous=result[^1];
                var multipliers=previous.Kind is Operation.Scale or Operation.Power&&current.Kind is Operation.Scale or Operation.Power;
                var canReducePowers=previous.Kind!=Operation.Power||current.Kind!=Operation.Power||previous.Radix==current.Radix;
                if(previous.Kind==current.Kind&&current.Kind==Operation.Offset||multipliers&&canReducePowers)
                {
                    result.RemoveAt(result.Count-1);
                    current=previous.Kind==Operation.Power&&current.Kind==Operation.Power
                        ?PowerStep(previous.Radix,checked(previous.Exponent+current.Exponent))
                        :multipliers?new(Operation.Scale,previous.Parameter.Multiply(current.Parameter)):new(Operation.Offset,previous.Parameter.Add(current.Parameter));
                    if(current.Kind is Operation.Scale or Operation.Power&&current.Parameter.CompareTo(JavaNumber.One)==0
                        ||current.Kind==Operation.Offset&&current.Parameter.CompareTo(JavaNumber.Zero)==0)continue;
                }
                else if((previous.Kind==Operation.Log&&current.Kind==Operation.Exp||previous.Kind==Operation.Exp&&current.Kind==Operation.Log)
                    &&previous.Parameter.Equals(current.Parameter)){result.RemoveAt(result.Count-1);continue;}
            }
            result.Add(current);
        }
        return result;
    }
    /// <summary>Compares normalized operation sequences rather than sampled outputs.</summary>
    public bool Equals(JavaUnitConverterAdapter? other)=>other is not null&&steps.SequenceEqual(other.steps);
    /// <inheritdoc/>
    public override bool Equals(object? obj)=>obj is JavaUnitConverterAdapter other&&Equals(other);
    /// <inheritdoc/>
    public override int GetHashCode(){var hash=new HashCode();foreach(var step in steps)hash.Add(step);return hash.ToHashCode();}
}
