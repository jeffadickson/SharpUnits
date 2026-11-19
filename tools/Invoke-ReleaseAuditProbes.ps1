param([Parameter(Mandatory)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
if(!(Test-Path -LiteralPath $root -PathType Container) -or !(Test-Path -LiteralPath $OutputDirectory -PathType Container)){throw 'Missing source/output directory'}
Push-Location -LiteralPath $root
try {
 foreach($name in @('java.jsonl','dotnet.jsonl')){if(Test-Path -LiteralPath (Join-Path $OutputDirectory $name)){throw 'Use a fresh observation directory'}}
 $runtime=Join-Path (Split-Path -Parent $root) 'tools-runtime'
 $classpath='conformance/java-reference/target/classes;'+(Get-Content -LiteralPath conformance/java-reference/classpath.txt -Raw).Trim()
 & (Join-Path $runtime 'jdk/jdk-17.0.20.1+1/bin/java.exe') '-Dfile.encoding=UTF-8' -cp $classpath org.sharpunits.UnitFormatReferenceRunner conformance/release-audit-probes.json > (Join-Path $OutputDirectory 'java.jsonl')
 if($LASTEXITCODE){throw 'Java observation failed'}
 & dotnet src/SharpUnits.ConformanceRunner/bin/Release/net11.0/SharpUnits.ConformanceRunner.dll conformance/release-audit-probes.json > (Join-Path $OutputDirectory 'dotnet.jsonl')
 if($LASTEXITCODE){throw '.NET observation failed'}
 $java=@(Get-Content -LiteralPath (Join-Path $OutputDirectory 'java.jsonl')|ForEach-Object {$_|ConvertFrom-Json})
 $net=@(Get-Content -LiteralPath (Join-Path $OutputDirectory 'dotnet.jsonl')|ForEach-Object {$_|ConvertFrom-Json})
 $cases=(Get-Content -LiteralPath conformance/release-audit-probes.json -Raw|ConvertFrom-Json).cases
 if($java.Count -ne $cases.Count -or $net.Count -ne $cases.Count){throw 'Incomplete diagnostic observations'}
 $observations=@(foreach($case in $cases){
  $j=@($java|Where-Object id -CEQ $case.id);$n=@($net|Where-Object id -CEQ $case.id)
  if($j.Count -ne 1 -or $n.Count -ne 1){throw 'Missing/duplicate diagnostic ID'}
  # Expected values were captured from independent pinned Java/public .NET runs;
  # disagreements are retained findings, never treated as conformance passes.
  $jv=ConvertTo-Json -InputObject $j[0].result -Compress;$nv=ConvertTo-Json -InputObject $n[0].result -Compress
  if($jv -cne (ConvertTo-Json -InputObject $case.expectedJava -Compress) -or $nv -cne (ConvertTo-Json -InputObject $case.expectedDotnet -Compress)){throw ('Diagnostic drift: '+$case.id)}
  @{id=$case.id;java=$j[0].result;dotnet=$n[0].result;matches=($jv -ceq $nv);disposition=$case.disposition}
 })
 @{observations=$observations;note='Diagnostic replay consistency only; mismatches remain open. Normalized text/results do not establish complete structural equivalence.'}|ConvertTo-Json -Depth 7|Set-Content -LiteralPath (Join-Path $OutputDirectory 'observations.json')
} finally {Pop-Location}
