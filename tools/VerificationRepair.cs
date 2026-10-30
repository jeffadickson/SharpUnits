#:property PublishAot=false

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;


// Diagnostic audit, not a replacement oracle or a repair of the production harness.
// Each negative witness names input the verification tool must reject. Retaining
// ACCEPTED_UNSAFE_INPUT findings makes the existing false greens reproducible.
if(args.Length!=3)throw new ArgumentException("Usage: VerificationRepair <python> <dotnet> <built-runner>");
if(!Directory.Exists("conformance")||!Directory.Exists("tools")||!Directory.Exists(".."))
    throw new DirectoryNotFoundException("Run from the existing SharpUnits checkout.");
var python=Path.GetFullPath(args[0]);if(!File.Exists(python))throw new FileNotFoundException("Existing Python required",python);
var output=Path.GetFullPath("../verification-repair-probes");
if(!Directory.Exists(Path.GetDirectoryName(output)))throw new DirectoryNotFoundException("Audit output parent missing");
Directory.CreateDirectory(output);
var results=new JsonArray();
(int Code,string Output) Run(string script,params string[] arguments)
{
    var start=new ProcessStartInfo(python){UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true};
    start.ArgumentList.Add(script);foreach(var argument in arguments)start.ArgumentList.Add(argument);
    using var process=Process.Start(start)??throw new InvalidOperationException("Cannot start existing verifier");
    var stdout=process.StandardOutput.ReadToEndAsync();var stderr=process.StandardError.ReadToEndAsync();
    if(!process.WaitForExit(30000)){process.Kill(entireProcessTree:true);throw new TimeoutException("Audit probe timed out");}
    var text=stdout.GetAwaiter().GetResult()+stderr.GetAwaiter().GetResult();
    return (process.ExitCode,text.Length>2000?text[..2000]:text);
}
void Probe(string id,string tool,string first,string second,bool shouldAccept,string failure)
{
    var left=Path.Combine(output,id+"-input.json");var right=Path.Combine(output,id+"-output.jsonl");
    File.WriteAllText(left,first);File.WriteAllText(right,second);
        var inventory=Path.Combine(output,id+"-cases.json");
    File.WriteAllText(inventory,"{\"cases\":[{\"id\":\"x\",\"expected\":17,\"absoluteTolerance\":0}]}");
    if(tool=="compare_java_dotnet.py")
    {
        try
        {
            var row=JsonNode.Parse(first)!;var fixture=new JsonObject{{"id","x"},{"absoluteTolerance",0}};
            if(row.AsObject().ContainsKey("result"))fixture["expected"]=row["result"]?.DeepClone();
            else fixture["expectedError"]=row["error"]?.DeepClone();
            File.WriteAllText(inventory,new JsonObject{{"cases",new JsonArray(fixture)}}.ToJsonString());
        }
        catch(JsonException){ /* Invalid JSON deliberately reaches the strict reader. */ }
    }
    var execution=tool=="compare_java_dotnet.py"?Run("tools/"+tool,left,right,"--cases",inventory):Run("tools/"+tool,left,right);var accepted=execution.Code==0;
    results.Add(new JsonObject{{"id",id},{"tool",tool},{"shouldAccept",shouldAccept},{"accepted",accepted},
        {"status",accepted==shouldAccept?"CONTROL_PASS":shouldAccept?"CONTROL_FAILURE":"ACCEPTED_UNSAFE_INPUT"},{"finding",failure},
        {"exitCode",execution.Code},{"diagnostics",execution.Output}});
}
Probe("AUD-C01","compare_java_dotnet.py","{\"id\":\"x\",\"result\":17}\n","{\"id\":\"x\",\"result\":17}\n",true,"Matching exact observations must pass.");
Probe("AUD-C02","compare_java_dotnet.py","{\"id\":\"x\",\"result\":17}\n","{\"id\":\"x\",\"result\":22}\n",false,"Noncommuting affine composition produces 17 versus 22 at zero; an explicit witness detects reversed order.");
Probe("AUD-C03","compare_java_dotnet.py","{\"id\":\"x\",\"result\":true}\n","{\"id\":\"x\",\"result\":1}\n",false,"Boolean and numeric observations must remain distinct.");
Probe("AUD-P01","compare_java_dotnet.py","{\"id\":\"x\",\"result\":9007199254740992}\n","{\"id\":\"x\",\"result\":9007199254740993}\n",false,"Zero-tolerance integers must not collapse through binary64.");
Probe("AUD-P02","compare_java_dotnet.py","","",false,"An empty comparison cannot qualify an expected campaign.");
Probe("AUD-P03","compare_java_dotnet.py","{\"id\":\"x\",\"error\":null}\n","{\"id\":\"x\",\"error\":null}\n",false,"Null is not a valid error category.");
Probe("AUD-P04","compare_java_dotnet.py","{\"id\":\"x\",\"error\":\"\"}\n","{\"id\":\"x\",\"error\":\"\"}\n",false,"Empty error categories do not establish a matched failure.");
Probe("AUD-P05","compare_java_dotnet.py","{\"id\":\"x\",\"result\":Infinity}\n","{\"id\":\"x\",\"result\":Infinity}\n",false,"Nonfinite JSON numbers lie outside the declared finite profile.");
Probe("AUD-P06","compare_java_dotnet.py","{\"id\":\"x\",\"result\":1,\"absoluteTolerance\":Infinity}\n","{\"id\":\"x\",\"result\":999}\n",false,"An invalid self-declared infinite tolerance must not hide a numerical defect.");
Probe("AUD-P07","check_fixture_expectations.py","{\"cases\":[{\"id\":\"x\",\"expected\":1e-20}]}","{\"id\":\"x\",\"result\":0}\n",false,"An omitted tolerance must not silently authorize loss of a tiny value.");
Probe("AUD-P08","check_fixture_expectations.py","{\"cases\":[{\"id\":\"x\",\"expectedError\":\"ARITHMETIC\"}]}","{\"id\":\"x\",\"error\":\"ARITHMETIC\",\"result\":999}\n",false,"A record cannot be both a result and an expected error.");
Probe("AUD-P09","check_fixture_expectations.py","{\"cases\":[{\"id\":\"x\",\"expected\":1},{\"id\":\"x\",\"expected\":1}]}","{\"id\":\"x\",\"result\":1}\n",false,"Duplicate fixture IDs must not collapse into one executed case.");

