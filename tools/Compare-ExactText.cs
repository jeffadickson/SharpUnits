#:project ../src/SharpUnits/SharpUnits.csproj
#:property PublishAot=false
using System.Globalization;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using SharpUnits;

// Integration glue for the pinned Java observer. Full graph identity and exact
// fractions are checked separately in xUnit; this comparison uses named tolerances.
if(args.Length!=3)throw new ArgumentException("Expected cases JSON, Java JSONL and output JSON.");
using var cases=JsonDocument.Parse(File.ReadAllText(args[0]));
var fixture=cases.RootElement.GetProperty("cases").EnumerateArray().ToArray();
var java=File.ReadLines(args[1]).Where(line=>!string.IsNullOrWhiteSpace(line)).Select(line=>JsonNode.Parse(line)!.AsObject()).ToArray();
if(java.Length!=fixture.Length||java.Select(row=>row["id"]!.GetValue<string>()).Distinct(StringComparer.Ordinal).Count()!=fixture.Length)
    throw new InvalidDataException("Incomplete/duplicate Java observations.");
var format=new ExactTextFormat();var records=new JsonArray();var failures=0;var differences=0;var numericChecks=0;
var inputs=new[]{0,1,2,-3};
var atol=cases.RootElement.GetProperty("comparison").GetProperty("absoluteTolerance").GetDouble();
var rtol=cases.RootElement.GetProperty("comparison").GetProperty("relativeTolerance").GetDouble();
bool Close(double a,double b)=>double.IsFinite(a)&&double.IsFinite(b)&&Math.Abs(a-b)<=atol+rtol*Math.Max(Math.Abs(a),Math.Abs(b));
static double Fraction(string text)
{
    var pair=text.Split('/');var n=BigInteger.Parse(pair[0],CultureInfo.InvariantCulture);var d=pair.Length==2?BigInteger.Parse(pair[1],CultureInfo.InvariantCulture):BigInteger.One;
    return (double)n/(double)d;
}
foreach(var test in fixture)
{
    var id=test.GetProperty("id").GetString()!;
    var observation=java.Single(row=>row["id"]!.GetValue<string>()==id);
    var ja=observation["accepted"]!.GetValue<bool>();
    var parsed=format.ParseUnit(test.GetProperty("text").GetString());
    var (unit,error)=parsed.Match(value=>((JavaUnit?)value,(TextError?)null),failure=>((JavaUnit?)null,(TextError?)failure));
    var accepted=unit is not null;var stage=accepted?"accepted":"parse";
    var target=JavaUnit.FromCatalog(test.GetProperty("target").GetString()!);
    if(accepted&&unit!.Dimension!=target.Dimension){accepted=false;stage="target";}
    var native=new JsonArray();
    if(accepted)
    {
        var converter=unit!.GetConverterToAny(target);
        foreach(var input in inputs)native.Add(converter.Convert(JavaNumber.Integer(input)).ToDouble());
    }
    var ok=ja==test.GetProperty("expectedJavaAccepted").GetBoolean()&&accepted==test.GetProperty("expectedNativeAccepted").GetBoolean();
    if(ja!=accepted)
    {
        differences++;
        if(!test.TryGetProperty("disposition",out var disposition)||disposition.GetString()!="INTENTIONAL_NATIVE_GRAMMAR_DIFFERENCE")ok=false;
    }
    if(ja&&accepted)
    {
        var samples=observation["numericSamples"]!.AsArray();
        if(samples.Count!=inputs.Length)throw new InvalidDataException("Incomplete Java samples.");
        for(var i=0;i<inputs.Length;i++){numericChecks++;ok&=Close(samples[i]!.GetValue<double>(),native[i]!.GetValue<double>());}
    }
    if(accepted&&test.TryGetProperty("analyticalScale",out var scale))
    {
        var s=Fraction(scale.GetString()!);var o=Fraction(test.GetProperty("analyticalOffset").GetString()!);
        for(var i=0;i<inputs.Length;i++)ok&=Close((inputs[i]+o)*s,native[i]!.GetValue<double>());
    }
    if(!ok)failures++;
    records.Add(new JsonObject{{"id",id},{"javaAccepted",ja},{"nativeAccepted",accepted},{"nativeStage",stage},
        {"nativeError",error?.Code.ToString()},{"position",error?.Position},{"nativeSamples",native},{"passedDeclaredContract",ok}});
}
var result=new JsonObject{{"cases",fixture.Length},{"failures",failures},{"intentionalAcceptanceDifferences",differences},
    {"pairedNumericChecks",numericChecks},{"records",records},
    {"scope","Pinned Java parser/converter observations versus native exact text. Explicit tolerance double projections, not full Java structural/runtime equivalence. Exact graph/fraction tests are separate."}};
File.WriteAllText(args[2],result.ToJsonString(new JsonSerializerOptions{WriteIndented=true})+"\n");
Console.WriteLine($"Exact text: {fixture.Length} cases, {failures} failures, {differences} intentional grammar differences, {numericChecks} paired numeric observations.");
return failures==0?0:1;
