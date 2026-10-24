namespace SharpUnits;

/// <summary>Parses bounded Java-style linear unit expressions without changing the caller's registry.</summary>
internal static class JavaUnitTextParser
{
    // @relation(JP-014-ALIAS, JP-014-COMPOUND, scope=file)
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
                return new UnitDefinition(symbol,expression.Dimension,UnitConversion.Linear(expression.Scale));
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
            throw;
        }
    }

    private sealed class ExpressionReader(string text,UnitRegistry registry)
    {
        private int position;
        internal RationalUnitExpression Read()
        {
            var expression=Expression(0);
            SkipWhitespace();
            if(position!=text.Length)throw new FormatException("Unexpected unit-expression suffix.");
            return expression;
        }

        private RationalUnitExpression Expression(int depth)
        {
            if(depth>64)throw new FormatException("Unit expressions exceed 64 nested groups/operators.");
            var left=Factor(depth);
            SkipWhitespace();
            if(position==text.Length||text[position]==')')return left;
            var operation=text[position++];
            if(operation is not ('*' or '/' or '·'))throw new FormatException("Expected a unit operator.");
            // Pinned SimpleUnitFormat recurses on the entire right operand. Do not substitute
            // the legacy UnitExpressionParser's left-associative division convention here.
            var right=Expression(depth+1);
            return operation=='/'?left/right:left*right;
        }

        private RationalUnitExpression Factor(int depth)
        {
            SkipWhitespace();
            RationalUnitExpression result;
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
                result=RationalUnitExpression.From(ResolveSimple(text[start..position],registry));
            }
            SkipWhitespace();
            if(position<text.Length&&(text[position] is '^' or '¹' or '²' or '³'
                ||text[position]=='*'&&position+1<text.Length&&text[position+1]=='*'))
            {
                if(text[position]=='^')position++;
                else if(text[position]=='*')position+=2;
                var power=ReadExponentInteger();
                result=result.Pow(new Rational(power,1));
            }
            return result;
        }

        private int ReadExponentInteger()
        {
            var negative=position<text.Length&&text[position]=='-';
            if(negative)position++;
            var start=position;var magnitude=0;
            while(position<text.Length)
            {
                var digit=text[position] switch {'¹'=>1,'²'=>2,'³'=>3,>= '0' and <= '9'=>text[position]-'0',_=>-1};
                if(digit<0)break;
                magnitude=magnitude*10+digit;
                if(magnitude>1024)throw new FormatException("Unit exponent magnitude exceeds 1024.");
                position++;
            }
            if(position==start)throw new FormatException("Expected exponent digits.");
            // Pinned SimpleUnitFormat.readExponent uses zero as its default-one sentinel,
            // even for an explicit zero literal. Native Pow(0) retains conventional algebra.
            if(magnitude==0)magnitude=1;
            return negative?-magnitude:magnitude;
        }

        private void SkipWhitespace()
        {
            while(position<text.Length&&JavaQuantityTextParser.IsJavaWhitespace(text[position]))position++;
        }
    }
}