// Execute the actual built observer, rather than reproducing its projection here.
var witness=Path.Combine(output,"unit-witness.json");
File.WriteAllText(witness,"""
{"cases":[{"id":"a","operation":"core","domain":"quantity","action":"identity","left":{"value":{"kind":"integer","value":"1"},"unit":"AMPERE"}},{"id":"b","operation":"core","domain":"quantity","action":"identity","left":{"value":{"kind":"integer","value":"1"},"unit":{"kind":"alternate","left":"AMPERE","symbol":"audit_ampere"}}}]}
""");
var start=new ProcessStartInfo(args[1]){RedirectStandardOutput=true,RedirectStandardError=true,UseShellExecute=false};
start.ArgumentList.Add(Path.GetFullPath(args[2]));start.ArgumentList.Add(witness);
using(var process=Process.Start(start)!)
{
    var text=process.StandardOutput.ReadToEnd();var error=process.StandardError.ReadToEnd();process.WaitForExit();
    if(process.ExitCode!=0)throw new InvalidOperationException(error);
    var rows=text.Split('\n',StringSplitOptions.RemoveEmptyEntries).Select(line=>JsonNode.Parse(line)!).ToArray();
    var a=rows[0]["result"]!;var b=rows[1]["result"]!;
    var valid=rows.Length==2&&JsonNode.DeepEquals(a["value"],b["value"])&&JsonNode.DeepEquals(a["scale"],b["scale"])&&!JsonNode.DeepEquals(a["unit"],b["unit"])&&a["converter"] is not null&&b["converter"] is not null;
    results.Add(new JsonObject{{"id","AUD-P10"},{"status",valid?"CONTROL_PASS":"CONTROL_FAILURE"},{"actualObserverOutput",text}});
}
var isolated=Path.Combine(output,"ledger");Directory.CreateDirectory(Path.Combine(isolated,"conformance"));
File.Copy("conformance/tck-assertions.json",Path.Combine(isolated,"conformance/tck-assertions.json"),true);
var ledger=JsonNode.Parse(File.ReadAllText("conformance/tck-closure-ledger.json"))!;
var entry=ledger["entries"]!.AsArray().First(item=>item!["test"] is not null)!;
entry["status"]="VERIFIED";entry["evidence"]="fabricated";entry["javaEvidence"]="fabricated";
File.WriteAllText(Path.Combine(isolated,"conformance/tck-closure-ledger.json"),ledger.ToJsonString());
var closure=Run("tools/validate_tck_ledger.py","--root",isolated,"--sources",Directory.GetCurrentDirectory());
results.Add(new JsonObject{{"id","AUD-P11"},{"status",closure.Code!=0?"CONTROL_PASS":"ACCEPTED_FABRICATED_CLOSURE"},{"diagnostics",closure.Output}});
var hashes=new JsonObject();foreach(var path in new[]{"tools/VerificationRepair.cs","tools/verification_contract.py","tools/compare_java_dotnet.py","tools/check_fixture_expectations.py","tools/validate_tck_ledger.py","tools/qualification_evidence.py",args[2]})hashes[path]=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
var validAll=results.All(item=>item!["status"]!.GetValue<string>()=="CONTROL_PASS");
File.WriteAllText("conformance/verification-repair-results.json",new JsonObject{{"verdict",validAll?"LOCAL_REPAIR_PROBES_PASS":"REPAIR_PROBES_FAILED"},{"probes",results},{"sourceAndRunnerSha256",hashes},{"limitations","Local negative witnesses only; does not grant formal assertion closure or platform CI qualification. Historical audit artifacts remain unchanged."}}.ToJsonString(new JsonSerializerOptions{WriteIndented=true})+"\n");
Console.WriteLine($"Repair probes: {results.Count}, passing: {results.Count(item=>item!["status"]!.GetValue<string>()=="CONTROL_PASS")}");
return validAll?0:1;
