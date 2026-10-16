namespace SharpUnits;
/// <summary>A declared procedure variable, optionally requiring inferred dimensions.</summary>
public sealed record ProcedureVariable(string Name,RationalDimension? Dimension=null,QuantityKind? Kind=null);
/// <summary>A formula statement in an engineering procedure.</summary>
public sealed record ProcedureFormula(string Expression);
/// <summary>Static validation result for an engineering procedure.</summary>
public sealed record ProcedureValidationResult(
 IReadOnlyDictionary<string,FormulaType> Symbols,
 IReadOnlyList<FormulaDiagnostic> Diagnostics)
{
 /// <summary>True when all formulas are dimensionally consistent.</summary>
 public bool IsValid=>Diagnostics.Count==0;
}
/// <summary>Validates a sequence of engineering formulas against declared and inferred dimensions.</summary>
/// <remarks>Unknown dimensions are inferred from multiplicative equality constraints when a unique solution exists.</remarks>
public static class ProcedureValidator
{
 /// <summary>Checks declarations and formulas without evaluating their numerical values.</summary>
 public static ProcedureValidationResult Validate(
  IEnumerable<ProcedureVariable> variables,IEnumerable<ProcedureFormula> formulas)
 {
  ArgumentNullException.ThrowIfNull(variables);
  ArgumentNullException.ThrowIfNull(formulas);
  var declarations=variables.ToArray();
  var statements=formulas.ToArray();
  var diagnostics=new List<FormulaDiagnostic>();
  var symbols=new Dictionary<string,FormulaType>(StringComparer.Ordinal);
  foreach(var variable in declarations)
  {
   if(variable is null||string.IsNullOrWhiteSpace(variable.Name))
   {
    diagnostics.Add(new("SU-PROC-001","Invalid variable declaration."));
    continue;
   }
   if(variable.Dimension is not null)
   {
    if(!symbols.TryAdd(variable.Name,new FormulaType(variable.Dimension.Value,variable.Kind)))
     diagnostics.Add(new("SU-PROC-002",$"Duplicate variable '{variable.Name}'."));
   }
   else if(declarations.Count(d=>d?.Name==variable.Name)>1)
    diagnostics.Add(new("SU-PROC-002",$"Duplicate variable '{variable.Name}'."));
  }
  // Inference currently supports simple multiplicative equations in the form
  // known = known * unknown, or unknown = known / known.
  var equations=new List<DimensionEquation>();
  foreach(var statement in statements)
  {
   if(statement is null||string.IsNullOrWhiteSpace(statement.Expression))
   {
    diagnostics.Add(new("SU-PROC-003","Empty formula."));
    continue;
   }
   var text=statement.Expression;
   int equal=text.IndexOf('=');
   if(equal<0||text.IndexOf('=',equal+1)>=0)continue;
   try
   {
    var left=FormulaParser.Parse(text[..equal]);
    var right=FormulaParser.Parse(text[(equal+1)..]);
    var lhs=AsDimensionExpression(left,symbols,declarations);
    var rhs=AsDimensionExpression(right,symbols,declarations);
    if(lhs is not null&&rhs is not null)equations.Add(new(lhs,rhs));
   }
   catch(FormatException){}
  }
  if(equations.Count>0)
  {
   try
   {
    var inferred=DimensionInference.Solve(equations);
    foreach(var entry in inferred)symbols[entry.Key]=new(entry.Value);
   }
   catch(InvalidOperationException ex)
   {
    diagnostics.Add(new("SU-PROC-004",ex.Message));
   }
  }
  foreach(var variable in declarations)
   if(variable is not null&&!symbols.ContainsKey(variable.Name))
    diagnostics.Add(new("SU-PROC-005",$"Cannot infer dimensions for '{variable.Name}'."));
  foreach(var statement in statements)
  {
   if(statement is null||string.IsNullOrWhiteSpace(statement.Expression))continue;
   var result=FormulaAnalyzer.Analyze(statement.Expression,symbols);
   diagnostics.AddRange(result.Diagnostics);
  }
  return new ProcedureValidationResult(
   new System.Collections.ObjectModel.ReadOnlyDictionary<string,FormulaType>(symbols),
   diagnostics.AsReadOnly());
 }
 private static DimensionExpression? AsDimensionExpression(FormulaNode node,
  IReadOnlyDictionary<string,FormulaType> known,IEnumerable<ProcedureVariable> declarations)
 {
  switch(node)
  {
   case FormulaVariable v:
    if(known.TryGetValue(v.Name,out var type))return DimensionExpression.Of(type.Dimension);
    return declarations.Any(d=>d?.Name==v.Name)?DimensionExpression.Variable(v.Name):null;
   case FormulaConstant c:return DimensionExpression.Of(c.Dimension);
   case FormulaBinary b when b.Operator==FormulaOperator.Multiply:
    var ml=AsDimensionExpression(b.Left,known,declarations);
    var mr=AsDimensionExpression(b.Right,known,declarations);
    return ml is null||mr is null?null:ml*mr;
   case FormulaBinary b when b.Operator==FormulaOperator.Divide:
    var dl=AsDimensionExpression(b.Left,known,declarations);
    var dr=AsDimensionExpression(b.Right,known,declarations);
    return dl is null||dr is null?null:dl/dr;
   case FormulaBinary b when b.Operator==FormulaOperator.Power && b.Right is FormulaConstant exponent
       && exponent.Value==Math.Truncate(exponent.Value)
       && exponent.Value>=int.MinValue&&exponent.Value<=int.MaxValue:
    return AsDimensionExpression(b.Left,known,declarations)?.Pow(new Rational((int)exponent.Value,1));
   default:return null;
  }
 }
}
