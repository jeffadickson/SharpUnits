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
var manifestPath="conformance/format-review-100.json";
var command=args[0];var count=int.Parse(args[1],CultureInfo.InvariantCulture);
var plans=new List<JsonObject>();
void Add(double value,string unit,object expected,string operation="formatQuantity",string scale="ABSOLUTE",string prefix="",string observe="",string? locale=null)
{
 var test=new JsonObject {["id"]=$"F100-{plans.Count+1:000}",["operation"]=operation,["value"]=value,["from"]=unit,["scale"]=scale,["expected"]=JsonSerializer.SerializeToNode(expected),["absoluteTolerance"]=0};
 if(prefix.Length>0)test["prefix"]=prefix;if(observe.Length>0)test["observe"]=observe;if(locale is not null)test["locale"]=locale;plans.Add(test);
}
// 1-20: integer narrowing, signed zero and binary64/long boundaries.
foreach(var value in new[]{0d,-0d,1d,-1d,42d})Add(value,"A",(value==0?"0":value.ToString("0",CultureInfo.InvariantCulture))+" A");
Add(250,"mA","250 mA");Add(-250,"mA","-250 mA");Add(2147483647,"A","2147483647 A");Add(2147483648,"A","2147483648 A");Add(-2147483649,"A","-2147483649 A");
Add(4294967296,"A","4294967296 A");Add(9007199254740991,"A","9007199254740991 A");Add(9007199254740992,"A","9007199254740992 A");
Add(9223372036854774784d,"A","9223372036854774784 A");Add(9223372036854775808d,"A","9223372036854775807 A");Add(-9223372036854775808d,"A","-9223372036854775808 A");
Add(1e20,"A","100000000000000000000 A");Add(1e23,"A","99999999999999990000000 A");Add(-1e23,"A","-99999999999999990000000 A");Add(1e100,"A","1"+new string('0',100)+" A");
// 21-40: fractional/scientific boundaries and subnormal values.
foreach(var (value,label) in new (double,string)[]{(.5,"0.5"),(-.5,"-0.5"),(.125,"0.125"),(-.125,"-0.125"),(.1,"0.1"),(-.1,"-0.1"),(1d/3,"0.3333333333333333"),(Math.PI,"3.141592653589793"),(.001,"0.001"),(.0001,"1.0E-4"),(.00001,"1.0E-5"),(1e-20,"1.0E-20"),(-1e-20,"-1.0E-20"),(double.Epsilon,"4.9E-324"),(2*double.Epsilon,"1.0E-323"),(20*double.Epsilon,"1.0E-322"),(2.2250738585072014e-308,"2.2250738585072014E-308"),(10000000.5,"1.00000005E7"),(9999999.5,"9999999.5"),(-10000000.5,"-1.00000005E7")})Add(value,"A",label+" A");
// 41-60: canonical supported labels, dimensionless omission and relative scale.
Add(20,"°C","20 ℃");Add(-40,"°C","-40 ℃");Add(273.15,"K","273.15 K");Add(250,"mA","250 mA");Add(1.25,"mV","1.25 mV");Add(.5,"kΩ","0.5 kΩ");
Add(1,"µA","1 μA");Add(2,"μA","2 μA");Add(3,"µV","3 μV");Add(4,"µΩ","4 μΩ");Add(5,"µK","5 μK");Add(6,"nA","6 nA");Add(7,"kV","7 kV");
Add(8,"A²","8 A²");Add(9,"V²","9 V²");Add(10,"Ω²","10 Ω²");Add(1,"1","1");Add(.5,"1","0.5");Add(10,"°C","10 ℃",scale:"RELATIVE");Add(.25,"mA","0.25 mA",scale:"RELATIVE");
// 61-80: builder/writer prefixes and destination identity.
string[] prefixes=["","prefix: ","Ω→","line\n","\t"];
for(var i=0;i<5;i++)Add(i+.25,"A",prefixes[i]+(i+.25).ToString(CultureInfo.InvariantCulture)+" A","formatAppendable",prefix:prefixes[i]);
for(var i=0;i<5;i++)Add(i+1,"°C",true,"formatAppendable",prefix:prefixes[i],observe:"identity");
for(var i=0;i<5;i++)Add(i+.5,"mA","w"+i+"|"+(i+.5).ToString(CultureInfo.InvariantCulture)+" mA","formatWriter",scale:i%2==0?"RELATIVE":"ABSOLUTE",prefix:"w"+i+"|");
for(var i=0;i<5;i++)Add(i+1,"A",true,"formatWriter",prefix:"p",observe:"identity");
// 81-90: invariant text and Java's inherited locale-sensitivity result.
string[] locales=["de-DE","fr-FR","tr-TR","ar-EG","en-US"];
foreach(var locale in locales)Add(1.25,"°C","1.25 ℃",locale:locale);
foreach(var locale in locales)Add(0,"A",false,"formatLocale",locale:locale);
// 91-100: round-trip public value/dimension, and explicit loss of relative scale markers.
foreach(var (value,unit) in new (double,string)[]{(1,"A"),(.1,"A"),(.0001,"A"),(double.Epsilon,"A"),(1e23,"A"),(20,"°C"),(.25,"µA"),(-.5,"1")})Add(value,unit,true,"formatRoundTrip");
Add(10,"°C",false,"formatRoundTrip",scale:"RELATIVE",observe:"scale");Add(1,"A",false,"formatRoundTrip",scale:"RELATIVE",observe:"scale");
if(plans.Count!=100)throw new InvalidOperationException("Expected one hundred review scenarios.");
if(command=="prepare")
{
    var root=JsonNode.Parse(File.ReadAllText(casesPath))!.AsObject();
    var corpus=root["cases"]!.AsArray();
    for(var i=corpus.Count-1;i>=0;i--)if(corpus[i]!["id"]!.GetValue<string>().StartsWith("F100-",StringComparison.Ordinal))corpus.RemoveAt(i);
    foreach(var test in plans.Take(count))corpus.Add(test.DeepClone());
    File.WriteAllText(casesPath,root.ToJsonString(jsonOptions)+"\n");
    if(!File.Exists(manifestPath))
        File.WriteAllText(manifestPath,new JsonObject {
            ["startingCommit"]="e9dcd8e",["ciPolicy"]="Disabled by user; local evidence only",
            ["reviews"]=new JsonArray(plans.Select((p,i)=>(JsonNode)new JsonObject {
                ["iteration"]=i+1,["case"]=p["id"]!.DeepClone(),["input"]=p.DeepClone(),
                ["source"]="SimpleQuantityFormat.format; DefaultNumberSystem.narrow; Java17 Double.toString",
                ["before"]="See format-baseline.json; appendable API was absent at e9dcd8e; no unexecuted per-case baseline output is claimed",["status"]="PENDING"}).ToArray()),
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
var java=Rows($"../format100-java-{count}.jsonl");var clr=Rows($"../format100-dotnet-{count}.jsonl");
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
var build=File.ReadAllText($"../format100-build-{count}.log");var tests=File.ReadAllText($"../format100-test-{count}.log");
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
