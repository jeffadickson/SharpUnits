using System.Globalization;
using System.Numerics;

namespace SharpUnits;

/// <summary>Decimal text for the supported pinned-Java finite-double formatting profile.</summary>
internal static class JavaNumberText
{
    // @relation(JP-015-JAVA-DOUBLE, scope=file)
    internal static string Format(double value,bool narrowInteger=true)
    {
        if(!double.IsFinite(value))throw new ArgumentOutOfRangeException(nameof(value));
        if(value==0)return "0";
        const double longUpper=9223372036854775808d;
        if(narrowInteger&&value==Math.Truncate(value)&&value>=long.MinValue&&value<=longUpper)
        {
            // DefaultNumberSystem narrows through Java's saturating double-to-long cast.
            var integer=value>=longUpper?long.MaxValue:(long)value;
            return integer.ToString(CultureInfo.InvariantCulture);
        }
        var negative=value<0;
        var (coefficient,power)=Decimal(Math.Abs(value));
        var digits=coefficient.ToString(CultureInfo.InvariantCulture);
        var sign=negative?"-":"";
        if(value==Math.Truncate(value))return sign+digits+new string('0',power);
        var exponent=digits.Length-1+power;
        if(exponent is < -3 or >=7)
            return sign+digits[0]+"."+(digits.Length==1?"0":digits[1..])+"E"+exponent.ToString(CultureInfo.InvariantCulture);
        var point=digits.Length+power;
        if(point<=0)return sign+"0."+new string('0',-point)+digits;
        if(point>=digits.Length)return sign+digits+new string('0',point-digits.Length);
        return sign+digits[..point]+"."+digits[point..];
    }

    private static (BigInteger Coefficient,int Power) Decimal(double value)
    {
        // Work with exact binary values and strict rounding intervals, not the CLR's
        // round-trip decimal spelling. Java 17 excludes midpoint boundary spellings.
        var exact=Binary(value);var previous=Binary(Math.BitDecrement(value));
        var next=Math.BitIncrement(value);
        var lower=(exact+previous)/2;
        var upper=double.IsInfinity(next)?exact+(exact-previous)/2:(exact+Binary(next))/2;
        var exponent=(int)Math.Floor(Math.Log10(value));
        while(Compare(exact,Ten(exponent))<0)exponent--;
        while(Compare(exact,Ten(exponent+1))>=0)exponent++;
        // Compatible Java scientific output keeps a power-of-ten spelling when it
        // lies inside the interval (notably 2*Double.MIN_VALUE -> 1.0E-323).
        foreach(var candidateExponent in new[]{exponent,exponent+1})
        {
            var candidate=Ten(candidateExponent);
            if(Compare(candidate,lower)>0&&Compare(candidate,upper)<0)return (BigInteger.One,candidateExponent);
        }
        var first=exponent is < -3 or >=7?2:1;
        for(var precision=first;precision<=18;precision++)
        {
            var power=exponent-precision+1;var quantum=Ten(power);
            var nearest=Round(exact/quantum);
            BigInteger? selected=null;Rational best=default;
            foreach(var coefficient in new[]{nearest-1,nearest,nearest+1})
            {
                if(coefficient<=0)continue;
                var candidate=new Rational(coefficient,1)*quantum;
                if(Compare(candidate,lower)<=0||Compare(candidate,upper)>=0)continue;
                var distance=candidate-exact;
                if(distance.Numerator.Sign<0)distance=new Rational(-distance.Numerator,distance.Denominator);
                if(selected is null||Compare(distance,best)<0||Compare(distance,best)==0&&coefficient.IsEven)
                {selected=coefficient;best=distance;}
            }
            if(selected is null)continue;
            var result=selected.Value;
            while(result%10==0){result/=10;power++;}
            return (result,power);
        }
        throw new InvalidOperationException("No finite double decimal representation found.");
    }

    private static BigInteger Round(Rational value)
    {
        var quotient=BigInteger.DivRem(value.Numerator,value.Denominator,out var remainder);
        var half=(remainder*2).CompareTo(value.Denominator);
        return half>0||half==0&&!quotient.IsEven?quotient+1:quotient;
    }
    private static int Compare(Rational left,Rational right)=>(left.Numerator*right.Denominator).CompareTo(right.Numerator*left.Denominator);
    private static Rational Ten(int exponent)=>exponent>=0
        ?new(BigInteger.Pow(10,exponent),1):new(1,BigInteger.Pow(10,-exponent));
    private static Rational Binary(double value)
    {
        var bits=(ulong)BitConverter.DoubleToInt64Bits(value);
        var exponent=(int)(bits>>52)&0x7ff;var fraction=bits&0x000fffffffffffffUL;
        var mantissa=new BigInteger(exponent==0?fraction:fraction|(1UL<<52));
        var power=exponent==0?-1074:exponent-1075;
        return power>=0?new Rational(mantissa<<power,1):new Rational(mantissa,BigInteger.One<<-power);
    }
}
