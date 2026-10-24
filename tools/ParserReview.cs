#:property PublishAot=false
#:property JsonSerializerIsReflectionEnabledByDefault=true
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

// Reproducible scenario preparation and checkpoint recording. Run from the repository root.
// This tool never infers a pass from an authored expectation: Java and CLR outputs must both match it.
var jsonOptions=new JsonSerializerOptions { WriteIndented=true,Encoder=System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
var casesPath="conformance/java-reference/cases.json";
var manifestPath="conformance/parser-review-50.json";
var command=args[0];var count=int.Parse(args[1],CultureInfo.InvariantCulture);
var plans=new List<JsonObject>();
void Add(string text,object expected,string observe="magnitude",bool admission=false)
{
    plans.Add(new JsonObject { ["id"]=$"P50-{plans.Count+1:00}",["operation"]=admission?"parseAccepts":"parseQuantity",
        ["text"]=text,["observe"]=observe,["expected"]=JsonSerializer.SerializeToNode(expected),["absoluteTolerance"]=0 });
}
Add("250 mA",250d);Add("+2.5 V",2.5);Add("-12.5 °C",-12.5);Add("0 K",0d);Add("-0 A",false,"negativeZero");
Add("1e-3 A",.001);Add(".5 V",.5);Add("5. Ω",5d);Add("12 kΩ",12d);Add("0.123 °C",.123);
Add("  2 A",2d);Add("\t2 V",2d);Add("2\tmA",2d);Add("2\nV",2d);Add("2\r\nΩ",2d);
Add("2\u2003A",2d);Add("2\u001cV",2d);Add("2\u2028K",2d);Add("2\u2007A",false,admission:true);Add("2\u00a0A",false,admission:true);
Add("1÷3 A",1d/3);Add("-1÷3 V",-1d/3);Add("1÷-3 Ω",-1d/3);Add("-1÷-3 K",1d/3);
Add("5÷2 °C",2.5);Add("0÷7 A",0d);Add("+3÷+4 V",.75);Add("12345678901234567890÷10 A",1.2345678901234568e18);
Add("1÷0 A",false,admission:true);Add("1÷2÷3 A",false,admission:true);
Add("3",3d);Add("3 ","1","unit");Add("20 °C","°C","unit");Add("1e-20 °C",1e-20);
Add("20 °C","ABSOLUTE","scale");Add("4 kΩ","kΩ","unit");Add("1e-3 mA","mA","unit");
Add("-0.0 °C",false,"negativeZero");Add("1e-320 A",1e-320);Add("1e-400 A",0d);
foreach(var text in new[]{"NaN A","Infinity V","1e+ A","1,5 A","1A","one A","1÷x A","1 unknown","1.2.3 V","1\u0085A"})
    Add(text,false,admission:true);
if(plans.Count!=50)throw new InvalidOperationException("Expected fifty review scenarios.");
if(command=="prepare")
{
    var root=JsonNode.Parse(File.ReadAllText(casesPath))!.AsObject();
    var corpus=root["cases"]!.AsArray();
    for(var i=corpus.Count-1;i>=0;i--)if(corpus[i]!["id"]!.GetValue<string>().StartsWith("P50-",StringComparison.Ordinal))corpus.RemoveAt(i);
    foreach(var test in plans.Take(count))corpus.Add(test.DeepClone());
    File.WriteAllText(casesPath,root.ToJsonString(jsonOptions)+"\n");
    if(!File.Exists(manifestPath))
        File.WriteAllText(manifestPath,new JsonObject {
            ["startingCommit"]="098acf2",["ciPolicy"]="Disabled by user; local evidence only",
            ["reviews"]=new JsonArray(plans.Select((p,i)=>(JsonNode)new JsonObject {
                ["iteration"]=i+1,["case"]=p["id"]!.DeepClone(),["text"]=p["text"]!.DeepClone(),
                ["source"]="SimpleQuantityFormat.parse; RationalNumberScanner; SimpleUnitFormat.parse",
                ["before"]="API_ABSENT: existing Parse returns SI-only Quantity",["status"]="PENDING"}).ToArray()),
            ["checkpoints"]=new JsonArray() }.ToJsonString(jsonOptions)+"\n");
    return;
}

Dictionary<string,JsonObject> Rows(string path)=>File.ReadLines(path).Where(l=>!string.IsNullOrWhiteSpace(l))
    .Select(l=>JsonNode.Parse(l)!.AsObject()).ToDictionary(r=>r["id"]!.GetValue<string>());
bool Match(JsonNode? a,JsonNode? b,double tolerance)
{
    if(a is JsonValue av&&b is JsonValue bv&&av.TryGetValue<double>(out var x)&&bv.TryGetValue<double>(out var y))
        return double.IsFinite(x)&&double.IsFinite(y)&&Math.Abs(x-y)<=tolerance;
    return JsonNode.DeepEquals(a,b);
}
if(command!="record")throw new ArgumentException("Use prepare or record.");
var java=Rows($"../parse50-java-{count}.jsonl");var clr=Rows($"../parse50-dotnet-{count}.jsonl");
var all=JsonNode.Parse(File.ReadAllText(casesPath))!["cases"]!.AsArray();
if(java.Count!=all.Count||clr.Count!=all.Count)throw new InvalidOperationException("Output count differs from corpus.");
foreach(var test in all)
{
    var id=test!["id"]!.GetValue<string>();var j=java[id];var c=clr[id];
    var hasError=test["expectedError"] is not null;
    if((j["result"] is null)==(j["error"] is null)||(c["result"] is null)==(c["error"] is null))
        throw new InvalidOperationException($"{id}: exactly one result or error required.");
    var key=hasError?"error":"result";var expected=test[hasError?"expectedError":"expected"];
    var jt=j["absoluteTolerance"]?.GetValue<double>()??0;
    var et=test["absoluteTolerance"]?.GetValue<double>()??1e-10;
    if(!Match(j[key],c[key],jt)||!Match(expected,j[key],et)||!Match(expected,c[key],et))
        throw new InvalidOperationException($"{id}: Java {j[key]}, CLR {c[key]}, expected {expected}");
}
var manifest=JsonNode.Parse(File.ReadAllText(manifestPath))!.AsObject();
foreach(var r in manifest["reviews"]!.AsArray().Where(r=>r!["iteration"]!.GetValue<int>()>count-10&&r["iteration"]!.GetValue<int>()<=count))
{
    var id=r!["case"]!.GetValue<string>();r["java"]=java[id].DeepClone();r["dotnet"]=clr[id].DeepClone();r["status"]="CASE_MATCHED";
}
var build=File.ReadAllText($"../parse50-build-{count}.log");var tests=File.ReadAllText($"../parse50-test-{count}.log");
if(!build.Contains("Build succeeded.")||!tests.Contains("Failed:     0"))throw new InvalidOperationException("Checkpoint did not pass.");
manifest["checkpoints"]!.AsArray().Add(new JsonObject { ["iteration"]=count,["build"]="PASS",
    ["dotnetTestsPassed"]=int.Parse(Regex.Match(tests,@"Passed:\s+(\d+)").Groups[1].Value,CultureInfo.InvariantCulture),
    ["differentialCases"]=all.Count,["mismatches"]=0 });
File.WriteAllText(manifestPath,manifest.ToJsonString(jsonOptions)+"\n");
Console.WriteLine($"Validated {all.Count} shared Java/CLR cases; checkpoint {count} passed.");
