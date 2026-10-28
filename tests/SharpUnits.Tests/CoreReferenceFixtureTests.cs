using System.Text.Json;
using Xunit;

namespace SharpUnits.Tests;

/// <summary>Runs the independently executed pinned-Java core corpus against public CLR behavior.</summary>
[Trait("WorkPackage","1")]
public sealed class CoreReferenceFixtureTests
{
    public static IEnumerable<object[]> Cases()
    {
        using var document=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"work-package-one-cases.json")));
        foreach(var item in document.RootElement.GetProperty("cases").EnumerateArray())
            yield return new object[]{item.GetProperty("id").GetString()!,item.GetRawText()};
    }
    [Theory]
    [MemberData(nameof(Cases))]
    public void MatchesPinnedCoreObservation(string id,string json)
    {
        using var document=JsonDocument.Parse(json);var test=document.RootElement;
        var expected=test.GetProperty("expected");var actual=CoreFixtures.Evaluate(test);
        Assert.False(string.IsNullOrWhiteSpace(id));
        if(expected.ValueKind==JsonValueKind.String)Assert.Equal(expected.GetString(),Assert.IsType<string>(actual));
        else if(expected.ValueKind==JsonValueKind.Number)
        {
            if(test.TryGetProperty("relativeTolerance",out var tolerance))
            {
                var target=expected.GetDouble();var result=Assert.IsType<double>(actual);
                var absolute=test.GetProperty("absoluteTolerance").GetDouble();
                Assert.True(Math.Abs(target-result)<=Math.Max(absolute,Math.Abs(target)*tolerance.GetDouble()),id);
            }
            else Assert.Equal(expected.GetInt32(),Assert.IsType<int>(actual));
        }
        else Assert.Equal(expected.GetBoolean(),Assert.IsType<bool>(actual));
    }
}
