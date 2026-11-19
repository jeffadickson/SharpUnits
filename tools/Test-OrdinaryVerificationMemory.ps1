<# Integration witnesses for VER-RAM-TELEMETRY. A one-MiB legacy guard would
   terminate these real .NET workloads; ordinary dispatch must ignore that
   mutation-only setting and reject an explicit managed-heap limit. #>
param([Parameter(Mandatory)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
$parent=Split-Path -Parent ([IO.Path]::GetFullPath($OutputDirectory))
if(!(Test-Path -LiteralPath $parent -PathType Container)){throw 'Missing output parent'}
if(Test-Path -LiteralPath $OutputDirectory){throw 'Use a fresh test directory'}
New-Item -ItemType Directory -Path $OutputDirectory|Out-Null
$rejected=$false
try {
 & (Join-Path $PSScriptRoot 'Invoke-TypedErrorVerification.ps1') -Mode Example -OutputDirectory (Join-Path $OutputDirectory 'invalid') -ManagedHeapMiB 1
}catch {
 if($_.Exception.Message -ne 'Managed heap limits are mutation-only'){throw}
 $rejected=$true
}
if(!$rejected -or (Test-Path -LiteralPath (Join-Path $OutputDirectory 'invalid'))){throw 'Invalid ordinary limit was not rejected before output creation'}
foreach($runner in @('TypedError','ExactText')){
 $run=Join-Path $OutputDirectory $runner
 & (Join-Path $PSScriptRoot "Invoke-${runner}Verification.ps1") -Mode Example -OutputDirectory $run -ProcessMiB 1 -AggregateMiB 1
 if(!(Test-Path -LiteralPath (Join-Path $run 'workload.log') -PathType Leaf)){throw 'Child workload did not run'}
 if(Test-Path -LiteralPath (Join-Path $run 'watchdog.json')){throw 'Ordinary workload entered the mutation guard'}
}
@{ordinaryRunnersPassed=2;invalidHeapRejected=$rejected;legacyMutationLimitsMiB=1;scope='Real example child processes; no assertion about limits imposed by an external parent or operating system.'}|
 ConvertTo-Json|Set-Content -LiteralPath (Join-Path $OutputDirectory 'result.json')
