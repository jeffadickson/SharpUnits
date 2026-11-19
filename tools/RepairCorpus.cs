#:property PublishAot=false
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Security.Cryptography;
using System.Text;
// Retain only actually executed Java observations; never derive expectations from CLR output.
if(args.Length is <2 or >3||!Directory.Exists("conformance")||!Directory.Exists("tools"))throw new ArgumentException("Usage: RepairCorpus <prepare|retain> <java.jsonl-or-unused> [corpus]");
var path=args.Length==3?args[2]:"conformance/work-package-one-cases.json";
var document=JsonNode.Parse(File.ReadAllText(path))!;var cases=document["cases"]!.AsArray();
if(args[0]=="prepare")
{
    foreach(var fixture in cases)
    {
        if(fixture!["domain"]!.GetValue<string>()=="number"&&fixture["action"]!.GetValue<string>() is "add" or "subtract")fixture["compatibilityProfile"]="PINNED_INDRIYA_QUIRKS";
    }
    if(!cases.Any(item=>item!["id"]!.GetValue<string>()=="RAW-DECIMAL"))
        foreach(var (id,number) in new[]{("RAW-DECIMAL","{\"kind\":\"decimal\",\"value\":\"1.00\"}"),("RAW-DOUBLE","{\"kind\":\"double\",\"value\":\"1.0\"}"),("RAW-RATIONAL","{\"kind\":\"rational\",\"numerator\":\"2\",\"denominator\":\"2\"}"),("RAW-INTEGER","{\"kind\":\"integer\",\"value\":\"9007199254740993\"}")})
            cases.Add(new JsonObject{{"id",id},{"operation","core"},{"domain","number"},{"action","rawInput"},{"left",JsonNode.Parse(number)}});
}
else if(args[0]=="retain")
{
    var lines=File.ReadAllLines(args[1]).Where(line=>!string.IsNullOrWhiteSpace(line)).ToArray();
    var rows=lines.Select(line=>(Data:JsonNode.Parse(line)!,Hash:Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(line))))).ToDictionary(row=>row.Data["id"]!.GetValue<string>());
    if(rows.Count!=cases.Count)throw new InvalidDataException("Incomplete Java inventory");
    var approved=new Dictionary<string,string[]>{["java.lang.ArithmeticException"]=new[]{"System.DivideByZeroException"},["java.lang.IllegalArgumentException"]=new[]{"System.ArgumentException","System.ArgumentOutOfRangeException","System.InvalidOperationException","System.DivideByZeroException"},["java.lang.UnsupportedOperationException"]=new[]{"System.NotSupportedException"},["javax.measure.IncommensurableException"]=new[]{"System.InvalidOperationException"},["javax.measure.UnconvertibleException"]=new[]{"System.InvalidOperationException"},["java.lang.NumberFormatException"]=new[]{"System.FormatException"}};
    foreach(var fixture in cases)
    {
        var row=rows[fixture!["id"]!.GetValue<string>()];
        if(fixture["independentExpected"] is not null&&!JsonNode.DeepEquals(fixture["independentExpected"],row.Data["result"]))
            throw new InvalidDataException("Independent expectation disagrees with Java: "+fixture["id"]);
        fixture["expected"]=row.Data["result"]!.DeepClone();
        fixture["javaRepresentations"]=row.Data["javaRepresentations"]!.DeepClone();
        fixture["oracle"]=new JsonObject{{"kind",fixture["compatibilityProfile"] is null?"PINNED_IMPLEMENTATION_OBSERVATION":"EXPLICIT_PINNED_IMPLEMENTATION_QUIRKS"},{"indriyaCommit","07a9aac9a46695ea0a4acf95b74ab7d92766f49a"},{"observationSha256",row.Hash},{"scope","Measured implementation; not specification or mathematical acceptance"},{"representationMapping","Raw Java class/text/scale retained separately; CLR finite numeric profile is explicitly normalized"}};
        if(fixture["independentExpected"] is not null)
            fixture["oracle"]!["independentCheck"]="Literal authored before reference execution; capture rejects disagreement";
        if(row.Data["javaException"] is not null)
        {
            var type=row.Data["javaException"]!.GetValue<string>();if(!approved.TryGetValue(type,out var clrTypes))throw new InvalidDataException("Unapproved Java exception: "+type);
            fixture["javaException"]=type;fixture["allowedClrExceptions"]=JsonSerializer.SerializeToNode(clrTypes);
        }
    }
}
else if(args[0]=="annotate")
{
    // Annotate an already checked legacy corpus without rewriting its expectations.
    var rows=File.ReadAllLines(args[1]).Where(line=>!string.IsNullOrWhiteSpace(line))
        .Select(line=>(Data:JsonNode.Parse(line)!,Hash:Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(line))))).ToDictionary(row=>row.Data["id"]!.GetValue<string>());
    if(rows.Count!=cases.Count)throw new InvalidDataException("Incomplete legacy reference inventory");
    foreach(var fixture in cases)
    {
        var row=rows[fixture!["id"]!.GetValue<string>()];
        fixture["absoluteTolerance"]??=JsonValue.Create(0);
        fixture["oracle"]=new JsonObject{{"kind","PINNED_IMPLEMENTATION_OBSERVATION"},{"indriyaCommit","07a9aac9a46695ea0a4acf95b74ab7d92766f49a"},{"observationSha256",row.Hash},{"scope","Legacy normalized projection; raw Java representation/full structural identity not observed. Passing projection is not full behavioral conformance."}};
    }
}
else throw new ArgumentException("Unknown mode");
File.WriteAllText(path,document.ToJsonString(new JsonSerializerOptions{WriteIndented=true})+"\n");
Console.WriteLine($"{args[0]}: {cases.Count} cases");
