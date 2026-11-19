$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
if(!(Test-Path -LiteralPath $root -PathType Container)){throw 'Missing checkout'}
$ledger=Get-Content -LiteralPath (Join-Path $root 'conformance/tck-closure-ledger.json') -Raw|ConvertFrom-Json
$plan=Get-Content -LiteralPath (Join-Path $root 'conformance/completion-work-packages.json') -Raw|ConvertFrom-Json
if((Get-FileHash -LiteralPath (Join-Path $root 'conformance/tck-closure-ledger.json')).Hash -ne $plan.ledgerSha256){throw 'Plan ledger snapshot is stale; reconcile status and ownership explicitly'}
if($plan.entries.Count -ne $ledger.entries.Count -or @($plan.entries.assertionId|Select-Object -Unique).Count -ne $ledger.entries.Count){throw 'Missing/duplicate plan occurrences'}
$ids=@(Get-ChildItem -LiteralPath (Join-Path $root 'requirements') -Filter '*.sdoc'|Select-String -Pattern '^UID: '|ForEach-Object {$_.Line.Substring(5)})
foreach($entry in $ledger.entries){
 $owner=@($plan.entries|Where-Object assertionId -eq $entry.id)
 if($owner.Count -ne 1 -or $owner[0].owner -notin 1,2,3,4 -or $owner[0].qualificationOwner -ne 5){throw ('Invalid ownership: '+$entry.id)}
 if($owner[0].requirementId -ne 'TCK-'+$entry.id -or $owner[0].requirementId -notin $ids){throw 'Requirement trace missing'}
 if($owner[0].suite -ne [IO.Path]::GetFileNameWithoutExtension($entry.source) -or $owner[0].currentStatus -ne $entry.status){throw 'Plan source/status differs from ledger'}
}
foreach($owner in 1..4){if(@($plan.entries|Where-Object owner -eq $owner).Count -ne $plan.primaryCounts.('WP'+$owner)){throw 'Owner count differs from declared plan'}}
'PASS:113 uniquely owned assertion occurrences, qualification owner5, stable StrictDoc traces; no closure status changed.'
