using System.Globalization;
using System.Numerics;
using System.Text.Json;

namespace SharpUnits;

public sealed partial class ExactTextFormat
{
    // A value is either a scalar or a unit, not a dimension-only projection. This
    // distinction lets m*0.5 construct a transform rather than a spurious product atom.
    private readonly record struct Term(JavaUnit? Unit,JavaNumber? Number)
    {
        internal static Term U(JavaUnit unit)=>new(unit,null);
        internal static Term N(JavaNumber value)=>new(null,value);
        internal JavaUnit AsUnit()=>Unit??JavaUnit.One.Multiply(Number!);
    }

    private sealed class Reader(string text,IReadOnlyDictionary<string,JavaUnit> names)
    {
        private int position;
        private TextReadFailure Fail(TextErrorCode code,int? at=null)=>new(new(code,"text",at??position));
        private void White(){while(position<text.Length&&char.IsWhiteSpace(text[position]))position++;}
        private bool Take(char token){White();if(position<text.Length&&text[position]==token){position++;return true;}return false;}
        private void Need(char token){if(!Take(token))throw Fail(TextErrorCode.UnexpectedToken);}
        private void End(){White();if(position!=text.Length)throw Fail(TextErrorCode.UnexpectedToken);}
        internal JavaUnit ReadUnit()
        {
            // Operator-containing bindings require unit("name"); aliases cannot intercept syntax.
            var term=Expression(0);End();return term.AsUnit();
        }
        internal JavaNumberQuantity ReadQuantity()
        {
            White();var number=Number();var split=position;
            White();if(position==split&&position<text.Length&&text[position]!='@')throw Fail(TextErrorCode.UnexpectedToken);
            var unit=position==text.Length||text[position]=='@'?JavaUnit.One:Expression(0).AsUnit();
            var scale=JavaQuantityScale.Absolute;
            if(Take('@'))
            {
                var start=position;while(position<text.Length&&char.IsLetter(text[position]))position++;
                scale=text[start..position] switch {"absolute"=>JavaQuantityScale.Absolute,"relative"=>JavaQuantityScale.Relative,_=>throw Fail(TextErrorCode.InvalidScale,start)};
            }
            End();return new JavaNumberQuantity(number,unit,scale);
        }
        private Term Expression(int depth)
        {
            if(depth>64)throw Fail(TextErrorCode.DepthLimit);
            var left=Product(depth);
            while(true)
            {
                White();if(position==text.Length||text[position] is not ('+' or '-'))return left;
                var op=text[position++];var start=position;var right=Product(depth);
                if(left.Unit is null||right.Number is null)throw Fail(TextErrorCode.UnexpectedToken,start);
                var offset=op=='+'?right.Number:right.Number.Negate();
                left=Guard(()=>Term.U(left.Unit.Shift(offset)),start);
            }
        }
        private Term Product(int depth)
        {
            var left=Power(depth);
            while(true)
            {
                White();if(position==text.Length||text[position] is not ('*' or '/' or '·'))return left;
                var op=text[position++];var start=position;var right=Power(depth);
                left=Guard(()=>Combine(left,right,op,start),start);
            }
        }
        private Term Combine(Term left,Term right,char op,int start)
        {
            if(op=='/'&&right.Number?.Fraction.Numerator.IsZero==true)throw Fail(TextErrorCode.ZeroDivisor,start);
            if(left.Unit is not null)
                return right.Unit is not null?Term.U(op=='/'?left.Unit.Divide(right.Unit):left.Unit.Multiply(right.Unit))
                    :Term.U(op=='/'?left.Unit.Divide(right.Number!):left.Unit.Multiply(right.Number!));
            if(right.Unit is not null)
                return Term.U(op=='/'?right.Unit.Pow(-1).Multiply(left.Number!):right.Unit.Multiply(left.Number!));
            var fraction=op=='/'?left.Number!.Fraction/right.Number!.Fraction:left.Number!.Fraction*right.Number!.Fraction;
            return Term.N(Ratio(fraction.Numerator,fraction.Denominator,start));
        }
        private Term Power(int depth)
        {
            var value=Primary(depth);White();
            if(!Take('^'))return value;
            var start=position;var grouped=Take('(');var numerator=Exponent();var denominator=1;
            if(grouped&&Take('/'))denominator=Exponent();
            if(grouped)Need(')');
            if(denominator==0)throw Fail(TextErrorCode.InvalidExponent,start);
            var exponent=new Rational(numerator,denominator);
            return Guard(()=>Term.U(value.AsUnit().Pow((int)exponent.Numerator).Root((int)exponent.Denominator)),start);
        }
        private int Exponent(bool bounded=true)
        {
            White();var start=position;if(position<text.Length&&text[position] is '+' or '-')position++;
            while(position<text.Length&&char.IsAsciiDigit(text[position]))position++;
            if(!int.TryParse(text.AsSpan(start,position-start),NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture,out var value)||bounded&&Math.Abs((long)value)>1024)
                throw Fail(TextErrorCode.InvalidExponent,start);
            return value;
        }
        private Term Primary(int depth)
        {
            White();if(depth>64)throw Fail(TextErrorCode.DepthLimit);
            if(position==text.Length)throw Fail(TextErrorCode.UnexpectedToken);
            if(Take('(')){var term=Expression(depth+1);Need(')');return term;}
            if(char.IsAsciiDigit(text[position])||text[position] is '.' or '+' or '-'||text.AsSpan(position).StartsWith("binary(",StringComparison.Ordinal))return Term.N(Number());
            var start=position;
            while(position<text.Length&&!char.IsWhiteSpace(text[position])&&text[position] is not ('(' or ')' or '*' or '/' or '·' or '^' or '+' or '-' or ';' or ',' or '@'))position++;
            if(position==start)throw Fail(TextErrorCode.UnexpectedToken);
            var symbol=text[start..position];
            if(symbol=="unit"&&Take('('))
            {
                if(Take(')'))return Term.U(JavaUnit.One);
                var name=Quoted();Need(')');return Term.U(Resolve(name,start));
            }
            if(symbol=="transform"&&Take('('))return Term.U(Transform(depth+1));
            return Term.U(Resolve(symbol,start));
        }
        private JavaUnit Resolve(string name,int start)
        {
            if(names.TryGetValue(name,out var unit))return unit;
            var normalized=name.StartsWith('μ')?"µ"+name[1..]:name;
            foreach(var prefix in SiPrefixes.Definitions.OrderByDescending(pair=>pair.Key.Length))
            {
                if(!normalized.StartsWith(prefix.Key,StringComparison.Ordinal)||normalized.Length==prefix.Key.Length)continue;
                if(names.TryGetValue(normalized[prefix.Key.Length..],out var basis)&&basis.Kind is JavaUnitKind.Base or JavaUnitKind.Alternate)
                    return Guard(()=>Term.U(basis.Transform(JavaUnitConverterAdapter.ForPrefix(10,prefix.Value))),start).Unit!;
            }
            throw Fail(TextErrorCode.UnknownUnit,start);
        }
        private JavaUnit Transform(int depth)
        {
            var parent=Expression(depth).AsUnit();Need(';');var converter=JavaUnitConverterAdapter.Identity;
            do
            {
                White();var start=position;while(position<text.Length&&char.IsLetter(text[position]))position++;
                var operation=text[start..position];Need('(');
                JavaUnitConverterAdapter step;
                if(operation=="prefix")
                {
                    var radix=Exponent(false);Need(',');var exponent=Exponent();Need(')');
                    if(radix<2)throw Fail(TextErrorCode.InvalidNumber,start);
                    step=GuardConverter(()=>JavaUnitConverterAdapter.ForPrefix(radix,exponent),start);
                }
                else
                {
                    var parameter=Number();Need(')');
                    step=operation switch
                    {
                        "scale"=>JavaUnitConverterAdapter.Multiply(parameter),"offset"=>JavaUnitConverterAdapter.Add(parameter),
                        "log"=>LogConverter(parameter,false,start),
                        "exp"=>LogConverter(parameter,true,start),
                        _=>throw Fail(TextErrorCode.UnsupportedGraph,start)
                    };
                }
                // Stored operations are in composition order, not evaluation order.
                converter=GuardConverter(()=>converter.Concatenate(step),start);
            }while(Take(';'));
            Need(')');
            // Native text composes a local map with this exact parent. The Java
            // compatibility Transform(identity) shortcut selects SystemUnit and
            // would discard an already scaled or shifted parent's mapping.
            return converter.IsIdentity?parent:Guard(()=>Term.U(parent.Transform(converter)),position).Unit!;
        }
        private JavaUnitConverterAdapter LogConverter(JavaNumber parameter,bool inverse,int start)
        {
            var value=parameter.ToDouble();
            if(!double.IsFinite(value)||value<=0||value==1)throw Fail(TextErrorCode.InvalidNumber,start);
            var converter=JavaUnitConverterAdapter.Logarithm(value);
            var stored=converter.Operations[0].Parameter;
            if(stored.Kind!=parameter.Kind||stored.Fraction!=parameter.Fraction||stored.DecimalScale!=parameter.DecimalScale)
                throw Fail(TextErrorCode.UnsupportedGraph,start);
            return inverse?converter.Inverse():converter;
        }
        private JavaUnitConverterAdapter GuardConverter(Func<JavaUnitConverterAdapter> action,int start)
        {
            try{return action();}
            catch(ArgumentOutOfRangeException){throw Fail(TextErrorCode.NumericLimit,start);}
            catch(OverflowException){throw Fail(TextErrorCode.NumericLimit,start);}
        }
        private Term Guard(Func<Term> action,int start)
        {
            try{return action();}
            catch(ArgumentOutOfRangeException error){throw Fail(error.ParamName=="value"?TextErrorCode.NumericLimit:TextErrorCode.GraphLimit,start);}
            catch(OverflowException){throw Fail(TextErrorCode.NumericLimit,start);}
            catch(DivideByZeroException){throw Fail(TextErrorCode.ZeroDivisor,start);}
        }
        private string Quoted()
        {
            White();var start=position;Need('"');var escaped=false;
            while(position<text.Length)
            {
                var c=text[position++];if(c=='"'&&!escaped)
                {
                    try{using var document=JsonDocument.Parse(text[start..position]);return document.RootElement.GetString()!;}
                    catch(Exception error) when(error is JsonException or InvalidOperationException){throw Fail(TextErrorCode.UnexpectedToken,start);}
                }
                escaped=c=='\\'&&!escaped;
            }
            throw Fail(TextErrorCode.UnexpectedToken,position);
        }
        private JavaNumber Number()
        {
            White();var start=position;
            if(text.AsSpan(position).StartsWith("binary(",StringComparison.Ordinal))
            {
                position+=7;var tokenStart=position;while(position<text.Length&&text[position]!=')')position++;
                if(position==text.Length||!double.TryParse(text.AsSpan(tokenStart,position-tokenStart),NumberStyles.Float,CultureInfo.InvariantCulture,out var value)||!double.IsFinite(value))
                    throw Fail(TextErrorCode.InvalidNumber,tokenStart);
                position++;return JavaNumber.FromDouble(value);
            }
            if(position<text.Length&&text[position] is '+' or '-')position++;
            var digits=0;while(position<text.Length&&char.IsAsciiDigit(text[position])){position++;digits++;}
            var decimalToken=false;
            if(position<text.Length&&text[position]=='.')
            {
                decimalToken=true;position++;while(position<text.Length&&char.IsAsciiDigit(text[position])){position++;digits++;}
            }
            if(digits==0)throw Fail(TextErrorCode.UnexpectedToken,start);
            if(position<text.Length&&text[position] is 'e' or 'E')
            {
                decimalToken=true;position++;if(position<text.Length&&text[position] is '+' or '-')position++;
                var exponentStart=position;while(position<text.Length&&char.IsAsciiDigit(text[position]))position++;
                if(position==exponentStart)throw Fail(TextErrorCode.InvalidNumber,position);
            }
            if(decimalToken)
                return JavaNumber.ParseDecimal(text[start..position]).Match(value=>value,error=>throw Fail(error.Code is NumericErrorCode.NumericBudget or NumericErrorCode.ScaleBudget or NumericErrorCode.TokenLength?TextErrorCode.NumericLimit:TextErrorCode.InvalidNumber,start+(error.Position??0)));
            var numerator=BigInteger.Parse(text.AsSpan(start,position-start),NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture);
            if(position<text.Length&&text[position]=='÷')
            {
                position++;var denominatorStart=position;if(position<text.Length&&text[position] is '+' or '-')position++;
                var firstDigit=position;while(position<text.Length&&char.IsAsciiDigit(text[position]))position++;
                if(position==firstDigit)throw Fail(TextErrorCode.InvalidNumber,denominatorStart);
                var denominator=BigInteger.Parse(text.AsSpan(denominatorStart,position-denominatorStart),NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture);
                return Ratio(numerator,denominator,denominatorStart);
            }
            return Ratio(numerator,BigInteger.One,start);
        }
        private JavaNumber Ratio(BigInteger numerator,BigInteger denominator,int start)
        {
            if(denominator.IsZero)throw Fail(TextErrorCode.ZeroDivisor,start);
            if(numerator.GetBitLength()>16384||denominator.GetBitLength()>16384)throw Fail(TextErrorCode.NumericLimit,start);
            return JavaNumber.Ratio(numerator,denominator);
        }
    }
}
