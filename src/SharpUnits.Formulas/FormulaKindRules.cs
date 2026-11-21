namespace SharpUnits.Formulas;

/// <summary>Semantic inference rules for common physical quantity relationships.</summary>
/// <remarks>Physical kind inference is conservative; unrecognized products remain dimension-only.</remarks>
public static class FormulaKindRules
{
    /// <summary>Infers a known physical kind from operand kinds and an operation.</summary>
    public static QuantityKind? Infer(FormulaOperator operation, QuantityKind? left, QuantityKind? right)
    {
        if (left is null || right is null) return null;
        var a = left.Value;
        var b = right.Value;
        if (operation == FormulaOperator.Multiply)
        {
            if ((a == QuantityKind.Current && b == QuantityKind.Resistance) ||
                (a == QuantityKind.Resistance && b == QuantityKind.Current))
                return QuantityKind.Voltage;
            if ((a == QuantityKind.Voltage && b == QuantityKind.Current) ||
                (a == QuantityKind.Current && b == QuantityKind.Voltage))
                return QuantityKind.Power;
            if ((a == QuantityKind.Current && b == QuantityKind.Time) ||
                (a == QuantityKind.Time && b == QuantityKind.Current))
                return QuantityKind.Charge;
            if ((a == QuantityKind.Capacitance && b == QuantityKind.Voltage) ||
                (a == QuantityKind.Voltage && b == QuantityKind.Capacitance))
                return QuantityKind.Charge;
        }
        if (operation == FormulaOperator.Divide)
        {
            if (a == QuantityKind.Voltage && b == QuantityKind.Current) return QuantityKind.Resistance;
            if (a == QuantityKind.Voltage && b == QuantityKind.Resistance) return QuantityKind.Current;
            if (a == QuantityKind.Charge && b == QuantityKind.Time) return QuantityKind.Current;
            if (a == QuantityKind.Charge && b == QuantityKind.Voltage) return QuantityKind.Capacitance;
            if (a == QuantityKind.Power && b == QuantityKind.Voltage) return QuantityKind.Current;
            if (a == QuantityKind.Power && b == QuantityKind.Current) return QuantityKind.Voltage;
        }
        return null;
    }
}
