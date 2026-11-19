#:property PublishAot=false
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

// Retain scoped mutation evidence without treating timeouts or a score as proof.
if(args.Length is <2 or >3)throw new ArgumentException("Usage: CoreMutationReview <report.json> <evidence.json> [tested-snapshot-root]");
if(!Directory.Exists("conformance")||!Directory.Exists(Path.GetDirectoryName(Path.GetFullPath(args[1]))))
    throw new DirectoryNotFoundException("Existing repository/output parent required.");
var report=JsonNode.Parse(File.ReadAllText(args[0]))!;
var scope=new HashSet<string>{"JavaNumber.cs","JavaUnit.cs","JavaNumberQuantity.cs","JavaUnitAdapter.cs","JavaUnitConverterAdapter.cs","JavaDimensionAdapter.cs","SiPrefix.cs","SiPrefixes.cs"};
var files=new JsonArray();var counts=new Dictionary<string,int>(StringComparer.Ordinal);
foreach(var file in report["files"]!.AsObject())
{
    var name=Path.GetFileName(file.Key);if(!scope.Contains(name))continue;
    var source=file.Value!["source"]!.GetValue<string>();var mutants=file.Value["mutants"]!.AsArray();
    var grouped=new Dictionary<string,int>();var outstanding=new JsonArray();
    foreach(var mutant in mutants)
    {
        var status=mutant!["status"]!.GetValue<string>();counts[status]=counts.GetValueOrDefault(status)+1;grouped[status]=grouped.GetValueOrDefault(status)+1;
        if(status is not ("Survived" or "NoCoverage" or "Timeout"))continue;
        var line=mutant["location"]!["start"]!["line"]!.GetValue<int>();
        var lines=source.Split('\n');var excerpt=string.Join("\n",lines.Skip(line-1).Take(Math.Min(4,mutant["location"]!["end"]!["line"]!.GetValue<int>()-line+1))).Trim();
        outstanding.Add(new JsonObject{{"id",mutant["id"]!.DeepClone()},{"status",status},{"line",line},
            {"mutator",mutant["mutatorName"]!.DeepClone()},{"replacement",mutant["replacement"]!.DeepClone()},
            {"sourceExcerpt",excerpt},{"disposition",status=="Timeout"?"INCONCLUSIVE_TIMEOUT":status=="NoCoverage"?"OPEN_COVERAGE_DEBT":mutant["mutatorName"]!.GetValue<string>()=="String mutation"?"OPEN_TEXT_OR_DIAGNOSTIC_CONTRACT": "OPEN_SEMANTIC_OR_REDUNDANT_GUARD_REVIEW"}});
    }
    files.Add(new JsonObject{{"file",name},{"sourceTextSha256",Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)))},
        {"counts",JsonSerializer.SerializeToNode(grouped)},{"outstanding",outstanding}});
}
var output=new JsonObject{{"tool","Stryker.NET 5.0.0"},{"reportSha256",Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(args[0])))},
    {"scope","Eight core library files; excludes Java, tests, automation and factory/projection bridge lines in other files."},
    {"counts",JsonSerializer.SerializeToNode(counts)},{"files",files},
    {"limitations","Timeouts are reported separately, not counted as demonstrated kills. Surviving/uncovered mutants require review; compile errors/ignored mutants are not execution evidence. Source hashes refer to report UTF-8 source text."}};
var snapshot=args.Length==3?Path.GetFullPath(args[2]):Directory.GetCurrentDirectory();
var testRoot=Path.Combine(snapshot,"tests/SharpUnits.Tests");if(!Directory.Exists(testRoot))throw new DirectoryNotFoundException("Test source snapshot missing");
var tests=new JsonObject();
foreach(var file in Directory.EnumerateFiles(testRoot,"*",SearchOption.AllDirectories).Where(file=>file.EndsWith(".cs")||file.EndsWith(".csproj")).Where(file=>!file.Contains(Path.DirectorySeparatorChar+"obj"+Path.DirectorySeparatorChar)&&!file.Contains(Path.DirectorySeparatorChar+"bin"+Path.DirectorySeparatorChar)))
    tests[Path.GetRelativePath(snapshot,file).Replace('\\','/')]=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(File.ReadAllText(file).Replace("\r\n","\n"))));
output["canonicalTestSourceSha256"]=tests;
output["testSnapshotPolicy"]="Hashes identify the actual frozen test-source snapshot; later strengthening requires its own scoped replay. Metadata-only fixture annotations do not change outcomes.";
File.WriteAllText(args[1],output.ToJsonString(new JsonSerializerOptions{WriteIndented=true})+"\n");
Console.WriteLine(JsonSerializer.Serialize(counts));
