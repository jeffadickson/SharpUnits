using System.Collections.Frozen;
using System.Globalization;
using System.Numerics;

namespace SharpUnits;

/// <summary>Invariant, lossless text for registered exact unit graphs and finite quantities.</summary>
/// <remarks>Native operators associate left-to-right. The codec preserves numeric family/scale,
/// converter operation order and prefix metadata. It does not evaluate product/root converters.
/// Text/output is limited to8192 UTF16 characters; nesting to64 and graph/numeric construction
/// to the existing exact-core limits. Decimal tokens use the numeric parser's4096-character
/// and scale limits. Bindings are immutable snapshots; parsing never publishes new symbols.
/// This native grammar is distinct from Java's SIMPLE/EBNF and from locale display formats.</remarks>
public sealed partial class ExactTextFormat
{
    internal const int TextLimit=8192;
    private readonly FrozenDictionary<string,JavaUnit> bindings;

    /// <summary>Creates an isolated formatter, optionally overlaying explicit unit bindings.</summary>
    /// <remarks>Bindings overlay catalog spellings in this instance only. Alternate catalog
    /// spellings not explicitly overlaid remain available. Use explicit bindings for custom base/alternate units.</remarks>
    public ExactTextFormat(IReadOnlyDictionary<string,JavaUnit>? bindings=null)
    {
        var names=new Dictionary<string,JavaUnit>(StringComparer.Ordinal);
        foreach(var pair in JavaUnit.CreateCatalog())
        {
            names[pair.Key]=pair.Value;
            if(!string.IsNullOrEmpty(pair.Value.Symbol))names[pair.Value.Symbol]=pair.Value;
        }
        names["one"]=JavaUnit.One;names["1"]=JavaUnit.One;
        names["°C"]=JavaUnit.FromCatalog("CELSIUS");
        names["°F"]=JavaUnit.FromDefinition(UnitRegistry.CreateStandard().Resolve("°F"));
        if(bindings is not null)foreach(var pair in bindings)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(pair.Key);ArgumentNullException.ThrowIfNull(pair.Value);
            if(!WellFormed(pair.Key))throw new ArgumentException("Binding names require valid UTF16.",nameof(bindings));
            if(pair.Key.Length>TextLimit)throw new ArgumentOutOfRangeException(nameof(bindings));
            names[pair.Key]=pair.Value;
        }
        this.bindings=names.ToFrozenDictionary(StringComparer.Ordinal);
    }

    /// <summary>Parses an exact unit expression with strict complete consumption and typed failures.</summary>
    public TextResult<JavaUnit> ParseUnit(string? text)=>Read(text,reader=>reader.ReadUnit());
    /// <summary>Parses exact numeric value, display graph and optional @absolute/@relative scale.</summary>
    /// <remarks>Unmarked quantities are absolute; a missing unit is dimensionless.</remarks>
    public TextResult<JavaNumberQuantity> ParseQuantity(string? text)=>Read(text,reader=>reader.ReadQuantity());
    private TextResult<T> Read<T>(string? text,Func<Reader,T> parse) where T:notnull
    {
        if(text is null)return TextResult<T>.Failure(new(TextErrorCode.NullInput,"text"));
        if(text.Length>TextLimit)return TextResult<T>.Failure(new(TextErrorCode.InputLimit,"text",TextLimit));
        try{return TextResult<T>.Success(parse(new Reader(text,bindings)));}
        catch(TextReadFailure failure){return TextResult<T>.Failure(failure.Error);}
    }

    /// <summary>Formats a graph without a double projection; throws for an expected unsupported graph.</summary>
    public string FormatUnit(JavaUnit unit)=>TryFormatUnit(unit).Match(text=>text,error=>throw error.ToException());
    /// <summary>Formats exact magnitude, unit graph and explicit scale marker.</summary>
    public string FormatQuantity(JavaNumberQuantity quantity)=>TryFormatQuantity(quantity).Match(text=>text,error=>throw error.ToException());
    /// <summary>Formats a graph, returning typed null/unknown-atom/size/unsupported-converter failures.</summary>
    public TextResult<string> TryFormatUnit(JavaUnit? unit)=>Format(unit,"unit",value=>WriteUnit(value,0));
    /// <summary>Formats a quantity without losing exact magnitude, numeric family or scale.</summary>
    public TextResult<string> TryFormatQuantity(JavaNumberQuantity? quantity)=>Format(quantity,"quantity",value=>
        NumberText(value.Value)+" "+WriteUnit(value.Unit,0)+(value.Scale==JavaQuantityScale.Relative?" @relative":" @absolute"));
    private static TextResult<string> Format<T>(T? value,string parameter,Func<T,string> format) where T:class
    {
        if(value is null)return TextResult<string>.Failure(new(TextErrorCode.NullInput,parameter));
        try
        {
            var text=format(value);
            return text.Length<=TextLimit?TextResult<string>.Success(text):TextResult<string>.Failure(new(TextErrorCode.InputLimit,parameter));
        }
        catch(TextReadFailure failure){return TextResult<string>.Failure(new(failure.Error.Code,parameter));}
    }

    // The public Rational equality intentionally follows decimal128 projection. A codec
    // must compare raw fractions too, or a near-equal registered alias can erase precision.
    private sealed class GraphMatcher
    {
        // Memoize reference pairs, not the deliberately projection-based Java equality.
        // A work cap also bounds matching against unusually large caller binding maps.
        private int remaining=65536;
        private readonly Dictionary<(JavaUnit,JavaUnit),bool> seen=new(new PairComparer());
        internal bool Same(JavaUnit a,JavaUnit b)
        {
            if(--remaining<0)throw new TextReadFailure(new(TextErrorCode.GraphLimit,"unit"));
            if(ReferenceEquals(a,b))return true;
            if(seen.TryGetValue((a,b),out var answer))return answer;
            answer=Compare(a,b);seen[(a,b)]=answer;return answer;
        }
        private bool Compare(JavaUnit a,JavaUnit b)
        {
            if(a.Kind!=b.Kind||a.Dimension!=b.Dimension||a.Factors.Count!=b.Factors.Count)return false;
            if(a.Kind is JavaUnitKind.Base or JavaUnitKind.Alternate&&a.Symbol!=b.Symbol)return false;
            if(a.ParentUnit is not null&&(b.ParentUnit is null||!Same(a.ParentUnit,b.ParentUnit)))return false;
            if(a.TransformConverter is not null)
            {
                if(b.TransformConverter is null)return false;
                var x=a.TransformConverter.Operations;var y=b.TransformConverter.Operations;
                if(x.Count!=y.Count)return false;
                for(var i=0;i<x.Count;i++)if(x[i].Kind!=y[i].Kind||x[i].Radix!=y[i].Radix||x[i].Exponent!=y[i].Exponent
                    ||x[i].Parameter.Fraction!=y[i].Parameter.Fraction||x[i].Parameter.Kind!=y[i].Parameter.Kind
                    ||x[i].Parameter.DecimalScale!=y[i].Parameter.DecimalScale)return false;
            }
            return a.Factors.All(x=>b.Factors.Any(y=>x.Value==y.Value&&Same(x.Key,y.Key)));
        }
        private sealed class PairComparer:IEqualityComparer<(JavaUnit,JavaUnit)>
        {
            public bool Equals((JavaUnit,JavaUnit) x,(JavaUnit,JavaUnit) y)=>ReferenceEquals(x.Item1,y.Item1)&&ReferenceEquals(x.Item2,y.Item2);
            public int GetHashCode((JavaUnit,JavaUnit) pair)=>HashCode.Combine(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(pair.Item1),System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(pair.Item2));
        }
    }
    private static bool WellFormed(string text)
    {
        for(var i=0;i<text.Length;i++)
            if(char.IsHighSurrogate(text[i])){if(++i==text.Length||!char.IsLowSurrogate(text[i]))return false;}
            else if(char.IsLowSurrogate(text[i]))return false;
        return true;
    }
    internal static UnitDefinition ParseDefinition(string text,UnitRegistry registry)
    {
        var names=registry.Snapshot().ToDictionary(pair=>pair.Key,pair=>JavaUnit.FromDefinition(pair.Value),StringComparer.Ordinal);
        return new ExactTextFormat(names).ParseUnit(text).Match(unit=>
        {
            try
            {
                var projected=unit.ToDefinition(text);
                if(!double.IsFinite(projected.Converter.Scale)||projected.Converter.Scale==0||!double.IsFinite(projected.Converter.Offset))
                    throw new FormatException("The exact graph has no finite invertible native projection.");
                return projected;
            }
            catch(Exception error) when(error is NotSupportedException or ArgumentOutOfRangeException)
            {throw new FormatException("The exact graph has no native affine projection.",error);}
        },error=>throw error.ToException());
    }

    internal static string NumberText(JavaNumber number)
    {
        var fraction=number.Fraction;
        if(number.Kind==JavaNumberKind.Binary64)return "binary("+number.ToDouble().ToString("R",CultureInfo.InvariantCulture)+")";
        if(number.Kind==JavaNumberKind.Rational)return fraction.Numerator.ToString(CultureInfo.InvariantCulture)+"÷"+fraction.Denominator.ToString(CultureInfo.InvariantCulture);
        if(number.Kind==JavaNumberKind.Decimal)
        {
            var scale=number.DecimalScale;
            var coefficient=scale>=0?fraction.Numerator*BigInteger.Pow(10,scale)/fraction.Denominator
                :fraction.Numerator/(fraction.Denominator*BigInteger.Pow(10,-scale));
            var token=coefficient.ToString(CultureInfo.InvariantCulture)+"e"+(-scale).ToString(CultureInfo.InvariantCulture);
            if(token.Length>4096)throw new TextReadFailure(new(TextErrorCode.NumericLimit,"value"));
            return token;
        }
        return fraction.Numerator.ToString(CultureInfo.InvariantCulture);
    }
}
