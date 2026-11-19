param([Parameter(Mandatory)][string]$Report)
$ErrorActionPreference='Stop'
# Some engine compilation failures return exit0 and print score0. A process exit
# is not evidence that mutants executed; reject a missing/empty execution report.
if(!(Test-Path -LiteralPath $Report -PathType Leaf)){throw 'Mutation engine produced no completed report; inspect compiler diagnostics'}
$data=Get-Content -LiteralPath $Report -Raw|ConvertFrom-Json
$executed=@($data.files.PSObject.Properties.Value.mutants|Where-Object status -in 'Killed','Survived','Timeout')
if(!$executed.Count){throw 'Mutation report has no executed mutants; compilation/ignored entries are not qualification'}
'Executed mutation report exists; assess raw statuses and scope separately.'
