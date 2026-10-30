param([string]$Dotnet='C:/Users/jeffd/.dotnet/dotnet.exe',
      [string]$Java='../tools-runtime/jdk/jdk-17.0.20.1+1/bin/java.exe',
      [string]$Python='../tools-runtime/python/python.exe')
$ErrorActionPreference='Stop'
# Separate JVM/process execution prevents shared-label mutations contaminating other corpora.
$classpath='conformance/java-reference/target/classes;'+(Get-Content conformance/java-reference/classpath.txt -Raw).Trim()
& $Java '-Dfile.encoding=UTF-8' -cp $classpath org.sharpunits.UnitFormatReferenceRunner conformance/unitformat-supplement-cases.json > ../unitformat100-supplement-java.jsonl
if($LASTEXITCODE){throw 'Java supplement failed'}
& $Dotnet src/SharpUnits.ConformanceRunner/bin/Release/net10.0/SharpUnits.ConformanceRunner.dll conformance/unitformat-supplement-cases.json > ../unitformat100-supplement-dotnet.jsonl
if($LASTEXITCODE){throw 'CLR supplement failed'}
& $Python tools/compare_java_dotnet.py ../unitformat100-supplement-java.jsonl ../unitformat100-supplement-dotnet.jsonl --cases conformance/unitformat-supplement-cases.json
if($LASTEXITCODE){throw 'Supplemental mismatch'}
