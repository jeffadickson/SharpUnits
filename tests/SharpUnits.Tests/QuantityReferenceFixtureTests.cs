using System.Text.Json;
using Xunit;

namespace SharpUnits.Tests;

/// <summary>Challenges the public adapter with the same fixtures executed by real Indriya quantities.</summary>
public sealed class QuantityReferenceFixtureTests
{
    public static IEnumerable<object[]> Cases()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "quantity-reference-cases.json")));
        foreach (var test in document.RootElement.GetProperty("cases").EnumerateArray())
            if (test.GetProperty("operation").GetString()!.StartsWith("quantity", StringComparison.Ordinal))
                yield return new object[] { test.GetProperty("id").GetString()!, test.GetRawText() };
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void MatchesIndriyaQuantityFixture(string id, string json)
    {
        using var document = JsonDocument.Parse(json);
        var test = document.RootElement;
        var registry = UnitRegistry.CreateStandard();
        if (test.TryGetProperty("expectedError", out _))
        {
            Assert.Throws<InvalidOperationException>(() => QuantityFixtures.Evaluate(test, registry));
            return;
        }
        var result = QuantityFixtures.Evaluate(test, registry);
        var expected = test.GetProperty("expected");
        if (expected.ValueKind == JsonValueKind.Number)
            Assert.True(Math.Abs(expected.GetDouble() - Convert.ToDouble(result, System.Globalization.CultureInfo.InvariantCulture)) <= 1e-10, id);
        else if (expected.ValueKind == JsonValueKind.String)
            Assert.Equal(expected.GetString(), Assert.IsType<string>(result));
        else
            Assert.Equal(expected.GetBoolean(), Assert.IsType<bool>(result));
    }
}
