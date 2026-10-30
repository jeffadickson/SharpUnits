#:property PublishAot=false
#:property JsonSerializerIsReflectionEnabledByDefault=true
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

// One explicit contract or ASCII binding per item. Expectations come only from
// the separately executed Java runner; this generator never evaluates CLR APIs.
var options=new JsonSerializerOptions{WriteIndented=true};
var plans=new List<JsonObject>();
JsonObject Act(string op,string? name=null,string unit="A")=>new(){["op"]=op,["unit"]=unit,["name"]=name};
void State(string gap,string kind="format",JsonObject[]? actions=null,string unit="A",string? text=null,string? prefix=null,bool identity=false,bool ascii=false)
{
    plans.Add(new JsonObject{["gap"]=gap,["kind"]=kind,["actions"]=new JsonArray((actions??[]).Select(a=>a.DeepClone()).ToArray()),
        ["unit"]=unit,["text"]=text,["prefix"]=prefix,["identity"]=identity,["ascii"]=ascii});
}
State("StringBuilder append text","builder",prefix:"start|");
State("StringBuilder destination identity","builder",identity:true);
State("TextWriter append text","writer",prefix:"start|");
State("TextWriter destination identity","writer",identity:true);
State("Builder observes mutable label","builder",[Act("label","ampx")],prefix:"Ω→");
State("Writer observes mutable label","writer",[Act("label","ampx")],prefix:"Ω→");
State("Label changes formatting",actions:[Act("label","ampx")]);
State("Label changes parsing","parse",[Act("label","ampx")],text:"ampx");
State("Relabel changes current display",actions:[Act("label","old"),Act("label","new")]);
State("Relabel retains old parse alias","parse",[Act("label","old"),Act("label","new")],text:"old");
State("Alias admits parse name","parse",[Act("alias","amps")],text:"amps");
State("Alias does not rename display",actions:[Act("alias","amps")]);
State("Alias rebinding replaces parser owner","parse",[Act("alias","shared"),Act("alias","shared","V")],unit:"V",text:"shared");
State("Label collision replaces parser owner","parse",[Act("label","shared"),Act("label","shared","V")],unit:"V",text:"shared");
State("Label collision preserves old owner's display",actions:[Act("label","shared"),Act("label","shared","V")]);
State("Remove label restores intrinsic display",actions:[Act("label","amps"),Act("removeLabel")]);
State("Remove label removes aliases","parse",[Act("label","amps"),Act("alias","other"),Act("removeLabel")],text:"other");
State("Remove aliases preserves current label","parse",[Act("label","old"),Act("label","new"),Act("removeAliases")],text:"new");
State("Remove aliases removes old labels","parse",[Act("label","old"),Act("label","new"),Act("removeAliases")],text:"old");
State("Remove alias ignores unit argument","parse",[Act("alias","shared"),Act("removeAlias","shared","V")],text:"shared");
State("Remove alias retains display label",actions:[Act("label","amps"),Act("removeAlias","amps")]);
State("Reject leading digit",actions:[Act("label","2amps")]);
State("Reject leading mathematical operator",actions:[Act("label","+amps")]);
State("Reject empty label",actions:[Act("label","")]);
State("Reject null label",actions:[Act("label")]);
State("Default admits Unicode label",actions:[Act("label","αμπ")]);
State("Default validates only initial character",actions:[Act("label","name/with space")]);
State("ASCII rejects non-ASCII label",actions:[Act("label","αμπ")],ascii:true);
State("Fresh formatter label state is isolated","isolated",[Act("label","local")]);
State("Default and ASCII state are distinct","otherFlavor",[Act("label","local")]);
State("Quantity formatting observes shared unit labels","quantityFormat");
State("Quantity parsing observes shared unit aliases","quantityParse");
// Intrinsic spellings ported from ASCIIFormat.init/nameFor/format. Unicode square/cubic
// fallbacks are intentional; ASCII flavor is not a blanket character replacement.
const string definitions="""
ONE|one|true
AMPERE|A|true
CANDELA|cd|true
KELVIN|K|true
KILOGRAM|kg|true
METRE|m|true
MOLE|mol|true
SECOND|s|true
GRAM|g|true
RADIAN|rad|true
STERADIAN|sr|true
HERTZ|Hz|true
NEWTON|N|true
PASCAL|Pa|true
JOULE|J|true
WATT|W|true
COULOMB|C|true
VOLT|V|true
FARAD|F|true
OHM|Ohm|true
SIEMENS|S|true
WEBER|Wb|true
TESLA|T|true
HENRY|H|true
CELSIUS|Celsius|true
LUMEN|lm|true
LUX|lx|true
BECQUEREL|Bq|true
GRAY|Gy|true
SIEVERT|Sv|true
KATAL|kat|true
METRE_PER_SECOND|m*s^-1|false
METRE_PER_SQUARE_SECOND|m*s^-2|false
SQUARE_METRE|m²|false
CUBIC_METRE|m³|false
KILOMETRE_PER_HOUR|km/h|true
PERCENT|%|false
MINUTE|min|false
HOUR|h|false
DAY|d|false
WEEK|wk|false
YEAR|yr|false
MONTH|mo|false
LITRE|l|true
""";
foreach(var row in definitions.Split('\n'))
{
    var p=row.Trim().Split('|');plans.Add(new JsonObject{["gap"]=$"ASCII catalog binding {p[0]}",["kind"]="asciiUnit",["field"]=p[0]});
}
const string prefixes="""
QUETTA|Q
RONNA|R
YOTTA|Y
ZETTA|Z
EXA|E
PETA|P
TERA|T
GIGA|G
MEGA|M
KILO|k
HECTO|h
DECA|da
DECI|d
CENTI|c
MILLI|m
MICRO|µ
NANO|n
PICO|p
FEMTO|f
ATTO|a
ZEPTO|z
YOCTO|y
RONTO|r
QUECTO|q
""";
foreach(var row in prefixes.Split('\n'))
{
    var p=row.Trim().Split('|');plans.Add(new JsonObject{["gap"]=$"ASCII prefixed ohm binding {p[0]}",["kind"]="asciiPrefix",["prefixName"]=p[0],["prefixSymbol"]=p[1]});
}
if(plans.Count!=100)throw new Exception("Incomplete implementation inventory.");
for(var i=0;i<plans.Count;i++){plans[i]["id"]=$"UF100-{i+1:000}";plans[i]["requirement"]=$"JP-UF-{i+1:000}";plans[i]["operation"]="unitFormat";}
const string manifestPath="conformance/unitformat-gap-review-100.json",casesPath="conformance/unitformat-reference-cases.json";
var command=args[0];var count=args.Length>1?int.Parse(args[1]):0;
if(command=="init")
{
    if(File.Exists(manifestPath))throw new Exception("Refusing to overwrite the review inventory.");
    File.WriteAllText(manifestPath,new JsonObject{["baselineCommit"]="8cbdf09",["policy"]="100 scoped obligations, not 100 TCK assertions or formal closures",
        ["reviews"]=new JsonArray(plans.Select((p,i)=>new JsonObject{["iteration"]=i+1,["requirement"]=p["requirement"]!.DeepClone(),["input"]=p.DeepClone(),["status"]="PENDING"}).ToArray()),["checkpoints"]=new JsonArray()}.ToJsonString(options));
    var sdoc=new StringBuilder("[DOCUMENT]\nTITLE: SharpUnits mutable unit formatting and ASCII requirements\n");
    foreach(var plan in plans)sdoc.Append($"\n[REQUIREMENT]\nUID: {plan["requirement"]}\nSTATUS: Active\nTITLE: {plan["gap"]}\nSTATEMENT: The supported CLR unit-format profile shall reconcile {plan["gap"]} against independently executed pinned Indriya, retaining explicit API/identity limitations.\nCOMMENT: Implementation: JavaUnitTextFormat.cs and JavaUnitAsciiData.cs. Verification: UnitFormatReferenceFixtureTests.MatchesPinnedUnitFormat. Evidence: conformance/unitformat-gap-review-100.json. No formal TCK closure is implied.\nRELATIONS:\n- TYPE: Parent\n  VALUE: JP-015\n");
    File.WriteAllText("requirements/unit-format-state.sdoc",sdoc.ToString());return;
}
if(count<10||count>100||count%10!=0)throw new ArgumentException("Use checkpoints 10..100.");
if(command=="prepare")
{
    var source=new StringBuilder("// Reviewed ASCII bindings generated by tools/UnitFormatReview.cs.\nnamespace SharpUnits;\ninternal static class JavaUnitAsciiData\n{\n internal static (string Field,string Label,bool Own)[] Definitions()=>new (string,string,bool)[]\n {\n");
    foreach(var row in definitions.Split('\n').Take(Math.Clamp(count-32,0,44))){var p=row.Trim().Split('|');source.AppendLine($"  ({JsonSerializer.Serialize(p[0])},{JsonSerializer.Serialize(p[1])},{p[2]}),");}
    source.AppendLine(" };\n internal static (string Prefix,string Label)[] Prefixes()=>new (string,string)[]\n {");
    foreach(var row in prefixes.Split('\n').Take(Math.Clamp(count-76,0,24))){var p=row.Trim().Split('|');source.AppendLine($"  ({JsonSerializer.Serialize(p[1])},{JsonSerializer.Serialize(p[0]=="MICRO"?"microOhm":p[1]+"Ohm")}),");}
    source.AppendLine(" };\n}");File.WriteAllText("src/SharpUnits/JavaUnitAsciiData.cs",source.ToString());
    File.WriteAllText(casesPath,new JsonObject{["cases"]=new JsonArray(plans.Take(count).Select(p=>p.DeepClone()).ToArray())}.ToJsonString(options));return;
}
Dictionary<string,JsonObject> Rows(string path)=>File.ReadLines(path).Select(line=>JsonNode.Parse(line)!.AsObject()).ToDictionary(row=>row["id"]!.GetValue<string>());
var java=Rows($"../unitformat100-java-{count}.jsonl");var data=JsonNode.Parse(File.ReadAllText(casesPath))!.AsObject();
if(java.Count!=count||data["cases"]!.AsArray().Count!=count)throw new Exception("Incomplete Java observations.");
if(command=="expect")
{
    foreach(var test in data["cases"]!.AsArray())test!["expected"]=java[test["id"]!.GetValue<string>()]["result"]!.DeepClone();
    File.WriteAllText(casesPath,data.ToJsonString(options));return;
}
var clr=Rows($"../unitformat100-dotnet-{count}.jsonl");if(clr.Count!=count)throw new Exception("Incomplete CLR observations.");
foreach(var test in data["cases"]!.AsArray())
{
    var id=test!["id"]!.GetValue<string>();
    if(!JsonNode.DeepEquals(java[id]["result"],clr[id]["result"])||!JsonNode.DeepEquals(test["expected"],java[id]["result"]))
        throw new Exception($"{id}: Java {java[id]}, CLR {clr[id]}");
}
if(command=="check"){Console.WriteLine($"Matched {count} unit-format obligations.");return;}
if(command!="record")throw new ArgumentException("Use init/prepare/expect/record/check.");
var manifest=JsonNode.Parse(File.ReadAllText(manifestPath))!.AsObject();
foreach(var review in manifest["reviews"]!.AsArray().Take(count))
{
    var id=review!["input"]!["id"]!.GetValue<string>();review["input"]!["operation"]="unitFormat";review["java"]=java[id]["result"]!.DeepClone();review["dotnet"]=clr[id]["result"]!.DeepClone();review["status"]="IMPLEMENTED_PROFILE_MATCH";
}
var log=File.ReadAllText($"../unitformat100-test-{count}.log");if(!log.Contains("Failed:     0"))throw new Exception("Full tests did not pass.");
var checkpoints=manifest["checkpoints"]!.AsArray();for(var i=checkpoints.Count-1;i>=0;i--)if(checkpoints[i]!["iteration"]!.GetValue<int>()==count)checkpoints.RemoveAt(i);
checkpoints.Add(new JsonObject{["iteration"]=count,["build"]="PASS",["testsPassed"]=int.Parse(Regex.Match(log,@"Passed:\s+(\d+)").Groups[1].Value),["matched"]=count,["mismatches"]=0});
File.WriteAllText(manifestPath,manifest.ToJsonString(options));Console.WriteLine($"Checkpoint {count}: all {count} unit-format items match Java.");
