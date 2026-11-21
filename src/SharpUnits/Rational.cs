using System.Numerics;

namespace SharpUnits;

/// <summary>Normalized, allocation-free exact rational number for dimensional exponents.</summary>
/// <remarks>The default value of this struct is canonical zero (<c>0/1</c>).</remarks>
public readonly record struct Rational : IComparable<Rational>
{
    private readonly long numerator;
    private readonly long denominatorMinusOne;

    /// <summary>Canonical zero (<c>0/1</c>).</summary>
    public static Rational Zero => default;

    /// <summary>Canonical one (<c>1/1</c>).</summary>
    public static Rational One => new(1, 1);

    /// <summary>Numerator in lowest terms.</summary>
    public long Numerator => numerator;

    /// <summary>Positive denominator in lowest terms.</summary>
    public long Denominator => denominatorMinusOne + 1;

    /// <summary>True when the rational value is zero.</summary>
    public bool IsZero => numerator == 0;

    /// <summary>True when the rational value is one.</summary>
    public bool IsOne => numerator == 1 && denominatorMinusOne == 0;

    /// <summary>True when the rational value is an exact integer.</summary>
    public bool IsInteger => denominatorMinusOne == 0;

    /// <summary>Constructs a reduced rational; rejects zero denominators.</summary>
    public Rational(long numerator, long denominator)
    {
        if (denominator == 0) throw new DivideByZeroException("Rational denominator cannot be zero.");
        if (numerator == 0)
        {
            this.numerator = 0;
            denominatorMinusOne = 0;
            return;
        }
        if (denominator < 0)
        {
            numerator = checked(-numerator);
            denominator = checked(-denominator);
        }
        ulong absNum = numerator < 0 ? unchecked((ulong)(-numerator)) : (ulong)numerator;
        ulong gcd = Gcd(absNum, (ulong)denominator);
        this.numerator = numerator / (long)gcd;
        denominatorMinusOne = (denominator / (long)gcd) - 1;
    }

    /// <summary>Converts an exact rational value to double precision.</summary>
    public double ToDouble() => (double)Numerator / Denominator;

    /// <summary>Converts an integer exactly.</summary>
    public static implicit operator Rational(int value) => new(value, 1);

    /// <summary>Converts a 64-bit integer exactly.</summary>
    public static implicit operator Rational(long value) => new(value, 1);

    /// <summary>Unary negation.</summary>
    public static Rational operator -(Rational a) => new(checked(-a.Numerator), a.Denominator);

    /// <summary>Unary plus.</summary>
    public static Rational operator +(Rational a) => a;

    /// <summary>Adds exact fractions.</summary>
    public static Rational operator +(Rational a, Rational b)
    {
        if (a.IsZero) return b;
        if (b.IsZero) return a;
        if (a.Denominator == b.Denominator) return new(checked(a.Numerator + b.Numerator), a.Denominator);
        long gcd = (long)Gcd((ulong)a.Denominator, (ulong)b.Denominator);
        long aScale = b.Denominator / gcd;
        long bScale = a.Denominator / gcd;
        return new(checked(a.Numerator * aScale + b.Numerator * bScale), checked(a.Denominator * aScale));
    }

    /// <summary>Subtracts exact fractions.</summary>
    public static Rational operator -(Rational a, Rational b) => a + (-b);

    /// <summary>Multiplies exact fractions.</summary>
    public static Rational operator *(Rational a, Rational b)
    {
        if (a.IsZero || b.IsZero) return Zero;
        ulong g1 = Gcd(AbsUlong(a.Numerator), (ulong)b.Denominator);
        ulong g2 = Gcd(AbsUlong(b.Numerator), (ulong)a.Denominator);
        long num = checked((a.Numerator / (long)g1) * (b.Numerator / (long)g2));
        long den = checked((a.Denominator / (long)g2) * (b.Denominator / (long)g1));
        return new(num, den);
    }

    /// <summary>Divides exact fractions.</summary>
    public static Rational operator /(Rational a, Rational b)
    {
        if (b.IsZero) throw new DivideByZeroException("Cannot divide by zero rational.");
        return a * new Rational(b.Denominator, b.Numerator);
    }

    /// <summary>Compares two rationals exactly.</summary>
    public int CompareTo(Rational other)
    {
        Int128 lhs = (Int128)Numerator * other.Denominator;
        Int128 rhs = (Int128)other.Numerator * Denominator;
        return lhs.CompareTo(rhs);
    }

    /// <summary>Less-than comparison.</summary>
    public static bool operator <(Rational a, Rational b) => a.CompareTo(b) < 0;

    /// <summary>Less-than-or-equal comparison.</summary>
    public static bool operator <=(Rational a, Rational b) => a.CompareTo(b) <= 0;

    /// <summary>Greater-than comparison.</summary>
    public static bool operator >(Rational a, Rational b) => a.CompareTo(b) > 0;

    /// <summary>Greater-than-or-equal comparison.</summary>
    public static bool operator >=(Rational a, Rational b) => a.CompareTo(b) >= 0;

    /// <summary>Canonical fractional representation.</summary>
    public override string ToString() => IsInteger ? Numerator.ToString() : $"{Numerator}/{Denominator}";

    private static ulong AbsUlong(long value) => value < 0 ? unchecked((ulong)(-value)) : (ulong)value;

    private static ulong Gcd(ulong a, ulong b)
    {
        while (b != 0)
        {
            ulong t = a % b;
            a = b;
            b = t;
        }
        return a == 0 ? 1 : a;
    }
}