param(
 [Parameter(Mandatory)][ValidateSet('Tests','Mutation','FocusedMutation','Net11Mutation','Reference','JavaTck','Requirements','Example','Demo')][string]$Mode,
 [Parameter(Mandatory)][string]$OutputDirectory,
 [int]$ProcessMiB=512,[int]$AggregateMiB=768,[int]$ReserveMiB=512,
 [ValidateRange(0,2048)][int]$ManagedHeapMiB=0
)
$ErrorActionPreference='Stop'
if($ManagedHeapMiB -gt 0 -and $Mode -notin 'Mutation','FocusedMutation','Net11Mutation'){
 throw 'Managed heap limits are mutation-only'
}
$repository=Split-Path -Parent $PSScriptRoot
if(!(Test-Path -LiteralPath $repository -PathType Container)){throw 'Missing checkout'}
$artifactRoot=if($env:CODEX_ARTIFACT_ROOT){$env:CODEX_ARTIFACT_ROOT}else{'E:/Artifacts'}
if(!(Test-Path -LiteralPath $artifactRoot -PathType Container)){throw 'Missing artifact root'}
$output=[IO.Path]::GetFullPath($OutputDirectory)
$root=[IO.Path]::GetFullPath($artifactRoot).TrimEnd([char[]]@('\','/'))+[IO.Path]::DirectorySeparatorChar
if(!$output.StartsWith($root,[StringComparison]::OrdinalIgnoreCase)){throw 'Bulk output must be inside the configured artifact root'}
if(Test-Path -LiteralPath $output){throw 'Use a fresh run directory'}
$parent=Split-Path -Parent $output
if(!(Test-Path -LiteralPath $parent -PathType Container)){throw 'Create and verify the project artifact parent first'}
New-Item -ItemType Directory -Path $output|Out-Null
$probe=Join-Path $output 'write-probe';'writable'|Set-Content -LiteralPath $probe
if((Get-Content -LiteralPath $probe -Raw).Trim() -ne 'writable'){throw 'Artifact write probe failed'}
$disks=Get-PSDrive C,E|Select-Object Name,Free
$disks|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $output 'disk-space.json')
if(($disks|Where-Object Name -eq 'C').Free -lt 256MB){throw 'Insufficient C: headroom for bounded NTFS build intermediates'}
@{mode=$Mode;repository=$repository;output=$output;artifactRoot=$artifactRoot;dotnet=(Get-Command dotnet -ErrorAction Stop).Source;managedHeapMiB=$ManagedHeapMiB}|
 ConvertTo-Json|Set-Content -LiteralPath (Join-Path $output 'inputs.json')
@'
$ErrorActionPreference='Stop'
$inputs=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'inputs.json') -Raw|ConvertFrom-Json
if(!(Test-Path -LiteralPath $inputs.repository -PathType Container)){throw 'Missing checkout'}
Set-Location -LiteralPath $inputs.repository
$env:MSBUILDDISABLENODEREUSE='1'
$env:VSTEST_CONNECTION_TIMEOUT='180'
if($inputs.mode -in 'Mutation','FocusedMutation','Net11Mutation'){
 # One Stryker worker does not bound Roslyn/GC's own machine-sized pools.
 $env:DOTNET_PROCESSOR_COUNT='2';$env:DOTNET_gcServer='0'
}
if($inputs.managedHeapMiB -gt 0){
 $env:DOTNET_GCHeapHardLimit='0x'+([long]$inputs.managedHeapMiB*1MB).ToString('X')
 $env:DOTNET_GCConserveMemory='9'
}
$env:NUGET_PACKAGES=Join-Path $inputs.artifactRoot 'SharpUnits/nuget'
if(!(Test-Path -LiteralPath $env:NUGET_PACKAGES -PathType Container)){New-Item -ItemType Directory -Path $env:NUGET_PACKAGES -Force|Out-Null}
switch($inputs.mode){
 'Tests' {
  & $inputs.dotnet test tests/SharpUnits.Tests/SharpUnits.Tests.csproj -c Release -m:1 -p:UseSharedCompilation=false --collect 'XPlat Code Coverage' --results-directory (Join-Path $inputs.output 'coverage') --logger 'trx;LogFileName=typed-errors.trx'
 }
 'Mutation' {
  & $inputs.dotnet tool restore
  if($LASTEXITCODE){exit $LASTEXITCODE}
  & $inputs.dotnet tool run dotnet-stryker -- --config-file tools/stryker-typed-errors.json --skip-version-check --output (Join-Path $inputs.output 'stryker')
 }
 'FocusedMutation' {
  & ./tools/Set-TypedErrorMutationScope.ps1
  & $inputs.dotnet tool restore
  if($LASTEXITCODE){exit $LASTEXITCODE}
  & $inputs.dotnet tool run dotnet-stryker -- --config-file tools/stryker-typed-errors-focused.json --skip-version-check --output (Join-Path $inputs.output 'stryker')
 }
 'Net11Mutation' {
  & ./tools/Set-Net11MutationScope.ps1
  & $inputs.dotnet tool restore
  if($LASTEXITCODE){exit $LASTEXITCODE}
  & $inputs.dotnet tool run dotnet-stryker -- --config-file tools/stryker-net11.json --skip-version-check --output (Join-Path $inputs.output 'stryker')
 }
 'Reference' {
  & $inputs.dotnet build src/SharpUnits.ConformanceRunner/SharpUnits.ConformanceRunner.csproj -c Release -m:1 -p:UseSharedCompilation=false
  if($LASTEXITCODE){exit $LASTEXITCODE}
  $env:JAVA_TOOL_OPTIONS='-XX:ActiveProcessorCount=2'
  & ./tools/Invoke-AllVerificationCorpora.ps1 -Runner 'src/SharpUnits.ConformanceRunner/bin/Release/net11.0/SharpUnits.ConformanceRunner.dll' -OutputDirectory (Join-Path $inputs.output 'corpora')
 }
 'JavaTck' {
  $runtime=Join-Path (Split-Path -Parent $inputs.repository) 'tools-runtime'
  $env:JAVA_HOME=Join-Path $runtime 'jdk/jdk-17.0.20.1+1'
  $maven=Join-Path $runtime 'apache-maven-3.9.9/bin'
  if(!(Test-Path -LiteralPath $maven -PathType Container) -or !(Test-Path -LiteralPath $env:JAVA_HOME -PathType Container)){throw 'Missing pinned Maven/JDK'}
  $env:PATH=$maven+[IO.Path]::PathSeparator+$env:PATH
  $env:JAVA_TOOL_OPTIONS='-XX:ActiveProcessorCount=2'
  & (Join-Path $maven 'mvn.cmd') --version
  if($LASTEXITCODE){exit $LASTEXITCODE}
  & (Join-Path $runtime 'python/python.exe') tools/run_pinned_tck.py -o
 }
 'Requirements' {
  # Optional numerical dependencies must not create a machine-sized BLAS pool
  # while importing the existing Python documentation environment.
  $env:OPENBLAS_NUM_THREADS='1';$env:OMP_NUM_THREADS='1';$env:MKL_NUM_THREADS='1'
  $runtime=Join-Path (Split-Path -Parent $inputs.repository) 'tools-runtime/strictdoc'
  if(!(Test-Path -LiteralPath $runtime -PathType Container)){throw 'Missing installed StrictDoc'}
  $env:PYTHONPATH=$runtime
  & ./tools/Export-Requirements.ps1 -StrictDocCommand (Join-Path $runtime 'bin/strictdoc.exe') -OutputDirectory (Join-Path $inputs.output 'strictdoc') -EvidencePath (Join-Path $inputs.output 'strictdoc-evidence.json')
 }
 'Example' {
  & $inputs.dotnet --info
  if($LASTEXITCODE){exit $LASTEXITCODE}
  & $inputs.dotnet run --file examples/TypedNumericErrors.cs -c Release -p:UseSharedCompilation=false
 }
 'Demo' {
  & $inputs.dotnet --info
  & $inputs.dotnet build demo/SharpUnits.BodeDemo.csproj -c Release -m:1 -p:UseSharedCompilation=false
  if($LASTEXITCODE){exit $LASTEXITCODE}
  & $inputs.dotnet demo/bin/Release/net11.0/SharpUnits.BodeDemo.dll > (Join-Path $inputs.output 'bode.csv')
  if($LASTEXITCODE){exit $LASTEXITCODE}
  & $inputs.dotnet run --file examples/ExactJavaCore.cs -c Release -p:PublishAot=false -p:UseSharedCompilation=false
 }
}
if($LASTEXITCODE){exit $LASTEXITCODE}
if($inputs.mode -in 'Mutation','FocusedMutation','Net11Mutation'){
 & ./tools/Assert-MutationReport.ps1 -Report (Join-Path $inputs.output 'stryker/reports/mutation-report.json')
}
'@|Set-Content -LiteralPath (Join-Path $output 'workload.ps1')
Push-Location -LiteralPath $repository
try {
 if($Mode -in 'Mutation','FocusedMutation','Net11Mutation'){
  & (Join-Path $PSScriptRoot 'Invoke-Budgeted.ps1') -Script (Join-Path $output 'workload.ps1') -OutputDirectory $output -ProcessMiB $ProcessMiB -AggregateMiB $AggregateMiB -ReserveMiB $ReserveMiB
 }else{
  # Ordinary verification uses OS-managed memory; do not attach a limiting job.
  & (Get-Process -Id $PID).Path -NoProfile -File (Join-Path $output 'workload.ps1') *> (Join-Path $output 'workload.log')
  if($LASTEXITCODE){throw "Verification failed ($LASTEXITCODE); see $output/workload.log"}
 }
}
finally {Pop-Location}
