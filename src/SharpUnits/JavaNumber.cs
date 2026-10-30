using System.Globalization;
using System.Numerics;

namespace SharpUnits;

/// <summary>Numeric families supported by the finite Java compatibility profile.</summary>
public enum JavaNumberKind
{
    /// <summary>An arbitrary-size integer, narrowed independently of CLR integer widths.</summary>
    Integer,
    /// <summary>An exact rational number.</summary>
    Rational,
    /// <summary>A finite binary64 number, with Java decimal promotion when required.</summary>
    Binary64,
    /// <summary>An arbitrary-precision decimal with a retained decimal scale.</summary>
    Decimal
}

/// <summary>Immutable finite numeric value with pinned Indriya-style arithmetic promotion.</summary>
/// <remarks>Uses BigInteger and Rational rather than a double as its primary storage.
/// Decimal/decimal operations use decimal128 (34 significant digits, ties to even).
/// Decimal/rational operations round the rational operand to decimal128 first, as
/// pinned DefaultNumberSystem does. CLR integer widths are not Java runtime classes.
/// Inputs and intermediates have a 16,384-bit budget; decimal scales are bounded by 4,096.</remarks>
public sealed class JavaNumber : IEquatable<JavaNumber>, IComparable<JavaNumber>
{
    // @relation(JP-WP1-NUMBER, scope=file)
    private const int Budget=16384;
    private readonly double binary;
    /// <summary>Exact value of the retained decimal/rational representation.</summary>
    public Rational Fraction { get; }
    /// <summary>Numeric family after integer narrowing.</summary>
    public JavaNumberKind Kind { get; }
    /// <summary>Retained scale for decimals; zero for other families.</summary>
    public int DecimalScale { get; }
    /// <summary>Canonical integer zero.</summary>
    public static JavaNumber Zero { get; }=Integer(BigInteger.Zero);
    /// <summary>Canonical integer one.</summary>
    public static JavaNumber One { get; }=Integer(BigInteger.One);

