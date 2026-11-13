$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
if(!(Test-Path -LiteralPath $root -PathType Container)){throw 'Missing checkout'}
$source=[IO.File]::ReadAllText((Join-Path $root 'src/SharpUnits/JavaUnitTextFormat.cs'))
$start=$source.IndexOf('private static FrozenSet<string> CreateMetricFields()')
$end=$source.IndexOf('/// <summary>Creates a fresh formatter',$start)
if($start -lt 0 -or $end -le $start){throw 'Missing metric membership source span'}
$config=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'stryker-core.json') -Raw|ConvertFrom-Json
$config.'stryker-config'.mutate=@('**/SiPrefixes.cs',('**/JavaUnitTextFormat.cs{'+$start+'..'+$end+'}'))
$config.'stryker-config'.concurrency=1
$config.'stryker-config'.'test-case-filter'='FullyQualifiedName~Net11ModernizationTests|FullyQualifiedName~Prefix|FullyQualifiedName~UnitFormat'
$path=Join-Path $PSScriptRoot 'stryker-net11.json'
$config|ConvertTo-Json -Depth 6|Set-Content -LiteralPath $path
@{configSha256=(Get-FileHash -LiteralPath $path).Hash;siPrefixesSha256=(Get-FileHash -LiteralPath (Join-Path $root 'src/SharpUnits/SiPrefixes.cs')).Hash;formatterSha256=(Get-FileHash -LiteralPath (Join-Path $root 'src/SharpUnits/JavaUnitTextFormat.cs')).Hash;testsSha256=(Get-FileHash -LiteralPath (Join-Path $root 'tests/SharpUnits.Tests/Net11ModernizationTests.cs')).Hash;scope='SiPrefixes and metric membership construction; other formatter/number behavior excluded; offsets regenerated for current checkout'}|
 ConvertTo-Json|Set-Content -LiteralPath (Join-Path $root 'conformance/net11-mutation-freeze.json')
