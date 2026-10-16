namespace SharpUnits;
/// <summary>Semantic inference rules for common electrical quantity relationships.</summary>
/// <remarks>Physical kind inference is conservative; unrecognized products remain dimension-only.</remarks>
public static class FormulaKindRules
{
 /// <summary>Infers a known physical kind from operand kinds and an operation.</summary>
 public static QuantityKind? Infer(FormulaOperator operation,QuantityKind? left,QuantityKind? right)
 {
  if(left is null||right is null)return null;
  var a=left.Value;var b=right.Value;
  if(operation==FormulaOperator.Multiply)
  {
   if(a==QuantityKind.Current&&b==QuantityKind.Resistance||a==QuantityKind.Resistance&&b==QuantityKind.Current)
    return QuantityKind.Voltage;
   if(a==QuantityKind.Voltage&&b==QuantityKind.Current||a==QuantityKind.Current&&b==QuantityKind.Voltage)
    return null; // Power kind not yet registered.
  }
  if(operation==FormulaOperator.Divide)
  {
   if(a==QuantityKind.Voltage&&b==QuantityKind.Current)return QuantityKind.Resistance;
   if(a==QuantityKind.Voltage&&b==QuantityKind.Resistance)return QuantityKind.Current;
  }
  return null;
 }
}
