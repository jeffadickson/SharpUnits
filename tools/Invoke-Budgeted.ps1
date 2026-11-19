param(
 [Parameter(Mandatory)][string]$Script,
 [Parameter(Mandatory)][string]$OutputDirectory,
 [ValidateRange(128,16384)][int]$ProcessMiB=512,
 [ValidateRange(128,16384)][int]$AggregateMiB=768,
 [ValidateRange(128,16384)][int]$ReserveMiB=512
)
$ErrorActionPreference='Stop'
$admission=[Threading.Mutex]::new($false,('Global\SharpUnitsVerificationBudget-'+[Security.Principal.WindowsIdentity]::GetCurrent().User.Value))
$held=$false
try {
try {$held=$admission.WaitOne(0)}catch [Threading.AbandonedMutexException]{$held=$true}
if(!$held){throw 'Another guarded SharpUnits workload holds the shared memory admission mutex'}
if(!($IsWindows)){throw 'This watchdog uses Windows Job Objects; use a native cgroup for Linux.'}
if(!(Test-Path -LiteralPath $Script -PathType Leaf)){throw 'Missing workload script'}
if(!(Test-Path -LiteralPath $OutputDirectory -PathType Container)){throw 'Missing run directory'}
if(Test-Path -LiteralPath (Join-Path $OutputDirectory 'watchdog.json')){throw 'Use a fresh watchdog directory; preserve earlier evidence'}
if($ProcessMiB -gt $AggregateMiB){throw 'Per-process limit exceeds aggregate budget'}
$memory=Get-CimInstance Win32_OperatingSystem
# Free physical RAM is telemetry only. Paging is managed by Windows; admission
# and termination depend on explicit process/job commit budgets, never free RAM.
# The workload process waits on an event until assignment succeeds. The kernel
# enforces commit limits for every descendant and kills the job on watchdog exit.
# Independent jobs must not be dispatched concurrently through this serial wrapper.
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class SharpUnitsJob {
 [StructLayout(LayoutKind.Sequential)] public struct Basic { public long PerProcess,PerJob; public uint Flags; public UIntPtr Min,Max; public uint Active; public UIntPtr Affinity; public uint Priority,Scheduling; }
 [StructLayout(LayoutKind.Sequential)] public struct Io { public ulong ReadOps,WriteOps,OtherOps,ReadBytes,WriteBytes,OtherBytes; }
 [StructLayout(LayoutKind.Sequential)] public struct Limits { public Basic Basic; public Io Io; public UIntPtr Process,Job,PeakProcess,PeakJob; }
 [StructLayout(LayoutKind.Sequential)] public struct Association { public IntPtr Key,Port; }
 [DllImport("kernel32.dll",SetLastError=true)] public static extern IntPtr CreateJobObject(IntPtr attrs,string name);
 [DllImport("kernel32.dll",SetLastError=true)] public static extern bool SetInformationJobObject(IntPtr job,int kind,ref Limits value,uint size);
 [DllImport("kernel32.dll",SetLastError=true)] public static extern bool QueryInformationJobObject(IntPtr job,int kind,out Limits value,uint size,IntPtr returned);
 [DllImport("kernel32.dll",SetLastError=true)] public static extern bool AssignProcessToJobObject(IntPtr job,IntPtr process);
 [DllImport("kernel32.dll",SetLastError=true,EntryPoint="SetInformationJobObject")] public static extern bool Associate(IntPtr job,int kind,ref Association value,uint size);
 [DllImport("kernel32.dll",SetLastError=true)] public static extern IntPtr CreateIoCompletionPort(IntPtr file,IntPtr port,IntPtr key,uint threads);
 [DllImport("kernel32.dll",SetLastError=true)] public static extern bool GetQueuedCompletionStatus(IntPtr port,out uint message,out IntPtr key,out IntPtr process,uint milliseconds);
 [DllImport("kernel32.dll",SetLastError=true)] public static extern bool TerminateJobObject(IntPtr job,uint code);
 [DllImport("kernel32.dll")] public static extern bool CloseHandle(IntPtr handle);
}
'@
$gateName='SharpUnitsBudget-'+[Guid]::NewGuid().ToString('N')
$gate=[Threading.EventWaitHandle]::new($false,[Threading.EventResetMode]::ManualReset,$gateName)
$job=[SharpUnitsJob]::CreateJobObject([IntPtr]::Zero,$null)
$port=[IntPtr]::Zero
$process=$null
$outputStream=$null;$errorStream=$null
$record=[ordered]@{processMiB=$ProcessMiB;aggregateMiB=$AggregateMiB;reserveMiB=$ReserveMiB;freeKiB=$memory.FreePhysicalMemory;peakProcessBytes=0L;peakJobBytes=0L;outcome='not started';script=(Resolve-Path -LiteralPath $Script).Path;enforcement='Windows Job Object commit limits, descendants inherit job; KILL_ON_JOB_CLOSE; one workload at a time'}
$record.reservePolicy='Compatibility argument only; no free-RAM admission or reservation.'
$record.processWatermarkBytes=[long]($ProcessMiB*1MB*0.85)
$record.jobWatermarkBytes=[long]($AggregateMiB*1MB*0.85)
$record.watermarkPolicy='Terminate at85% of declared hard commit limits, including retained peaks. Kernel allocation-failure notifications are best-effort, so reconcile every250ms and after root exit. Same-user global mutex refuses concurrent wrapper runs.'
try {
 if($job -eq [IntPtr]::Zero){throw 'CreateJobObject failed'}
 $limits=[SharpUnitsJob+Limits]::new()
 # PowerShell returns a copy of nested value-type fields. Configure the local
 # Basic struct and assign it back; mutating $limits.Basic.Flags loses the flags.
 $basic=[SharpUnitsJob+Basic]::new();$basic.Flags=0x100 -bor 0x200 -bor 0x2000;$limits.Basic=$basic
 $limits.Process=[UIntPtr]::new([ulong]($ProcessMiB*1MB));$limits.Job=[UIntPtr]::new([ulong]($AggregateMiB*1MB))
 $size=[uint32][Runtime.InteropServices.Marshal]::SizeOf($limits)
 if(![SharpUnitsJob]::SetInformationJobObject($job,9,[ref]$limits,$size)){throw 'Job limit configuration failed'}
 $effective=[SharpUnitsJob+Limits]::new()
 if(![SharpUnitsJob]::QueryInformationJobObject($job,9,[ref]$effective,$size,[IntPtr]::Zero)){throw 'Cannot read back active limits'}
 $record.effectiveFlags=$effective.Basic.Flags
 $record.effectiveProcessBytes=$effective.Process.ToUInt64();$record.effectiveJobBytes=$effective.Job.ToUInt64()
 if(($effective.Basic.Flags -band $basic.Flags) -ne $basic.Flags -or $effective.Process.ToUInt64() -ne $limits.Process.ToUInt64() -or $effective.Job.ToUInt64() -ne $limits.Job.ToUInt64()){throw 'Native limit read-back differs from requested configuration'}
 $port=[SharpUnitsJob]::CreateIoCompletionPort([IntPtr]::new(-1),[IntPtr]::Zero,[IntPtr]::Zero,1)
 if($port -eq [IntPtr]::Zero){throw 'Cannot create memory notification port'}
 $association=[SharpUnitsJob+Association]::new();$association.Key=[IntPtr]::new(1);$association.Port=$port
 if(![SharpUnitsJob]::Associate($job,7,[ref]$association,[uint32][Runtime.InteropServices.Marshal]::SizeOf($association))){throw 'Cannot activate memory notifications'}
 # Arguments use ProcessStartInfo.ArgumentList, never shell-generated quoting.
 $launcher=Join-Path $OutputDirectory 'gated-workload.ps1'
 @'
param($Gate,$Workload)
$ErrorActionPreference='Stop'
$event=[Threading.EventWaitHandle]::OpenExisting($Gate)
try {if(!$event.WaitOne(30000)){throw 'Watchdog did not activate'} } finally {$event.Dispose()}
& $Workload
if(!$?){exit 1}
'@ | Set-Content -LiteralPath $launcher
 $info=[Diagnostics.ProcessStartInfo]::new((Get-Command pwsh).Source)
 $info.WorkingDirectory=(Get-Location).Path
 $info.UseShellExecute=$false;$info.CreateNoWindow=$true
 $info.RedirectStandardOutput=$true;$info.RedirectStandardError=$true
 foreach($argument in @('-NoProfile','-File',$launcher,$gateName,$record.script)){$info.ArgumentList.Add($argument)}
 $process=[Diagnostics.Process]::Start($info)
 $outputStream=[IO.FileStream]::new((Join-Path $OutputDirectory 'stdout.log'),[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::Read)
 $errorStream=[IO.FileStream]::new((Join-Path $OutputDirectory 'stderr.log'),[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::Read)
 $stdout=$process.StandardOutput.BaseStream.CopyToAsync($outputStream)
 $stderr=$process.StandardError.BaseStream.CopyToAsync($errorStream)
 if(![SharpUnitsJob]::AssignProcessToJobObject($job,$process.Handle)){throw 'Job assignment failed; workload has not been released'}
 $record.outcome='active';$record|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $OutputDirectory 'watchdog.json')
 Write-Output ('ACTIVE: process={0} MiB, aggregate={1} MiB, reserveArgument={2} MiB (informational); artifacts={3}' -f $ProcessMiB,$AggregateMiB,$ReserveMiB,$OutputDirectory)
 $null=$gate.Set()
 # Await kernel job notifications, with bounded reconciliation of process exit.
 # A memory-limit event terminates the entire offending tree immediately.
 while($true){
  $message=[uint32]0;$key=[IntPtr]::Zero;$member=[IntPtr]::Zero
  if([SharpUnitsJob]::GetQueuedCompletionStatus($port,[ref]$message,[ref]$key,[ref]$member,250)){
   if($message -in 9,10){
    $record.outcome='memory limit breached; entire descendant tree terminated';$record.limitMessage=$message
    $null=[SharpUnitsJob]::TerminateJobObject($job,137)
    break
   }
  } else {
   if([Runtime.InteropServices.Marshal]::GetLastWin32Error() -ne 258){throw 'Memory notification wait failed; terminating workload'}
  }
  # Windows does not guarantee delivery of allocation-limit notifications.
  # An explicit conservative watermark provides an independently queried fallback.
  $usage=[SharpUnitsJob+Limits]::new()
  if(![SharpUnitsJob]::QueryInformationJobObject($job,9,[ref]$usage,$size,[IntPtr]::Zero)){throw 'Cannot reconcile memory usage'}
  if($usage.PeakProcess.ToUInt64() -ge $record.processWatermarkBytes -or $usage.PeakJob.ToUInt64() -ge $record.jobWatermarkBytes){
   $record.budgetBreach='fail-fast commit watermark';$record.outcome='memory limit breached; entire descendant tree terminated'
   $null=[SharpUnitsJob]::TerminateJobObject($job,137);break
  }
  if($process.HasExited -and $message -eq 0){break}
 }
 $process.WaitForExit()
 $null=[SharpUnitsJob]::TerminateJobObject($job,137)
 $null=$stdout.GetAwaiter().GetResult();$null=$stderr.GetAwaiter().GetResult()
 $observed=[SharpUnitsJob+Limits]::new()
 if(![SharpUnitsJob]::QueryInformationJobObject($job,9,[ref]$observed,$size,[IntPtr]::Zero)){throw 'Cannot query watchdog peaks'}
 $record.peakProcessBytes=$observed.PeakProcess.ToUInt64();$record.peakJobBytes=$observed.PeakJob.ToUInt64()
 $record.exitCode=$process.ExitCode
 if(!$record.Contains('limitMessage') -and !$record.Contains('budgetBreach')){$record.outcome=if($process.ExitCode -eq 0){'completed'}else{'failed; entire descendant tree terminated'}}
 if($record.Contains('limitMessage') -or $record.Contains('budgetBreach') -or $process.ExitCode -ne 0){$null=[SharpUnitsJob]::TerminateJobObject($job,137);throw ('Workload failed or exceeded memory policy: '+$process.ExitCode)}
} finally {
 # Closing the final job handle kills all remaining descendants, including any
 # compiler servers. Never kill unrelated processes by executable name.
 if($job -ne [IntPtr]::Zero){$null=[SharpUnitsJob]::TerminateJobObject($job,137);$null=[SharpUnitsJob]::CloseHandle($job)}
 if($port -ne [IntPtr]::Zero){$null=[SharpUnitsJob]::CloseHandle($port)}
 if($process -and !$process.HasExited){$process.Kill($true)}
 if($outputStream){$outputStream.Dispose()};if($errorStream){$errorStream.Dispose()}
 if($process){$process.Dispose()}
 $gate.Dispose()
 $record|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $OutputDirectory 'watchdog.json')
}
} finally {if($held){$admission.ReleaseMutex()};$admission.Dispose()}
