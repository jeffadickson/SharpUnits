namespace SharpUnits;

/// <summary>A typed engineering formula expression.</summary>
public abstract record FormulaNode;
/// <summary>A variable resolved through the formula environment.</summary>
public sealed record FormulaVariable(string Name):FormulaNode;
/// <summary>A numeric constant; dimensionless unless explicitly annotated.</summary>
public sealed record FormulaConstant(double Value,RationalDimension Dimension):FormulaNode
{
 /// <summary>Creates a dimensionless numeric literal.</summary>
 public FormulaConstant(double value):this(value,RationalDimension.From(SharpUnits.Dimension.None)){}
}
/// <summary>Supported binary engineering operations.</summary>
public enum FormulaOperator { Add,Subtract,Multiply,Divide,Power }
/// <summary>A binary engineering expression.</summary>
public sealed record FormulaBinary(FormulaOperator Operator,FormulaNode Left,FormulaNode Right):FormulaNode;
/// <summary>A unary mathematical function.</summary>
public sealed record FormulaFunction(string Name,FormulaNode Argument):FormulaNode;
/// <summary>A validated formula result.</summary>
public sealed record FormulaType(RationalDimension Dimension,QuantityKind? Kind=null);
/// <summary>A diagnostic describing a dimensional type error.</summary>
public sealed record FormulaDiagnostic(string Code,string Message);

/// <summary>Checks formula dimensions and optional physical-kind identity without evaluating magnitudes.</summary>
public static class FormulaTypeChecker
{
 /// <summary>Checks a formula against a symbol environment and returns diagnostics.</summary>
 public static (FormulaType? Result,IReadOnlyList<FormulaDiagnostic> Diagnostics) Check(
  FormulaNode expression,IReadOnlyDictionary<string,FormulaType> variables)
 {
  ArgumentNullException.ThrowIfNull(expression);
  ArgumentNullException.ThrowIfNull(variables);
  var diagnostics=new List<FormulaDiagnostic>();
  var result=Visit(expression,variables,diagnostics);
  return (diagnostics.Count==0?result:null,diagnostics.AsReadOnly());
 }
 private static FormulaType? Visit(FormulaNode node,IReadOnlyDictionary<string,FormulaType> variables,List<FormulaDiagnostic> diagnostics)
 {
  switch(node)
  {
   case FormulaVariable variable:
    if(variables.TryGetValue(variable.Name,out var resolved))return resolved;
    diagnostics.Add(new("SU-FORM-001",$"Unknown variable '{variable.Name}'."));
    return null;
   case FormulaConstant constant:
    if(!double.IsFinite(constant.Value))
    {
     diagnostics.Add(new("SU-FORM-002","Nonfinite literal."));
     return null;
    }
    return new(constant.Dimension);
   case FormulaBinary binary:
    var left=Visit(binary.Left,variables,diagnostics);
    var right=Visit(binary.Right,variables,diagnostics);
    if(left is null||right is null)return null;
    switch(binary.Operator)
    {
     case FormulaOperator.Add:
     case FormulaOperator.Subtract:
      if(binary.Operator==FormulaOperator.Subtract && binary.Left is FormulaConstant zero
         && zero.Value==0 && zero.Dimension==RationalDimension.From(Dimension.None))
       return right;
      if(left.Dimension!=right.Dimension)
      {
       diagnostics.Add(new("SU-FORM-003","Addition/subtraction requires matching dimensions."));
       return null;
      }
      if(left.Kind is not null&&right.Kind is not null&&left.Kind!=right.Kind)
      {
       diagnostics.Add(new("SU-FORM-004","Addition/subtraction mixes different physical kinds."));
       return null;
      }
      return new(left.Dimension,left.Kind??right.Kind);
     case FormulaOperator.Multiply:return new(left.Dimension+right.Dimension);
     case FormulaOperator.Divide:return new(left.Dimension-right.Dimension);
     case FormulaOperator.Power:
      if(right.Dimension!=RationalDimension.From(Dimension.None))
      {
       diagnostics.Add(new("SU-FORM-005","Exponent must be dimensionless."));
       return null;
      }
      if(binary.Right is not FormulaConstant power)
      {
       diagnostics.Add(new("SU-FORM-006","Exponent must be a constant for static dimensional checking."));
       return null;
      }
      if(!double.IsFinite(power.Value)||power.Value!=Math.Truncate(power.Value)||power.Value<int.MinValue||power.Value>int.MaxValue)
      {
       diagnostics.Add(new("SU-FORM-007","Exponent must be a representable integer."));
       return null;
      }
      return new(left.Dimension.Pow(new Rational((int)power.Value,1)));
     default:
      diagnostics.Add(new("SU-FORM-008","Unsupported binary operator."));
      return null;
    }
   case FormulaFunction function:
    var arg=Visit(function.Argument,variables,diagnostics);
    if(arg is null)return null;
    if(function.Name is "sin" or "cos" or "tan" or "exp" or "log")
    {
     if(arg.Dimension!=RationalDimension.From(Dimension.None))
     {
      diagnostics.Add(new("SU-FORM-009",$"Function '{function.Name}' requires a dimensionless argument."));
      return null;
     }
     return new(RationalDimension.From(Dimension.None));
    }
    if(function.Name=="sqrt")return new(arg.Dimension.Root(2));
    if(function.Name=="abs")return arg;
    diagnostics.Add(new("SU-FORM-010",$"Unsupported function '{function.Name}'."));
    return null;
   default:
    diagnostics.Add(new("SU-FORM-011","Unsupported expression node."));
    return null;
  }
 }
}
