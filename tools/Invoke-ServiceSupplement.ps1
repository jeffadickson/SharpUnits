param([string]$Dotnet='C:/Users/jeffd/.dotnet/dotnet.exe',
      [string]$Java='../tools-runtime/jdk/jdk-17.0.20.1+1/bin/java.exe',
      [string]$Python='../tools-runtime/python/python.exe')
$ErrorActionPreference='Stop'
# Re-execute supplemental API edges and every catalog dimension's membership.
# Canonical expectations are retained in JSON and separately checked by xUnit.
$classpath='conformance/java-reference/target/classes;'+(Get-Content conformance/java-reference/classpath.txt -Raw).Trim()
foreach($kind in @('edge','filter')){
 $cases="conformance/service-$kind-cases.json"
 & $Java '-Dfile.encoding=UTF-8' -cp $classpath org.sharpunits.ServiceReferenceRunner $cases > "../services100-$kind-java.jsonl"
 if($LASTEXITCODE){throw 'Java supplement failed'}
 & $Dotnet src/SharpUnits.ConformanceRunner/bin/Release/net10.0/SharpUnits.ConformanceRunner.dll $cases > "../services100-$kind-dotnet.jsonl"
 if($LASTEXITCODE){throw 'CLR supplement failed'}
 & $Python tools/compare_java_dotnet.py "../services100-$kind-java.jsonl" "../services100-$kind-dotnet.jsonl"
 if($LASTEXITCODE){throw 'Supplemental differential mismatch'}
}
