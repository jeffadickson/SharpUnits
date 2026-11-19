param([Parameter(Mandatory)][string]$OutputDirectory,[switch]$RootExitsEarly)
$ErrorActionPreference='Stop'
if(!(Test-Path -LiteralPath $OutputDirectory -PathType Container)){throw 'Missing isolated test output directory'}
$wrapper=Join-Path $PSScriptRoot 'Invoke-Budgeted.ps1'
$allocator=Join-Path $OutputDirectory 'allocate.ps1'
if($RootExitsEarly){
@'
param([string]$Ready,[int]$Owner)
$ownerProcess=[Diagnostics.Process]::GetProcessById($Owner)
$event=[Threading.EventWaitHandle]::OpenExisting($Ready)
$retained=[Collections.Generic.List[byte[]]]::new()
$null=$event.Set()
$event.Dispose()
$ownerProcess.WaitForExit()
while($true){
 $block=[byte[]]::new(16MB)
 for($i=0;$i -lt $block.Length;$i+=4096){$block[$i]=1}
 $retained.Add($block)
}
'@|Set-Content -LiteralPath $allocator
@'
Write-Output ('WorkloadParent='+$PID)
$name='SharpUnitsEarlyRoot-'+[Guid]::NewGuid().ToString('N')
$ready=[Threading.EventWaitHandle]::new($false,[Threading.EventResetMode]::ManualReset,$name)
$start=[Diagnostics.ProcessStartInfo]::new((Get-Command pwsh).Source)
$start.UseShellExecute=$false;$start.CreateNoWindow=$true
foreach($arg in @('-NoProfile','-File',(Join-Path $PSScriptRoot 'allocate.ps1'),'-Ready',$name,'-Owner',"$PID")){$start.ArgumentList.Add($arg)}
$child=[Diagnostics.Process]::Start($start)
Write-Output ('AllocatingChild='+$child.Id)
if(!$ready.WaitOne(10000)){throw 'Child readiness timeout'}
$ready.Dispose()
exit 0
'@|Set-Content -LiteralPath (Join-Path $OutputDirectory 'descendant.ps1')
} else {
@'
Write-Output ('AllocatingChild='+$PID)
$retained=[Collections.Generic.List[byte[]]]::new()
while($true){
 $block=[byte[]]::new(16MB)
 for($i=0;$i -lt $block.Length;$i+=4096){$block[$i]=1}
 $retained.Add($block)
}
'@|Set-Content -LiteralPath $allocator
@'
Write-Output ('WorkloadParent='+$PID)
& (Get-Command pwsh).Source -NoProfile -File (Join-Path $PSScriptRoot 'allocate.ps1')
exit $LASTEXITCODE
'@|Set-Content -LiteralPath (Join-Path $OutputDirectory 'descendant.ps1')
}
$child=Join-Path $OutputDirectory 'breach'
if(Test-Path -LiteralPath $child){throw 'Use fresh test outputs'}
New-Item -ItemType Directory -Path $child|Out-Null
$failed=$false
try {& $wrapper -Script (Join-Path $OutputDirectory 'descendant.ps1') -OutputDirectory $child -ProcessMiB 128 -AggregateMiB 128 -ReserveMiB 128}
catch {$failed=$true;$_|Out-String|Set-Content -LiteralPath (Join-Path $OutputDirectory 'expected-failure.log')}
$record=Get-Content -LiteralPath (Join-Path $child 'watchdog.json') -Raw|ConvertFrom-Json
if(!$failed -or (!$record.budgetBreach -and $record.limitMessage -notin 9,10) -or $record.outcome -notmatch '^memory limit breached'){
 throw 'Watchdog did not observe and terminate the memory breach'
}
if($RootExitsEarly -and $record.exitCode -ne 0){throw 'Early-root witness did not preserve root exit code zero'}
$ids=Select-String -LiteralPath (Join-Path $child 'stdout.log') -Pattern '(?:AllocatingChild|WorkloadParent)=(\d+)' -AllMatches
if(($ids.Matches|Measure-Object).Count -ne 2){throw 'Missing parent/descendant witnesses'}
foreach($match in $ids.Matches){if(Get-Process -Id ([int]$match.Groups[1].Value) -ErrorAction SilentlyContinue){throw 'Owned process survived job termination'}}
@{result='PASS';limitMessage=$record.limitMessage;budgetBreach=$record.budgetBreach;rootExitCode=$record.exitCode;rootExitsEarly=[bool]$RootExitsEarly;processMiB=128;aggregateMiB=128;peakProcessBytes=$record.peakProcessBytes;peakJobBytes=$record.peakJobBytes;descendantsGone=$true;scope='One real Windows allocation workload with explicit fail-fast watermark; not exhaustive job/watchdog qualification'}|
 ConvertTo-Json|Set-Content -LiteralPath (Join-Path $OutputDirectory 'watchdog-test.json')
