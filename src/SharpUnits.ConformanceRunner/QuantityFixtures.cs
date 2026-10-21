using System.Text.Json;
using SharpUnits;

/// <summary>Executes fixtures through the public Java quantity adapter.</summary>
internal static class QuantityFixtures
{
    public static string? ErrorCategory(Exception error, string? operation) => error switch
    {
        DivideByZeroException => "DIVIDE_BY_ZERO",
        InvalidOperationException when operation == "quantityAsType" => "INCOMPATIBLE_QUANTITY_TYPE",
        InvalidOperationException => "INCOMPATIBLE_DIMENSION",
        _ => null
    };
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
            "quantityInverse" => left.Inverse(),
            "quantityAsType" => left.AsType(Text("kind") switch
            {
                "current" => QuantityKind.Current,
                "voltage" => QuantityKind.Voltage,
                "resistance" => QuantityKind.Resistance,
                _ => throw new NotSupportedException("Unknown quantity kind")
            }, QuantityKindRegistry.CreateStandard()),
            _ => throw new NotSupportedException(operation)
        };
        if (test.TryGetProperty("observe", out var observe))
        {
            if (observe.GetString() == "scale") return result.Scale.ToString().ToUpperInvariant();
            if (observe.GetString() == "magnitude") return result.Value;
            if (observe.GetString() == "unitScale") return result.Unit.Converter.ToSi(1);
        }
        return result.To(registry.Resolve(Text("resultUnit"))).Value;
    }
}
