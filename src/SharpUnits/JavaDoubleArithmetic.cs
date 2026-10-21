using System.Globalization;
using System.Numerics;

namespace SharpUnits;

/// <summary>Finite display-double addition through round-trip decimal representations.</summary>
/// <remarks>Mirrors common Indriya decimal promotion for addition without introducing a public Number model.
/// Java MathContext rounding, numeric type identity and converter rational coefficients remain separate concerns.</remarks>
internal static class JavaDoubleArithmetic
{
    internal static double Add(double left,double right,double offset)
    {
        if(!double.IsFinite(left)||!double.IsFinite(right)||!double.IsFinite(offset))return left+right+offset;
        var a=Parts(left);var b=Parts(right);var c=Parts(offset);
        var exponent=Math.Min(a.Exponent,Math.Min(b.Exponent,c.Exponent));
        var sum=a.Digits*BigInteger.Pow(10,a.Exponent-exponent)
            +b.Digits*BigInteger.Pow(10,b.Exponent-exponent)
            +c.Digits*BigInteger.Pow(10,c.Exponent-exponent);
        return double.Parse(sum.ToString(CultureInfo.InvariantCulture)+"E"+exponent.ToString(CultureInfo.InvariantCulture),
            NumberStyles.Float,CultureInfo.InvariantCulture);
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
