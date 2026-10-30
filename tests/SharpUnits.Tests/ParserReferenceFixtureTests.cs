using System.Text.Json;
using Xunit;

namespace SharpUnits.Tests;

/// <summary>Runs the same parser inputs observed independently through pinned Indriya.</summary>
public sealed class ParserReferenceFixtureTests
{
    // @relation(JP-014-DECIMAL, JP-014-RATIONAL, JP-014-INVALID, scope=file)
    public static IEnumerable<object[]> Cases()
    {
        using var doc=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"quantity-reference-cases.json")));
        foreach(var test in doc.RootElement.GetProperty("cases").EnumerateArray())
            if(test.GetProperty("operation").GetString()!.StartsWith("parse",StringComparison.Ordinal))
                yield return new object[]{test.GetProperty("id").GetString()!,test.GetRawText()};
    }
    [Theory]
    [MemberData(nameof(Cases))]
    public void MatchesIndriyaParserFixture(string id,string json)
    {
        using var doc=JsonDocument.Parse(json);
        var actual=ParserFixtures.Evaluate(doc.RootElement);var expected=doc.RootElement.GetProperty("expected");
        if(expected.ValueKind==JsonValueKind.Number)Assert.Equal(expected.GetDouble(),Assert.IsType<double>(actual));
        else if(expected.ValueKind==JsonValueKind.String)Assert.Equal(expected.GetString(),Assert.IsType<string>(actual));
        else Assert.Equal(expected.GetBoolean(),Assert.IsType<bool>(actual));
    }
}
