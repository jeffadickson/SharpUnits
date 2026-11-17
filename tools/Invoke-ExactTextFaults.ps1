<# Runs finite curated semantic fault controls only in an isolated artifact snapshot.
   The caller must apply its process-tree memory watchdog before invoking this script.
   This is not Stryker.NET and does not provide an exhaustive mutation score. #>
[CmdletBinding()]
param([Parameter(Mandatory)][string]$OutputDirectory,[string[]]$ControlIds=@())
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if (!(Test-Path -LiteralPath $repo -PathType Container)){throw 'Repository missing'}
$output=[IO.Path]::GetFullPath($OutputDirectory)
$artifactRoot=if($env:CODEX_ARTIFACT_ROOT){[IO.Path]::GetFullPath($env:CODEX_ARTIFACT_ROOT)}else{'E:\Artifacts'}
if (!(Test-Path -LiteralPath $artifactRoot -PathType Container)){throw 'Artifact root missing'}
if ([IO.Path]::GetPathRoot($output) -ne 'E:\' -or !$output.StartsWith($artifactRoot.TrimEnd('\')+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Output must be below the E: artifact root'}
$parent=Split-Path -Parent $output
if (!(Test-Path -LiteralPath $parent -PathType Container)){throw 'Output parent must already exist'}
if (Test-Path -LiteralPath $output){throw 'Output directory must be fresh'}
$null=New-Item -ItemType Directory -Path $output
$snapshot=Join-Path $output 'snapshot'
$null=New-Item -ItemType Directory -Path $snapshot
$dotnet=(Get-Command dotnet -ErrorAction Stop).Source
$cache=Join-Path $artifactRoot 'SharpUnits/nuget'
if (!(Test-Path -LiteralPath $cache -PathType Container)){throw 'Existing SharpUnits NuGet cache missing'}
$env:NUGET_PACKAGES=$cache
$env:DOTNET_PROCESSOR_COUNT='2'
# Copy only source/fixture inputs. Never import build outputs or mutate checkout files.
$manifest=[Collections.Generic.List[object]]::new()
foreach($tree in @('src','tests','conformance')){
 $sourceRoot=Join-Path $repo $tree
 if (!(Test-Path -LiteralPath $sourceRoot -PathType Container)){throw "Missing $tree"}
 foreach($file in Get-ChildItem -LiteralPath $sourceRoot -File -Recurse){
  $relative=[IO.Path]::GetRelativePath($repo,$file.FullName)
  if($relative -match '(^|[\\/])(bin|obj|target)([\\/]|$)' -or $file.Extension -notin @('.cs','.csproj','.json')){continue}
  $destination=Join-Path $snapshot $relative
  $destinationParent=Split-Path -Parent $destination
  if (!(Test-Path -LiteralPath $destinationParent -PathType Container)){$null=New-Item -ItemType Directory -Path $destinationParent -Force}
  Copy-Item -LiteralPath $file.FullName -Destination $destination
  $manifest.Add([ordered]@{path=$relative;sha256=(Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash})
 }
}
foreach($name in @('global.json','NuGet.Config','nuget.config','Directory.Build.props','Directory.Build.targets','Directory.Packages.props')){
 $inputPath=Join-Path $repo $name
 if(Test-Path -LiteralPath $inputPath -PathType Leaf){
  $destination=Join-Path $snapshot $name
  Copy-Item -LiteralPath $inputPath -Destination $destination -Force
  $manifest.Add([ordered]@{path=$name;sha256=(Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash})
 }
}
$manifest|ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $output 'snapshot-inputs.json')
$reader='src/SharpUnits/ExactTextFormat.Reader.cs'
$core='src/SharpUnits/ExactTextFormat.cs'
$writer='src/SharpUnits/ExactTextFormat.Writer.cs'
$mutants=@(
 @{id='ConverterOrder';file=$reader;before='converter.Concatenate(step)';after='step.Concatenate(converter)';count=1},
 @{id='UngroupedExponentDivision';file=$reader;before="grouped&&Take('/')";after="Take('/')";count=1},
 @{id='DecimalExponent';file=$core;before='(-scale).ToString(CultureInfo.InvariantCulture)';after='(1-scale).ToString(CultureInfo.InvariantCulture)';count=1},
 @{id='ProjectedAliasEquality';file=$core;before='x[i].Parameter.Fraction!=y[i].Parameter.Fraction';after='!x[i].Parameter.Equals(y[i].Parameter)';count=1},
 @{id='ZeroDivisorPosition';file=$reader;before='Fail(TextErrorCode.ZeroDivisor,start)';after='Fail(TextErrorCode.ZeroDivisor,start+1)';count=3},
 @{id='RelativeScale';file=$reader;before='"relative"=>JavaQuantityScale.Relative';after='"relative"=>JavaQuantityScale.Absolute';count=1},
 @{id='DimensionlessAliasCapture';file=$writer;before='return "unit()";';after='return "one";';count=1},
 @{id='DepthBudget';file=$reader;before='depth>64';after='depth>128';count=2}
)
if($ControlIds.Count){$known=@($mutants.id);foreach($id in $ControlIds){if($id -notin $known){throw "Unknown control: $id"}};$mutants=@($mutants|Where-Object {$_.id -in $ControlIds})}
$original=@{}
foreach($file in @($reader,$core,$writer)){$original[$file]=[IO.File]::ReadAllBytes((Join-Path $snapshot $file))}
$records=[Collections.Generic.List[object]]::new()
$project=Join-Path $snapshot 'tests/SharpUnits.Tests/SharpUnits.Tests.csproj'
function Invoke-Control([string]$id){
 $run=Join-Path $output $id
 if(Test-Path -LiteralPath $run){throw "Run exists: $id"}
 $null=New-Item -ItemType Directory -Path $run
 $artifacts=Join-Path $run 'artifacts'
 $results=Join-Path $run 'results'
 $null=New-Item -ItemType Directory -Path $results
 $buildLog=Join-Path $run 'build.log'
 $testLog=Join-Path $run 'test.log'
 & $dotnet build $project -c Release --nologo --artifacts-path $artifacts -m:1 -p:UseSharedCompilation=false *> $buildLog
 $buildExit=$LASTEXITCODE
 if($buildExit -ne 0){throw "Noncompiling control $id (exit $buildExit); see $buildLog"}
 & $dotnet test $project -c Release --no-build --no-restore --nologo --artifacts-path $artifacts -m:1 -p:UseSharedCompilation=false --filter 'FullyQualifiedName~ExactTextFormatTests' --results-directory $results --logger 'trx;LogFileName=tests.trx' *> $testLog
 $testExit=$LASTEXITCODE
 $trx=Join-Path $results 'tests.trx'
 if (!(Test-Path -LiteralPath $trx -PathType Leaf)){throw "Missing TRX: $id"}
 [xml]$document=Get-Content -LiteralPath $trx -Raw
 $summary=$document.TestRun.ResultSummary
 $counters=$summary.Counters
 $total=[int]$counters.total;$executed=[int]$counters.executed;$failed=[int]$counters.failed
 $passed=[int]$counters.passed
 if($total -le 0 -or $executed -ne $total -or ($passed+$failed) -ne $executed -or [int]$counters.aborted -ne 0 -or [int]$counters.error -ne 0){throw "Incomplete TRX: $id"}
 if(($failed -eq 0 -and $testExit -ne 0) -or ($failed -gt 0 -and $testExit -ne 1)){throw "Unexpected test exit $testExit for $id"}
 return [ordered]@{id=$id;compiled=$true;buildExit=$buildExit;testExit=$testExit;total=$total;passed=$passed;failed=$failed;trxOutcome=[string]$summary.outcome;status=if($failed -gt 0){'KILLED'}else{'SURVIVED'};buildLogSha256=(Get-FileHash -LiteralPath $buildLog -Algorithm SHA256).Hash;testLogSha256=(Get-FileHash -LiteralPath $testLog -Algorithm SHA256).Hash;trxSha256=(Get-FileHash -LiteralPath $trx -Algorithm SHA256).Hash}
}
$failure=$null
try{
 $baseline=Invoke-Control 'baseline'
 $baseline.status='BASELINE'
 $records.Add($baseline)
 if($baseline.failed -ne 0){throw 'Baseline tests failed'}
 foreach($mutant in $mutants){
  foreach($file in $original.Keys){[IO.File]::WriteAllBytes((Join-Path $snapshot $file),$original[$file])}
  $path=Join-Path $snapshot $mutant.file
  $text=[Text.Encoding]::UTF8.GetString($original[$mutant.file])
  if([regex]::Matches($text,[regex]::Escape($mutant.before)).Count -ne $mutant.count){throw "Literal anchor drift: $($mutant.id)"}
  [IO.File]::WriteAllText($path,$text.Replace($mutant.before,$mutant.after),[Text.UTF8Encoding]::new($false))
  $record=Invoke-Control $mutant.id
  $record.sourceSha256=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
  $records.Add($record)
  $record|ConvertTo-Json -Compress
 }
}catch{$failure=$_.ToString();throw}
finally{
 foreach($file in $original.Keys){[IO.File]::WriteAllBytes((Join-Path $snapshot $file),$original[$file])}
 [ordered]@{method='Finite selected curated semantic fault controls; workers=1';selectedIds=@($mutants.id);nugetPackages=$cache;snapshot=$snapshot;failure=$failure;records=$records.ToArray();limitations='Not Stryker.NET; not exhaustive mutation coverage or a mutation score. Noncompiling/incomplete runs abort and are never killed controls. Source/test hashes are in snapshot-inputs.json. Caller provides process-tree memory watchdog.'}|ConvertTo-Json -Depth 10|Set-Content -LiteralPath (Join-Path $output 'fault-controls.json')
}
if(@($records|Where-Object {$_.status -eq 'SURVIVED'}).Count -gt 0){throw 'One or more semantic controls survived'}