using System.Text.Json;
using Xunit;

namespace SharpUnits.Tests;

/// <summary>Challenges Java-formatting results against independently executed Indriya fixtures.</summary>
public sealed class FormatReferenceFixtureTests
{
    // @relation(JP-015-DISPLAY, JP-015-JAVA-DOUBLE, JP-015-UNIT-LABELS, JP-015-APPENDABLE, JP-015-LOCALE, scope=file)
    public static IEnumerable<object[]> Cases()
    {
        using var document=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"quantity-reference-cases.json")));
        foreach(var test in document.RootElement.GetProperty("cases").EnumerateArray())
            if(test.GetProperty("operation").GetString()!.StartsWith("format",StringComparison.Ordinal))
                yield return new object[]{test.GetProperty("id").GetString()!,test.GetRawText()};
    }
    [Theory]
    [MemberData(nameof(Cases))]
    public void MatchesIndriyaFormattingFixture(string id,string json)
    {
        Assert.False(string.IsNullOrEmpty(id));
        using var document=JsonDocument.Parse(json);var test=document.RootElement;
        var actual=FormatFixtures.Evaluate(test);var expected=test.GetProperty("expected");
        if(expected.ValueKind==JsonValueKind.String)Assert.Equal(expected.GetString(),Assert.IsType<string>(actual));
        else Assert.Equal(expected.GetBoolean(),Assert.IsType<bool>(actual));
    }
}
