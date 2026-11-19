using System.Text.Json;
using Xunit;

namespace SharpUnits.Tests;

/// <summary>Mutation boundary cases checked independently against the pinned Java runtime.</summary>
[Trait("WorkPackage","1")]
public sealed class MutationDebtReferenceTests
{
    public static IEnumerable<object[]> Cases()
    {
        using var document=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"mutation-debt-cases.json")));
        foreach(var item in document.RootElement.GetProperty("cases").EnumerateArray())
            yield return new object[]{item.GetProperty("id").GetString()!,item.GetRawText()};
    }
    [Theory]
    [MemberData(nameof(Cases))]
    public void MatchesIndependentBoundaryAndPinnedJava(string id,string json)
    {
        Assert.False(string.IsNullOrWhiteSpace(id));
        using var document=JsonDocument.Parse(json);var fixture=document.RootElement;
        CoreReferenceFixtureTests.AssertObservation(fixture.GetProperty("independentExpected"),CoreFixtures.Evaluate(fixture),fixture);
    }
}
