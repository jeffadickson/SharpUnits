param([Parameter(Mandatory)][string]$RunDirectory)
$ErrorActionPreference='Stop'
$repository=Split-Path -Parent $PSScriptRoot
if(!(Test-Path -LiteralPath $repository -PathType Container) -or !(Test-Path -LiteralPath $RunDirectory -PathType Container)){throw 'Missing checkout/run directory'}
Push-Location -LiteralPath $repository
try {
 $phases=@('final-tests','final-reference','final-focused-standard4','final-requirements2','final-tck2','final-example')
 $limits=[ordered]@{};$raw=[ordered]@{}
 foreach($phase in $phases){
  $directory=Join-Path $RunDirectory $phase
  if(!(Test-Path -LiteralPath $directory -PathType Container)){throw ('Missing phase: '+$phase)}
  $state=Get-Content -LiteralPath (Join-Path $directory 'watchdog.json') -Raw|ConvertFrom-Json
  if($state.outcome -ne 'completed' -or $state.exitCode -ne 0 -or $state.effectiveFlags -ne 8960){throw ('Unqualified phase: '+$phase)}
  if($state.effectiveProcessBytes -ne $state.processMiB*1MB -or $state.effectiveJobBytes -ne $state.aggregateMiB*1MB){throw 'Read-back budget mismatch'}
  $limits[$phase]=$state
 }
 $freeze=Get-Content -LiteralPath conformance/typed-errors-freeze.json -Raw|ConvertFrom-Json
 foreach($entry in $freeze.sourceBytesSha256.PSObject.Properties){if((Get-FileHash -LiteralPath $entry.Name).Hash -ne $entry.Value){throw ('Source freeze drift: '+$entry.Name)}}
 $mutation=Join-Path $RunDirectory 'final-focused-standard4/stryker/reports/mutation-report.json'
 & ./tools/Review-TypedErrorMutations.ps1 -Report $mutation -Output 'conformance/typed-errors-mutations.json'
 $reviewed=Get-Content -LiteralPath conformance/typed-errors-mutations.json -Raw|ConvertFrom-Json
 $counts=[ordered]@{}
 foreach($file in $reviewed.files){foreach($entry in $file.counts.PSObject.Properties){if(!$counts.Contains($entry.Name)){$counts[$entry.Name]=0};$counts[$entry.Name]+=[int]$entry.Value}}
 $coverageFiles=@(Get-ChildItem -LiteralPath (Join-Path $RunDirectory 'final-tests/coverage') -Recurse -Filter coverage.cobertura.xml)
 if(!$coverageFiles.Count){throw 'Missing Coverlet results'}
 $coverageHashes=@($coverageFiles|ForEach-Object {(Get-FileHash -LiteralPath $_.FullName).Hash}|Select-Object -Unique)
 if($coverageHashes.Count -ne 1){throw 'Conflicting coverage copies'}
 [xml]$coverage=Get-Content -LiteralPath $coverageFiles[0].FullName -Raw
 $trx=Join-Path $RunDirectory 'final-tests/coverage/typed-errors.trx'
 [xml]$tests=Get-Content -LiteralPath $trx -Raw
 $counter=$tests.TestRun.ResultSummary.Counters
 if([int]$counter.failed -ne 0 -or [int]$counter.passed -ne [int]$counter.total){throw 'Test suite not completely green'}
 $corpora=Get-Content -LiteralPath (Join-Path $RunDirectory 'final-reference/corpora/summary.json') -Raw|ConvertFrom-Json
 if(@($corpora|Where-Object {$_.expectedCheckExit -ne 0 -or $_.differentialExit -ne 0}).Count){throw 'Failed differential corpus'}
 $strictdoc=Get-Content -LiteralPath (Join-Path $RunDirectory 'final-requirements2/strictdoc-evidence.json') -Raw|ConvertFrom-Json
 if($strictdoc.result -ne 'PASS'){throw 'StrictDoc did not pass'}
 $tckLog=Get-Content -LiteralPath (Join-Path $RunDirectory 'final-tck2/stdout.log') -Raw
 $tckMatches=[regex]::Matches($tckLog,'Tests run:\s*(\d+), Failures:\s*(\d+), Errors:\s*(\d+), Skipped:\s*(\d+)')
 if(!$tckMatches.Count){throw 'Missing actual TCK summary'}
 $tck=$tckMatches[$tckMatches.Count-1]
 if($tck.Groups[1].Value -eq '0' -or $tck.Groups[2].Value -ne '0' -or $tck.Groups[3].Value -ne '0' -or $tck.Groups[4].Value -ne '0'){throw 'TCK summary not green'}
 foreach($check in @('watchdog-green-early2/watchdog-test.json','watchdog-green-normal/watchdog-test.json','admission-green/admission-test.json')){
  $state=Get-Content -LiteralPath (Join-Path $RunDirectory $check) -Raw|ConvertFrom-Json
  if($state.result -ne 'PASS'){throw ('Guard check failed: '+$check)}
 }
 # Keep bounded hashes in Git; raw logs, TRX, XML and full mutation JSON stay on E:.
 foreach($file in Get-ChildItem -LiteralPath $RunDirectory -Recurse -File|Where-Object {$_.Extension -in '.log','.trx','.xml','.json','.jsonl','.ps1' -and $_.FullName -notmatch '[\\/]strictdoc[\\/]|[\\/]retain-evidence\d*[\\/]'}){
  $raw[[IO.Path]::GetRelativePath($RunDirectory,$file.FullName).Replace('\','/')]=(Get-FileHash -LiteralPath $file.FullName).Hash
 }
 $source=[ordered]@{}
 foreach($directory in @('src','tests','tools','requirements','examples')){
  foreach($file in Get-ChildItem -LiteralPath $directory -Recurse -File|Where-Object {$_.Extension -in '.cs','.csproj','.ps1','.sdoc','.json' -and $_.FullName -notmatch '[\\/](bin|obj)[\\/]'}){
   $relative=[IO.Path]::GetRelativePath($repository,$file.FullName).Replace('\','/')
   $source[$relative]=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes([IO.File]::ReadAllText($file.FullName).Replace("`r`n","`n"))))
  }
 }
 $source['global.json']=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes([IO.File]::ReadAllText((Join-Path $repository 'global.json')).Replace("`r`n","`n"))))
 $evidence=[ordered]@{
  date='2026-11-12';baseCommit=(& git rev-parse HEAD).Trim();sourceIdentification='Uncommitted implementation bound by exact hashes; commit association recorded separately after commit.'
  canonicalTextSha256=$source;sourceFreezeSha256=(Get-FileHash conformance/typed-errors-freeze.json).Hash
  tests=@{passed=[int]$counter.passed;failed=[int]$counter.failed;skipped=0;trxSha256=(Get-FileHash -LiteralPath $trx).Hash}
  coverage=@{linesCovered=[int]$coverage.coverage.'lines-covered';linesValid=[int]$coverage.coverage.'lines-valid';branchesCovered=[int]$coverage.coverage.'branches-covered';branchesValid=[int]$coverage.coverage.'branches-valid';scope='Coverlet managed SharpUnits library; tools/Java/workflows unmeasured'}
  mutations=@{counts=$counts;reportSha256=(Get-FileHash -LiteralPath $mutation).Hash;reviewSha256=(Get-FileHash conformance/typed-errors-mutations.json).Hash;scope='Standard level; full NumericResult and JavaNumber parsing/conversion/budget spans, one worker. Broader arithmetic excluded; historical timeout unresolved; no global-score comparison'}
  reference=@{corpora=@($corpora).Count;cases=($corpora|Measure-Object cases -Sum).Sum;summarySha256=(Get-FileHash -LiteralPath (Join-Path $RunDirectory 'final-reference/corpora/summary.json')).Hash}
  javaTck=@{tests=[int]$tck.Groups[1].Value;failures=0;errors=0;skipped=0}
  strictdoc=$strictdoc
  randomized=@{framework='FsCheck3.2.0';malformedTokenSeed='385,1101';exactLongSeed='385,1103';configuredTrialsPerProperty=300;dedicatedCoverageGuidedFuzzer='not run'}
  toolchain=@{sdkSelection='10.0.402/latestPatch, no prereleases';stryker='5.0.0';coverlet='6.0.4';xunit='2.9.2';strictdoc='0.30.4';java='17.0.20.1+1';maven='3.9.9';mutationRuntime='2 .NET processors, workstation GC,256MiB managed heap, one Stryker worker';actualVersionLogs=@('final-example/stdout.log','final-tck2/stdout.log','final-focused-standard4/stdout.log')}
  budgetedPhases=$limits;rawArtifactRoot=$RunDirectory;rawSha256=$raw
  earlierAttempts='Initial completed mutation run had unset nested job flags and is diagnostic, not guarded qualification. Heap128MiB replay failed in Roslyn. Broader/Basic replays terminated at configured aggregate watermarks. None of those contributes final outcomes. Refused launches are not test results.'
  ci='Disabled; no workflow enabled or dispatched';ciState=@(Get-Content -LiteralPath (Join-Path $RunDirectory 'github-ci-state.jsonl')|ForEach-Object {$_|ConvertFrom-Json});formalClosures=0;platform='Windows local execution; Ubuntu/CI not executed'
 }
 $evidence|ConvertTo-Json -Depth 12|Set-Content -LiteralPath conformance/typed-errors-evidence.json
} finally {Pop-Location}
