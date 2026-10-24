namespace SharpUnits;

/// <summary>Parses bounded Java-style linear unit expressions without changing the caller's registry.</summary>
internal static class JavaUnitTextParser
{
    // @relation(JP-014-ALIAS, JP-014-COMPOUND, JP-014-POWERS, JP-014-ROOTS, JP-014-INTEGER-FACTOR, JP-014-PREFIXES, scope=file)
    internal static UnitDefinition Resolve(string symbol,UnitRegistry registry)
    {
        try{return ResolveSimple(symbol,registry);}
        catch(KeyNotFoundException) when(symbol.IndexOfAny(['*','/','·','(',')','^','¹','²','³'])>=0)
        {
            // Exact registrations take priority. Composite metadata retains the input expression;
            // dimensional compatibility does not imply equality with a named alternate Java unit.
            try
            {
                var expression=new ExpressionReader(symbol,registry).Read();
                return UnitDefinition.FromExactLinear(symbol,expression.Dimension,expression.Scale);
            }
            catch(Exception error) when(error is InvalidOperationException or ArgumentOutOfRangeException or OverflowException)
            {
                throw new FormatException("Compound units require finite nonzero linear scale and bounded powers.",error);
            }
        }
    }

    private static UnitDefinition ResolveSimple(string symbol,UnitRegistry registry)
    {
        try{return registry.Resolve(symbol);}
        catch(KeyNotFoundException)
        {
            // An exact caller registration wins over this compatibility alias.
            if(symbol=="℃")return registry.Resolve("°C");
            if(symbol=="1")return new UnitDefinition("1",Dimension.None,UnitConversion.Linear(1));
            var normalized=symbol.Length>0&&symbol[0]=='μ'?"µ"+symbol[1..]:symbol;
            foreach(var entry in SiPrefixes.Definitions.OrderByDescending(pair=>pair.Key.Length))
            {
                if(!normalized.StartsWith(entry.Key,StringComparison.Ordinal)||normalized.Length==entry.Key.Length)continue;
                UnitDefinition basis;
                try{basis=registry.Resolve(normalized[entry.Key.Length..]);}
                catch(KeyNotFoundException){continue;}
                // Default Java labels contain a single prefix on coherent units, not nested
                // prefixes on mA/kΩ. Affine prefixes remain a separate compatibility gap.
                if(!basis.Converter.IsIdentity||basis.ExactLinearScale is not null)continue;
                var power=System.Numerics.BigInteger.Pow(10,Math.Abs(entry.Value));
                var factor=entry.Value>=0?new Rational(power,1):new Rational(1,power);
                return UnitDefinition.FromExactLinear(normalized,basis.RationalDimension,factor);
            }
            throw;
        }
    }

    // Exact linear factors prevent A/3 from becoming a rounded decimal coefficient before
    // quantity conversion. Roots deliberately retain the reference's floating scale evaluation.
    private readonly record struct LinearUnit(RationalDimension Dimension,Rational Scale)
    {
        internal static LinearUnit Create(RationalDimension dimension,Rational scale)
        {
            if(Math.Max(System.Numerics.BigInteger.Abs(scale.Numerator).GetBitLength(),scale.Denominator.GetBitLength())>16384)
                throw new FormatException("Exact unit factor exceeds the 16384-bit resource budget.");
            var projection=JavaDoubleArithmetic.ToDouble(scale);
            if(!double.IsFinite(projection)||projection==0)throw new FormatException("Unit factor must have a finite nonzero double projection.");
            return new(dimension,scale);
        }
        internal static LinearUnit From(UnitDefinition unit)
        {
            if(!unit.Converter.IsLinear)throw new InvalidOperationException("Affine units cannot participate in unit products.");
            return Create(unit.RationalDimension,JavaDoubleArithmetic.ScaleOf(unit));
        }
        internal LinearUnit Pow(int power)
        {
            var magnitude=Math.Abs(power);
            if(Math.Max(System.Numerics.BigInteger.Abs(Scale.Numerator).GetBitLength(),Scale.Denominator.GetBitLength())*magnitude>16384)
                throw new FormatException("Exact unit power exceeds the resource budget.");
            var value=new Rational(System.Numerics.BigInteger.Pow(Scale.Numerator,magnitude),System.Numerics.BigInteger.Pow(Scale.Denominator,magnitude));
            if(power<0)value=new Rational(1,1)/value;
            return Create(Dimension.Pow(power),value);
        }
        internal LinearUnit Root(int degree)
        {
            var projection=Math.Pow(JavaDoubleArithmetic.ToDouble(Scale),1d/degree);
            if(!double.IsFinite(projection)||projection==0)throw new FormatException("Unit root has no finite nonzero real scale.");
            return Create(Dimension.Root(degree),JavaDoubleArithmetic.Fraction(projection));
        }
        public static LinearUnit operator *(LinearUnit left,LinearUnit right)=>Create(left.Dimension+right.Dimension,left.Scale*right.Scale);
        public static LinearUnit operator /(LinearUnit left,LinearUnit right)=>Create(left.Dimension-right.Dimension,left.Scale/right.Scale);
    }

