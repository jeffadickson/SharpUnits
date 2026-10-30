param([string]$Stryker='../tools-runtime/stryker/dotnet-stryker.exe',[string]$OutputDirectory='../repair-stryker-focused')
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
if(!(Test-Path -LiteralPath $root -PathType Container)){throw 'Missing checkout'}
Push-Location -LiteralPath $root
try {
 $parent=Split-Path -Parent ([IO.Path]::GetFullPath($OutputDirectory));if(!(Test-Path -LiteralPath $parent -PathType Container)){throw 'Missing output parent'}
 $config=Get-Content -LiteralPath tools/stryker-core.json -Raw|ConvertFrom-Json
 $config.'stryker-config'.'test-case-filter'='FullyQualifiedName~MutationQualificationTests|FullyQualifiedName~CoreRobustnessTests'
 $ranges=@()
 # Character ranges bind the exact reviewed statements, not unstable line numbers.
 foreach($spec in @(
 @('JavaNumber.cs','if(multiply&&(Fraction.Numerator'),
 @('JavaNumber.cs','if(pinnedQuirks&&!multiply&&Kind'),
 @('JavaNumber.cs','if(pinnedQuirks&&!multiply&&other'),
 @('JavaNumberQuantity.cs','if(!converter.IsAffine)'),
 @('JavaUnit.cs','depth=1+Math.Max'),
 @('JavaUnit.cs','public bool IsEquivalentTo'),
 @('JavaUnit.cs','transformed=transformed.Shift'),
 @('JavaUnit.cs','var result=offset==0?'),
 @('JavaUnitAdapter.cs','public bool IsEquivalentTo'),
 @('SiPrefixes.cs','return unit.JavaModel is null?'))){
  $file=Join-Path $root ('src/SharpUnits/'+$spec[0]);$source=[IO.File]::ReadAllText($file)
  $start=$source.IndexOf($spec[1]);if($start -lt 0){throw 'Missing reviewed mutation statement'}
  $end=$source.IndexOf("`n",$start);if($spec[1] -eq 'var result=offset==0?'){$end=$source.IndexOf("`n",$end+1)}
  $ranges+='**/'+$spec[0]+'{'+$start+'..'+$end+'}'
 }
 $config.'stryker-config'.mutate=$ranges
 $temporary=Join-Path ([IO.Path]::GetTempPath()) ('sharpunits-repair-'+[guid]::NewGuid()+'.json')
 $config|ConvertTo-Json -Depth 8|Set-Content -LiteralPath $temporary
 $previous=$env:VSTEST_CONNECTION_TIMEOUT
 try {$env:VSTEST_CONNECTION_TIMEOUT='180'; & $Stryker -f $temporary --skip-version-check -O $OutputDirectory;if($LASTEXITCODE){throw 'Focused Stryker failed'}}
 finally {$env:VSTEST_CONNECTION_TIMEOUT=$previous;Remove-Item -LiteralPath $temporary}
} finally {Pop-Location}
