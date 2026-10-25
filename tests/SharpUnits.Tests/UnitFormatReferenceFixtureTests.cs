using System.Text.Json;
using Xunit;

namespace SharpUnits.Tests;

/// <summary>Compares mutable-label and ASCII obligations with independently executed pinned Java.</summary>
[Collection("Mutable unit-format compatibility")]
public sealed class UnitFormatReferenceFixtureTests
{
    // @relation(JP-015, scope=file)
    public static IEnumerable<object[]> Cases()
    {
        foreach(var file in new[]{"unitformat-reference-cases.json","unitformat-supplement-cases.json"})
        {
            using var document=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,file)));
            foreach(var test in document.RootElement.GetProperty("cases").EnumerateArray())
                yield return new object[]{test.GetProperty("id").GetString()!,test.GetRawText()};
        }
    }
    [Theory]
    [MemberData(nameof(Cases))]
    public void MatchesPinnedUnitFormat(string id,string json)
    {
        Assert.False(string.IsNullOrWhiteSpace(id));using var document=JsonDocument.Parse(json);
        var actual=UnitFormatFixtures.Evaluate(document.RootElement);var expected=document.RootElement.GetProperty("expected");
        if(expected.ValueKind==JsonValueKind.String)Assert.Equal(expected.GetString(),Assert.IsType<string>(actual));
        else Assert.Equal(expected.GetBoolean(),Assert.IsType<bool>(actual));
    }
}

/// <summary>Cases that temporarily mutate the built-in formatter cannot race other collections.</summary>
[CollectionDefinition("Mutable unit-format compatibility",DisableParallelization=true)]
public sealed class MutableUnitFormatCollection {}
