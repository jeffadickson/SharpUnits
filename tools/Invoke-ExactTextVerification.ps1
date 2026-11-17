param(
 [Parameter(Mandatory)][ValidateSet('Reference','Mutation','Faults','Example')][string]$Mode,
 [Parameter(Mandatory)][string]$OutputDirectory,
 [int]$ProcessMiB=1024,[int]$AggregateMiB=1536
)
$ErrorActionPreference='Stop'
$repository=Split-Path -Parent $PSScriptRoot
$artifactRoot=if($env:CODEX_ARTIFACT_ROOT){$env:CODEX_ARTIFACT_ROOT}else{'E:/Artifacts'}
if(!(Test-Path -LiteralPath $repository -PathType Container) -or !(Test-Path -LiteralPath $artifactRoot -PathType Container)){throw 'Missing checkout/artifact root'}
$output=[IO.Path]::GetFullPath($OutputDirectory,$repository)
$prefix=[IO.Path]::GetFullPath($artifactRoot).TrimEnd([char[]]@('\','/'))+[IO.Path]::DirectorySeparatorChar
if(!$output.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase)){throw 'Output must be inside configured artifact root'}
if(Test-Path -LiteralPath $output){throw 'Use a fresh run directory'}
if(!(Test-Path -LiteralPath (Split-Path -Parent $output) -PathType Container)){throw 'Missing output parent'}
New-Item -ItemType Directory -Path $output|Out-Null
@{repository=$repository;output=$output;artifactRoot=$artifactRoot;mode=$Mode;dotnet=(Get-Command dotnet -ErrorAction Stop).Source}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $output 'inputs.json')
@'
$ErrorActionPreference='Stop'
$inputs=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'inputs.json') -Raw|ConvertFrom-Json
if(!(Test-Path -LiteralPath $inputs.repository -PathType Container)){throw 'Missing checkout'}
Set-Location -LiteralPath $inputs.repository
$env:MSBUILDDISABLENODEREUSE='1';$env:DOTNET_PROCESSOR_COUNT='2';$env:DOTNET_gcServer='0'
$env:NUGET_PACKAGES=Join-Path $inputs.artifactRoot 'SharpUnits/nuget'
if(!(Test-Path -LiteralPath $env:NUGET_PACKAGES -PathType Container)){throw 'Missing existing package cache'}
switch($inputs.mode){
 'Reference' {
  $runtime=Join-Path (Split-Path -Parent $inputs.repository) 'tools-runtime'
  $env:JAVA_HOME=Join-Path $runtime 'jdk/jdk-17.0.20.1+1'
  $maven=Join-Path $runtime 'apache-maven-3.9.9/bin'
  if(!(Test-Path -LiteralPath $maven -PathType Container) -or !(Test-Path -LiteralPath $env:JAVA_HOME -PathType Container)){throw 'Missing existing JDK/Maven'}
  $env:JAVA_TOOL_OPTIONS='-Xmx128m -XX:MaxMetaspaceSize=96m -XX:+UseSerialGC -XX:ActiveProcessorCount=2'
  $env:PATH=$maven+[IO.Path]::PathSeparator+$env:PATH
  & (Join-Path $maven 'mvn.cmd') -B -o -f conformance/java-reference/pom.xml compile
  if($LASTEXITCODE){exit $LASTEXITCODE}
  $classpath='conformance/java-reference/target/classes;'+(Get-Content -LiteralPath conformance/java-reference/classpath.txt -Raw).Trim()
  & (Join-Path $env:JAVA_HOME 'bin/java.exe') '-Dfile.encoding=UTF-8' -cp $classpath org.sharpunits.ExactTextReferenceRunner conformance/exact-text-reference-cases.json > (Join-Path $inputs.output 'java.jsonl')
  if($LASTEXITCODE){exit $LASTEXITCODE}
  & $inputs.dotnet run --file tools/Compare-ExactText.cs -c Release --artifacts-path (Join-Path $inputs.output 'build') -p:UseSharedCompilation=false -- conformance/exact-text-reference-cases.json (Join-Path $inputs.output 'java.jsonl') (Join-Path $inputs.output 'comparison.json')
 }
 'Mutation' {
  $env:DOTNET_GCHeapHardLimit='0x20000000';$env:DOTNET_GCConserveMemory='9'
  & $inputs.dotnet tool restore
  if($LASTEXITCODE){exit $LASTEXITCODE}
  & $inputs.dotnet tool run dotnet-stryker -- --config-file tools/stryker-exact-text.json --skip-version-check --output (Join-Path $inputs.output 'stryker')
  if($LASTEXITCODE){exit $LASTEXITCODE}
  & ./tools/Assert-MutationReport.ps1 -Report (Join-Path $inputs.output 'stryker/reports/mutation-report.json')
 }
 'Faults' {& ./tools/Invoke-ExactTextFaults.ps1 -OutputDirectory (Join-Path $inputs.output 'controls');$global:LASTEXITCODE=0}
 'Example' {& $inputs.dotnet run --file examples/ExactText.cs -c Release --artifacts-path (Join-Path $inputs.output 'build') -p:UseSharedCompilation=false}
}
if($LASTEXITCODE){exit $LASTEXITCODE}
'@|Set-Content -LiteralPath (Join-Path $output 'workload.ps1')
& (Join-Path $PSScriptRoot 'Invoke-Budgeted.ps1') -Script (Join-Path $output 'workload.ps1') -OutputDirectory $output -ProcessMiB $ProcessMiB -AggregateMiB $AggregateMiB
