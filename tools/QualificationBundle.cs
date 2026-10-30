#:property PublishAot=false
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Xml.Linq;
// CI evidence producer. Local rehearsal is explicitly untrusted for formal closure.
if(args.Length is <3 or >4||!Directory.Exists("src")||!Directory.Exists("tests"))throw new ArgumentException("QualificationBundle <windows|ubuntu|java> <output-directory> <trx-or-surefire-directory> [--local]");
var role=args[0];if(role is not ("windows" or "ubuntu" or "java"))throw new ArgumentException("Unknown evidence role");
var local=args.Length==4&&args[3]=="--local";
if(!local&&Environment.GetEnvironmentVariable("GITHUB_ACTIONS")!="true")throw new InvalidOperationException("CI context required; --local rehearsals cannot qualify");
var output=Path.GetFullPath(args[1]);if(!Directory.Exists(Path.GetDirectoryName(output)))throw new DirectoryNotFoundException("Evidence parent missing");
Directory.CreateDirectory(output);
byte[] Git(string root,params string[] arguments)
{
    var start=new ProcessStartInfo("git"){RedirectStandardOutput=true,RedirectStandardError=true,UseShellExecute=false};
    start.ArgumentList.Add("-C");start.ArgumentList.Add(root);foreach(var argument in arguments)start.ArgumentList.Add(argument);
    using var process=Process.Start(start)!;var error=process.StandardError.ReadToEndAsync();using var buffer=new MemoryStream();process.StandardOutput.BaseStream.CopyTo(buffer);
    process.WaitForExit();if(process.ExitCode!=0)throw new InvalidOperationException(error.GetAwaiter().GetResult());return buffer.ToArray();
}
string Hash(byte[] bytes)=>Convert.ToHexString(SHA256.HashData(bytes));
var revision=Encoding.UTF8.GetString(Git(".","rev-parse","HEAD")).Trim();
// Bind all tracked blobs, including resources and the complete build graph.
var paths=Encoding.UTF8.GetString(Git(".","ls-tree","-r",revision)).Split('\n',StringSplitOptions.RemoveEmptyEntries)
    .Where(line=>line.Split(' ')[1]=="blob").Select(line=>line.Split('\t',2)[1]);
if(!local&&Encoding.UTF8.GetString(Git(".","status","--porcelain","--untracked-files=no")).Length!=0)
    throw new InvalidDataException("CI requires an unchanged tracked checkout");
var proof=new SortedDictionary<string,string>(StringComparer.Ordinal);
foreach(var path in paths)
{
    // Exact Git blob bytes preserve binary resources. Git status above applies
    // repository clean/text filters when detecting tracked working-tree drift.
    proof[path]=Hash(Git(".","show",revision+":"+path));
}
var pins=JsonNode.Parse(File.ReadAllText("conformance/upstream-pins.json"))!;var pin=pins["tck"]!["commit"]!.GetValue<string>();
string payload;var rawReports=new List<object>();
if(role=="java")
{
    if(!Directory.Exists(args[2]))throw new DirectoryNotFoundException("Surefire output missing");
    var reports=Directory.GetFiles(args[2],"TEST-*.xml");
    Directory.CreateDirectory(Path.Combine(output,"surefire"));
    foreach(var file in reports)
    {
        var relative="surefire/"+Path.GetFileName(file);File.Copy(file,Path.Combine(output,relative),overwrite:true);
        rawReports.Add(new{path=relative,sha256=Hash(File.ReadAllBytes(file))});
    }
    var testcases=reports.SelectMany(file=>XDocument.Load(file).Descendants("testcase")).ToArray();
    var failed=testcases.Count(test=>test.Elements("failure").Any()||test.Elements("error").Any());
    var methods=testcases.Where(test=>!test.Elements("failure").Any()&&!test.Elements("error").Any()&&!test.Elements("skipped").Any())
        .Select(test=>((string?)test.Attribute("classname"))!.Split('.').Last()+"."+(string?)test.Attribute("name")).ToHashSet();
    var assertions=new List<string>();var inventory=JsonNode.Parse(File.ReadAllText("conformance/tck-assertions.json"))!;
    foreach(var suite in inventory["suites"]!.AsArray())
    {
        var path=suite!["path"]!.GetValue<string>();var text=Encoding.UTF8.GetString(Git("upstream/unit-tck","show",pin+":"+path));
        var annotations=Regex.Matches(text,"@SpecAssertion\\s*\\(([\\s\\S]*?)\\)");var found=new List<(string Id,string Method)>();
        for(var i=0;i<annotations.Count;i++)
        {
            var annotation=annotations[i];var id=Regex.Match(annotation.Groups[1].Value,"\\bid\\s*=\\s*\"([^\"]+)\"").Groups[1].Value;
            var end=i+1<annotations.Count?annotations[i+1].Index:text.Length;
            var method=Regex.Match(text[(annotation.Index+annotation.Length)..end],"\\bpublic\\s+void\\s+(\\w+)\\s*\\(").Groups[1].Value;
            found.Add((id,Path.GetFileNameWithoutExtension(path)+"."+method));
        }
        var seen=new Dictionary<string,int>();foreach(var item in found){seen[item.Id]=seen.GetValueOrDefault(item.Id)+1;if(methods.Contains(item.Method))assertions.Add(found.Count(other=>other.Id==item.Id)>1?item.Id+"-"+seen[item.Id]:item.Id);}
    }
    if(failed!=0||methods.Count==0)throw new InvalidDataException("Nonempty passing pinned Java execution required");
    payload="java-report.json";File.WriteAllText(Path.Combine(output,payload),JsonSerializer.Serialize(new{tckCommit=pin,failed,passed=methods.Count,assertions,passedMethods=methods.OrderBy(x=>x,StringComparer.Ordinal).ToArray()}));
}
else
{
    if(!File.Exists(args[2]))throw new FileNotFoundException("TRX missing");
    var document=XDocument.Load(args[2]);var counters=document.Descendants().Single(node=>node.Name.LocalName=="Counters");
    if((int?)counters.Attribute("passed")<=0||(int?)counters.Attribute("failed")!=0)throw new InvalidDataException("Passing nonempty TRX required");
    payload="results.trx";File.Copy(args[2],Path.Combine(output,payload),overwrite:true);
}
var manifest=new{schemaVersion=1,role,producerContext=local?"LOCAL_UNTRUSTED":"CI",runId=local?"LOCAL":Environment.GetEnvironmentVariable("GITHUB_RUN_ID"),testedCommit=revision,tckCommit=pin,sourceSha256=proof,rawReports,payload=new{path=payload,sha256=Hash(File.ReadAllBytes(Path.Combine(output,payload)))}};
File.WriteAllText(Path.Combine(output,"qualification.json"),JsonSerializer.Serialize(manifest,new JsonSerializerOptions{WriteIndented=true}));
Console.WriteLine(local?"Untrusted local rehearsal captured; cannot establish formal qualification.":"CI qualification bundle captured.");
