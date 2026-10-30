using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;

namespace SharpUnits;

/// <summary>Parses a bounded subset of Indriya's invariant quantity syntax.</summary>
/// <remarks>Accepts decimal/scientific literals or two signed integers joined by ÷, followed by
/// Java whitespace and registered symbols, Celsius aliases or bounded linear unit expressions.
/// Missing units mean dimensionless. Display values round once to double;
/// arbitrary Number identity is not retained.
/// Input is limited to 4096 characters and numeric tokens to 1024 characters to bound allocation.</remarks>
internal static partial class JavaQuantityTextParser
{
    // @relation(JP-014-DECIMAL, JP-014-RATIONAL, JP-014-INVALID, scope=file)
    internal static JavaQuantityAdapter Parse(string text,UnitRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(text);
        if(text.Length>4096)throw new FormatException("Quantity text exceeds 4096 characters.");
        var position=0;
        SkipWhitespace(text,ref position);
        var token=ReadLiteral(text,ref position);
        double magnitude;
        if(position<text.Length&&text[position]=='÷')
        {
            position++;
            SkipWhitespace(text,ref position);
            var numerator=token;var denominator=ReadLiteral(text,ref position);
            if(numerator.Length+denominator.Length+1>1024)
                throw new FormatException("Rational numeric tokens exceed 1024 characters.");
            if(!Integer().IsMatch(numerator)||!Integer().IsMatch(denominator))
                throw new FormatException("Rational quantities require two signed integers.");
            var fraction=new Rational(BigInteger.Parse(numerator,CultureInfo.InvariantCulture),
                BigInteger.Parse(denominator,CultureInfo.InvariantCulture));
            magnitude=JavaDoubleArithmetic.ToDouble(fraction);
        }
        else
        {
            if(!Decimal().IsMatch(token)||!double.TryParse(token,NumberStyles.Float,CultureInfo.InvariantCulture,out magnitude))
                throw new FormatException("Invalid invariant decimal quantity.");
        }
        if(!double.IsFinite(magnitude))throw new FormatException("Display magnitude must be finite.");
        SkipWhitespace(text,ref position);
        var end=text.Length;
        while(end>position&&IsJavaWhitespace(text[end-1]))end--;
        var symbol=text[position..end];
        var unit=symbol.Length==0?new UnitDefinition("1",Dimension.None,UnitConversion.Linear(1)):JavaUnitTextParser.Resolve(symbol,registry);
        return new JavaQuantityAdapter(magnitude,unit);
    }

    // Match RationalNumberScanner: a division sign ends the numerator; Java whitespace
    // may precede the next literal after that sign, but not precede the division sign itself.
    private static string ReadLiteral(string text,ref int position)
    {
        var start=position;
        while(position<text.Length&&!IsJavaWhitespace(text[position])&&text[position]!='÷')position++;
        var token=text[start..position];
        if(token.Length==0||token.Length>1024)throw new FormatException("Invalid numeric token length.");
        return token;
    }

    private static void SkipWhitespace(string text,ref int position)
    {
        while(position<text.Length&&IsJavaWhitespace(text[position]))position++;
    }

    // Java Character.isWhitespace excludes nonbreaking spaces, and includes the four
    // information separators. CLR char.IsWhiteSpace has a different set (notably U+0085).
    internal static bool IsJavaWhitespace(char value)=>value is >= '\t' and <= '\r' or >= '\u001c' and <= '\u001f'
        ||value is not ('\u00a0' or '\u2007' or '\u202f')&&char.GetUnicodeCategory(value) is
            UnicodeCategory.SpaceSeparator or UnicodeCategory.LineSeparator or UnicodeCategory.ParagraphSeparator;

    [GeneratedRegex(@"\A[+-]?[0-9]+\z",RegexOptions.CultureInvariant|RegexOptions.NonBacktracking)]
    private static partial Regex Integer();
    [GeneratedRegex(@"\A[+-]?(?:[0-9]+(?:\.[0-9]*)?|\.[0-9]+)(?:[eE][+-]?[0-9]+)?\z",RegexOptions.CultureInvariant|RegexOptions.NonBacktracking)]
    private static partial Regex Decimal();
}
