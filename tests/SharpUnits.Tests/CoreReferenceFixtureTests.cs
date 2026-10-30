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
        AssertObservation(expected,actual,test);
        if(test.TryGetProperty("allowedClrExceptions",out var types))
            Assert.Contains(types.EnumerateArray(),item=>item.GetString()==CoreFixtures.LastExceptionType);
    }
    internal static void AssertObservation(JsonElement expected,object? actual,JsonElement fixture)
    {
        var value=JsonSerializer.SerializeToElement(actual);
        var absolute=fixture.TryGetProperty("absoluteTolerance",out var a)?a.GetDouble():0;
        var relative=fixture.TryGetProperty("relativeTolerance",out var r)?r.GetDouble():0;
        Compare(expected,value,absolute,relative,"result");
    }
    private static void Compare(JsonElement expected,JsonElement actual,double absolute,double relative,string path)
    {
        Assert.Equal(expected.ValueKind,actual.ValueKind);
        switch(expected.ValueKind)
        {
            case JsonValueKind.Object:
                Assert.Equal(expected.EnumerateObject().Count(),actual.EnumerateObject().Count());
                foreach(var property in expected.EnumerateObject())
                {
                    Assert.True(actual.TryGetProperty(property.Name,out var member),path+"."+property.Name);
                    Compare(property.Value,member,absolute,relative,path+"."+property.Name);
                }
                break;
            case JsonValueKind.Array:
                Assert.Equal(expected.GetArrayLength(),actual.GetArrayLength());
                for(var i=0;i<expected.GetArrayLength();i++)Compare(expected[i],actual[i],absolute,relative,path+"["+i+"]");
                break;
            case JsonValueKind.Number:
                if(absolute==0&&relative==0)Assert.Equal(expected.GetRawText(),actual.GetRawText());
                else Assert.True(double.IsFinite(actual.GetDouble())&&Math.Abs(expected.GetDouble()-actual.GetDouble())<=Math.Max(absolute,Math.Abs(expected.GetDouble())*relative),path);
                break;
            case JsonValueKind.String:Assert.Equal(expected.GetString(),actual.GetString());break;
            case JsonValueKind.True:case JsonValueKind.False:Assert.Equal(expected.GetBoolean(),actual.GetBoolean());break;
        }
    }
}