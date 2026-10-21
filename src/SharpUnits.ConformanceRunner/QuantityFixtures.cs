using System.Text.Json;
using SharpUnits;

/// <summary>Executes fixtures through the public Java quantity adapter.</summary>
internal static class QuantityFixtures
{
    public static object Evaluate(JsonElement test, UnitRegistry registry)
    {
        string Text(string name) => test.GetProperty(name).GetString()!;
        JavaQuantityScale Scale(string name) => test.TryGetProperty(name, out var scale)
            ? Enum.Parse<JavaQuantityScale>(scale.GetString()!, true) : JavaQuantityScale.Absolute;
        var left = new JavaQuantityAdapter(test.GetProperty("value").GetDouble(), registry.Resolve(Text("from")), Scale("scale"));
        var right = new JavaQuantityAdapter(test.TryGetProperty("rhsValue", out var rhs) ? rhs.GetDouble() : 0,
            registry.Resolve(Text("to")), Scale("rhsScale"));
        var operation = Text("operation");
        if (operation == "quantityCompare") return Math.Sign(left.CompareTo(right));
        if (operation == "quantityEquivalent") return left.IsEquivalentTo(right);
        if (operation == "quantityEquals") return left.Equals(right);
        var result = operation switch
        {
            "quantityAdd" => left.Add(right),
            "quantitySubtract" => left.Subtract(right),
            "quantityMultiply" => left.Multiply(right),
            "quantityDivide" => left.Divide(right),
            "quantityScalarMultiply" => left.Multiply((IConvertible)rhs.GetDouble()),
            "quantityScalarDivide" => left.Divide((IConvertible)rhs.GetDouble()),
            "quantityTo" => left.To(registry.Resolve(Text("to"))),
            _ => throw new NotSupportedException(operation)
        };
        if (test.TryGetProperty("observe", out var observe) && observe.GetString() == "scale")
            return result.Scale.ToString().ToUpperInvariant();
        return result.To(registry.Resolve(Text("resultUnit"))).Value;
    }
}
