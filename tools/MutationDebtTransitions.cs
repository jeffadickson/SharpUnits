#:property PublishAot=false
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
// Exact statement/span identity prevents same-line string tokens or shifted IDs
// from being mistaken for one another. Changed source statements stay unmatched.
if(args.Length!=3||!Directory.Exists("conformance"))throw new ArgumentException("MutationDebtTransitions <old-raw-report> <new-raw-report> <output>");
var scope=new HashSet<string>{"JavaNumber.cs","JavaUnit.cs","JavaNumberQuantity.cs","JavaUnitAdapter.cs","JavaUnitConverterAdapter.cs","JavaDimensionAdapter.cs","SiPrefix.cs","SiPrefixes.cs"};
Dictionary<string,JsonObject> Read(string path)
{
 var result=new Dictionary<string,JsonObject>();var report=JsonNode.Parse(File.ReadAllText(path))!;
 foreach(var pair in report["files"]!.AsObject())
 {
  var file=Path.GetFileName(pair.Key);if(!scope.Contains(file))continue;
  var lines=pair.Value!["source"]!.GetValue<string>().Split('\n');
  foreach(var item in pair.Value["mutants"]!.AsArray())
  {
   var location=item!["location"]!;var start=location["start"]!;var end=location["end"]!;
   var line=start["line"]!.GetValue<int>();var length=end["line"]!.GetValue<int>()-line+1;
   var statement=string.Join("\n",lines.Skip(line-1).Take(length)).TrimEnd('\r');
   var declaration=lines.Take(line).LastOrDefault(text=>System.Text.RegularExpressions.Regex.IsMatch(text,@"^\s*(public|private|internal|protected)\b"))?.Trim()??"";
   var key=JsonSerializer.Serialize(new[]{file,declaration,item["mutatorName"]!.GetValue<string>(),item["replacement"]!.GetValue<string>(),statement,start["column"]!.ToJsonString(),end["column"]!.ToJsonString()});
   if(!result.TryAdd(key,new JsonObject{{"file",file},{"id",item["id"]!.DeepClone()},{"status",item["status"]!.DeepClone()},{"source",statement},{"line",line}}))throw new InvalidDataException("Ambiguous mutant identity");
  }
 }
 return result;
}
var before=Read(args[0]);var after=Read(args[1]);var rows=new JsonArray();var counts=new SortedDictionary<string,int>();
foreach(var pair in before.Where(pair=>pair.Value["status"]!.GetValue<string>() is "Survived" or "NoCoverage" or "Timeout"))
{
 var old=pair.Value;var matched=after.GetValueOrDefault(pair.Key);var status=matched?["status"]?.GetValue<string>()??"SOURCE_REVISED_OR_UNMATCHED";
 var transition=old["status"]!.GetValue<string>()+"->"+status;counts[transition]=counts.GetValueOrDefault(transition)+1;
 rows.Add(new JsonObject{{"file",old["file"]!.DeepClone()},{"historicalId",old["id"]!.DeepClone()},{"historicalStatus",old["status"]!.DeepClone()},{"currentId",matched?["id"]?.DeepClone()},{"currentStatus",status},{"statement",old["source"]!.DeepClone()}});
}
var output=new JsonObject{{"historicalReportSha256",Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(args[0])))},{"currentReportSha256",Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(args[1])))},{"counts",JsonSerializer.SerializeToNode(counts)},{"transitions",rows},{"policy","Only exact statement/mutator/replacement/token-span matches qualify as transitions. Revised source, ignored, compile-error and timeout outcomes are never inferred kills. Reports with different scopes/universes have separate denominators."}};
File.WriteAllText(args[2],output.ToJsonString(new JsonSerializerOptions{WriteIndented=true})+"\n");Console.WriteLine(JsonSerializer.Serialize(counts));
