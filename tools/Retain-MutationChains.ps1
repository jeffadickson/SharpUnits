param([string]$JavaObservations='../debt-java-chains.jsonl')
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
if(!(Test-Path -LiteralPath $root -PathType Container)){throw 'Missing checkout'}
Push-Location -LiteralPath $root
try {
 $pins=Get-Content -LiteralPath conformance/upstream-pins.json -Raw|ConvertFrom-Json
 $expected=@('negate-big-boundary','negate-minimum-twice','add-big','add-long','add-double','multiply','divide','reciprocal-twice','negate-big-twice','negate-long-boundary','negate-long-twice','zero-add-long','zero-add-big','multiply-one-long','divide-one-long','zero-add-double')
 $seen=[Collections.Generic.HashSet[string]]::new();$cases=@()
 foreach($line in [IO.File]::ReadAllText((Resolve-Path -LiteralPath $JavaObservations)).Split("`n")){
  if([string]::IsNullOrWhiteSpace($line)){continue};$row=$line|ConvertFrom-Json
  if($row.id -notin $expected -or !$seen.Add($row.id) -or !$row.result){throw 'Invalid or duplicate Java chain observation'}
  $cases+=@{id=$row.id;operation='normalized-number-chain';expected=$row.result;absoluteTolerance=0;intermediateJavaClass=$row.intermediateJavaClass;javaClass=$row.javaClass;javaText=$row.javaText;oracle=@{kind='PINNED_IMPLEMENTATION_OBSERVATION';indriyaCommit=$pins.indriya.commit;rowSha256=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($line.TrimEnd("`r"))));contract='Normalize at each public operation; excludes Calculator accumulator chains'}}
 }
 if($seen.Count -ne $expected.Count){throw 'Missing Java chain observations'}
 @{schemaVersion=1;cases=$cases}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath conformance/mutation-debt-chain-cases.json
} finally {Pop-Location}
