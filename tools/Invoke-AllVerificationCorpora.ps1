param([Parameter(Mandatory)][string]$Runner,[string]$OutputDirectory='../repair-all-corpora',
      [string]$Dotnet=(Join-Path $env:USERPROFILE '.dotnet/dotnet.exe'),[string]$JavaHome='../tools-runtime/jdk/jdk-17.0.20.1+1',
      [string]$Python='../tools-runtime/python/python.exe')
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
if(!(Test-Path -LiteralPath $root -PathType Container)){throw 'Missing repository'}
Push-Location -LiteralPath $root
try {
 if(!(Test-Path -LiteralPath $JavaHome -PathType Container) -or !(Test-Path -LiteralPath $Runner -PathType Leaf)){throw 'Missing installed runtime or built runner'}
 $parent=Split-Path -Parent ([IO.Path]::GetFullPath($OutputDirectory));if(!(Test-Path -LiteralPath $parent -PathType Container)){throw 'Missing output parent'}
 if(!(Test-Path -LiteralPath $OutputDirectory -PathType Container)){New-Item -ItemType Directory -Path $OutputDirectory|Out-Null}
 $classpath='conformance/java-reference/target/classes;'+(Get-Content -LiteralPath conformance/java-reference/classpath.txt -Raw).Trim()
 $results=@()
 foreach($path in @('conformance/cases.json','conformance/java-reference/cases.json','conformance/service-reference-cases.json','conformance/service-edge-cases.json','conformance/service-filter-cases.json','conformance/unitformat-reference-cases.json','conformance/unitformat-supplement-cases.json','conformance/unitformat-affine-cases.json','conformance/work-package-one-cases.json','conformance/verification-repair-cases.json','conformance/mutation-debt-cases.json')){
  $name=([IO.Path]::GetFileNameWithoutExtension($path));if($path -eq 'conformance/cases.json'){$name='native-cases'}
  $main=if($name -like 'service-*'){'ServiceReferenceRunner'}elseif($name -like 'unitformat-*'){'UnitFormatReferenceRunner'}elseif($name -like 'work-package-*' -or $name -in @('verification-repair-cases','mutation-debt-cases')){'CoreReferenceRunner'}else{'ReferenceRunner'}
  $java=Join-Path $OutputDirectory ($name+'-java.jsonl');$clr=Join-Path $OutputDirectory ($name+'-clr.jsonl');$log=Join-Path $OutputDirectory ($name+'.log')
  & (Join-Path $JavaHome bin/java.exe) '-Dfile.encoding=UTF-8' -cp $classpath ('org.sharpunits.'+$main) $path > $java
  if($LASTEXITCODE){throw ('Java corpus execution failed: '+$path)}
  & $Dotnet $Runner $path > $clr
  if($LASTEXITCODE){throw ('CLR corpus execution failed: '+$path)}
  & $Python tools/check_fixture_expectations.py $path $java --observer java *> $log;$expected=$LASTEXITCODE
  & $Python tools/compare_java_dotnet.py $java $clr --cases $path *>> $log;$paired=$LASTEXITCODE
  $results+=@{corpus=$path;cases=@((Get-Content -LiteralPath $path -Raw|ConvertFrom-Json).cases).Count;expectedCheckExit=$expected;differentialExit=$paired;javaSha256=(Get-FileHash -LiteralPath $java).Hash;clrSha256=(Get-FileHash -LiteralPath $clr).Hash}
  Write-Output ($path+': expected='+$expected+', paired='+$paired)
 }
 $results|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $OutputDirectory summary.json)
 if(@($results|Where-Object {$_.expectedCheckExit -ne 0 -or $_.differentialExit -ne 0}).Count){throw 'One or more strict corpus checks failed; inspect retained logs'}
} finally {Pop-Location}
