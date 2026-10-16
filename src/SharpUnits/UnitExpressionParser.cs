namespace SharpUnits;
/// <summary>Parses multiplicative unit expressions with symbols, parentheses, powers, multiplication and division.</summary>
/// <remarks>Only registered linear units are accepted. Unit exponents are integers in this first grammar.</remarks>
public sealed class UnitExpressionParser
{
 private readonly UnitRegistry registry;
 private string input="";
 private int position;
 /// <summary>Constructs a parser against an explicit registry.</summary>
 public UnitExpressionParser(UnitRegistry registry)=>this.registry=registry??throw new ArgumentNullException(nameof(registry));
 /// <summary>Parses a complete expression, rejecting trailing characters.</summary>
 public RationalUnitExpression Parse(string text)
 {
  ArgumentNullException.ThrowIfNull(text);
  input=text;position=0;
  var result=ParseProduct();
  SkipSpace();
  if(position!=input.Length)throw new FormatException($"Unexpected character at position {position}.");
  return result;
 }
 private RationalUnitExpression ParseProduct()
 {
  var result=ParseFactor();
  while(true)
  {
   SkipSpace();
   if(position>=input.Length||input[position]==')')return result;
   char op=input[position];
   if(op!='*'&&op!='/'&&op!='·')return result;
   position++;
   var rhs=ParseFactor();
   result=op=='/'?result/rhs:result*rhs;
  }
 }
 private RationalUnitExpression ParseFactor()
 {
  SkipSpace();
  RationalUnitExpression value;
  if(position<input.Length&&input[position]=='(')
  {
   position++;
   value=ParseProduct();
   SkipSpace();
   if(position>=input.Length||input[position]!=')')throw new FormatException("Missing closing parenthesis.");
   position++;
  }
  else
  {
   int start=position;
   while(position<input.Length&&!char.IsWhiteSpace(input[position])&&input[position]!='*'&&input[position]!='/'&&input[position]!='·'&&input[position]!='^'&&input[position]!=')'&&input[position]!='(')position++;
   if(position==start)throw new FormatException("Expected a unit symbol.");
   value=RationalUnitExpression.From(registry.Resolve(input[start..position]));
  }
  SkipSpace();
  if(position<input.Length&&input[position]=='^')
  {
   position++;
   SkipSpace();
   int start=position;
   if(position<input.Length&&(input[position]=='-'||input[position]=='+'))position++;
   while(position<input.Length&&char.IsAsciiDigit(input[position]))position++;
   if(!int.TryParse(input[start..position],System.Globalization.NumberStyles.Integer,System.Globalization.CultureInfo.InvariantCulture,out int power))
    throw new FormatException("Invalid integer unit exponent.");
   value=value.Pow(power);
  }
  return value;
 }
 private void SkipSpace(){while(position<input.Length&&char.IsWhiteSpace(input[position]))position++;}
}
