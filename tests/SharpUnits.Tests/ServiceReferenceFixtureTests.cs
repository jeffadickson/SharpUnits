using System.Text.Json;
using Xunit;

namespace SharpUnits.Tests;

/// <summary>Challenges unit catalog dimensions/converters and service contracts against actual Java observations.</summary>
public sealed class ServiceReferenceFixtureTests
{
    // @relation(JP-020, JP-021, JP-006, scope=file)
    public static IEnumerable<object[]> Cases()
    {
        using var data=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"service-reference-cases.json")));
        foreach(var test in data.RootElement.GetProperty("cases").EnumerateArray())yield return new object[]{test.GetProperty("id").GetString()!,test.GetRawText()};
    }
    [Theory]
    [MemberData(nameof(Cases))]
    public void MatchesPinnedServiceObservation(string id,string json)
    {
        Assert.False(string.IsNullOrWhiteSpace(id));using var data=JsonDocument.Parse(json);
        var actual=JsonSerializer.SerializeToElement(ServiceFixtures.Evaluate(data.RootElement));
        Compare(data.RootElement.GetProperty("expected"),actual);
    }
    private static void Compare(JsonElement expected,JsonElement actual)
    {
        Assert.Equal(expected.ValueKind,actual.ValueKind);
        switch(expected.ValueKind)
        {
            case JsonValueKind.Object:
                Assert.Equal(expected.EnumerateObject().Count(),actual.EnumerateObject().Count());
                foreach(var property in expected.EnumerateObject())Compare(property.Value,actual.GetProperty(property.Name));break;
            case JsonValueKind.Array:
                Assert.Equal(expected.GetArrayLength(),actual.GetArrayLength());
                foreach(var pair in expected.EnumerateArray().Zip(actual.EnumerateArray()))Compare(pair.First,pair.Second);break;
            case JsonValueKind.Number:
                var x=expected.GetDouble();var y=actual.GetDouble();
                Assert.True(x==y||double.IsFinite(x)&&double.IsFinite(y)&&Math.Abs(x-y)<=Math.Max(Math.Abs(x),Math.Abs(y))*1e-13,$"Expected {x:R}, actual {y:R}");break;
            case JsonValueKind.String:Assert.Equal(expected.GetString(),actual.GetString());break;
            default:Assert.Equal(expected.GetRawText(),actual.GetRawText());break;
        }
    }
}
