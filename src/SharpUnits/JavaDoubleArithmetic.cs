using System.Globalization;
using System.Numerics;

namespace SharpUnits;

/// <summary>Finite display-double arithmetic through decimal representations and rational converter evaluation.</summary>
/// <remarks>Mirrors common Indriya decimal promotion for addition without introducing a public Number model.
/// Java MathContext rounding, numeric type identity and converter rational coefficients remain separate concerns.</remarks>
internal static class JavaDoubleArithmetic
{
    internal static double Reciprocal(double magnitude)=>ToDouble(new Rational(1,1)/Fraction(magnitude));
    internal static double Scale(double magnitude,double factor,UnitDefinition unit,bool relative,bool divide)
    {
        var converter=unit.Converter;
        var affine=!relative&&!converter.IsLinear;
        var value=Fraction(magnitude);
        if(affine)value=value*ScaleOf(unit)+OffsetOf(converter);
        value=divide?value/Fraction(factor):value*Fraction(factor);
        if(affine)value=(value-OffsetOf(converter))/ScaleOf(unit);
        return ToDouble(value);
    }
    internal static double Convert(double magnitude,UnitDefinition source,UnitDefinition target,bool relative)
    {
        var coherent=Fraction(magnitude)*ScaleOf(source);
        if(!relative)coherent=coherent+OffsetOf(source.Converter)-OffsetOf(target.Converter);
        return ToDouble(coherent/ScaleOf(target));
    }
    internal static double ToCoherent(double magnitude,UnitDefinition unit,bool relative)
        =>ToDouble(Fraction(magnitude)*ScaleOf(unit)+(relative?new Rational(0,1):OffsetOf(unit.Converter)));
    internal static Rational ScaleOf(UnitDefinition unit)=>unit.ExactLinearScale??ScaleOf(unit.Converter);
    // Recognize the standard Fahrenheit converter's definition, rather than treating
    // its public binary64 coefficients as exact rational metadata. Other user-defined
    // converters retain the supplied round-trip decimal coefficients.
    private static Rational ScaleOf(UnitConversion converter)=>converter==UnitConversion.Fahrenheit
        ?new Rational(5,9):Fraction(converter.Scale);
    private static Rational OffsetOf(UnitConversion converter)=>converter==UnitConversion.Fahrenheit
        ?new Rational(45967,180):Fraction(converter.Offset);

    internal static Rational Fraction(double value)
    {
        var parts=Parts(value);
        return parts.Exponent>=0
            ?new(parts.Digits*BigInteger.Pow(10,parts.Exponent),BigInteger.One)
            :new(parts.Digits,BigInteger.Pow(10,-parts.Exponent));
    }

    // Round the final rational once to nearest binary64, ties to even. Avoid casting
    // numerator and denominator separately: either may exceed binary64's range.
    internal static double ToDouble(Rational value)
    {
        if(value.Numerator.IsZero)return 0;
        var negative=value.Numerator.Sign<0;
        var numerator=BigInteger.Abs(value.Numerator);var denominator=value.Denominator;
        var exponent=(int)(numerator.GetBitLength()-denominator.GetBitLength());
        if(exponent>=0 ? numerator<(denominator<<exponent) : (numerator<<-exponent)<denominator)exponent--;
        if(exponent>1023)return negative?double.NegativeInfinity:double.PositiveInfinity;
        var shift=exponent< -1022?1074:52-exponent;
        var scaledNumerator=shift>=0?numerator<<shift:numerator;
        var scaledDenominator=shift>=0?denominator:denominator<<-shift;
        var quotient=BigInteger.DivRem(scaledNumerator,scaledDenominator,out var remainder);
        var half=(remainder*2).CompareTo(scaledDenominator);
        if(half>0||(half==0&&!quotient.IsEven))quotient++;
        var result=Math.ScaleB((double)quotient,-shift);
        return negative?-result:result;
    }

    internal static double Add(double left,double right,UnitDefinition source,UnitDefinition other,
        bool leftAbsolute,bool rightAbsolute,bool resultAbsolute,bool subtract)
    {
        var a=Fraction(left)*ScaleOf(source);
        var b=Fraction(right)*ScaleOf(other);
        if(leftAbsolute)a+=OffsetOf(source.Converter);
        if(rightAbsolute)b+=OffsetOf(other.Converter);
        var result=subtract?a-b:a+b;
        if(resultAbsolute)result-=OffsetOf(source.Converter);
        return ToDouble(result/ScaleOf(source));
    }

    private static (BigInteger Digits,int Exponent) Parts(double value)
    {
        var text=value.ToString("R",CultureInfo.InvariantCulture);
        var e=text.IndexOf('E');
        var exponent=e<0?0:int.Parse(text[(e+1)..],CultureInfo.InvariantCulture);
        var mantissa=e<0?text:text[..e];
        var point=mantissa.IndexOf('.');
        if(point>=0)exponent-=mantissa.Length-point-1;
        return (BigInteger.Parse(mantissa.Replace(".",""),CultureInfo.InvariantCulture),exponent);
    }
}
