param([string[]]$InputPaths=@('conformance/release-audit-core.json','conformance/release-audit-formats.json','conformance/release-audit-services.json'),[string]$OutputPath)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
if(!(Test-Path -LiteralPath $root -PathType Container)){throw 'Missing checkout'}
Push-Location -LiteralPath $root
try {
 $ledger=Get-Content -LiteralPath conformance/tck-closure-ledger.json -Raw|ConvertFrom-Json
 $owners=(Get-Content -LiteralPath conformance/completion-work-packages.json -Raw|ConvertFrom-Json).entries
 $rows=@(foreach($path in $InputPaths){Get-Content -LiteralPath $path -Raw|ConvertFrom-Json})
 if($rows.Count -ne $ledger.totalAssertions -or @($rows.id|Sort-Object -Unique).Count -ne $ledger.totalAssertions){throw 'Missing or duplicate audit occurrence'}
 $uids=@(Get-ChildItem -LiteralPath requirements -Filter '*.sdoc'|Select-String -Pattern '^UID: '|ForEach-Object {$_.Line.Substring(5)})
 foreach($row in $rows){
  $entry=@($ledger.entries|Where-Object id -CEQ $row.id)
  if($entry.Count -ne 1 -or ('TCK-'+$row.id) -cnotin $uids){throw ('Unknown ID or absent StrictDoc trace: '+$row.id)}
  if($row.suite -cne [IO.Path]::GetFileNameWithoutExtension($entry[0].source) -or $row.javaSource -cne ('upstream/unit-tck/'+$entry[0].source)){throw ('Wrong source/suite: '+$row.id)}
  if($row.assertionKind -cnotin @('API_SHAPE','BEHAVIOR','MIXED','CONFIGURATION') -or $row.implementationAssessment -cnotin @('MAPPED_BOUNDED','BEHAVIOR_GAP','API_ADAPTATION','UNASSESSED') -or $row.recommendation -cnotin @('PRESERVE_SEMANTICS','ADAPT_DOTNET','IMPLEMENT','DEFER_JAVA_MECHANISM')){throw ('Unknown classification: '+$row.id)}
  foreach($key in @('sourceMethod','expectedSemantics','residualBehavior','modernDotnetDecision','qualificationGap')){if([string]::IsNullOrWhiteSpace($row.$key)){throw ('Missing '+$key+': '+$row.id)}}
  $java=Get-Content -LiteralPath $row.javaSource
  if($row.javaLine -lt 1 -or $row.javaLine -gt $java.Count){throw ('Invalid Java line: '+$row.id)}
  $method=($row.sourceMethod -split '\.')[-1]
  if(($java -join "`n") -notmatch ('\b'+[regex]::Escape($method)+'\s*\(')){throw ('Java method absent: '+$row.id)}
  if(!$row.implementationRefs.Count -or !$row.testRefs.Count){throw ('Absent implementation/test mapping: '+$row.id)}
  foreach($reference in $row.implementationRefs){
   if($reference -notmatch '^(?<path>src/[^:]+\.cs):(?<line>\d+)$'){throw ('Malformed implementation anchor: '+$reference)}
   $file=$Matches.path;$line=[int]$Matches.line
   if(!(Test-Path -LiteralPath $file -PathType Leaf) -or $line -lt 1 -or $line -gt @(Get-Content -LiteralPath $file).Count){throw ('Invalid implementation anchor: '+$reference)}
  }
  foreach($reference in $row.testRefs){
   if($reference -notmatch '^(?<path>(tests|conformance)/[^\s:()]+\.(cs|json|md))'){throw ('Malformed test/evidence reference: '+$reference)}
   $file=$Matches.path
   if(!(Test-Path -LiteralPath $file -PathType Leaf)){throw ('Absent test/evidence: '+$reference)}
   if($file.EndsWith('.cs') -and $reference -match '\.cs:(?<method>[A-Za-z_][A-Za-z_0-9]*)'){
    if((Get-Content -LiteralPath $file -Raw) -notmatch ('\b'+[regex]::Escape($Matches.method)+'\s*\(')){throw ('Absent test method: '+$reference)}
   }
  }
  if(@($owners|Where-Object assertionId -CEQ $row.id).Count -ne 1){throw 'Absent planning ownership'}
 }
 $group={param($values,$property) $result=[ordered]@{};foreach($g in $values|Group-Object $property|Sort-Object Name){$result[$g.Name]=$g.Count};return $result}
 $summary=[ordered]@{result='AUDIT_ACCOUNTING_PASS';date='2026-11-16';occurrences=$rows.Count;assertionKinds=(& $group $rows 'assertionKind');implementationAssessments=(& $group $rows 'implementationAssessment');recommendations=(& $group $rows 'recommendation');formalLedgerStates=(& $group $ledger.entries 'status');formalClosures=@($ledger.entries|Where-Object status -eq VERIFIED).Count;scope='Source/evidence audit and reference existence, not executable semantic qualification or certification';entries=$rows}
 if($OutputPath){$OutputPath=[IO.Path]::GetFullPath($OutputPath,$root);$parent=Split-Path -Parent $OutputPath;if(!(Test-Path -LiteralPath $parent -PathType Container)){throw 'Missing output parent'};$summary|ConvertTo-Json -Depth 12|Set-Content -LiteralPath $OutputPath}
 [pscustomobject]$summary|Select-Object result,occurrences,assertionKinds,implementationAssessments,recommendations,formalLedgerStates|ConvertTo-Json -Depth 4
} finally {Pop-Location}
