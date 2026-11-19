param([Parameter(Mandatory)][string]$Report,[Parameter(Mandatory)][string]$Output)
$ErrorActionPreference='Stop'
if(!(Test-Path -LiteralPath (Split-Path -Parent ([IO.Path]::GetFullPath($Output))) -PathType Container)){throw 'Missing evidence parent'}
$raw=Get-Content -LiteralPath $Report -Raw|ConvertFrom-Json
$historical=Get-Content -LiteralPath (Join-Path $PSScriptRoot '../conformance/mutation-debt-reviewed.json') -Raw|ConvertFrom-Json
$files=@()
foreach($file in $raw.files.PSObject.Properties){
 $name=[IO.Path]::GetFileName($file.Name)
 if($name -notin 'JavaNumber.cs','NumericResult.cs'){continue}
 $source=$file.Value.source
 $current=[IO.File]::ReadAllText((Join-Path $PSScriptRoot ('../src/SharpUnits/'+$name)))
 if($source.Replace("`r`n","`n") -ne $current.Replace("`r`n","`n")){throw ('Mutation source differs from checkout: '+$name)}
 $lines=$source -split '\r?\n'
 $old=@($historical.files|Where-Object file -eq $name)
 $outstanding=@()
 foreach($mutant in $file.Value.mutants|Where-Object status -in 'Survived','NoCoverage','Timeout'){
  $statement=($lines[($mutant.location.start.line-1)..($mutant.location.end.line-1)] -join "`n").Trim()
  $disposition='OPEN_REVIEW';$rationale='Requires individual review.'
  $prior=@($old.outstanding|Where-Object {$null -ne $_ -and $null -ne $_.statement -and $_.statement.Trim() -eq $statement -and $_.replacement -eq $mutant.replacement -and $_.mutator -eq $mutant.mutatorName})
  if($prior.Count){$disposition=$prior[0].reviewedDisposition;$rationale=$prior[0].rationale}
  if($mutant.status -eq 'Timeout'){
   $disposition='INCONCLUSIVE_TIMEOUT';$rationale='Execution timed out. Not a demonstrated kill; historical infinite-loop mutation remains inconclusive.'
  } elseif($name -eq 'NumericResult.cs'){
   if($mutant.mutatorName -like 'Null coalescing*' -and $mutant.replacement -eq 'Parameter'){
    $disposition='UNUSED_OVERRIDE_INVARIANT'
    $rationale='The only callers are Decimal and FromNumeric. Decimal supplies an override only for NumericBudget, and FromNumeric supplies none. Removing the override for NullInput/ScaleBudget/NonFinite/UnsupportedNumericType is equivalent under this closed internal caller set. Not a kill.'
   } elseif($mutant.status -eq 'NoCoverage' -and $statement -match 'Undefined numeric error code'){
    $disposition='UNREACHABLE_VALIDATED_ERROR_CODE'
    $rationale='The sealed error validates Enum.IsDefined and exposes get-only properties; every defined enum case is mapped before the default arm. No reflection-corrupted object is fabricated to exercise this defensive arm.'
   } elseif($mutant.mutatorName -eq 'String mutation' -and $statement -match 'new (FormatException|ArgumentException|ArgumentOutOfRangeException|InvalidOperationException)\('){
    $disposition='DIAGNOSTIC_TEXT_VARIATION'
    $rationale='Observable English message variation. The recovery contract is the typed code, parameter, position and short-circuit behavior; legacy exception types/parameter names remain tested. Exact display wording is outside this contract. Not identical behavior or a kill.'
   }
  } elseif($statement -match 'var fraction=scale>=0\?new Rational' -and $mutant.replacement -eq 'scale>0'){
   $disposition='ZERO_POWER_EQUIVALENCE';$rationale='Only scale zero changes branch; 10^0=1 makes both rational constructions identical.'
  }
  $outstanding+=@{id=$mutant.id;status=$mutant.status;line=$mutant.location.start.line;mutator=$mutant.mutatorName;replacement=$mutant.replacement;statement=$statement;disposition=$disposition;rationale=$rationale}
 }
 $counts=[ordered]@{};foreach($group in $file.Value.mutants|Group-Object status){$counts[$group.Name]=$group.Count}
 $files+=@{file=$name;canonicalSourceSha256=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($source.Replace("`r`n","`n"))));counts=$counts;outstanding=$outstanding}
}
if($files.Count -ne 2){throw 'Expected both changed files in the selected profile'}
@{reportSha256=(Get-FileHash -LiteralPath $Report).Hash;policy='Raw outcomes retained. Conditional invariants and diagnostic variations are reviewed dispositions, never kills. Scope: full NumericResult and selected JavaNumber boundary/budget spans; Standard level. Other JavaNumber methods remain excluded and unqualified by this profile.';files=$files}|
 ConvertTo-Json -Depth 9|Set-Content -LiteralPath $Output
if(@($files.outstanding|Where-Object disposition -eq 'OPEN_REVIEW').Count){throw 'Unclassified outstanding mutants remain; inspect evidence before making acceptance claims'}
