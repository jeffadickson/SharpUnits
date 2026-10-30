#:property PublishAot=false
#:project ../src/SharpUnits/SharpUnits.csproj
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using SharpUnits;

// Diagnostic audit, not a replacement oracle or a repair of the production harness.
// Each negative witness names input the verification tool must reject. Retaining
// ACCEPTED_UNSAFE_INPUT findings makes the existing false greens reproducible.
if(args.Length!=1)throw new ArgumentException("Usage: VerificationAudit <existing-python-executable>");
if(!Directory.Exists("conformance")||!Directory.Exists("tools")||!Directory.Exists(".."))
    throw new DirectoryNotFoundException("Run from the existing SharpUnits checkout.");
var python=Path.GetFullPath(args[0]);if(!File.Exists(python))throw new FileNotFoundException("Existing Python required",python);
var output=Path.GetFullPath("../verification-audit-probes");
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
    var execution=Run("tools/"+tool,left,right);var accepted=execution.Code==0;
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

// Reproduce a missing observation field using real public domain objects. The
// quantity runner's value/scale projection collides although unit identity differs.
var ampere=JavaUnit.FromCatalog("AMPERE");
var correct=new JavaNumberQuantity(JavaNumber.One,ampere);
var wrongUnit=new JavaNumberQuantity(JavaNumber.One,ampere.Alternate("audit_ampere"));
string Projection(JavaNumberQuantity quantity)=>$"{quantity.Value.Kind}|{quantity.Value.Fraction.Numerator}/{quantity.Value.Fraction.Denominator}|{quantity.Value.DecimalScale}|{quantity.Scale.ToString().ToUpperInvariant()}";
var collision=Projection(correct)==Projection(wrongUnit)&&!correct.Unit.Equals(wrongUnit.Unit);
if(!collision)throw new InvalidOperationException("The quantity-unit audit witness no longer reproduces; re-audit the observation schema");
results.Add(new JsonObject{{"id","AUD-P10"},{"status","OBSERVATION_COLLISION"},{"correctProjection",Projection(correct)},
    {"wrongUnitProjection",Projection(wrongUnit)},{"unitEquality",correct.Unit.Equals(wrongUnit.Unit)}});

// Exercise closure accounting against an isolated copy, never the real ledger.
var isolated=Path.Combine(output,"ledger");Directory.CreateDirectory(Path.Combine(isolated,"tools"));Directory.CreateDirectory(Path.Combine(isolated,"conformance"));
File.Copy("tools/validate_tck_ledger.py",Path.Combine(isolated,"tools/validate_tck_ledger.py"),overwrite:true);
File.Copy("conformance/tck-assertions.json",Path.Combine(isolated,"conformance/tck-assertions.json"),overwrite:true);
var ledger=JsonNode.Parse(File.ReadAllText("conformance/tck-closure-ledger.json"))!;
var fabricated=ledger["entries"]![0]!;fabricated["status"]="VERIFIED";fabricated["test"]="NoSuchClass.NoSuchTest";
fabricated["evidence"]="nonexistent.md";fabricated["javaEvidence"]="nonexistent.md";
File.WriteAllText(Path.Combine(isolated,"conformance/tck-closure-ledger.json"),ledger.ToJsonString());
var closure=Run(Path.Combine(isolated,"tools/validate_tck_ledger.py"));
results.Add(new JsonObject{{"id","AUD-P11"},{"status",closure.Code==0?"ACCEPTED_FABRICATED_CLOSURE":"CONTROL_PASS"},
    {"exitCode",closure.Code},{"diagnostics",closure.Output}});

var cases=JsonNode.Parse(File.ReadAllText("conformance/work-package-one-cases.json"))!["cases"]!.AsArray();
var quantities=cases.Where(item=>item!["domain"]!.GetValue<string>()=="quantity").ToArray();
var quantityUnits=quantities.Count(item=>item!.AsObject().ContainsKey("resultUnit"));
var quantityPredicates=quantities.Count(item=>item!["action"]!.GetValue<string>() is "equal" or "equivalent" or "compare");
var manifest=JsonNode.Parse(File.ReadAllText("conformance/work-package-one-verification.json"))!;
var hashes=new JsonArray();
foreach(var group in new[]{"sourceSha256","rawEvidenceSha256"})
    foreach(var entry in manifest[group]!.AsObject())
    {
        var actual=File.Exists(entry.Key)?Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(entry.Key))):"MISSING";
        hashes.Add(new JsonObject{{"group",group},{"path",entry.Key},{"matches",actual==entry.Value!.GetValue<string>()}});
    }
var invalidExecution=results.Any(item=>item!["status"]!.GetValue<string>()=="CONTROL_FAILURE")
    ||hashes.Any(item=>!item!["matches"]!.GetValue<bool>());
var verifierHashes=new JsonObject();
foreach(var path in new[]{"tools/compare_java_dotnet.py","tools/check_fixture_expectations.py","tools/validate_tck_ledger.py", "tools/VerificationAudit.cs"})
    verifierHashes[path]=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
var evidence=new JsonObject{{"auditedBaselineCommit","563a692d0f7b7c78e147eb09965d97d933bc56f6"},
    {"verdict",invalidExecution?"AUDIT_EXECUTION_INVALID":"NOT_QUALIFIED_FOR_CLOSURE"},
    {"verifierSourceSha256",verifierHashes},{"probes",results},{"historicalHashChecks",hashes},
    {"corpus",new JsonObject{{"cases",cases.Count},{"quantityCases",quantities.Length},{"unitOnlyQuantityCases",quantityUnits},
        {"valueScaleOnlyQuantityCases",quantities.Length-quantityUnits-quantityPredicates},{"predicateOrOrderQuantityCases",quantityPredicates},
        {"normalizedQuantityCases",quantities.Count(item=>item!.AsObject().ContainsKey("normalizeTo"))},
        {"broadErrorObservations",cases.Count(item=>item!["expected"] is JsonValue value&&value.TryGetValue<string>(out var text)&&text.StartsWith("ERR:",StringComparison.Ordinal))}}},
    {"limitations","Findings collector for the stated historical baseline, not a qualification gate. Current verifier hashes identify the executed tools. Control/hash failures invalidate execution; a clean probe set would still require an independent qualification decision. No production/harness repair, formal closure, native compilation, or audit-tool coverage is claimed."}};
File.WriteAllText("conformance/verification-audit-results.json",evidence.ToJsonString(new JsonSerializerOptions{WriteIndented=true})+"\n");
Console.WriteLine($"Audit: {results.Count} probes; {results.Count(item=>item!["status"]!.GetValue<string>()!="CONTROL_PASS")} findings/witnesses; {hashes.Count} historical hashes checked.");
// Deliberately nonzero: callers must not mistake a completed audit for qualified evidence.
return invalidExecution?2:results.Any(item=>item!["status"]!.GetValue<string>()!="CONTROL_PASS")?1:0;
