using System.Globalization;
namespace SharpUnits;

/// <summary>Parses textual engineering formulas into a typed expression tree.</summary>
/// <remarks>Grammar: identifiers, numeric literals, parentheses, unary minus,
/// binary + - * / ^, and one-argument functions. Power binds tighter than unary minus.</remarks>
public sealed class FormulaParser
{
 private readonly string source;
 private int position;
 private FormulaParser(string source)=>this.source=source;
 /// <summary>Parses a complete expression and rejects trailing tokens.</summary>
 public static FormulaNode Parse(string source)
 {
  ArgumentNullException.ThrowIfNull(source);
  var parser=new FormulaParser(source);
  var expression=parser.Additive();
  parser.Skip();
  if(parser.position!=source.Length)throw parser.Error("Unexpected trailing input");
  return expression;
 }
 private FormulaNode Additive()
 {
  var left=Multiplicative();
  while(true)
  {
   Skip();
   if(Take('+'))left=new FormulaBinary(FormulaOperator.Add,left,Multiplicative());
   else if(Take('-'))left=new FormulaBinary(FormulaOperator.Subtract,left,Multiplicative());
   else return left;
  }
 }
 private FormulaNode Multiplicative()
 {
  var left=Unary();
  while(true)
  {
   Skip();
   if(Take('*'))left=new FormulaBinary(FormulaOperator.Multiply,left,Unary());
   else if(Take('/'))left=new FormulaBinary(FormulaOperator.Divide,left,Unary());
   else return left;
  }
 }
 private FormulaNode Unary()
 {
  Skip();
  if(Take('+'))return Unary();
  if(Take('-'))return new FormulaBinary(FormulaOperator.Subtract,new FormulaConstant(0),Unary());
  return Power();
 }
 private FormulaNode Power()
 {
  var left=Primary();
  Skip();
  if(Take('^'))return new FormulaBinary(FormulaOperator.Power,left,Unary());
  return left;
 }
 private FormulaNode Primary()
 {
  Skip();
  if(Take('('))
  {
   var expression=Additive();
   Skip();
   if(!Take(')'))throw Error("Missing closing parenthesis");
   return expression;
  }
  if(position<source.Length&&(char.IsAsciiDigit(source[position])||source[position]=='.'))
  {
   int start=position;
   bool digits=false;
   while(position<source.Length&&char.IsAsciiDigit(source[position])){position++;digits=true;}
   if(Take('.'))while(position<source.Length&&char.IsAsciiDigit(source[position])){position++;digits=true;}
   if(!digits)throw Error("Expected numeric literal");
   if(position<source.Length&&(source[position]=='e'||source[position]=='E'))
   {
    position++;
    if(position<source.Length&&(source[position]=='+'||source[position]=='-'))position++;
    int exponentStart=position;
    while(position<source.Length&&char.IsAsciiDigit(source[position]))position++;
    if(position==exponentStart)throw Error("Malformed numeric exponent");
   }
   if(!double.TryParse(source[start..position],NumberStyles.Float,CultureInfo.InvariantCulture,out double value)||!double.IsFinite(value))
    throw Error("Invalid or nonfinite number");
   return new FormulaConstant(value);
  }
  if(position<source.Length&&(char.IsLetter(source[position])||source[position]=='_'))
  {
   int start=position++;
   while(position<source.Length&&(char.IsLetterOrDigit(source[position])||source[position]=='_'))position++;
   string name=source[start..position];
   Skip();
   if(!Take('('))return new FormulaVariable(name);
   var argument=Additive();
   Skip();
   if(!Take(')'))throw Error("Missing function closing parenthesis");
   return new FormulaFunction(name,argument);
  }
  throw Error("Expected number, variable or parenthesized expression");
 }
 private bool Take(char character)
 {
  if(position<source.Length&&source[position]==character){position++;return true;}
  return false;
 }
 private void Skip(){while(position<source.Length&&char.IsWhiteSpace(source[position]))position++;}
 private FormatException Error(string message)=>new($"{message} at position {position}.");
}

/// <summary>Validates textual formula equalities without evaluating their numerical values.</summary>
public static class FormulaAnalyzer
{
 /// <summary>Checks a formula or an equality of two formulas.</summary>
 public static (FormulaType? Result,IReadOnlyList<FormulaDiagnostic> Diagnostics) Analyze(
  string source,IReadOnlyDictionary<string,FormulaType> variables)
 {
  ArgumentNullException.ThrowIfNull(source);
  ArgumentNullException.ThrowIfNull(variables);
  try
  {
   // Equality is a top-level statement, not a binary arithmetic operator.
   int equals=source.IndexOf('=');
   if(equals<0)return FormulaTypeChecker.Check(FormulaParser.Parse(source),variables);
   if(source.IndexOf('=',equals+1)>=0)
    return (null,new[]{new FormulaDiagnostic("SU-FORM-012","Only one equality sign is permitted.")});
   var left=FormulaTypeChecker.Check(FormulaParser.Parse(source[..equals]),variables);
   var right=FormulaTypeChecker.Check(FormulaParser.Parse(source[(equals+1)..]),variables);
   var diagnostics=left.Diagnostics.Concat(right.Diagnostics).ToList();
   if(left.Result is not null&&right.Result is not null)
   {
    if(left.Result.Dimension!=right.Result.Dimension)
     diagnostics.Add(new("SU-FORM-013","Equality has incompatible dimensions."));
    else if(left.Result.Kind is not null&&right.Result.Kind is not null&&left.Result.Kind!=right.Result.Kind)
     diagnostics.Add(new("SU-FORM-014","Equality has incompatible physical kinds."));
   }
   return diagnostics.Count==0?(left.Result,Array.Empty<FormulaDiagnostic>()):(null,diagnostics.AsReadOnly());
  }
  catch(FormatException ex)
  {
   return (null,new[]{new FormulaDiagnostic("SU-FORM-015",ex.Message)});
  }
 }
}
