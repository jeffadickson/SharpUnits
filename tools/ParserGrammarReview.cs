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
var manifestPath="conformance/parser-grammar-review-100.json";
var command=args[0];var count=int.Parse(args[1],CultureInfo.InvariantCulture);
var plans=new List<JsonObject>();
void Add(string text,object expected,string observe="magnitude",bool admission=false,string? target=null)
{
    var test=new JsonObject { ["id"]=$"H100-{plans.Count+1:000}",["operation"]=admission?"parseAccepts":"parseQuantity",
        ["text"]=text,["observe"]=observe,["expected"]=JsonSerializer.SerializeToNode(expected),["absoluteTolerance"]=0 };
    // Unit equality is distinct from SimpleUnitFormat's canonical label (e.g. Celsius's ℃).
    if(observe=="unit") { test["observe"]="unitMatches";test["resultUnit"]=(string)expected;test["expected"]=true; }
    if(target is not null)test["resultUnit"]=target; plans.Add(test);
}
// 1-20: superscript, double-star and literal-zero power semantics.
Add("2 A²",2d,"converted",target:"A²");Add("3 V²",3d,"converted",target:"V²");Add("4 Ω²",4d,"converted",target:"Ω²");Add("5 A³",true,"compatible",target:"A³");
Add("6 A¹",6d,"converted",target:"A");Add("7 (A²)/A",7d,"converted",target:"A");Add("8 A²/A²",8d,"converted",target:"1");Add("9 (V²)/V",9d,"converted",target:"V");
Add("10 Ω²/Ω",10d,"converted",target:"Ω");Add("11 (A³)/(A²)",11d,"converted",target:"A");Add("1 A**2",true,"compatible",target:"A²");Add("2 (A**2)/A",2d,"converted",target:"A");
Add("3 A^0",3d,"converted",target:"A");Add("4 A**0",4d,"converted",target:"A");Add("5 A^00",5d,"converted",target:"A");Add("6 A^-0",true,"compatible",target:"invA");
Add("7 (A²)^0",true,"compatible",target:"A²");Add("8 A²³",true,"compatible",target:"pow23A");Add("1 A**foo",false,admission:true);Add("1 A^2foo",false,admission:true);
// 21-40: fractional dimensions, sequential scale powers/roots and their boundaries.
Add("1 A^1:2",true,"compatible",target:"sqrtA");Add("2 (A²)^1:2",2d,"converted",target:"A");Add("3 (V²)^1:2",3d,"converted",target:"V");Add("4 (Ω²)^1:2",4d,"converted",target:"Ω");
Add("5 (A³)^1:3",5d,"converted",target:"A");Add("6 A^2:2",6d,"converted",target:"A");Add("7 A^3:3",7d,"converted",target:"A");Add("8 A^0:2",true,"compatible",target:"sqrtA");
Add("9 A^1:0",9d,"converted",target:"A");Add("10 A^1:-2",true,"compatible",target:"invSqrtA");Add("1 (A^1:2)^2",1d,"converted",target:"A");Add("2 A^2:1",true,"compatible",target:"A²");
Add("3 A**1:2",true,"compatible",target:"sqrtA");Add("4 A¹:²",true,"compatible",target:"sqrtA");Add("5 (A²)^-1:2",true,"compatible",target:"invA");Add("6 (A^1:2)*(A^1:2)",6d,"converted",target:"A");
Add("7 (mA²)^1:2",.007,"converted",target:"A");Add("8 (kΩ²)^1:2",8000d,"converted",target:"Ω");Add("9 (mA²)^1:2",.001,"unitScale");Add("1 A^1:bad",false,admission:true);
// 41-60: signed integer factors bind immediately before further unit recursion.
Add("2 A*2",4d,"converted",target:"A");Add("3 A/2",1.5,"converted",target:"A");Add("4 V*3",12d,"converted",target:"V");Add("5 Ω/4",1.25,"converted",target:"Ω");
Add("6 A*-2",-12d,"converted",target:"A");Add("7 A/-2",-3.5,"converted",target:"A");Add("8 (A*2)/A",16d,"converted",target:"1");Add("9 A/2/Ω",4.5,"converted",target:"AperΩ");
Add("10 A/2*Ω",5d,"converted",target:"V");Add("11 A*2/Ω",22d,"converted",target:"AperΩ");Add("1 A/(2)",false,admission:true);Add("1 A*2147483647",2147483647d,"unitScale");
Add("1 A*-2147483648",-2147483648d,"unitScale");Add("2 mA*1000",2d,"converted",target:"A");Add("3 kΩ/1000",3d,"converted",target:"Ω");Add("4 A/2/V",2d,"converted",target:"AperV");
Add("5 (A/2)*2",5d,"converted",target:"A");Add("6 A*2*3",36d,"converted",target:"A");Add("7 A/2/2",1.75,"converted",target:"A");Add("1 A*word",false,admission:true);
// 61-90: every SI decimal prefix, micro aliases and longest-prefix matching.
foreach(var (prefix,exponent) in new (string,int)[]{("Q",30),("R",27),("Y",24),("Z",21),("E",18),("P",15),("T",12),("G",9),("M",6),("k",3),("h",2),("da",1),("d",-1),("c",-2),("m",-3),("µ",-6),("n",-9),("p",-12),("f",-15),("a",-18),("z",-21),("y",-24),("r",-27),("q",-30)})
    Add("1 "+prefix+"A",double.Parse("1e"+exponent,CultureInfo.InvariantCulture),"unitScale");
