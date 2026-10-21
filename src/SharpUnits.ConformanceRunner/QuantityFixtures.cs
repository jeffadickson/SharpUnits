using System.Text.Json;
using System.Globalization;
using SharpUnits;

/// <summary>Executes fixtures through the public Java quantity adapter.</summary>
internal static class QuantityFixtures
{
    private static IConvertible ScalarInput(JsonElement test)
    {
        var value=test.GetProperty("scalarValue").GetString()!;
        var culture=CultureInfo.InvariantCulture;
        return test.GetProperty("scalarKind").GetString() switch
        {
            "bool" => bool.Parse(value), "string" => value, "char" => value[0],
            "datetime" => new DateTime(2020,1,1), "enum" => DayOfWeek.Monday, "dbnull" => DBNull.Value,
            "sbyte" => sbyte.Parse(value,culture), "byte" => byte.Parse(value,culture),
            "short" => short.Parse(value,culture), "ushort" => ushort.Parse(value,culture),
            "int" => int.Parse(value,culture), "uint" => uint.Parse(value,culture),
            "long" => long.Parse(value,culture), "ulong" => ulong.Parse(value,culture),
            "float" => float.Parse(value,culture), "double" => double.Parse(value,culture),
            "decimal" => decimal.Parse(value,culture),
            _ => throw new NotSupportedException("Unknown scalar kind")
        };
    }
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
        UnitDefinition Resolve(string symbol) => symbol switch
        {
            "quarterA" => new(symbol,registry.Resolve("A").RationalDimension,UnitConversion.Linear(.25)),
            "eighthA" => new(symbol,registry.Resolve("A").RationalDimension,UnitConversion.Linear(.125)),
            "C-copy" => new(symbol,registry.Resolve("°C").RationalDimension,UnitConversion.Celsius),
            "F-copy" => new(symbol,registry.Resolve("°F").RationalDimension,UnitConversion.Fahrenheit),
            _ => registry.Resolve(symbol)
        };
        JavaQuantityScale Scale(string name) => test.TryGetProperty(name, out var scale)
            ? Enum.Parse<JavaQuantityScale>(scale.GetString()!, true) : JavaQuantityScale.Absolute;
        var left = new JavaQuantityAdapter(test.GetProperty("value").GetDouble(), Resolve(Text("from")), Scale("scale"));
        var right = new JavaQuantityAdapter(test.TryGetProperty("rhsValue", out var rhs) ? rhs.GetDouble() : 0,
            Resolve(Text("to")), Scale("rhsScale"));
        var operation = Text("operation");
        if(operation=="quantityAcceptsScalar")
        {
            var scalar=ScalarInput(test);
            try { _=left.Multiply(scalar); return true; }
            catch(ArgumentException) { return false; }
            catch(InvalidCastException) { return false; }
        }
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
            "quantityTypedScalarMultiply" => left.Multiply(ScalarInput(test)),
            "quantityScalarDivide" => left.Divide((IConvertible)rhs.GetDouble()),
            "quantityTo" => left.To(Resolve(Text("to"))),
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
        return result.To(Resolve(Text("resultUnit"))).Value;
    }
}
