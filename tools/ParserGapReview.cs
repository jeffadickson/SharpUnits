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
var manifestPath="conformance/parser-gap-review-50.json";
var command=args[0];var count=int.Parse(args[1],CultureInfo.InvariantCulture);
var plans=new List<JsonObject>();
void Add(string text,object expected,string observe="magnitude",bool admission=false,string? target=null)
{
    var test=new JsonObject { ["id"]=$"G50-{plans.Count+1:00}",["operation"]=admission?"parseAccepts":"parseQuantity",
        ["text"]=text,["observe"]=observe,["expected"]=JsonSerializer.SerializeToNode(expected),["absoluteTolerance"]=0 };
    // Unit equality is distinct from SimpleUnitFormat's canonical label (e.g. Celsius's ℃).
    if(observe=="unit") { test["observe"]="unitMatches";test["resultUnit"]=(string)expected;test["expected"]=true; }
    if(target is not null)test["resultUnit"]=target; plans.Add(test);
}
Add("1÷ 2 A",.5);Add("-3÷ 4 V",-.75);Add("1÷\t3 Ω",1d/3);Add("5÷\n2 K",2.5);Add("7÷\r\n2 A",3.5);
Add("1÷\u20033 A",1d/3);Add("0÷ 7 A",0d);Add("1÷ -2 A",-.5);Add("1 ÷2 A",false,admission:true);Add("1÷ 0 A",false,admission:true);
Add("1 ℃",true,admission:true);Add("20 ℃",20d);Add("-40 ℃",-40d);Add("0.000001 ℃",.000001);Add("-0 ℃",false,"negativeZero");
Add("20 ℃","°C","unit");Add("20 ℃","ABSOLUTE","scale");Add("1÷ 2 ℃",.5);Add("1\t℃",1d);Add("1 ℃ trailing",false,admission:true);
Add("2 A/V",1d,"unitScale");Add("3 V/Ω",3d,"converted",target:"A");Add("4 A*Ω",4d,"converted",target:"V");Add("5 mA*kΩ",5d,"converted",target:"V");
Add("6 mV/A",.006,"converted",target:"Ω");Add("7 V/kΩ",.007,"converted",target:"A");Add("8 (V/Ω)",8d,"converted",target:"A");
Add("9 (A*Ω)",9d,"converted",target:"V");Add("10 A/A",10d,"converted",target:"1");Add("11 V·A",1d,"unitScale");
Add("1 A/A/A",1d,"converted",target:"A");Add("2 A/(A/A)",2d,"converted",target:"A");Add("3 V/Ω/A",true,"compatible",target:"A²");
Add("4 (A/A)/A",false,"compatible",target:"A");Add("5 A^2",true,"compatible",target:"A²");Add("6 (A^2)/A",6d,"converted",target:"A");
Add("7 A^-1*A",7d,"converted",target:"1");Add("8 mA^2",.000001,"unitScale");Add("9 (mA*kΩ)/V",9d,"converted",target:"1");Add("10 V/(mA*kΩ)",10d,"converted",target:"1");
Add("1÷ 2 V/Ω",.5,"converted",target:"A");Add("2 (mA*kΩ)/(V/A)",2d,"converted",target:"A");Add("3 A/A/A/A",3d,"converted",target:"1");
Add("4 (A/A/A)/A",4d,"converted",target:"1");Add("5 V/(Ω/A)",true,"compatible",target:"A²");
foreach(var text in new[]{"1 unknown/A","1 (A/V","1 A**Ω","1 A/unknown","1 A trailing"})Add(text,false,admission:true);
if(plans.Count!=50)throw new InvalidOperationException("Expected fifty review scenarios.");
if(command=="prepare")
{
    var root=JsonNode.Parse(File.ReadAllText(casesPath))!.AsObject();
    var corpus=root["cases"]!.AsArray();
    for(var i=corpus.Count-1;i>=0;i--)if(corpus[i]!["id"]!.GetValue<string>().StartsWith("G50-",StringComparison.Ordinal))corpus.RemoveAt(i);
    foreach(var test in plans.Take(count))corpus.Add(test.DeepClone());
    File.WriteAllText(casesPath,root.ToJsonString(jsonOptions)+"\n");
    if(!File.Exists(manifestPath))
        File.WriteAllText(manifestPath,new JsonObject {
            ["startingCommit"]="9d469b2",["ciPolicy"]="Disabled by user; local evidence only",
            ["reviews"]=new JsonArray(plans.Select((p,i)=>(JsonNode)new JsonObject {
                ["iteration"]=i+1,["case"]=p["id"]!.DeepClone(),["text"]=p["text"]!.DeepClone(),
                ["source"]="SimpleQuantityFormat.parse; RationalNumberScanner; SimpleUnitFormat.parse",
                ["before"]="See baseline parser-known-differences.json at 9d469b2; per-scenario baseline output is not claimed",["status"]="PENDING"}).ToArray()),
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
if(command is not ("record" or "check"))throw new ArgumentException("Use prepare, record or check.");
var java=Rows($"../gap50-java-{count}.jsonl");var clr=Rows($"../gap50-dotnet-{count}.jsonl");
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
if(command=="check") { Console.WriteLine($"Validated {all.Count} shared Java/CLR cases."); return; }
var manifest=JsonNode.Parse(File.ReadAllText(manifestPath))!.AsObject();
foreach(var r in manifest["reviews"]!.AsArray().Where(r=>r!["iteration"]!.GetValue<int>()>count-10&&r["iteration"]!.GetValue<int>()<=count))
{
    var id=r!["case"]!.GetValue<string>();r["java"]=java[id].DeepClone();r["dotnet"]=clr[id].DeepClone();r["status"]="CASE_MATCHED";
}
var build=File.ReadAllText($"../gap50-build-{count}.log");var tests=File.ReadAllText($"../gap50-test-{count}.log");
if(!build.Contains("Build succeeded.")||!tests.Contains("Failed:     0"))throw new InvalidOperationException("Checkpoint did not pass.");
var checkpoints=manifest["checkpoints"]!.AsArray();
// Re-executing a checkpoint replaces its record rather than inventing another iteration.
for(var i=checkpoints.Count-1;i>=0;i--)if(checkpoints[i]!["iteration"]!.GetValue<int>()==count)checkpoints.RemoveAt(i);
checkpoints.Add(new JsonObject { ["iteration"]=count,["build"]="PASS",
    ["dotnetTestsPassed"]=int.Parse(Regex.Match(tests,@"Passed:\s+(\d+)").Groups[1].Value,CultureInfo.InvariantCulture),
    ["differentialCases"]=all.Count,["mismatches"]=0 });
manifest["checkpoints"]=new JsonArray(checkpoints.OrderBy(c=>c!["iteration"]!.GetValue<int>()).Select(c=>c!.DeepClone()).ToArray());
File.WriteAllText(manifestPath,manifest.ToJsonString(jsonOptions)+"\n");
Console.WriteLine($"Validated {all.Count} shared Java/CLR cases; checkpoint {count} passed.");
