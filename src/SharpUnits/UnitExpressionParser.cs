using System.Globalization;

namespace SharpUnits;

/// <summary>Thread-safe parser for multiplicative unit expressions with dynamic SI prefixes, parentheses, Unicode superscripts, and integer or rational exponents.</summary>
/// <remarks>Examples: <c>mA*kΩ</c>, <c>m/s^2</c>, <c>m·s⁻²</c>, <c>V/Hz^(1/2)</c>, <c>(A·Ω)</c>. Affine units are rejected.</remarks>
public sealed class UnitExpressionParser
{
    private readonly UnitRegistry registry;

    /// <summary>Constructs a parser against an explicit registry (defaults to <see cref="UnitRegistry.Standard"/>).</summary>
    public UnitExpressionParser(UnitRegistry? registry = null) =>
        this.registry = registry ?? UnitRegistry.Standard;

    /// <summary>Parses a complete unit expression into a <see cref="RationalUnitExpression"/>, rejecting trailing characters.</summary>
    public RationalUnitExpression Parse(string text)
    {
        var symbolic = ParseSymbolic(text);
        return new RationalUnitExpression(symbolic.Dimension, symbolic.ScaleToSi);
    }

    /// <summary>Parses a complete unit expression into an algebraic <see cref="SymbolicUnit"/> preserving unit symbols and factors.</summary>
    public SymbolicUnit ParseSymbolic(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var state = new ParserState(registry, text);
        var result = state.ParseProduct();
        state.SkipSpace();
        if (state.Position != text.Length)
            throw new FormatException($"Unexpected character '{text[state.Position]}' at position {state.Position}.");
        return result;
    }

    private ref struct ParserState
    {
        private readonly UnitRegistry registry;
        private readonly string input;
        public int Position;

        public ParserState(UnitRegistry registry, string input)
        {
            this.registry = registry;
            this.input = input;
            Position = 0;
        }

        public SymbolicUnit ParseProduct()
        {
            var result = ParseFactor();
            while (true)
            {
                SkipSpace();
                if (Position >= input.Length || input[Position] == ')') return result;
                char op = input[Position];
                if (op != '*' && op != '/' && op != '·') return result;
                Position++;
                var rhs = ParseFactor();
                result = op == '/' ? result / rhs : result * rhs;
            }
        }

        private SymbolicUnit ParseFactor()
        {
            SkipSpace();
            SymbolicUnit value;
            if (Position < input.Length && input[Position] == '(')
            {
                Position++;
                value = ParseProduct();
                SkipSpace();
                if (Position >= input.Length || input[Position] != ')')
                    throw new FormatException("Missing closing parenthesis.");
                Position++;
            }
            else
            {
                int start = Position;
                while (Position < input.Length && !IsDelimiter(input[Position]) && !IsSuperscript(input[Position]))
                    Position++;
                if (Position == start) throw new FormatException("Expected a unit symbol.");
                string token = input[start..Position];
                if (token == "1")
                {
                    value = SymbolicUnit.One;
                }
                else
                {
                    var resolved = registry.Resolve(token);
                    if (resolved.IsAffine)
                        throw new InvalidOperationException("Affine units cannot participate in unit products.");
                    value = SymbolicUnit.From(resolved);
                }
            }

            SkipSpace();
            if (Position < input.Length && input[Position] == '^')
            {
                Position++;
                SkipSpace();
                var exponent = ParseExponent();
                value = value.Pow(exponent);
            }
            else if (Position < input.Length && IsSuperscript(input[Position]))
            {
                int power = ParseSuperscriptInteger();
                value = value.Pow(power);
            }
            return value;
        }

        private Rational ParseExponent()
        {
            if (Position < input.Length && input[Position] == '(')
            {
                Position++;
                SkipSpace();
                long num = ParseSignedLong();
                SkipSpace();
                long den = 1;
                if (Position < input.Length && input[Position] == '/')
                {
                    Position++;
                    SkipSpace();
                    den = ParseSignedLong();
                    SkipSpace();
                }
                if (Position >= input.Length || input[Position] != ')')
                    throw new FormatException("Missing closing parenthesis in unit exponent.");
                Position++;
                return new Rational(num, den);
            }
            return new Rational(ParseSignedLong(), 1);
        }

        private long ParseSignedLong()
        {
            int start = Position;
            if (Position < input.Length && (input[Position] == '-' || input[Position] == '+')) Position++;
            while (Position < input.Length && char.IsAsciiDigit(input[Position])) Position++;
            if (!long.TryParse(input[start..Position], NumberStyles.Integer, CultureInfo.InvariantCulture, out long value))
                throw new FormatException("Invalid integer unit exponent.");
            return value;
        }

        private int ParseSuperscriptInteger()
        {
            bool negative = false;
            if (input[Position] == '⁻') { negative = true; Position++; }
            else if (input[Position] == '⁺') { Position++; }

            int digits = 0;
            int value = 0;
            while (Position < input.Length && TryDecodeSuperscriptDigit(input[Position], out int digit))
            {
                value = checked(value * 10 + digit);
                digits++;
                Position++;
            }
            if (digits == 0) throw new FormatException("Invalid superscript exponent.");
            return negative ? -value : value;
        }

        public void SkipSpace()
        {
            while (Position < input.Length && char.IsWhiteSpace(input[Position])) Position++;
        }

        private static bool IsDelimiter(char c) =>
            char.IsWhiteSpace(c) || c is '*' or '/' or '·' or '^' or '(' or ')';

        private static bool IsSuperscript(char c) =>
            c is '⁰' or '¹' or '²' or '³' or '⁴' or '⁵' or '⁶' or '⁷' or '⁸' or '⁹' or '⁺' or '⁻';

        private static bool TryDecodeSuperscriptDigit(char c, out int digit)
        {
            digit = c switch
            {
                '⁰' => 0,
                '¹' => 1,
                '²' => 2,
                '³' => 3,
                '⁴' => 4,
                '⁵' => 5,
                '⁶' => 6,
                '⁷' => 7,
                '⁸' => 8,
                '⁹' => 9,
                _ => -1
            };
            return digit >= 0;
        }
    }
}
