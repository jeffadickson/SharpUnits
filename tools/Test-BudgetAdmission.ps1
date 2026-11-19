param([Parameter(Mandatory)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
if(!(Test-Path -LiteralPath $OutputDirectory -PathType Container)){throw 'Missing isolated output directory'}
$identity=[Security.Principal.WindowsIdentity]::GetCurrent().User.Value
$mutex=[Threading.Mutex]::new($false,('Global\SharpUnitsVerificationBudget-'+$identity))
$held=$false
try {
 $held=$mutex.WaitOne(0)
 if(!$held){throw 'Another real workload holds admission; run this check serially'}
 @'
'WORKLOAD MUST NOT START'|Set-Content -LiteralPath (Join-Path $PSScriptRoot 'incorrectly-started.txt')
'@|Set-Content -LiteralPath (Join-Path $OutputDirectory 'forbidden.ps1')
 # A separate process is required: mutexes permit recursive acquisition on the
 # owning thread, so invoking the wrapper inline would not test admission.
 @'
param($Wrapper,$Directory)
try {& $Wrapper -Script (Join-Path $Directory 'forbidden.ps1') -OutputDirectory $Directory -ProcessMiB 128 -AggregateMiB 128 -ReserveMiB 128}
catch {$_|Out-String|Set-Content -LiteralPath (Join-Path $Directory 'refusal.txt');exit 19}
exit 0
'@|Set-Content -LiteralPath (Join-Path $OutputDirectory 'contender.ps1')
 $info=[Diagnostics.ProcessStartInfo]::new((Get-Command pwsh).Source)
 $info.UseShellExecute=$false;$info.CreateNoWindow=$true
 foreach($arg in @('-NoProfile','-File',(Join-Path $OutputDirectory 'contender.ps1'),(Join-Path $PSScriptRoot 'Invoke-Budgeted.ps1'),$OutputDirectory)){$info.ArgumentList.Add($arg)}
 $child=[Diagnostics.Process]::Start($info)
 try {if(!$child.WaitForExit(15000)){$child.Kill($true);throw 'Admission check timed out'}
  if($child.ExitCode -ne 19){throw 'Concurrent workload was not refused'}
 } finally {$child.Dispose()}
 $refusal=Get-Content -LiteralPath (Join-Path $OutputDirectory 'refusal.txt') -Raw
 if($refusal -notmatch 'Another guarded SharpUnits workload'){throw 'Refusal did not come from shared admission'}
 if(Test-Path -LiteralPath (Join-Path $OutputDirectory 'incorrectly-started.txt')){throw 'Workload executed before admission'}
 @{result='PASS';sharedAdmission=$true;workloadStarted=$false;scope='Same-user separate-process mutex exclusion on Windows'}|
  ConvertTo-Json|Set-Content -LiteralPath (Join-Path $OutputDirectory 'admission-test.json')
} finally {if($held){$mutex.ReleaseMutex()};$mutex.Dispose()}
