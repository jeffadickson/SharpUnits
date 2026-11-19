#:property PublishAot=false
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
// Preserve actual phase outcomes. An unchanged file may use a later executed
// mutation outcome; changed source requires a complete replacement report for it.
if(args.Length is <3 or >8||!Directory.Exists("conformance"))throw new ArgumentException("MutationDebtMerge <broad> <final> [additional-reports...] <output>");
var names=new HashSet<string>{"JavaNumber.cs","JavaUnit.cs","JavaNumberQuantity.cs","JavaUnitAdapter.cs","JavaUnitConverterAdapter.cs","JavaDimensionAdapter.cs","SiPrefix.cs","SiPrefixes.cs"};
string Identity(string[] lines,JsonNode mutant)
{
 var start=mutant["location"]!["start"]!;var end=mutant["location"]!["end"]!;var line=start["line"]!.GetValue<int>();
 var statement=string.Join("\n",lines.Skip(line-1).Take(end["line"]!.GetValue<int>()-line+1));
 var declaration=lines.Take(line).LastOrDefault(text=>System.Text.RegularExpressions.Regex.IsMatch(text,@"^\s*(public|private|internal|protected)\b"))?.Trim()??"";
 return JsonSerializer.Serialize(new[]{declaration,statement,mutant["mutatorName"]!.ToJsonString(),mutant["replacement"]!.ToJsonString(),start["column"]!.ToJsonString(),end["column"]!.ToJsonString()});
}
var files=new Dictionary<string,(string Source,Dictionary<string,JsonNode> Mutants)>();var reports=new JsonArray();
for(var phase=0;phase<args.Length-1;phase++)
{
 var path=args[phase];var report=JsonNode.Parse(File.ReadAllText(path))!;
 reports.Add(new JsonObject{{"phase",phase},{"path",path},{"sha256",Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)))}});
 foreach(var pair in report["files"]!.AsObject())
 {
  var name=Path.GetFileName(pair.Key);if(!names.Contains(name))continue;
  var source=pair.Value!["source"]!.GetValue<string>();var lines=source.Split('\n');
  var replace=!files.TryGetValue(name,out var current)||current.Source!=source;
  var mutations=replace?new Dictionary<string,JsonNode>():current.Mutants;
  foreach(var mutant in pair.Value["mutants"]!.AsArray())
  {
   var status=mutant!["status"]!.GetValue<string>();
   if(!replace&&status is not ("Killed" or "Survived" or "Timeout" or "NoCoverage"))continue;
   var item=mutant.DeepClone();item["evidencePhase"]=phase;mutations[Identity(lines,mutant)]=item;
  }
  files[name]=(source,mutations);
 }
}
var outputFiles=new JsonArray();var totals=new SortedDictionary<string,int>();
foreach(var pair in files.OrderBy(pair=>pair.Key))
{
 var counts=new SortedDictionary<string,int>();var outstanding=new JsonArray();var lines=pair.Value.Source.Split('\n');
 foreach(var mutant in pair.Value.Mutants.Values)
 {
  var status=mutant["status"]!.GetValue<string>();counts[status]=counts.GetValueOrDefault(status)+1;totals[status]=totals.GetValueOrDefault(status)+1;
  if(status is not ("Survived" or "NoCoverage" or "Timeout"))continue;
  var line=mutant["location"]!["start"]!["line"]!.GetValue<int>();var end=mutant["location"]!["end"]!["line"]!.GetValue<int>();
  outstanding.Add(new JsonObject{{"id",mutant["id"]!.DeepClone()},{"phase",mutant["evidencePhase"]!.DeepClone()},{"status",status},{"line",line},{"mutator",mutant["mutatorName"]!.DeepClone()},{"replacement",mutant["replacement"]!.DeepClone()},{"statement",string.Join("\n",lines.Skip(line-1).Take(end-line+1)).Trim()},{"disposition","OPEN_REVIEW"}});
 }
 outputFiles.Add(new JsonObject{{"file",pair.Key},{"sourceSha256",Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(pair.Value.Source)))},{"counts",JsonSerializer.SerializeToNode(counts)},{"outstanding",outstanding}});
}
File.WriteAllText(args[^1],new JsonObject{{"policy","Combined measured outcomes, not a single-run mutation score. Later executed outcomes supersede only matching mutations in unchanged source. A changed source file is replaced in full by its later full-file report; ignored/compile errors never become kills."},{"reports",reports},{"counts",JsonSerializer.SerializeToNode(totals)},{"files",outputFiles}}.ToJsonString(new JsonSerializerOptions{WriteIndented=true})+"\n");
Console.WriteLine(JsonSerializer.Serialize(totals));