Add("1 µA",.000001,"converted",target:"A");Add("1 μA",.000001,"converted",target:"A");Add("2 daV",20d,"converted",target:"V");Add("3 hΩ",300d,"converted",target:"Ω");Add("4 nA",4e-9,"converted",target:"A");Add("5 kV",5000d,"converted",target:"V");
// 91-100: independent combinations and malformed tokens.
Add("2 (µA*GΩ)",2000d,"converted",target:"V");Add("3 (kV/mA)",3e6,"converted",target:"Ω");Add("4 µA²",1e-12,"unitScale");Add("5 (µA²)^1:2",5e-6,"converted",target:"A");
Add("6 (kΩ/1000)",6d,"converted",target:"Ω");Add("7 A/2*Ω",3.5,"converted",target:"V");Add("8 (A²)^1:2",8d,"converted",target:"A");Add("9 qA",9e-30,"converted",target:"A");Add("1 xxA",false,admission:true);Add("1 A*2^foo",false,admission:true);
if(plans.Count!=100)throw new InvalidOperationException("Expected one hundred review scenarios.");
if(command=="prepare")
{
    var root=JsonNode.Parse(File.ReadAllText(casesPath))!.AsObject();
    var corpus=root["cases"]!.AsArray();
    for(var i=corpus.Count-1;i>=0;i--)if(corpus[i]!["id"]!.GetValue<string>().StartsWith("H100-",StringComparison.Ordinal))corpus.RemoveAt(i);
    foreach(var test in plans.Take(count))corpus.Add(test.DeepClone());
    File.WriteAllText(casesPath,root.ToJsonString(jsonOptions)+"\n");
    if(!File.Exists(manifestPath))
        File.WriteAllText(manifestPath,new JsonObject {
            ["startingCommit"]="e982406",["ciPolicy"]="Disabled by user; local evidence only",
            ["reviews"]=new JsonArray(plans.Select((p,i)=>(JsonNode)new JsonObject {
                ["iteration"]=i+1,["case"]=p["id"]!.DeepClone(),["text"]=p["text"]!.DeepClone(),
                ["source"]="SimpleQuantityFormat.parse; RationalNumberScanner; SimpleUnitFormat.parse",
                ["before"]="See parser-grammar-baseline.json at e982406; no unexecuted per-case baseline output is claimed",["status"]="PENDING"}).ToArray()),
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
var java=Rows($"../grammar100-java-{count}.jsonl");var clr=Rows($"../grammar100-dotnet-{count}.jsonl");
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
var build=File.ReadAllText($"../grammar100-build-{count}.log");var tests=File.ReadAllText($"../grammar100-test-{count}.log");
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
