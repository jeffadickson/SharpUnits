namespace SharpUnits;

/// <summary>Parses bounded Java-style linear unit expressions without changing the caller's registry.</summary>
internal static class JavaUnitTextParser
{
    // @relation(JP-014-ALIAS, JP-014-COMPOUND, scope=file)
    internal static UnitDefinition Resolve(string symbol,UnitRegistry registry)
    {
        try{return ResolveSimple(symbol,registry);}
        catch(KeyNotFoundException) when(symbol.IndexOfAny(['*','/','·','(',')','^'])>=0)
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
                    &&text[position] is not ('*' or '/' or '·' or '(' or ')' or '^'))position++;
                if(position==start)throw new FormatException("Expected a registered unit symbol.");
                result=RationalUnitExpression.From(ResolveSimple(text[start..position],registry));
            }
            SkipWhitespace();
            if(position<text.Length&&text[position]=='^')
            {
                position++;var start=position;
                if(position<text.Length&&text[position]=='-')position++;
                while(position<text.Length&&char.IsAsciiDigit(text[position]))position++;
                if(!int.TryParse(text[start..position],System.Globalization.NumberStyles.AllowLeadingSign,
                    System.Globalization.CultureInfo.InvariantCulture,out var power)||power is < -1024 or >1024)
                    throw new FormatException("Unit powers must be integers from -1024 to 1024.");
                result=result.Pow(new Rational(power,1));
            }
            return result;
        }

        private void SkipWhitespace()
        {
            while(position<text.Length&&JavaQuantityTextParser.IsJavaWhitespace(text[position]))position++;
        }
    }
}