    private JavaNumber(Rational value,JavaNumberKind kind,int scale=0,double binary=0)
    {
        Check(value);
        Fraction=value;Kind=value.Denominator.IsOne?JavaNumberKind.Integer:kind;
        DecimalScale=Kind==JavaNumberKind.Decimal?scale:0;this.binary=binary;
    }
    /// <summary>Creates an exact arbitrary-size integer.</summary>
    public static JavaNumber Integer(BigInteger value)=>new(new Rational(value,1),JavaNumberKind.Integer);
    /// <summary>Creates an exact rational, narrowing integers.</summary>
    public static JavaNumber Ratio(BigInteger numerator,BigInteger denominator)=>new(new Rational(numerator,denominator),JavaNumberKind.Rational);
    /// <summary>Parses invariant decimal/scientific notation without passing through binary64.</summary>
    public static JavaNumber Decimal(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if(text.Length is 0 or >4096)throw new FormatException("Decimal token length is outside the supported budget.");
        var span=text.AsSpan();var sign=1;var index=0;
        if(span[0] is '+' or '-') {if(span[0]=='-')sign=-1;index++;}
        var digits=new System.Text.StringBuilder();var fractional=0;var point=false;
        for(;index<span.Length&&span[index] is not ('e' or 'E');index++)
        {
            var c=span[index];
            if(c=='.'&&!point){point=true;continue;}
            if(c is <'0' or >'9')throw new FormatException("Invalid decimal digit.");
            digits.Append(c);if(point)fractional++;
        }
        if(digits.Length==0)throw new FormatException("Decimal digits required.");
        var exponent=0;
        if(index<span.Length&&!int.TryParse(span[(index+1)..],NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture,out exponent))
            throw new FormatException("Invalid decimal exponent.");
        var scale=(long)fractional-exponent;
        if(scale is < -4096 or >4096)throw new ArgumentOutOfRangeException(nameof(text),"Decimal scale budget exceeded.");
        var coefficient=sign*BigInteger.Parse(digits.ToString(),CultureInfo.InvariantCulture);
        return FromDecimal(coefficient,(int)scale);
    }
    /// <summary>Creates and narrows a finite Java Double, including pinned long-boundary saturation.</summary>
    public static JavaNumber FromDouble(double value)
    {
        if(!double.IsFinite(value))throw new ArgumentOutOfRangeException(nameof(value));
        if(value==0)return Zero;
        // Use the independently reconciled Java binary64 text, not CLR's differing
        // shortest spelling at Java's historical boundary cases.
        var decimalValue=Decimal(JavaNumberText.Format(value));
        if(value==Math.Truncate(value))
        {
            // Java's narrowing uses a saturating long cast at its double boundary.
            if(value>=long.MinValue&&value<=9223372036854775808d)
                return Integer(value>=9223372036854775808d?long.MaxValue:(long)value);
            return Integer(decimalValue.Fraction.Numerator);
        }
        return new(decimalValue.Fraction,JavaNumberKind.Binary64,binary:value);
    }
    /// <summary>Implements RationalNumber.of(double) using Java's shortest decimal, before Number narrowing.</summary>
    /// <remarks>Primitive unit double overloads use this path. It differs from FromDouble at
    /// Java's signed-long narrowing boundary and does not mean the exact binary fraction.</remarks>
    public static JavaNumber FromDoubleAsRational(double value)
    {
        if(!double.IsFinite(value))throw new ArgumentOutOfRangeException(nameof(value));
        var fraction=Decimal(JavaNumberText.Format(value,narrowInteger:false)).Fraction;return Ratio(fraction.Numerator,fraction.Denominator);
    }
    /// <summary>Accepts built-in numeric values, BigInteger, Rational or an existing JavaNumber.</summary>
    /// <exception cref="ArgumentException">The object is not a supported numeric type.</exception>
    public static JavaNumber FromNumeric(object value)=>value switch
    {
        null=>throw new ArgumentNullException(nameof(value)),JavaNumber n=>n,BigInteger n=>Integer(n),Rational r=>Ratio(r.Numerator,r.Denominator),
        sbyte n=>Integer(n),byte n=>Integer(n),short n=>Integer(n),ushort n=>Integer(n),int n=>Integer(n),uint n=>Integer(n),
        long n=>Integer(n),ulong n=>Integer(n),decimal n=>Decimal(n.ToString(CultureInfo.InvariantCulture)),float n=>FromDouble(n),double n=>FromDouble(n),
        _=>throw new ArgumentException("A supported numeric value is required.",nameof(value))
    };
    /// <summary>Adds values with decimal promotion while preserving arbitrary integer magnitude.</summary>
    public JavaNumber Add(JavaNumber other)=>Operate(other,false);
    /// <summary>Explicitly reproduces pinned Indriya addition, including its lossy signed-long quirk.</summary>
    /// <remarks>Mixed Decimal/BigInteger addition can discard integer magnitude. Use only
    /// for deliberate reproduction of that implementation; Add is the safe default.</remarks>
    public JavaNumber AddPinnedIndriya(JavaNumber other)=>Operate(other,false,pinnedQuirks:true);
    /// <summary>Explicitly reproduces pinned Indriya subtraction, including signed-long narrowing.</summary>
    public JavaNumber SubtractPinnedIndriya(JavaNumber other){ArgumentNullException.ThrowIfNull(other);return AddPinnedIndriya(other.Negate());}
    /// <summary>Adds exact rational values without Java's decimal promotion or integer-narrowing quirk.</summary>
    public JavaNumber AddExact(JavaNumber other)
    {
        ArgumentNullException.ThrowIfNull(other);var result=Fraction+other.Fraction;return Ratio(result.Numerator,result.Denominator);
    }
    /// <summary>Subtracts values with the same promotion rules as addition.</summary>
    public JavaNumber Subtract(JavaNumber other){ArgumentNullException.ThrowIfNull(other);return Add(other.Negate());}
    /// <summary>Multiplies values; two fractional double operands retain binary64 multiplication.</summary>
    public JavaNumber Multiply(JavaNumber other)=>Operate(other,true);
    /// <summary>Divides by multiplying by the exact rational reciprocal.</summary>
    public JavaNumber Divide(JavaNumber other){ArgumentNullException.ThrowIfNull(other);return Multiply(other.Reciprocal());}
    /// <summary>Returns an exact reciprocal, rejecting zero.</summary>
    public JavaNumber Reciprocal()=>Ratio(Fraction.Denominator,Fraction.Numerator);
    /// <summary>Negates the retained numeric representation.</summary>
    public JavaNumber Negate()=>new(new Rational(-Fraction.Numerator,Fraction.Denominator),Kind,DecimalScale,-binary);
    private JavaNumber Operate(JavaNumber other,bool multiply,bool pinnedQuirks=false)
    {
        ArgumentNullException.ThrowIfNull(other);
        if(multiply&&(Fraction.Numerator.IsZero||other.Fraction.Numerator.IsZero))return Zero;
        if(!multiply&&Fraction.Numerator.IsZero)return other;
        if(!multiply&&other.Fraction.Numerator.IsZero)return this;
        if(Kind<=JavaNumberKind.Rational&&other.Kind<=JavaNumberKind.Rational)
        {
            var result=multiply?Fraction*other.Fraction:Fraction+other.Fraction;
            return Ratio(result.Numerator,result.Denominator);
        }
        if(multiply&&Kind==JavaNumberKind.Binary64&&other.Kind==JavaNumberKind.Binary64)
            return FromDouble(binary*other.binary);
        var a=AsDecimal();var b=other.AsDecimal();
        // DefaultNumberSystem.addWideAndNarrow uses narrow.longValue() in its
        // BigDecimal/integer branch. Keep this measured Java behavior confined
        // to the compatibility path, rather than corrupting native arithmetic.
        static Rational SignedLong(Rational value)
        {
            var bits=(ulong)(value.Numerator&((BigInteger.One<<64)-1));
            return new Rational(unchecked((long)bits),1);
        }
        if(pinnedQuirks&&!multiply&&Kind==JavaNumberKind.Decimal&&other.Kind==JavaNumberKind.Integer)b=(SignedLong(other.Fraction),0);
        if(pinnedQuirks&&!multiply&&other.Kind==JavaNumberKind.Decimal&&Kind==JavaNumberKind.Integer)a=(SignedLong(Fraction),0);
        var value=multiply?a.Fraction*b.Fraction:a.Fraction+b.Fraction;
        var scale=multiply?checked(a.Scale+b.Scale):Math.Max(a.Scale,b.Scale);
        // Java's decimal128 context is applied to decimal/decimal and
        // decimal/binary operations, and decimal * BigInteger. Other mixed
        // operations use exact decimal arithmetic after rational conversion.
        var round=Kind==JavaNumberKind.Decimal&&(other.Kind is JavaNumberKind.Decimal or JavaNumberKind.Binary64
                ||multiply&&other.Kind==JavaNumberKind.Integer&&BigInteger.Abs(other.Fraction.Numerator)>long.MaxValue)
            ||other.Kind==JavaNumberKind.Decimal&&(Kind is JavaNumberKind.Decimal or JavaNumberKind.Binary64
                ||multiply&&Kind==JavaNumberKind.Integer&&BigInteger.Abs(Fraction.Numerator)>long.MaxValue);
        if(round)return RoundedDecimal(value,scale);
        return new(value,JavaNumberKind.Decimal,scale);
    }
    private (Rational Fraction,int Scale) AsDecimal()
    {
        if(Kind==JavaNumberKind.Rational){var rounded=RoundedDecimal(Fraction,0);return (rounded.Fraction,rounded.DecimalScale);}
        if(Kind==JavaNumberKind.Binary64){var d=Decimal(JavaNumberText.Format(binary));return (d.Fraction,d.DecimalScale);}
        return (Fraction,DecimalScale);
    }
    internal static JavaNumber RoundedDecimal(Rational value,int preferredScale=0)
    {
        var parts=Decimal128Parts(value,preferredScale);return FromDecimal(parts.Coefficient,parts.Scale);
    }
    private static (BigInteger Coefficient,int Scale) Decimal128Parts(Rational value,int preferredScale=0)
    {
        if(value.Numerator.IsZero)return (0,0);
        var numerator=BigInteger.Abs(value.Numerator);var denominator=value.Denominator;
        var exponent=numerator.ToString(CultureInfo.InvariantCulture).Length-denominator.ToString(CultureInfo.InvariantCulture).Length;
        if(exponent>=0?numerator<denominator*BigInteger.Pow(10,exponent):numerator*BigInteger.Pow(10,-exponent)<denominator)exponent--;
        var scale=33-exponent;
        var n=scale>=0?numerator*BigInteger.Pow(10,scale):numerator;
        var d=scale>=0?denominator:denominator*BigInteger.Pow(10,-scale);
        var quotient=BigInteger.DivRem(n,d,out var remainder);var comparison=(remainder*2).CompareTo(d);
        if(comparison>0||comparison==0&&!quotient.IsEven)quotient++;
        // BigDecimal exact results keep the preferred scale when it fits the
        // context. Repeating fractions necessarily consume the precision budget.
        if(remainder.IsZero)while(scale>preferredScale&&quotient%10==0){quotient/=10;scale--;}
        return (value.Numerator.Sign*quotient,scale);
    }
    private static JavaNumber FromDecimal(BigInteger coefficient,int scale)
    {
        if(scale is < -8192 or >8192)throw new ArgumentOutOfRangeException(nameof(scale));
        var fraction=scale>=0?new Rational(coefficient,BigInteger.Pow(10,scale)):new Rational(coefficient*BigInteger.Pow(10,-scale),1);
        return new(fraction,JavaNumberKind.Decimal,scale);
    }
    private static void Check(Rational value)
    {
        if(value.Denominator.Sign<=0)throw new ArgumentException("A normalized rational is required.");
        if(value.Numerator.GetBitLength()>Budget||value.Denominator.GetBitLength()>Budget)
            throw new ArgumentOutOfRangeException(nameof(value),"Numeric bit budget exceeded.");
    }
    /// <summary>Projects to binary64; rational values first use Java's decimal128 projection.</summary>
    public double ToDouble()=>Kind==JavaNumberKind.Binary64?binary:JavaDoubleArithmetic.ToDouble(Kind==JavaNumberKind.Rational?RoundedDecimal(Fraction).Fraction:Fraction);
    /// <summary>Compares numeric values, independently of their representation.</summary>
    public int CompareTo(JavaNumber? other)
    {
        if(other is null)return 1;
        var a=Kind==JavaNumberKind.Rational&&other.Kind>=JavaNumberKind.Binary64?AsDecimal().Fraction:Fraction;
        var b=other.Kind==JavaNumberKind.Rational&&Kind>=JavaNumberKind.Binary64?other.AsDecimal().Fraction:other.Fraction;
        return (a.Numerator*b.Denominator).CompareTo(b.Numerator*a.Denominator);
    }
    /// <summary>Representation equality; decimal scale and numeric family matter.</summary>
    /// <remarks>Pinned RationalNumber.equals compares decimal128 projections even when exact
    /// rational comparison distinguishes the values. CLR hashes follow this equality to
    /// preserve dictionary invariants; Java's inconsistent exact-rational hash is not copied.</remarks>
    public bool Equals(JavaNumber? other)=>other is not null&&Kind==other.Kind&&(Kind==JavaNumberKind.Rational
        ?Decimal128Parts(Fraction)==Decimal128Parts(other.Fraction):Fraction==other.Fraction&&DecimalScale==other.DecimalScale);
    /// <inheritdoc/>
    public override bool Equals(object? obj)=>obj is JavaNumber other&&Equals(other);
    /// <inheritdoc/>
    public override int GetHashCode()=>Kind==JavaNumberKind.Rational?HashCode.Combine(Kind,Decimal128Parts(Fraction)):HashCode.Combine(Kind,Fraction,DecimalScale);
    /// <summary>Canonical exact fraction, suitable for diagnostic interchange.</summary>
    /// <remarks>This is not a replacement for Java's locale/quantity number formatting.</remarks>
    public override string ToString()=>Fraction.ToString();
}