    private sealed class ExpressionReader(string text,UnitRegistry registry)
    {
        private int position;
        internal LinearUnit Read()
        {
            var expression=Expression(0);
            SkipWhitespace();
            if(position!=text.Length)throw new FormatException("Unexpected unit-expression suffix.");
            return expression;
        }

        private LinearUnit Expression(int depth)
        {
            if(depth>64)throw new FormatException("Unit expressions exceed 64 nested groups/operators.");
            var left=Factor(depth);
            while(true)
            {
                SkipWhitespace();
                if(position==text.Length||text[position]==')')return left;
                var operation=text[position++];
                if(operation is not ('*' or '/' or '·'))throw new FormatException("Expected a unit operator.");
                SkipWhitespace();
                if(position<text.Length&&(char.IsAsciiDigit(text[position])||text[position]=='-'))
                {
                    // Java consumes an immediate integer factor before resuming its operator loop.
                    // Treating this as a recursively parsed unit gives wrong results for A/2/Ω.
                    var factor=ReadIntegerFactor();
                    var scalar=LinearUnit.Create(RationalDimension.From(Dimension.None),new Rational(factor,1));
                    left=operation=='/'?left/scalar:left*scalar;
                    continue;
                }
                // Unit operands, in contrast, recurse over the entire right side.
                var right=Expression(depth+1);
                return operation=='/'?left/right:left*right;
            }
        }

        private LinearUnit Factor(int depth)
        {
            SkipWhitespace();
            LinearUnit result;
            if(position<text.Length&&text[position]=='(')
            {
                position++;
                result=Expression(depth+1);
                SkipWhitespace();
                if(position==text.Length||text[position]!=')')throw new FormatException("Missing closing parenthesis.");
                position++;
            }
            else
            {
                var start=position;
                while(position<text.Length&&!JavaQuantityTextParser.IsJavaWhitespace(text[position])
                    &&text[position] is not ('*' or '/' or '·' or '(' or ')' or '^' or '¹' or '²' or '³'))position++;
                if(position==start)throw new FormatException("Expected a registered unit symbol.");
                result=LinearUnit.From(ResolveSimple(text[start..position],registry));
            }
            SkipWhitespace();
            if(position<text.Length&&(text[position] is '^' or '¹' or '²' or '³'
                ||text[position]=='*'&&position+1<text.Length&&text[position+1]=='*'))
            {
                if(text[position]=='^')position++;
                else if(text[position]=='*')position+=2;
                var power=ReadExponentInteger();
                result=result.Pow(power);
                if(position<text.Length&&text[position]==':')
                {
                    position++;
                    var root=ReadExponentInteger();
                    // Java applies pow and root sequentially; negative roots invert the
                    // positive root. Combining the floating exponents can change rounding.
                    result=result.Root(Math.Abs(root));
                    if(root<0)result=LinearUnit.Create(RationalDimension.From(Dimension.None),new Rational(1,1))/result;
                }
            }
            return result;
        }

        private int ReadIntegerFactor()
        {
            var start=position;
            if(text[position]=='-')position++;
            while(position<text.Length&&char.IsAsciiDigit(text[position]))position++;
            if(!int.TryParse(text[start..position],System.Globalization.NumberStyles.AllowLeadingSign,
                System.Globalization.CultureInfo.InvariantCulture,out var factor)||factor==0)
                throw new FormatException("Unit factors require nonzero signed 32-bit integers.");
            return factor;
        }

        private int ReadExponentInteger()
        {
            var negative=position<text.Length&&text[position]=='-';
            if(negative)position++;
            var magnitude=0;
            while(position<text.Length)
            {
                var digit=text[position] switch {'¹'=>1,'²'=>2,'³'=>3,>= '0' and <= '9'=>text[position]-'0',_=>-1};
                if(digit<0)break;
                magnitude=magnitude*10+digit;
                if(magnitude>1024)throw new FormatException("Unit exponent magnitude exceeds 1024.");
                position++;
            }
            // Pinned SimpleUnitFormat.readExponent uses zero as its default-one sentinel,
            // for missing digits as well as an explicit zero literal. Unrecognized suffixes
            // are still rejected by the expression reader. Native Pow(0) is unchanged.
            if(magnitude==0)magnitude=1;
            return negative?-magnitude:magnitude;
        }

        private void SkipWhitespace()
        {
            while(position<text.Length&&JavaQuantityTextParser.IsJavaWhitespace(text[position]))position++;
        }
    }
}
